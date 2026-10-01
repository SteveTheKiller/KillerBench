using KillerBench.Engine;

internal static class RouteChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string complete = "true", string entries = "[]") =>
            new(0, $"{{\"complete\":{complete},\"entries\":{entries}}}", "", false);
        var populated = Evidence(entries: "[{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":25,\"protocol\":\"Dhcp\"},{\"destination\":\"2001:db8::/32\",\"nextHop\":\"::\",\"interfaceIndex\":9,\"metric\":5,\"protocol\":\"Local\"}]");
        var result = RouteDiagnostics.Interpret(populated);
        Require(result.State == CheckState.Information && result.Repair is null && result.Evidence == populated &&
            result.Detail.Contains("Reviewed 2 IP routes; 1 default route", StringComparison.Ordinal) &&
            result.Detail.Contains("192.0.2.1", StringComparison.Ordinal) && result.Detail.Contains("No route", StringComparison.Ordinal),
            "Routing evidence reports bounded defaults without selecting a network change");
        Require(RouteDiagnostics.Interpret(Evidence()).State == CheckState.Information,
            "An empty complete route table remains informational rather than inventing a fault");
        foreach (var invalid in new[]
        {
            populated with { ExitCode = 5 }, populated with { Error = "denied" }, populated with { TimedOut = true }, populated with { Truncated = true },
            Evidence("false"), Evidence(entries: "[{}]"),
            Evidence(entries: "[{\"destination\":\"0.0.0.0/33\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":25,\"protocol\":\"Dhcp\"}]"),
            Evidence(entries: "[{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"not-an-address\",\"interfaceIndex\":7,\"metric\":25,\"protocol\":\"Dhcp\"}]"),
            Evidence(entries: "[{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":0,\"metric\":25,\"protocol\":\"Dhcp\"}]"),
            Evidence(entries: "[{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":-1,\"protocol\":\"Dhcp\"}]"),
            Evidence(entries: "[{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":25,\"protocol\":\"Dhcp\"},{\"destination\":\"0.0.0.0/0\",\"nextHop\":\"192.0.2.1\",\"interfaceIndex\":7,\"metric\":25,\"protocol\":\"dhcp\"}]")
        }) Require(RouteDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed and duplicate route evidence remains unknown");
        var capped = Evidence(entries: "[" + string.Join(",", Enumerable.Range(0, 1025).Select(index =>
            $"{{\"destination\":\"10.{index / 256}.{index % 256}.0/24\",\"nextHop\":\"0.0.0.0\",\"interfaceIndex\":1,\"metric\":0,\"protocol\":\"Local\"}}")) + "]");
        Require(RouteDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized route evidence cannot claim complete coverage");
        Console.WriteLine("PASS: bounded IP routes, strict identities, default summaries and incomplete evidence; no network changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
