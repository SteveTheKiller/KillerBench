using System.Text.Json;
using KillerBench.Engine;

internal static class PrinterQueueAttributeChecks
{
    internal static async Task RunAsync()
    {
        static ToolResult Evidence(object[] queues, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, queues }, Capture.JsonOptions), "", false);

        var ordinary = PrinterQueueAttributeDiagnostics.Interpret(Evidence([
            new { name = "Office", attributes = 0x1u | 0x40u | 0x800u }]));
        Require(ordinary.Single() is { State: CheckState.Information, Repair: null } &&
            ordinary.Single().Detail.Contains("Spooling mode: Queued", StringComparison.Ordinal),
            "Ordinary queued attributes remain descriptive");
        var implicitSpooling = PrinterQueueAttributeDiagnostics.Interpret(Evidence([
            new { name = "Virtual", attributes = 0x40u | 0x200u },
            new { name = "Fax", attributes = 0x40u | 0x4000u }]));
        Require(implicitSpooling.All(check => check.State == CheckState.Information) &&
            implicitSpooling.All(check => check.Detail.Contains("Not explicitly reported", StringComparison.Ordinal)),
            "Absent spool-mode bits and current SDK Fax attributes remain valid evidence");

        foreach (uint attributes in new[] { 0x2u | 0x40u, 0x1u | 0x40u | 0x100u, 0x1u | 0x40u | 0x400u, 0x1u | 0x40u | 0x2000u })
            Require(PrinterQueueAttributeDiagnostics.Interpret(Evidence([new { name = "Review", attributes }])).Single() is
                { State: CheckState.Attention, Repair: null },
                "Direct, retained-document, offline and inconsistent publication attributes request review");

        var sharedPublished = PrinterQueueAttributeDiagnostics.Interpret(Evidence([
            new { name = "Shared", attributes = 0x1u | 0x8u | 0x40u | 0x2000u }])).Single();
        Require(sharedPublished.State == CheckState.Information, "Shared published queues are not treated as inconsistent");

        foreach (var invalid in new[]
        {
            Evidence([], false), Evidence([new { name = "A", attributes = 0x3u }]),
            Evidence([new { name = "A", attributes = 0x1u }, new { name = "a", attributes = 0x1u }]),
            Evidence([new { name = "A", attributes = 0x01000000u }]),
            new ToolResult(1, "{}", "failed", false), new ToolResult(0, "{}", "", true)
        }) Require(PrinterQueueAttributeDiagnostics.Interpret(invalid).Single().State == CheckState.Unknown,
            "Incomplete, contradictory, duplicate, unsupported, failed and truncated queue attributes stay unknown");

        RepairStep? observed = null;
        var collected = await PrinterQueueAttributeDiagnostics.CheckAsync(execute: (step, _) =>
        {
            observed = step;
            return Task.FromResult(Evidence([]));
        });
        Require(observed is { Verification: true } && observed.Tool == PrintRecovery.Tool &&
            observed.Arguments.Last().Contains("Select-Object -First 257", StringComparison.Ordinal) &&
            collected.Single().State == CheckState.Information,
            "Native plan is bounded, read only and local-queue scoped");

        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await PrinterQueueAttributeDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("provider should not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents printer queue attribute collection");
        Console.WriteLine("PASS: local printer queue attributes, exact identities, review states, bounds and cancellation; fixtures only.");
    }

    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
