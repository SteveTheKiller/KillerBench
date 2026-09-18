using KillerBench.Engine;

internal static class NetworkConnectivityCorrelationChecks
{
    public static void Run()
    {
        WorkflowCheck Routes(string rows, bool complete = true) => RouteDiagnostics.Interpret(new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"entries\":[" + rows + "]}", "", false));
        IReadOnlyList<WorkflowCheck> Profiles(string ipv4 = "Internet", string ipv6 = "NoTraffic", int index = 7) =>
            NetworkProfileDiagnostics.Interpret(new(0, "{\"profiles\":[{\"InterfaceAlias\":\"Ethernet\",\"InterfaceIndex\":" + index +
                ",\"NetworkCategory\":\"Private\",\"IPv4Connectivity\":\"" + ipv4 + "\",\"IPv6Connectivity\":\"" + ipv6 + "\"}]}", "", false));
        WorkflowCheck Gateways(string rows, bool complete = true, CheckState state = CheckState.Information) => new("gateways", state, "fixture",
            Evidence: new(0, "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"observations\":[" + rows + "]}", "", false));
        const string route = "{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":10,\"protocol\":\"Dhcp\"}";
        const string probe = "{\"interfaceIndex\":7,\"addressFamily\":2,\"nextHop\":\"192.0.2.1\",\"status\":\"Reachable\",\"roundtripMilliseconds\":2,\"detail\":\"fixture\"}";

        var consistent = NetworkConnectivityCorrelation.Check(Profiles(), Routes(route), Gateways(probe));
        Require(consistent.State == CheckState.Information && consistent.Repair is null && consistent.Detail.Contains("No contradiction"),
            "Matching Windows connectivity, route and gateway evidence remains informational");
        var missingRoute = NetworkConnectivityCorrelation.Check(Profiles(), Routes(""), Gateways(""));
        Require(missingRoute.State == CheckState.Attention && missingRoute.Detail.Contains("disagree"),
            "Internet connectivity without a current default route requests review");
        var staleRoute = NetworkConnectivityCorrelation.Check(Profiles("Disconnected"), Routes(route), Gateways(probe));
        Require(staleRoute.State == CheckState.Attention, "Disconnected connectivity with a current default path requests review");
        var blockedIcmp = NetworkConnectivityCorrelation.Check(Profiles(), Routes(route), Gateways(probe.Replace("Reachable", "NoReply"), state: CheckState.Attention));
        Require(blockedIcmp.State == CheckState.Information, "A matching no-reply gateway does not contradict Windows Internet connectivity");
        var noProbe = NetworkConnectivityCorrelation.Check(Profiles(), Routes(route), Gateways(""));
        Require(noProbe.State == CheckState.Unknown, "A default path without exact gateway evidence remains incomplete");
        var vpnPath = NetworkConnectivityCorrelation.Check(Profiles(), Routes(route.Replace("\"interfaceIndex\":7", "\"interfaceIndex\":9")),
            Gateways(probe.Replace("\"interfaceIndex\":7", "\"interfaceIndex\":9")));
        Require(vpnPath.State == CheckState.Attention && vpnPath.Detail.Contains("no matching connection profile"),
            "Unprofiled defaults remain visible while the profile's Internet-without-route contradiction requests review");
        foreach (var invalid in new[]
        {
            NetworkConnectivityCorrelation.Check([], Routes(route), Gateways(probe)),
            NetworkConnectivityCorrelation.Check(Profiles(), Routes(route, false), Gateways(probe)),
            NetworkConnectivityCorrelation.Check(Profiles(), Routes(route), Gateways(probe, false)),
            NetworkConnectivityCorrelation.Check(Profiles(), Routes(route), Gateways(probe.Replace("192.0.2.1", "192.0.2.2")))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null, "Incomplete or mismatched source evidence cannot produce readiness");
        try { NetworkConnectivityCorrelation.Check(Profiles(), Routes(route), Gateways(probe), new(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: profile, route and exact-gateway connectivity correlation with conservative contradiction handling.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
