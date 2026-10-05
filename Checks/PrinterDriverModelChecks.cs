using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class PrinterDriverModelChecks
{
    internal static async Task RunAsync()
    {
        object Row(string name = "Driver", string environment = "Windows x64", int version = 4, bool packageAware = true) => new { name, environment, majorVersion = version, packageAware };
        ToolResult Evidence(object[] drivers, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, drivers }), "", false);
        var clearEvidence = Evidence([Row(), Row("Legacy", version: 3, packageAware: false)]);
        var clear = PrinterDriverModelDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) && clear.Detail.Contains("1 version 3", StringComparison.Ordinal), "Recognized version 3 and 4 registrations remain descriptive");
        Require(PrinterDriverModelDiagnostics.Interpret(Evidence([Row(version: 2)])).State == CheckState.Attention, "Unrecognized model versions request exact package review");
        Require(PrinterDriverModelDiagnostics.Interpret(Evidence([])).State == CheckState.Information, "Empty registration inventory remains scoped information");
        foreach (var invalid in new[]
        {
            Evidence([Row()], false), Evidence([Row(), Row("driver")]), Evidence([.. Enumerable.Range(0, 257).Select(i => Row("Driver " + i))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false), clearEvidence with { ExitCode = 5 }, clearEvidence with { Error = "denied" }, clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }
        }) Require(PrinterDriverModelDiagnostics.Interpret(invalid).State == CheckState.Unknown, "Incomplete, duplicate, excessive, malformed and failed evidence remains unknown");
        RepairStep? planned = null; bool bounded = false;
        var collected = await PrinterDriverModelDiagnostics.CheckAsync(execute: (step, token) => { planned = step; bounded = token.CanBeCanceled; return Task.FromResult(clearEvidence); });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } && planned.Arguments.Last().Contains("Get-PrinterDriver", StringComparison.Ordinal) && !planned.Arguments.Last().Contains("Add-PrinterDriver", StringComparison.Ordinal) && !planned.Arguments.Last().Contains("Remove-PrinterDriver", StringComparison.Ordinal), "Native plan reads bounded registrations without changing drivers");
        Require((await PrinterDriverModelDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown, "Provider failure remains unknown");
        using var stop = new CancellationTokenSource(); stop.Cancel(); bool canceled = false;
        try { await PrinterDriverModelDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); } catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: bounded printer driver model and package-awareness evidence remains read only; fixtures only.");
    }
    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
