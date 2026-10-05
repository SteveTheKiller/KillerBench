using System.IO;
using KillerBench.Engine;

internal static class ReservedStorageChecks
{
    internal static async Task RunAsync()
    {
        static ToolResult Evidence(string state) => new(0, "{\"state\":\"" + state + "\"}", "", false);
        foreach (string state in new[] { "Enabled", "Disabled", "InUse" })
        {
            var finding = ReservedStorageDiagnostics.Interpret(Evidence(state));
            Require(finding is { State: CheckState.Information, Repair: null }, "Documented reserved-storage states remain descriptive");
        }
        foreach (var invalid in new[]
        {
            Evidence("UnknownFutureState"), new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            Evidence("Enabled") with { ExitCode = 5 }, Evidence("Enabled") with { Error = "elevation required" }, Evidence("Enabled") with { TimedOut = true }, Evidence("Enabled") with { Truncated = true }
        }) Require(ReservedStorageDiagnostics.Interpret(invalid).State == CheckState.Unknown, "Unsupported, malformed and failed evidence remains unknown");
        RepairStep? planned = null; bool bounded = false;
        var collected = await ReservedStorageDiagnostics.CheckAsync(execute: (step, token) => { planned = step; bounded = token.CanBeCanceled; return Task.FromResult(Evidence("Enabled")); });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } && planned.Arguments.Last().Contains("Get-WindowsReservedStorageState", StringComparison.Ordinal) && !planned.Arguments.Last().Contains("Set-WindowsReservedStorageState", StringComparison.Ordinal), "Native plan reads reserved storage without changing it");
        Require((await ReservedStorageDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown, "Provider failure remains unknown");
        using var stop = new CancellationTokenSource(); stop.Cancel(); bool canceled = false;
        try { await ReservedStorageDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); } catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: reserved-storage states, malformed evidence and read-only planning; fixtures only.");
    }
    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
