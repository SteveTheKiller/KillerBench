using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public sealed record DriverInstallSection(string Identity, string? ExitCode, int StartLine, int EndLine, IReadOnlyList<string> ErrorLines);

public static partial class SetupApiDriverInstall
{
    private const int MaximumSections = 512;
    private const int MaximumFindings = 32;

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Driver installation history requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $path=Join-Path $env:windir 'INF\\setupapi.dev.log'; " +
            "if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw 'setupapi.dev.log is unavailable'}; " +
            "Get-Content -LiteralPath $path -Tail 12000 | Out-String -Width 4096";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        var findings = Interpret(evidence, token).ToList();
        if (!Complete(evidence)) return findings;
        var sections = ParseSections(evidence.Output, token, out _).Where(section => section.ExitCode is not null)
            .GroupBy(section => section.Identity, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).Take(MaximumFindings).ToArray();
        if (sections.Length == 0) return findings;
        const string devices = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$rows=@(Get-CimInstance -ClassName Win32_PnPEntity -OperationTimeoutSec 15 | Select-Object -First 1025 DeviceID,Name,Present,ConfigManagerErrorCode); " +
            "[pscustomobject]@{complete=($rows.Count -le 1024);devices=@($rows | Select-Object -First 1024)}|ConvertTo-Json -Depth 3 -Compress";
        var deviceEvidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", devices], true), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        findings.AddRange(DriverInstallCorrelation.Interpret(sections, deviceEvidence, token));
        return findings.AsReadOnly();
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        WorkflowCheck Unknown(string detail) => new("Driver installation history coverage", CheckState.Unknown,
            detail + " The retained tail is not a complete lifetime history. No device, driver or driver-store content changed.", Evidence: evidence);
        if (!Complete(evidence))
            return [Unknown("SetupAPI device-install history could not be read completely. Review the retained provider output.")];
        var sections = ParseSections(evidence.Output, token, out bool partial);
        var failedSections = sections.Where(section => section.ExitCode is not null).ToArray();
        var checks = failedSections.Take(MaximumFindings).Select(section => new WorkflowCheck(
            "Driver installation failure: " + section.Identity, CheckState.Attention,
            $"Retained SetupAPI lines {section.StartLine}-{section.EndLine}; exit: {section.ExitCode}. " +
            (section.ErrorLines.Count == 0 ? "No marked error line was retained. " : "Marked errors: " + string.Join(" | ", section.ErrorLines) + " ") +
            "KillerBench compares this identity with the current Plug and Play inventory in the complete workflow. Historical evidence alone does not prove the device is still present or the same failure is current. No driver was installed, removed, rolled back or updated.",
            Evidence: evidence)).ToList();
        if (failedSections.Length == 0)
            checks.Add(new("Driver installation history", sections.Count == 0 || partial ? CheckState.Unknown : CheckState.Information,
                $"{sections.Count} complete device-install sections were retained; none contained a marked error or FAILURE exit. " +
                "This does not establish that every device installed successfully or currently works. No device, driver or driver-store content changed.", Evidence: evidence));
        if (partial || failedSections.Length > MaximumFindings)
            checks.Add(Unknown(partial
                ? "The retained tail began or ended inside a section, exceeded the section bound, or contained overlapping sections. Completed findings are preserved."
                : $"Only the newest {MaximumFindings} failed sections are displayed; {failedSections.Length - MaximumFindings} additional failures remain in retained evidence."));
        return checks.AsReadOnly();
    }

    private static List<DriverInstallSection> ParseSections(string output, CancellationToken token, out bool partial)
    {
        string[] lines = output.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var sections = new List<DriverInstallSection>();
        string? identity = null;
        int startLine = 0;
        var errors = new List<string>();
        partial = false;
        for (int index = 0; index < lines.Length; index++)
        {
            token.ThrowIfCancellationRequested();
            string line = lines[index];
            if (line.StartsWith(">>>", StringComparison.Ordinal) && line.Contains("[Device Install", StringComparison.OrdinalIgnoreCase))
            {
                if (identity is not null) partial = true;
                identity = Identity(line);
                startLine = index + 1;
                errors.Clear();
                if (sections.Count >= MaximumSections) { partial = true; break; }
                continue;
            }
            if (identity is null) continue;
            if (line.TrimStart().StartsWith("!!!", StringComparison.Ordinal))
            {
                string error = Clean(line);
                if (errors.Count < 8 && error.Length > 0) errors.Add(error);
            }
            if (!line.StartsWith("<<<", StringComparison.Ordinal) || !line.Contains("[Exit status:", StringComparison.OrdinalIgnoreCase)) continue;
            string? code = ExitCodePattern().Match(line) is { Success: true } match ? match.Value.ToUpperInvariant() : null;
            bool failed = line.Contains("FAILURE", StringComparison.OrdinalIgnoreCase) || errors.Count > 0;
            sections.Add(new(identity, failed ? code ?? "unreported" : null, startLine, index + 1, [.. errors]));
            identity = null;
            errors.Clear();
        }
        if (identity is not null) partial = true;
        return sections;
    }

    private static bool Complete(ToolResult evidence) => evidence.ExitCode == 0 && !evidence.TimedOut && !evidence.Truncated &&
        string.IsNullOrWhiteSpace(evidence.Error) && evidence.Output.Length <= TextEvidence.MaximumCharacters;

    private static string Identity(string line)
    {
        int separator = line.LastIndexOf(" - ", StringComparison.Ordinal);
        int close = line.LastIndexOf(']');
        string value = separator >= 0 && close > separator ? line[(separator + 3)..close] : "identity not retained";
        value = Clean(value);
        return value.Length is > 0 and <= 512 ? value : "identity not retained";
    }

    private static string Clean(string value)
    {
        string clean = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return clean.Length <= 512 ? clean : clean[..512] + " [shortened]";
    }

    [GeneratedRegex(@"0x[0-9A-Fa-f]{8}", RegexOptions.CultureInvariant)]
    private static partial Regex ExitCodePattern();
}
