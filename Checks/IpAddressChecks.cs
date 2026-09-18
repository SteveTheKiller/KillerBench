using KillerBench.Engine;

internal static class IpAddressChecks
{
    public static void Run()
    {
        ToolResult Evidence(string rows, bool complete = true) => new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"addresses\":[" + rows + "]}", "", false);
        const string preferred = "{\"interfaceIndex\":7,\"interfaceAlias\":\"Ethernet\",\"addressFamily\":\"IPv4\",\"address\":\"192.0.2.10\",\"prefixLength\":24,\"prefixOrigin\":\"Dhcp\",\"suffixOrigin\":\"OriginDhcp\",\"addressState\":\"Preferred\",\"skipAsSource\":false,\"connectionState\":\"Connected\"}";
        var clear = IpAddressDiagnostics.Interpret(Evidence(preferred));
        Require(clear.Count == 1 && clear[0].State == CheckState.Information && clear[0].Repair is null && clear[0].Detail.Contains("192.0.2.10/24"),
            "Preferred address retains exact read-only configuration");
        var apipa = IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("192.0.2.10", "169.254.20.30")));
        Require(apipa.Single().State == CheckState.Attention && apipa.Single().Detail.Contains("automatic private range"), "APIPA requests contextual review");
        var disconnectedApipa = IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("192.0.2.10", "169.254.20.30").Replace("Connected", "Disconnected")));
        Require(disconnectedApipa.Single().State == CheckState.Information && disconnectedApipa.Single().Repair is null &&
            disconnectedApipa.Single().Detail.Contains("does not establish a current connectivity problem"), "Disconnected APIPA remains context");
        var unknownApipa = IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("192.0.2.10", "169.254.20.30").Replace("Connected", "Unknown")));
        Require(unknownApipa.Single().State == CheckState.Attention, "Unknown interface state does not hide APIPA");
        foreach (string state in new[] { "Deprecated", "Tentative", "Duplicate", "Invalid" })
        {
            var result = IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("Preferred", state)));
            Require(result.Single().State == CheckState.Attention && result.Single().Detail.Contains("does not report this address as Preferred"), state + " requests review");
            var disconnected = IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("Preferred", state).Replace("Connected", "Disconnected")));
            Require(disconnected.Single().State == CheckState.Information && disconnected.Single().Repair is null, state + " on a disconnected interface remains context");
        }
        var duplicated = IpAddressDiagnostics.Interpret(Evidence(preferred + "," + preferred.Replace("\"interfaceIndex\":7", "\"interfaceIndex\":8").Replace("Ethernet", "Wi-Fi")));
        Require(duplicated.Count == 2 && duplicated.All(check => check.State == CheckState.Attention && check.Detail.Contains("more than one interface")),
            "A repeated non-link-local address across interfaces requests review");
        var linkLocalV6 = preferred.Replace("\"addressFamily\":\"IPv4\"", "\"addressFamily\":\"IPv6\"")
            .Replace("\"address\":\"192.0.2.10\"", "\"address\":\"fe80::1\"").Replace("\"prefixLength\":24", "\"prefixLength\":64");
        var repeatedLinkLocal = IpAddressDiagnostics.Interpret(Evidence(linkLocalV6 + "," + linkLocalV6.Replace("\"interfaceIndex\":7", "\"interfaceIndex\":8")));
        Require(repeatedLinkLocal.All(check => check.State == CheckState.Information), "Scoped IPv6 link-local addresses are not treated as conflicts");
        Require(IpAddressDiagnostics.Interpret(Evidence("")).Single().State == CheckState.Attention, "Empty complete inventory requests review");
        foreach (var invalid in new[]
        {
            IpAddressDiagnostics.Interpret(Evidence(preferred, false)),
            IpAddressDiagnostics.Interpret(Evidence(preferred + "," + preferred)),
            IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("\"prefixLength\":24", "\"prefixLength\":99"))),
            IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("\"addressFamily\":\"IPv4\"", "\"addressFamily\":\"IPv6\""))),
            IpAddressDiagnostics.Interpret(Evidence(preferred.Replace("Connected", "Dormant"))),
            IpAddressDiagnostics.Interpret(new ToolResult(1, "", "denied", false))
        }) Require(invalid.Single().State == CheckState.Unknown && invalid.Single().Repair is null, "Invalid or incomplete address evidence remains unknown");
        Console.WriteLine("PASS: bounded IP address inventory, APIPA, address states, conflicts and incomplete evidence; no network changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
