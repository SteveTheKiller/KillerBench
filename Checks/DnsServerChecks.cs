using KillerBench.Engine;

internal static class DnsServerChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string complete = "true", string rows = "[]") =>
            new(0, $"{{\"complete\":{complete},\"rows\":{rows}}}", "", false);
        var configured = Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":2,\"serverAddresses\":[\"192.0.2.53\",\"192.0.2.54\"]},{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":23,\"serverAddresses\":[\"2001:db8::53\"]}]");
        var configuredResult = DnsServerDiagnostics.Interpret(configured);
        Require(configuredResult.State == CheckState.Information && configuredResult.Repair is null && configuredResult.Evidence == configured &&
            configuredResult.Detail.Contains("3 distinct interface and server pairs", StringComparison.Ordinal),
            "Configured DNS server rows remain informational and non-mutating");
        var absent = DnsServerDiagnostics.Interpret(Evidence(rows: "[{\"interfaceIndex\":9,\"interfaceAlias\":\"Isolated\",\"addressFamily\":2,\"serverAddresses\":[]}]"));
        Require(absent.State == CheckState.Attention && absent.Repair is null && absent.Detail.Contains("No DNS server was reported", StringComparison.Ordinal),
            "Complete configuration without a reported DNS server requests correlation without selecting repair");
        Require(DnsServerDiagnostics.Interpret(Evidence()).State == CheckState.Attention,
            "A complete empty DNS server inventory cannot claim configured resolution");
        foreach (var invalid in new[]
        {
            configured with { ExitCode = 1 }, configured with { Error = "denied" }, configured with { TimedOut = true }, configured with { Truncated = true },
            Evidence("false"), Evidence(rows: "[{}]"),
            Evidence(rows: "[{\"interfaceIndex\":0,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":2,\"serverAddresses\":[]}]"),
            Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\" Ethernet\",\"addressFamily\":2,\"serverAddresses\":[]}]"),
            Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":4,\"serverAddresses\":[]}]"),
            Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":2,\"serverAddresses\":[\"2001:db8::53\"]}]"),
            Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":2,\"serverAddresses\":[]},{\"interfaceIndex\":7,\"interfaceAlias\":\"Renamed\",\"addressFamily\":2,\"serverAddresses\":[]}]")
        }) Require(DnsServerDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed, mismatched and duplicate DNS server evidence remains unknown");
        var capped = Evidence(rows: "[" + string.Join(",", Enumerable.Range(1, 513).Select(index =>
            $"{{\"interfaceIndex\":{index},\"interfaceAlias\":\"Adapter{index}\",\"addressFamily\":2,\"serverAddresses\":[]}}")) + "]");
        Require(DnsServerDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized DNS server evidence cannot claim complete coverage");
        Console.WriteLine("PASS: bounded per-interface DNS server evidence, absent configuration and incomplete evidence; no resolver changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
