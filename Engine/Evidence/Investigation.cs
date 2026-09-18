using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public sealed record ParsedLogEntry(int LineNumber, string Timestamp, string Level, string Component, string Message)
{
    public ushort? EventId { get; init; }
}
public sealed record DiagnosticFinding(string RuleId, int RuleVersion, string Title, string Severity,
    string Certainty, string Explanation, string NextStep, string ReferenceUrl, int Occurrences,
    IReadOnlyList<EvidenceReference> Evidence);
public sealed record InvestigationReport(string SchemaVersion, string Source, string Format,
    int LinesAnalyzed, int ParsedLines, bool Partial, IReadOnlyList<ParsedLogEntry> Timeline,
    IReadOnlyList<DiagnosticFinding> Findings, LogAnalysis Codes, string Limitation);

public static partial class Investigation
{
    public const int MaximumEntries = 10_000;
    public const string Limitation = "Findings describe recorded events, not current machine health or a proven root cause. Timestamps have no known time zone. No repairs were executed.";
    private const string CbsReference = "https://learn.microsoft.com/en-us/troubleshoot/windows-server/installing-updates-features-roles/fix-windows-update-errors";
    private const string SetupReference = "https://learn.microsoft.com/en-us/windows/deployment/upgrade/log-files";

    [GeneratedRegex(@"^(?<time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:[.,]\d+)?)\s*,\s*(?<level>Info|Warning|Error|Fatal)\s+(?<component>[A-Za-z0-9_]+)\s+(?<message>.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EntryPattern();

    [GeneratedRegex(@"\b0x[0-9a-f]{8}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ErrorCodePattern();

    public static async Task<InvestigationReport> AnalyzeFileAsync(string path, CancellationToken cancellationToken = default)
        => Path.GetExtension(path).Equals(".evtx", StringComparison.OrdinalIgnoreCase)
            ? EventArchive.Analyze(await EventArchive.ReadAsync(path, cancellationToken).ConfigureAwait(false), cancellationToken)
            : Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase)
            ? EventXmlImport.Analyze(await TextEvidence.ReadAsync(path, cancellationToken).ConfigureAwait(false), cancellationToken)
            : Analyze(await TextEvidence.ReadAsync(path, cancellationToken).ConfigureAwait(false), cancellationToken);

    public static InvestigationReport Analyze(TextEvidence evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var codes = LogAnalyzer.Analyze(evidence, cancellationToken);
        var entries = new List<ParsedLogEntry>();
        var groups = new Dictionary<string, List<ParsedLogEntry>>(StringComparer.Ordinal);
        bool cbs = false, setup = false, msi = false, partial = evidence.Truncated || codes.ObservationsTruncated;
        string name = evidence.Source.Replace('\\', '/').Split('/')[^1];
        bool setupName = name.Equals("setupact.log", StringComparison.OrdinalIgnoreCase) || name.Equals("setuperr.log", StringComparison.OrdinalIgnoreCase);
        int characters = 0;
        bool installerContext = false;
        for (int i = 0; i < codes.LinesAnalyzed; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (evidence.Truncated && i == evidence.Lines.Length - 1) break;
            if (entries.Count == MaximumEntries) { partial = true; break; }
            int remaining = TextEvidence.MaximumCharacters - characters;
            string raw = evidence.Lines[i];
            if (raw.Length > remaining) { partial = true; break; }
            characters += raw.Length + 1;
            installerContext |= MsiPrefixPattern().IsMatch(raw);
            var action = MsiActionFailurePattern().Match(raw);
            if (installerContext && action.Success)
            {
                msi = true;
                var failure = new ParsedLogEntry(i + 1, action.Groups["time"].Value, "ERROR", "MSI", raw[..Math.Min(raw.Length, 640)]);
                entries.Add(failure);
                Add("MSI-ACTION", failure);
                continue;
            }
            var success = MsiSuccessPattern().Match(raw);
            if (success.Success)
            {
                msi = true;
                string detail = success.Groups["code"].Value switch
                {
                    "1641" => "Installer recorded success and initiated a restart at that time. Current restart state is unverified.",
                    "3010" => "Installer recorded success requiring a restart at that time. Current restart state is unverified.",
                    _ => "Installer recorded success at that time. Current product health is unverified."
                };
                entries.Add(new(i + 1, success.Groups["time"].Value, "INFO", "MSI", detail));
                continue;
            }
            var installer = MsiOutcomePattern().Match(raw);
            if (installer.Success)
            {
                msi = true;
                var outcome = new ParsedLogEntry(i + 1, installer.Groups["time"].Value, "ERROR", "MSI", raw[..Math.Min(raw.Length, 640)]);
                entries.Add(outcome);
                Add("MSI-" + installer.Groups["code"].Value, outcome);
                continue;
            }
            var match = EntryPattern().Match(raw);
            if (!match.Success || !DateTime.TryParseExact(match.Groups["time"].Value[..19], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) continue;
            string component = match.Groups["component"].Value.ToUpperInvariant();
            bool isCbs = component is "CBS" or "CSI";
            bool isSetup = setupName && component is ("SP" or "MOUPG" or "CONX" or "MIG" or "IBSLIB" or "DISM");
            if (!isCbs && !isSetup) continue;
            cbs |= isCbs; setup |= isSetup;
            var entry = new ParsedLogEntry(i + 1, match.Groups["time"].Value, match.Groups["level"].Value.ToUpperInvariant(), component, match.Groups["message"].Value);
            entries.Add(entry with { Message = entry.Message[..Math.Min(entry.Message.Length, 640)] });
            if (entry.Level is not ("ERROR" or "FATAL")) continue;
            if (isCbs)
            {
                foreach (string code in ErrorCodePattern().Matches(entry.Message).Select(m => m.Value.ToUpperInvariant()).Distinct())
                    if (IsKnownCbsCode(code)) Add(code, entry);
            }
            if (isSetup)
            {
                var setupCodes = ErrorCodePattern().Matches(entry.Message).Select(m => m.Value.ToUpperInvariant())
                    .Distinct().Where(IsKnownSetupCode).ToArray();
                if (setupCodes.Length == 0) Add("SETUP-ERROR", entry);
                else foreach (string code in setupCodes) Add("SETUP-" + code, entry);
            }
        }
        var findings = new List<DiagnosticFinding>();
        foreach (var group in groups.OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var (id, title, explanation, next, url) = group.Key.StartsWith("MSI-", StringComparison.Ordinal) ? MsiDescription(group.Key[4..]) : group.Key switch
            {
                "0X800F081F" => ("KB-CBS-001", "Servicing source files reported missing", "A CBS/CSI error entry contains CBS_E_SOURCE_MISSING (0x800F081F). The servicing operation reported unavailable source content.", "Review the surrounding servicing session and its repair-source configuration. Confirm whether a later attempt succeeded before planning a repair.", CbsReference),
                "0X80073712" => ("KB-CBS-002", "Component store inconsistency reported", "A CBS/CSI error entry contains ERROR_SXS_COMPONENT_STORE_CORRUPT (0x80073712). This records a component-store inconsistency at that time.", "Review the full servicing session and any subsequent verification or repair results. Establish the current state before changing the component store.", CbsReference),
                _ when group.Key.StartsWith("0X", StringComparison.Ordinal) => CbsCatalogDescription(group.Key),
                _ when group.Key.StartsWith("SETUP-0X", StringComparison.Ordinal) => SetupCatalogDescription(group.Key[6..]),
                _ => ("KB-SETUP-001", "Windows Setup error entries recorded", "Recognized Windows Setup components logged error or fatal entries. This is a review lead, not a diagnosis of why the upgrade failed.", "Review these lines together with setupact.log and the relevant setup phase. Earlier errors can be handled or superseded by a later successful attempt.", SetupReference)
            };
            findings.Add(new(id, 1, title, SetupSeverity(group.Key), "Recorded event", explanation, next, url, group.Value.Count,
                (group.Key == "MSI-ACTION"
                    ? group.Value.Take(2).SelectMany(e => new[] { e.LineNumber }.Concat(Enumerable.Range(Math.Max(1, e.LineNumber - 2), e.LineNumber - Math.Max(1, e.LineNumber - 2)))).Distinct()
                    : group.Value.Take(8).Select(e => e.LineNumber))
                .Select(line => new EvidenceReference(evidence.Source, line,
                    evidence.Lines[line - 1][..Math.Min(evidence.Lines[line - 1].Length, 640)])).ToArray()));
        }
        return new("1", evidence.Source, msi ? (cbs || setup ? "Mixed Windows Installer / servicing" : "Windows Installer") : cbs && setup ? "Mixed CBS / Windows Setup" : cbs ? "CBS / CSI" : setup ? "Windows Setup" : "Unrecognized text",
            codes.LinesAnalyzed, entries.Count, partial, entries.AsReadOnly(), findings.AsReadOnly(), codes, Limitation);

        void Add(string key, ParsedLogEntry entry)
        {
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = [];
            list.Add(entry);
        }
    }

    public static string ToJson(InvestigationReport report) => JsonSerializer.Serialize(report,
        new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static bool IsKnownCbsCode(string code)
    {
        if (code.Equals("0X80073712", StringComparison.Ordinal)) return true;
        return uint.TryParse(code.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value)
            && WindowsUpdateErrorCatalog.Find(value) is not null;
    }

    private static (string Id, string Title, string Explanation, string Next, string Url) CbsCatalogDescription(string code)
    {
        if (!uint.TryParse(code.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value)
            || WindowsUpdateErrorCatalog.Find(value) is not { } reference)
            throw new InvalidOperationException("CBS finding did not have a reviewed error-code reference.");

        return ("KB-CBS-" + code[2..], "Servicing error code " + reference.Symbol + " recorded",
            "A CBS/CSI error entry contains " + reference.Symbol + " (" + code + "). " + reference.Meaning,
            reference.DiagnosticScope + " Confirm whether a later servicing attempt succeeded before planning a repair.", CbsReference);
    }

    private static bool IsKnownSetupCode(string code) =>
        uint.TryParse(code.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value)
        && WindowsSetupErrorCatalog.Find(value) is not null;

    private static string SetupSeverity(string key)
    {
        if (!key.StartsWith("SETUP-0X", StringComparison.Ordinal)) return "Warning";
        return uint.TryParse(key.AsSpan(8), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value)
            && WindowsSetupErrorCatalog.Find(value) is { RequiresReview: false } ? "Information" : "Warning";
    }

    private static (string Id, string Title, string Explanation, string Next, string Url) SetupCatalogDescription(string code)
    {
        if (!uint.TryParse(code.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value)
            || WindowsSetupErrorCatalog.Find(value) is not { } reference)
            throw new InvalidOperationException("Setup finding did not have a reviewed error-code reference.");

        return ("KB-SETUP-" + code[2..], "Windows Setup result " + reference.Symbol + " recorded",
            "A Windows Setup error entry contains " + reference.Symbol + " (" + code + "). " + reference.Meaning,
            reference.DiagnosticScope + " Confirm whether a later Setup attempt succeeded before planning a repair.", SetupReference);
    }
}
