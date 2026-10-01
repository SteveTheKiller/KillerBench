using System.Text.Json;

namespace KillerBench.Engine.Security;

public static class DefenderThreatSummary
{
    private const int RetainedLimit = 1000;

    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
    ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$all=@(Get-MpThreat -ErrorAction Stop | Select-Object -First 1001); $rows=@($all | Select-Object -First 1000); " +
        "$complete=$true; $active=0; $executed=0; $severity=@{}; " +
        "foreach($row in $rows){ if($row.IsActive -isnot [bool] -or $row.DidThreatExecute -isnot [bool] -or $null -eq $row.SeverityID){$complete=$false; continue}; " +
        "try{$id=[int]$row.SeverityID}catch{$complete=$false; continue}; if($id -lt 0){$complete=$false; continue}; " +
        "if($row.IsActive){$active++}; if($row.DidThreatExecute){$executed++}; if($severity.ContainsKey($id)){$severity[$id]++}else{$severity[$id]=1} }; " +
        "$groups=@($severity.GetEnumerator() | Sort-Object Name | ForEach-Object { [pscustomobject]@{id=[int]$_.Name; count=[int]$_.Value} }); " +
        "[pscustomobject]@{complete=$complete; observed=[int]$all.Count; retained=[int]$rows.Count; active=$active; executed=$executed; capped=($all.Count -gt 1000); severity=$groups} | ConvertTo-Json -Depth 3 -Compress"], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Microsoft Defender threat history requires Windows.");
        return Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown(evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            string[] expected = ["complete", "observed", "retained", "active", "executed", "capped", "severity"];
            if (!ExactObject(root, expected) || !Boolean(root, "complete", out bool complete) || !Boolean(root, "capped", out bool capped) ||
                !Integer(root, "observed", 0, RetainedLimit + 1, out int observed) || !Integer(root, "retained", 0, RetainedLimit, out int retained) ||
                !Integer(root, "active", 0, retained, out int active) || !Integer(root, "executed", 0, retained, out int executed) ||
                !root.TryGetProperty("severity", out var severity) || severity.ValueKind != JsonValueKind.Array || severity.GetArrayLength() > RetainedLimit ||
                retained != severity.EnumerateArray().Sum(GroupCount) || (capped ? observed != RetainedLimit + 1 || retained != RetainedLimit : observed != retained)) return Unknown(evidence);
            var groups = new List<(int Id, int Count)>();
            foreach (var item in severity.EnumerateArray())
            {
                if (!ExactObject(item, ["id", "count"]) || !Integer(item, "id", 0, int.MaxValue, out int id) ||
                    !Integer(item, "count", 1, RetainedLimit, out int count) || groups.Any(group => group.Id == id)) return Unknown(evidence);
                groups.Add((id, count));
            }
            if (!complete) return Unknown(evidence);
            string details = $"Microsoft Defender returned {retained} retained threat history record{(retained == 1 ? "" : "s")}; {active} currently active and {executed} marked as having executed by the provider.";
            if (groups.Count > 0) details += " Severity IDs: " + string.Join(", ", groups.OrderBy(group => group.Id).Select(group => $"{group.Id}={group.Count}")) + ".";
            details += " Counts are provider records, not unique files, users or incidents. Threat names, identifiers, paths, resources, accounts and process details were not collected. This inventory does not prove remediation, file state or overall protection health. No scan, quarantine, removal, exclusion, protection or signature action was selected.";
            return new("Microsoft Defender threat history", capped ? CheckState.Unknown : active > 0 ? CheckState.Attention : CheckState.Information,
                capped ? details + " The 1,000-record retention limit was reached, so coverage is incomplete." : details, Evidence: evidence);
        }
        catch (JsonException) { return Unknown(evidence); }
        catch (InvalidOperationException) { return Unknown(evidence); }
    }

    private static WorkflowCheck Unknown(ToolResult evidence) => new("Microsoft Defender threat history", CheckState.Unknown,
        "Microsoft Defender threat history is unavailable, malformed or incomplete. No current threat, remediation, file or protection state was inferred, and no security action was selected.", Evidence: evidence);

    private static int GroupCount(JsonElement item) => item.ValueKind == JsonValueKind.Object && Integer(item, "count", 1, RetainedLimit, out int count) ? count : -1;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value) && value >= minimum && value <= maximum;
    }
    private static bool ExactObject(JsonElement value, string[] names) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == names.Length && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == names.Length &&
        value.EnumerateObject().All(property => names.Contains(property.Name, StringComparer.Ordinal));
}
