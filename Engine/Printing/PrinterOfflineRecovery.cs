using System.Text.Json;

namespace KillerBench.Engine;

public sealed record PrinterOfflineSnapshot(string[] Targets, IReadOnlyDictionary<string, bool> Queues);

public static class PrinterOfflineRecovery
{
    // The Windows PowerShell WMI provider enables privileges required to update Win32_Printer.
    private const string Prefix = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; function Get-LocalQueues { Get-WmiObject -Class Win32_Printer -EnableAllPrivileges -ErrorAction Stop | Where-Object { $_.Local -eq $true -and $_.Network -eq $false } }; ";
    private const string Load = "$queues=@(Get-LocalQueues); foreach($p in $queues) { if([string]::IsNullOrWhiteSpace($p.DeviceID) -or $p.WorkOffline -isnot [bool]) { throw 'Printer offline state is unavailable' } }; ";
    private const string Snapshot = Load + "[pscustomobject]@{targets=@($targets); queues=@($queues | ForEach-Object { [pscustomobject]@{id=$_.DeviceID; offline=$_.WorkOffline} })} | ConvertTo-Json -Depth 5 -Compress";
    private const string Recover = Load + "foreach($p in $queues) { if($p.WorkOffline) { $targets += $p.DeviceID; $p.WorkOffline=$false; $null=$p.Put() } }; ";
    public static RepairStep Step(bool verification) => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Prefix + "$targets=@(); " + (verification ? "" : Recover) + Snapshot], verification);

    public static RepairStep Step(bool verification, IReadOnlyList<string> selectedQueues)
    {
        ArgumentNullException.ThrowIfNull(selectedQueues);
        var selected = selectedQueues.ToArray();
        if (selected.Length is < 1 or > 64 || selected.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 256 || id.Any(char.IsControl)) ||
            selected.Distinct(StringComparer.OrdinalIgnoreCase).Count() != selected.Length)
            throw new ArgumentException("Select 1 to 64 unique local queue identities.", nameof(selectedQueues));
        string literals = string.Join(",", selected.Select(id => "'" + id.Replace("'", "''") + "'"));
        string prefix = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $selected=@(" + literals + "); " +
            "function Get-LocalQueues { $all=@(Get-WmiObject -Class Win32_Printer -EnableAllPrivileges -ErrorAction Stop | Where-Object { $_.Local -eq $true -and $_.Network -eq $false }); " +
            "foreach($id in $selected) { $matches=@($all | Where-Object { $_.DeviceID -eq $id }); if($matches.Count -ne 1) { throw 'Selected queue is missing or ambiguous' } }; " +
            "$all | Where-Object { $selected -contains $_.DeviceID } }; ";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            prefix + "$targets=@(); " + (verification ? "" : Recover) + Snapshot], verification);
    }

    public static PrinterOfflineSnapshot? Read(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return null;
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            if (!UniqueFields(json.RootElement)) return null;
            var targets = json.RootElement.GetProperty("targets").EnumerateArray().Select(x => x.GetString()!).ToArray();
            var queues = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in json.RootElement.GetProperty("queues").EnumerateArray())
            {
                if (!UniqueFields(item)) return null;
                string? id = item.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(id) || !queues.TryAdd(id, item.GetProperty("offline").GetBoolean())) return null;
            }
            return targets.Any(string.IsNullOrWhiteSpace) || targets.Distinct(StringComparer.OrdinalIgnoreCase).Count() != targets.Length ? null : new(targets, queues);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException) { return null; }
    }

    private static bool UniqueFields(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();

    public static WorkflowCheck Interpret(ToolResult result)
    {
        var state = Read(result);
        if (state is null) return new("Printer offline setting", CheckState.Unknown, "Local queue offline settings could not be read. No change selected.", Evidence: result);
        int offline = state.Queues.Values.Count(value => value);
        return offline > 0
            ? new("Printer offline setting", CheckState.Repairable, $"{offline} local queues use the offline setting. Clearing it can release waiting jobs and remove an intentional setting. Physical device availability remains untested.", RepairKind.PrinterOffline, result)
            : new("Printer offline setting", CheckState.Clear, $"{state.Queues.Count} local queues checked; none use the offline setting. Physical device availability remains untested.", Evidence: result);
    }

    public static bool Verify(IReadOnlyList<RepairStepResult> steps)
    {
        if (steps.Count != 2) return false;
        var changed = Read(steps[0].Result);
        var final = Read(steps[1].Result);
        return changed is not null && final is not null && changed.Targets.Length > 0 &&
            changed.Targets.All(id => changed.Queues.TryGetValue(id, out bool operationOffline) && !operationOffline) &&
            changed.Targets.All(id => final.Queues.TryGetValue(id, out bool offline) && !offline);
    }

    public static RepairPlan Plan(IReadOnlyList<string> selectedQueues)
    {
        ArgumentNullException.ThrowIfNull(selectedQueues);
        var selected = selectedQueues.ToArray();
        var change = Step(false, selected);
        var verify = Step(true, selected);
        return new(RepairKind.PrinterOffline, "Clear selected printer offline settings",
            "Clears the offline setting only on the selected local queues. This can release waiting jobs and override an intentional offline setting. Jobs are not deleted. Physical printer availability is not tested.",
            [change, verify]);
    }

    public static bool VerifySelected(IReadOnlyList<string> selectedQueues, IReadOnlyList<RepairStepResult> steps)
    {
        ArgumentNullException.ThrowIfNull(selectedQueues);
        ArgumentNullException.ThrowIfNull(steps);
        var selected = selectedQueues.ToArray();
        var expected = Plan(selected).Steps;
        if (steps.Count != expected.Count) return false;
        for (int i = 0; i < expected.Count; i++)
            if (steps[i].Step.Tool != expected[i].Tool || steps[i].Step.Verification != expected[i].Verification ||
                !steps[i].Step.Arguments.SequenceEqual(expected[i].Arguments)) return false;
        if (!Verify(steps)) return false;
        var changed = Read(steps[0].Result)!;
        var final = Read(steps[1].Result)!;
        return changed.Targets.All(id => selected.Contains(id, StringComparer.OrdinalIgnoreCase)) &&
            final.Targets.Length == 0 && final.Queues.Count == selected.Length &&
            selected.All(id => final.Queues.TryGetValue(id, out bool offline) && !offline);
    }
}
