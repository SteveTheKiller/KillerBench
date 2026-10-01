using System.Text.Json;

namespace KillerBench.Engine.Printing;

public sealed record PrintQueueSnapshot(string[] Names, string[] PausedNames, string[] Targets, string[] WaitingNames);

public static class PrintRecovery
{
    public const string Tool = @"WindowsPowerShell\v1.0\powershell.exe";
    private const string Prefix = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; ";
    private const string Query = "@(Get-CimInstance -ClassName Win32_Printer -OperationTimeoutSec 15 | Where-Object { $_.Local -and -not $_.Network })";
    private const string Validate = "$seen=@{}; foreach($printer in $p) { [uint32]$state=0; " +
        "if($null -eq $printer.PrinterState -or -not [uint32]::TryParse([string]$printer.PrinterState,[ref]$state) -or " +
        "[string]::IsNullOrWhiteSpace($printer.Name) -or $printer.Name.Length -gt 512 -or $printer.Name -match '[\\x00-\\x1f\\x7f]' -or $seen.ContainsKey($printer.Name)) " +
        "{ throw 'Queue identity or state is unavailable or ambiguous' }; $seen[$printer.Name]=$true }; ";
    private const string Snapshot = "$p=" + Query + "; " + Validate + "$pausedQueues=@($p | Where-Object { ([uint32]$_.PrinterState -band 1) -ne 0 }); [pscustomobject]@{count=$p.Count; paused=$pausedQueues.Count; names=@($p | Select-Object -ExpandProperty Name); pausedNames=@($pausedQueues | Select-Object -ExpandProperty Name); targets=@($targets)} | ConvertTo-Json -Compress";
    private const string Resume = "$targets=@(); $p=" + Query + "; " + Validate + "foreach($printer in $p) { if(([uint32]$printer.PrinterState -band 1) -ne 0) { $targets+=$printer.Name; $r=Invoke-CimMethod -InputObject $printer -MethodName Resume -OperationTimeoutSec 15; if($null -eq $r.ReturnValue -or $r.ReturnValue -ne 0) { throw ('Resume failed with code '+$r.ReturnValue) } } }; " + Snapshot;

