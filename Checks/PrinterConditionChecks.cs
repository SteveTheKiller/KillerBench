using System.Text.Json;
using KillerBench.Engine;

internal static class PrinterConditionChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(object[] queues, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, queues }, Capture.JsonOptions), "", false);
        var ordinary = PrinterConditionDiagnostics.Interpret(Evidence([new { name = "Office", printerStatus = 3, detectedErrorState = 2, workOffline = false }]));
        Require(ordinary.Single().State == CheckState.Information && ordinary.Single().Repair is null, "Idle no-error queue remains descriptive");
        var adverse = PrinterConditionDiagnostics.Interpret(Evidence([
            new { name = "Paper", printerStatus = 3, detectedErrorState = 4, workOffline = false },
            new { name = "Stopped", printerStatus = 6, detectedErrorState = 0, workOffline = false },
            new { name = "Offline", printerStatus = 3, detectedErrorState = 2, workOffline = true }]));
        Require(adverse.Count == 3 && adverse.All(check => check.State == CheckState.Attention) && adverse.Any(check => check.Detail.Contains("No paper")),
            "Paper, stopped and offline observations request exact-queue review");
        foreach (var invalid in new[]
        {
            Evidence([], false), Evidence([new { name = "A", printerStatus = 9, detectedErrorState = 2, workOffline = false }]),
            Evidence([new { name = "A", printerStatus = 3, detectedErrorState = 2, workOffline = false }, new { name = "a", printerStatus = 3, detectedErrorState = 2, workOffline = false }]),
            new ToolResult(1, "{}", "failed", false), new ToolResult(0, "{}", "", true)
        }) Require(PrinterConditionDiagnostics.Interpret(invalid).Single().State == CheckState.Unknown,
            "Incomplete, invalid, duplicate, failed and truncated condition evidence stays unknown");
        RepairStep? observed = null;
        var collected = await PrinterConditionDiagnostics.CheckAsync(execute: (step, _) => { observed = step; return Task.FromResult(Evidence([])); });
        Require(observed is { Verification: true } && observed.Tool == PrintRecovery.Tool && observed.Arguments.Last().Contains("Select-Object -First 257") &&
            collected.Single().State == CheckState.Information, "Native plan is bounded, read only and local-queue scoped");
        using var stop = new CancellationTokenSource(); stop.Cancel(); bool canceled = false;
        try { await PrinterConditionDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("provider should not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents printer-condition collection");
        Console.WriteLine("PASS: local printer condition codes, exact queue identity, bounds, invalid evidence and cancellation; fixtures only.");
    }
    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
