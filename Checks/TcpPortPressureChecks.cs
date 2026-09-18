using System.Text.Json;
using KillerBench.Engine;

internal static class TcpPortPressureChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(int used4 = 100, int wait4 = 20, int used6 = 50, int wait6 = 10, bool complete = true) => new(0,
            JsonSerializer.Serialize(new { complete, ipv4Start = 49152, ipv4Count = 16384, ipv4InUse = used4, ipv4TimeWait = wait4,
                ipv6Start = 49152, ipv6Count = 16384, ipv6InUse = used6, ipv6TimeWait = wait6 }, Capture.JsonOptions), "", false);
        var ordinary = TcpPortPressureDiagnostics.Interpret(Evidence());
        Require(ordinary.Count == 2 && ordinary.All(check => check.State == CheckState.Information) &&
            ordinary.All(check => check.Repair is null) && ordinary.All(check => check.Evidence is { Output: var output } &&
                !output.Contains("address", StringComparison.OrdinalIgnoreCase) &&
                !output.Contains("process", StringComparison.OrdinalIgnoreCase)),
            "Ordinary aggregate port usage remains descriptive and excludes endpoints");
        var pressure = TcpPortPressureDiagnostics.Interpret(Evidence(13_108, 12_000));
        Require(pressure.Single(check => check.Name.StartsWith("IPv4")).State == CheckState.Attention &&
            pressure.Single(check => check.Name.StartsWith("IPv6")).State == CheckState.Information,
            "The exact 80 percent threshold requests review for only the affected family");
        foreach (var invalid in new[]
        {
            Evidence(complete: false), Evidence(used4: 16_385), Evidence(used4: 10, wait4: 11),
            new ToolResult(0, "{}", "", false), new ToolResult(1, "{}", "failed", false), new ToolResult(0, "{}", "", true)
        }) Require(TcpPortPressureDiagnostics.Interpret(invalid).Single().State == CheckState.Unknown,
            "Incomplete, impossible, malformed, failed and truncated evidence stays unknown");
        RepairStep? observed = null;
        var collected = await TcpPortPressureDiagnostics.CheckAsync(execute: (step, _) =>
        {
            observed = step;
            return Task.FromResult(Evidence());
        });
        Require(observed is { Verification: true } && observed.Tool == PrintRecovery.Tool && observed.Arguments.Contains("-NonInteractive") &&
            observed.Arguments.Last().Contains("Select-Object -First 200001") && collected.Count == 2,
            "Native plan uses bounded aggregate collection without endpoint output");
        using var stop = new CancellationTokenSource(); stop.Cancel(); bool canceled = false;
        try { await TcpPortPressureDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("provider should not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents TCP port-pressure collection");
        Console.WriteLine("PASS: dynamic TCP port range and aggregate pressure, privacy, bounds, invalid evidence and cancellation; fixtures only.");
    }
    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
