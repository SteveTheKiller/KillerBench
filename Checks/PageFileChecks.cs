using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class PageFileChecks
{
    internal static async Task RunAsync()
    {
        object Row(string drive = "C:", ulong allocatedMiB = 8192, ulong currentMiB = 512, ulong peakMiB = 2048,
            bool temporary = false) => new { drive, allocatedMiB, currentMiB, peakMiB, temporary };
        ToolResult Evidence(object[] pageFiles, bool automaticManaged = true, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, automaticManaged, pageFiles }), "", false);

        var clearEvidence = Evidence([Row()]);
        var clear = PageFileDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) &&
            clear.Detail.Contains("automatic page-file management", StringComparison.Ordinal) && !clear.Detail.Contains("pagefile.sys", StringComparison.OrdinalIgnoreCase),
            "Normal allocation retains bounded counts without full paths or repair selection");
        Require(PageFileDiagnostics.Interpret(Evidence([Row()], automaticManaged: false)).State == CheckState.Information,
            "Manual page-file management remains descriptive rather than a fault");
        Require(PageFileDiagnostics.Interpret(Evidence([Row(currentMiB: 900, peakMiB: 950, allocatedMiB: 1000)])).State == CheckState.Attention,
            "Current usage at the review threshold requests memory correlation");
        Require(PageFileDiagnostics.Interpret(Evidence([Row(temporary: true)])).State == CheckState.Attention,
            "A provider-reported temporary page file requests configuration review");
        Require(PageFileDiagnostics.Interpret(Evidence([])).State == CheckState.Attention,
            "No allocated page file requests scoped review without selecting a change");

        foreach (var invalid in new[]
        {
            Evidence([Row()], complete: false), Evidence([Row(drive: "c:")]), Evidence([Row(drive: "C:\\pagefile.sys")]),
            Evidence([Row(allocatedMiB: 0)]), Evidence([Row(allocatedMiB: 100, currentMiB: 101, peakMiB: 101)]),
            Evidence([Row(allocatedMiB: 100, currentMiB: 50, peakMiB: 40)]), Evidence([Row(), Row(drive: "c:")]),
            Evidence([.. Enumerable.Range(0, 17).Select(index => Row(((char)('A' + index)).ToString() + ":"))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { Error = "denied" },
            clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }
        })
            Require(PageFileDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, contradictory, duplicate, excessive, malformed and failed evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await PageFileDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(clearEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Win32_PageFileUsage", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("AutomaticManagedPagefile", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Win32_PageFileSetting", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-CimInstance", StringComparison.Ordinal),
            "Native plan reads current usage and management state without changing page-file settings");
        Require((await PageFileDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await PageFileDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: page-file management, allocation pressure, temporary state, privacy bounds and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
