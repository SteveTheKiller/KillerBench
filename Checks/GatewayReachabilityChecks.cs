using System.Net;
using KillerBench.Engine;

internal static class GatewayReachabilityChecks
{
    public static async Task RunAsync()
    {
        WorkflowCheck Routes(string entries, bool complete = true) => RouteDiagnostics.Interpret(new ToolResult(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"entries\":[" + entries + "]}", "", false));
        const string v4 = "{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":10,\"protocol\":\"Dhcp\"}";
        const string v6 = "{\"destination\":\"::/0\",\"nextHop\":\"2001:db8::1\",\"interfaceIndex\":9,\"metric\":20,\"protocol\":\"RouterAdvertisement\"}";
        int calls = 0;
        Task<GatewayProbeObservation> Mixed(IPAddress address, TimeSpan timeout, CancellationToken token)
        {
            calls++;
            Require(timeout == TimeSpan.FromSeconds(3), "Each gateway probe has a three-second limit");
            return Task.FromResult(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                ? new GatewayProbeObservation(0, 2, address.ToString(), GatewayProbeStatus.Reachable, 4, "fixture reply")
                : new GatewayProbeObservation(0, 23, address.ToString(), GatewayProbeStatus.NoReply, null, "fixture timeout"));
        }
        var mixed = await GatewayReachabilityDiagnostics.CheckAsync(Routes(v4 + "," + v6), Mixed);
        Require(mixed.State == CheckState.Attention && calls == 2 && mixed.Detail.Contains("1 replied") &&
            mixed.Detail.Contains("1 did not reply") && mixed.Detail.Contains("not proof"),
            "Mixed gateway replies request review without claiming outage");

        calls = 0;
        var reachable = await GatewayReachabilityDiagnostics.CheckAsync(Routes(v4), (address, _, _) =>
        {
            calls++;
            return Task.FromResult(new GatewayProbeObservation(0, 2, address.ToString(), GatewayProbeStatus.Reachable, 0, "fixture reply"));
        });
        Require(reachable.State == CheckState.Information && calls == 1 && reachable.Evidence is { ExitCode: 0 } &&
            reachable.Evidence.Output.Contains("192.0.2.1"), "Exact reachable gateway identity is retained as informational evidence");

        var onLink = await GatewayReachabilityDiagnostics.CheckAsync(Routes(v4.Replace("192.0.2.1", "0.0.0.0")),
            (_, _, _) => throw new Exception("On-link route must not be probed"));
        Require(onLink.State == CheckState.Information && onLink.Detail.Contains("on-link"), "Unspecified on-link next hop is not contacted");

        calls = 0;
        var noDefault = await GatewayReachabilityDiagnostics.CheckAsync(Routes(v4.Replace("0.0.0.0/0", "192.0.2.0/24")),
            (_, _, _) => { calls++; throw new Exception("No target"); });
        Require(noDefault.State == CheckState.Attention && calls == 0, "No default route requests review without contact");

        calls = 0;
        var incomplete = await GatewayReachabilityDiagnostics.CheckAsync(new WorkflowCheck("routes", CheckState.Unknown,
            "fixture", Evidence: new ToolResult(1, "", "unavailable", false)), (_, _, _) =>
            { calls++; throw new Exception("Incomplete routes must not probe"); });
        Require(incomplete.State == CheckState.Unknown && calls == 0, "Incomplete route evidence blocks every probe");

        var mismatched = await GatewayReachabilityDiagnostics.CheckAsync(Routes(v4), (_, _, _) => Task.FromResult(
            new GatewayProbeObservation(0, 2, "192.0.2.99", GatewayProbeStatus.Reachable, 1, "wrong target")));
        Require(mismatched.State == CheckState.Unknown && mismatched.Detail.Contains("could not be tested"),
            "Mismatched provider identity cannot establish gateway reachability");

        var duplicate = await GatewayReachabilityDiagnostics.CheckAsync(Routes(v4 + "," + v4), (_, _, _) =>
            throw new Exception("Duplicate targets must fail before probing"));
        Require(duplicate.State == CheckState.Unknown, "Duplicate route identity cannot cause repeated contact");

        using var stopped = new CancellationTokenSource();
        stopped.Cancel();
        try { await GatewayReachabilityDiagnostics.CheckAsync(Routes(v4), token: stopped.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        try { await GatewayReachabilityDiagnostics.ProbeAsync(IPAddress.Loopback, TimeSpan.FromSeconds(4), default); throw new Exception("Excessive timeout accepted"); }
        catch (ArgumentOutOfRangeException) { }

        Console.WriteLine("PASS: exact default-gateway probes, bounded coverage, partial evidence and no-outage inference.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