    public static RepairStep Step(bool verification) => new(Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Prefix + (verification ? Snapshot : Resume)], verification);

    public static RepairStep Step(bool verification, IReadOnlyList<string> selectedQueues)
    {
        var selected = ValidateSelection(selectedQueues);
        string literals = string.Join(",", selected.Select(name => "'" + name.Replace("'", "''") + "'"));
        string load = "$selected=@(" + literals + "); $all=" + Query + "; $p=$all; " + Validate +
            "$p=@($all | Where-Object { $selected -contains $_.Name }); " +
            "if($p.Count -ne $selected.Count) { throw 'Selected queue is missing or ambiguous' }; " +
            "foreach($name in $selected) { if(@($p | Where-Object { $_.Name -eq $name }).Count -ne 1) { throw 'Selected queue is missing or ambiguous' } }; ";
        string capture = "$all=" + Query + "; $p=$all; " + Validate +
            "$p=@($all | Where-Object { $selected -contains $_.Name }); " +
            "if($p.Count -ne $selected.Count) { throw 'Selected queue is missing or ambiguous' }; " +
            "$pausedQueues=@($p | Where-Object { ([uint32]$_.PrinterState -band 1) -ne 0 }); " +
            "[pscustomobject]@{count=$p.Count; paused=$pausedQueues.Count; names=@($p | Select-Object -ExpandProperty Name); pausedNames=@($pausedQueues | Select-Object -ExpandProperty Name); targets=@($targets); waitingNames=@($waitingNames)} | ConvertTo-Json -Compress";
        string command = Prefix + load + "$targets=@(); $waitingNames=@(); ";
        if (!verification)
            command += "if(@($p | Where-Object { ([uint32]$_.PrinterState -band 1) -eq 0 }).Count -ne 0) { throw 'Selected queue is no longer paused' }; " +
                "$jobs=@(Get-CimInstance Win32_PrintJob -OperationTimeoutSec 15 -ErrorAction Stop | Select-Object -First 257 Name,JobId,StatusMask); " +
                "if($jobs.Count -gt 256) { throw 'Print job inventory is incomplete' }; " +
                "foreach($name in $selected) { $waiting=@($jobs | Where-Object { $null -ne $_.JobId -and $null -ne $_.StatusMask -and " +
                "$_.Name -eq ($name + ', ' + [string]([uint32]$_.JobId)) -and (([uint32]$_.StatusMask -band 9) -ne 0) -and (([uint32]$_.StatusMask -band 388) -eq 0) }); " +
                "if($waiting.Count -lt 1) { throw 'Selected queue no longer has exact waiting-job evidence' }; $waitingNames+=$name }; " +
                "foreach($printer in $p) { $targets+=$printer.Name; $r=Invoke-CimMethod -InputObject $printer -MethodName Resume -OperationTimeoutSec 15; " +
                "if($null -eq $r.ReturnValue -or $r.ReturnValue -ne 0) { throw ('Resume failed with code '+$r.ReturnValue) } }; ";
        return new(Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command + capture], verification);
    }

    public static RepairPlan Plan(IReadOnlyList<string> selectedQueues)
    {
        var selected = ValidateSelection(selectedQueues);
        return new(RepairKind.PrintQueues, "Resume selected paused print queues",
            "Resumes only exact selected local queues that fresh evidence tied to visible waiting jobs. Jobs are preserved. Physical printer availability and final document output remain unverified.",
            [Step(false, selected), Step(true, selected)]);
    }

    public static (int Count, int Paused)? Read(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return null;
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                root.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != root.EnumerateObject().Count() ||
                !root.TryGetProperty("count", out var countValue) || countValue.ValueKind != JsonValueKind.Number || !countValue.TryGetInt32(out int count) ||
                !root.TryGetProperty("paused", out var pausedValue) || pausedValue.ValueKind != JsonValueKind.Number || !pausedValue.TryGetInt32(out int paused)) return null;
            return count >= 0 && paused >= 0 && paused <= count ? (count, paused) : null;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException) { return null; }
    }

    public static PrintQueueSnapshot? ReadIdentities(ToolResult result)
    {
        var counts = Read(result);
        if (counts is null) return null;
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            string[] Values(string name) => [.. json.RootElement.GetProperty(name).EnumerateArray()
                .Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : null).OfType<string>()];
            var names = Values("names");
            var paused = Values("pausedNames");
            var targets = Values("targets");
            var waiting = json.RootElement.TryGetProperty("waitingNames", out var waitingValue) && waitingValue.ValueKind == JsonValueKind.Array
                ? Values("waitingNames") : [];
            bool Valid(string[] values) => values.All(value => DriverReference.ValidText(value, 512)) &&
                values.Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Length;
            return names.Length == counts.Value.Count && paused.Length == counts.Value.Paused &&
                Valid(names) && Valid(paused) && Valid(targets) && Valid(waiting) && paused.All(name => names.Contains(name, StringComparer.OrdinalIgnoreCase)) &&
                targets.All(name => names.Contains(name, StringComparer.OrdinalIgnoreCase)) &&
                waiting.All(name => names.Contains(name, StringComparer.OrdinalIgnoreCase))
                ? new(names, paused, targets, waiting) : null;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return null; }
    }

    public static bool Verify(IReadOnlyList<RepairStepResult> steps)
    {
        if (steps.Count != 2 || Read(steps[0].Result) is null || Read(steps[1].Result) is not { Count: > 0, Paused: 0 }) return false;
        try
        {
            using var changed = JsonDocument.Parse(steps[0].Result.Output);
            using var final = JsonDocument.Parse(steps[1].Result.Output);
            var targets = changed.RootElement.GetProperty("targets").EnumerateArray().Select(x => x.GetString()).ToArray();
            var changedNames = changed.RootElement.GetProperty("names").EnumerateArray().Select(x => x.GetString()).ToArray();
            var names = final.RootElement.GetProperty("names").EnumerateArray().Select(x => x.GetString()).ToArray();
            return targets.Length > 0 && targets.All(x => DriverReference.ValidText(x, 512)) &&
                targets.Distinct(StringComparer.OrdinalIgnoreCase).Count() == targets.Length &&
                changedNames.Length == Read(steps[0].Result)!.Value.Count && changedNames.All(x => DriverReference.ValidText(x, 512)) &&
                changedNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() == changedNames.Length &&
                targets.All(x => changedNames.Contains(x, StringComparer.OrdinalIgnoreCase)) &&
                names.Length == Read(steps[1].Result)!.Value.Count && names.All(x => DriverReference.ValidText(x, 512)) &&
                names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Length &&
                targets.All(x => names.Contains(x, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException) { return false; }
    }

    public static bool VerifySelected(IReadOnlyList<string> selectedQueues, IReadOnlyList<RepairStepResult> steps)
    {
        var selected = ValidateSelection(selectedQueues);
        var expected = Plan(selected).Steps;
        if (steps.Count != expected.Count) return false;
        for (int i = 0; i < expected.Count; i++)
            if (steps[i].Step.Tool != expected[i].Tool || steps[i].Step.Verification != expected[i].Verification ||
                !steps[i].Step.Arguments.SequenceEqual(expected[i].Arguments)) return false;
        var changed = ReadIdentities(steps[0].Result);
        var final = ReadIdentities(steps[1].Result);
        return changed is not null && final is not null && changed.Targets.Length == selected.Length && changed.WaitingNames.Length == selected.Length &&
            changed.Targets.All(name => selected.Contains(name, StringComparer.OrdinalIgnoreCase)) &&
            changed.WaitingNames.All(name => selected.Contains(name, StringComparer.OrdinalIgnoreCase)) &&
            final.Targets.Length == 0 && final.Names.Length == selected.Length && final.PausedNames.Length == 0 &&
            selected.All(name => final.Names.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        var state = Read(result);
        return state switch
        {
            null => new("Print queues", CheckState.Unknown, "Local print queue state could not be read. No resume selected.", Evidence: result),
            { Paused: > 0 } => new("Print queues", CheckState.Attention, $"{state.Value.Paused} local queues are paused. Waiting-job evidence is required before automatic resume because a pause can be intentional.", Evidence: result),
            _ => new("Print queues", CheckState.Clear, $"{state.Value.Count} local queues checked; none paused. This does not verify physical printing.", Evidence: result)
        };
    }

    private static string[] ValidateSelection(IReadOnlyList<string> selectedQueues)
    {
        ArgumentNullException.ThrowIfNull(selectedQueues);
        var selected = selectedQueues.ToArray();
        if (selected.Length is < 1 or > 64 || selected.Any(name => !DriverReference.ValidText(name, 512)) ||
            selected.Distinct(StringComparer.OrdinalIgnoreCase).Count() != selected.Length)
            throw new ArgumentException("Select 1 to 64 unique local queue identities.", nameof(selectedQueues));
        return selected;
    }

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            var result = await NativeTool.RunAsync(step.Tool, step.Arguments, progress: progress, token: timeout.Token).ConfigureAwait(false);
            if (!step.Verification &&
                (PrinterOfflineRecovery.Read(result) is { Targets.Length: 0 } || ReadIdentities(result) is { Targets.Length: 0 }))
                throw new RepairStepSkippedException(
                    "Fresh printer evidence no longer identifies a selected queue requiring change. No printer command was run.", result);
            return result;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { return new(-1, "", "Print queue operation timed out. Some queues may already have resumed; verification is required.", false) { TimedOut = true }; }
    }
}
