using KillerBench.Engine;

internal static class DnsClientConfigurationChecks
{
    public static void Run()
    {
        ToolResult Evidence(string complete = "true", string suffixes = "[]", string devolution = "true", string level = "2", string policy = "[]", string rows = "[]") =>
            new(0, $"{{\"complete\":{complete},\"suffixes\":{suffixes},\"useDevolution\":{devolution},\"devolutionLevel\":{level},\"policyValues\":{policy},\"rows\":{rows}}}", "", false);
        var populated = Evidence("true", "[\"corp.example\"]", "true", "2", "[\"SearchList\"]",
            "[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"connectionSuffix\":\"branch.example\",\"registerAddress\":true,\"useSuffixWhenRegistering\":true},{\"interfaceIndex\":12,\"interfaceAlias\":\"VPN\",\"connectionSuffix\":\"\",\"registerAddress\":false,\"useSuffixWhenRegistering\":false}]");
        var result = DnsClientConfigurationDiagnostics.Interpret(populated);
        Require(result.State == CheckState.Information && result.Repair is null && result.Evidence == populated &&
            result.Detail.Contains("2 DNS client interfaces", StringComparison.Ordinal) &&
            result.Detail.Contains("managed settings are preserved", StringComparison.Ordinal) &&
            !result.Detail.Contains("corp.example", StringComparison.Ordinal),
            "DNS client configuration summarizes suffix and registration state without exposing names or selecting changes");
        Require(DnsClientConfigurationDiagnostics.Interpret(Evidence()).State == CheckState.Information,
            "Empty complete DNS client configuration remains informational");
        foreach (var invalid in new[]
        {
            populated with { ExitCode = 1 }, populated with { Error = "denied" }, populated with { TimedOut = true }, populated with { Truncated = true },
            Evidence("false"), Evidence(suffixes: "[\" corp.example\"]"), Evidence(suffixes: "[\"corp.example\",\"CORP.EXAMPLE\"]"),
            Evidence(level: "-1"), Evidence(policy: "[\"Other\"]"), Evidence(policy: "[\"SearchList\",\"SearchList\"]"),
            Evidence(rows: "[{}]"), Evidence(rows: "[{\"interfaceIndex\":0,\"interfaceAlias\":\"Ethernet\",\"connectionSuffix\":\"\",\"registerAddress\":true,\"useSuffixWhenRegistering\":false}]"),
            Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\" Ethernet\",\"connectionSuffix\":\"\",\"registerAddress\":true,\"useSuffixWhenRegistering\":false}]"),
            Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"connectionSuffix\":\"bad\\nvalue\",\"registerAddress\":true,\"useSuffixWhenRegistering\":false}]"),
            Evidence(rows: "[{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"connectionSuffix\":\"\",\"registerAddress\":true,\"useSuffixWhenRegistering\":false},{\"interfaceIndex\":7,\"interfaceAlias\":\"Renamed\",\"connectionSuffix\":\"\",\"registerAddress\":false,\"useSuffixWhenRegistering\":false}]")
        }) Require(DnsClientConfigurationDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed and duplicate DNS client evidence remains unknown");
        var capped = Evidence(rows: "[" + string.Join(",", Enumerable.Range(1, 513).Select(index =>
            $"{{\"interfaceIndex\":{index},\"interfaceAlias\":\"Adapter{index}\",\"connectionSuffix\":\"\",\"registerAddress\":false,\"useSuffixWhenRegistering\":false}}")) + "]");
        Require(DnsClientConfigurationDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized DNS client evidence cannot claim complete coverage");
        Console.WriteLine("PASS: bounded DNS suffix, registration, devolution and policy evidence remains read only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
