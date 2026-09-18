using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record DefenderScanHistoryObservation(DateTimeOffset CollectedUtc, bool ScanInProgress,
    DateTimeOffset? QuickStartUtc, DateTimeOffset? QuickEndUtc, int? QuickAgeDays, bool QuickOverdue,
    DateTimeOffset? FullStartUtc, DateTimeOffset? FullEndUtc, int? FullAgeDays, bool FullOverdue);

public static class DefenderScanHistory
{
    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
    ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$s=Get-MpComputerStatus -ErrorAction Stop;" +
        "$utc={param($v)if($v-is[datetime]-and$v.Year-gt 1601){$v.ToUniversalTime().ToString('o',[Globalization.CultureInfo]::InvariantCulture)}else{$null}};" +
        "$age={param($v)if($null-eq$v-or[uint32]$v-eq[uint32]::MaxValue){$null}else{[int]$v}};" +
        "[pscustomobject]@{complete=$true;collectedUtc=[DateTime]::UtcNow.ToString('o',[Globalization.CultureInfo]::InvariantCulture);scanInProgress=[bool]$s.ScanInProgress;" +
        "quickStartUtc=(&$utc $s.QuickScanStartTime);quickEndUtc=(&$utc $s.QuickScanEndTime);quickAgeDays=(&$age $s.QuickScanAge);quickOverdue=[bool]$s.QuickScanOverdue;" +
        "fullStartUtc=(&$utc $s.FullScanStartTime);fullEndUtc=(&$utc $s.FullScanEndTime);fullAgeDays=(&$age $s.FullScanAge);fullOverdue=[bool]$s.FullScanOverdue}|ConvertTo-Json -Compress"], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Microsoft Defender scan history requires Windows.");
        return Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        const string name = "Microsoft Defender scan history";
        const string boundary = " This provider snapshot is not a malware scan and does not prove protection, threat absence, scan coverage, scan success or remediation. Threat names, paths, resources, accounts, exclusions and file contents are excluded. No scan or security action was requested.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Microsoft Defender scan timing evidence was unavailable or incomplete.");
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            string[] expected = ["complete", "collectedUtc", "scanInProgress", "quickStartUtc", "quickEndUtc", "quickAgeDays", "quickOverdue", "fullStartUtc", "fullEndUtc", "fullAgeDays", "fullOverdue"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Length ||
                expected.Any(property => !root.TryGetProperty(property, out _)) ||
                root.GetProperty("complete").ValueKind != JsonValueKind.True ||
                !TryBoolean(root, "scanInProgress", out bool scanInProgress) ||
                !TryBoolean(root, "quickOverdue", out bool quickOverdue) ||
                !TryBoolean(root, "fullOverdue", out bool fullOverdue) ||
                !TryUtc(root, "collectedUtc", false, out var collected) ||
                !TryUtc(root, "quickStartUtc", true, out var quickStart) ||
                !TryUtc(root, "quickEndUtc", true, out var quickEnd) ||
                !TryAge(root, "quickAgeDays", out int? quickAge) ||
                !TryUtc(root, "fullStartUtc", true, out var fullStart) ||
                !TryUtc(root, "fullEndUtc", true, out var fullEnd) ||
                !TryAge(root, "fullAgeDays", out int? fullAge))
                return Unknown("Microsoft Defender scan timing evidence was malformed.");

            var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
            if (collected is null || collected > now.AddMinutes(5) || collected < now.AddDays(-1) ||
                !Coherent(quickStart, quickEnd, quickAge, collected.Value) ||
                !Coherent(fullStart, fullEnd, fullAge, collected.Value) ||
                quickOverdue && quickEnd is null || fullOverdue && fullEnd is null)
                return Unknown("Microsoft Defender scan timing evidence was internally inconsistent.");

            var observation = new DefenderScanHistoryObservation(collected.Value, scanInProgress,
                quickStart, quickEnd, quickAge, quickOverdue, fullStart, fullEnd, fullAge, fullOverdue);
            string quick = Describe("quick", observation.QuickEndUtc, observation.QuickAgeDays, observation.QuickOverdue);
            string full = Describe("full", observation.FullEndUtc, observation.FullAgeDays, observation.FullOverdue);
            string active = observation.ScanInProgress ? " Microsoft Defender reports a scan currently in progress." : " Microsoft Defender does not report a scan currently in progress.";
            bool attention = observation.QuickOverdue || observation.FullOverdue;
            return new(name, attention ? CheckState.Attention : CheckState.Information,
                quick + " " + full + active + (attention ? " Review scan policy and the intended endpoint-security schedule." : "") + boundary,
                Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return Unknown("Microsoft Defender scan timing evidence could not be interpreted (" + error.GetType().Name + ").");
        }
    }

    private static bool Coherent(DateTimeOffset? start, DateTimeOffset? end, int? age, DateTimeOffset collected) =>
        (start is null) == (end is null) && (end is null) == (age is null) &&
        (start is null || start <= end && end <= collected.AddMinutes(5));

    private static string Describe(string kind, DateTimeOffset? end, int? age, bool overdue) => end is null
        ? "No completed " + kind + " scan timestamp was reported."
        : "Last completed " + kind + " scan: " + end.Value.ToString("u", CultureInfo.InvariantCulture) +
            " (provider age " + age + " day" + (age == 1 ? "" : "s") + (overdue ? ", overdue" : "") + ").";

    private static bool TryBoolean(JsonElement root, string name, out bool value)
    {
        var item = root.GetProperty(name);
        value = item.ValueKind == JsonValueKind.True;
        return item.ValueKind is JsonValueKind.True or JsonValueKind.False;
    }

    private static bool TryAge(JsonElement root, string name, out int? value)
    {
        var item = root.GetProperty(name);
        value = null;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out int number) || number is < 0 or > 36500) return false;
        value = number;
        return true;
    }

    private static bool TryUtc(JsonElement root, string name, bool nullable, out DateTimeOffset? value)
    {
        var item = root.GetProperty(name);
        value = null;
        if (nullable && item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParseExact(item.GetString(), "o", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) || parsed.Offset != TimeSpan.Zero) return false;
        value = parsed;
        return true;
    }
}
