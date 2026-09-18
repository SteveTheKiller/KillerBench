using KillerBench.Engine;

internal static class NetworkPathCorrelationChecks
{
    public static void Run()
    {
        const string routeJson = "{\"complete\":true,\"entries\":[{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":25,\"protocol\":\"Dhcp\"},{\"destination\":\"::/0\",\"nextHop\":\"fe80::1\",\"interfaceIndex\":7,\"metric\":35,\"protocol\":\"Ra\"}]}";
        const string dnsJson = "{\"complete\":true,\"rows\":[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":2,\"serverAddresses\":[\"192.0.2.53\"]},{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":23,\"serverAddresses\":[\"2001:db8::53\"]}]}";
        WorkflowCheck Routes(string json = routeJson, CheckState state = CheckState.Information) => new("IP routing table", state, "fixture", Evidence: new(0, json, "", false));
        WorkflowCheck Dns(string json = dnsJson, CheckState state = CheckState.Information) => new("DNS server configuration", state, "fixture", Evidence: new(0, json, "", false));

        var ready = NetworkPathCorrelation.Check(Routes(), Dns());
        Require(ready.State == CheckState.Information && ready.Repair is null && ready.Detail.Contains("2 default paths", StringComparison.Ordinal) &&
            ready.Detail.Contains("Every default-path interface", StringComparison.Ordinal), "Default routes and same-family DNS correlate without selecting a change");
        var missing = NetworkPathCorrelation.Check(Routes(), Dns(dnsJson.Replace("[\"2001:db8::53\"]", "[]"), CheckState.Attention));
        Require(missing.State == CheckState.Attention && missing.Repair is null && missing.Detail.Contains("interface 7 IPv6", StringComparison.Ordinal),
            "A default route without same-family DNS requests bounded follow-up without selecting recovery");
        var localOnly = NetworkPathCorrelation.Check(Routes("{\"complete\":true,\"entries\":[]}"), Dns());
        Require(localOnly.State == CheckState.Attention && localOnly.Detail.Contains("No IPv4 or IPv6 default route", StringComparison.Ordinal) && localOnly.Repair is null,
            "No default route remains a scoped readiness observation");
        foreach (var invalid in new[]
        {
            NetworkPathCorrelation.Check(Routes(state: CheckState.Unknown), Dns()),
            NetworkPathCorrelation.Check(Routes(), Dns(state: CheckState.Unknown)),
            NetworkPathCorrelation.Check(Routes("{}"), Dns()),
            NetworkPathCorrelation.Check(Routes(), Dns("{}")),
            NetworkPathCorrelation.Check(Routes(routeJson.Replace("\"interfaceIndex\":7", "\"interfaceIndex\":0")), Dns()),
            NetworkPathCorrelation.Check(Routes(), Dns(dnsJson.Replace("\"addressFamily\":23", "\"addressFamily\":4")))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Incomplete and malformed network evidence cannot produce path readiness");
        try { NetworkPathCorrelation.Check(Routes(), Dns(), new CancellationToken(true)); throw new Exception("Network correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: default-route and DNS correlation, missing path evidence and cancellation; no network changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
