using KillerBench.Engine;

internal static class NetworkFirewallCorrelationChecks
{
    internal static void Run()
    {
        const string networks = """
            {"profiles":[{"InterfaceAlias":"Ethernet","InterfaceIndex":7,"NetworkCategory":"DomainAuthenticated","IPv4Connectivity":"Internet","IPv6Connectivity":"LocalNetwork"},{"InterfaceAlias":"Wi-Fi","InterfaceIndex":11,"NetworkCategory":"Public","IPv4Connectivity":"Internet","IPv6Connectivity":"NoTraffic"}]}
            """;
        const string firewalls = """
            {"profiles":[{"Name":"Domain","Enabled":"True"},{"Name":"Private","Enabled":"True"},{"Name":"Public","Enabled":"True"}]}
            """;
        var networkChecks = NetworkProfileDiagnostics.Interpret(new(0, networks, "", false));
        var firewallChecks = FirewallProfiles.Interpret(new(0, firewalls, "", false));
        var aligned = NetworkFirewallCorrelation.Check(networkChecks, firewallChecks);
        Require(aligned.State == CheckState.Information && aligned.Detail.Contains("interface 7 uses DomainAuthenticated", StringComparison.Ordinal) &&
            aligned.Detail.Contains("interface 11 uses Public", StringComparison.Ordinal) && aligned.Repair is null && aligned.Evidence is not null,
            "Active network and firewall profiles did not correlate by exact category");

        var disabled = FirewallProfiles.Interpret(new(0, firewalls.Replace("\"Public\",\"Enabled\":\"True\"", "\"Public\",\"Enabled\":\"False\"", StringComparison.Ordinal), "", false));
        var exposed = NetworkFirewallCorrelation.Check(networkChecks, disabled);
        Require(exposed.State == CheckState.Attention && exposed.Detail.Contains("1 active network profile assignment", StringComparison.Ordinal) &&
            exposed.Detail.Contains("Public with its matching firewall profile disabled", StringComparison.Ordinal),
            "Disabled matching firewall profile did not request review");

        var empty = NetworkProfileDiagnostics.Interpret(new(0, "{\"profiles\":[]}", "", false));
        Require(NetworkFirewallCorrelation.Check(empty, firewallChecks).State == CheckState.Information,
            "Complete empty network profile evidence did not remain informational");
        var incompleteNetworks = NetworkProfileDiagnostics.Interpret(new(1, "", "denied", false));
        Require(NetworkFirewallCorrelation.Check(incompleteNetworks, firewallChecks).State == CheckState.Unknown,
            "Incomplete network profile evidence was correlated");
        var incompleteFirewalls = FirewallProfiles.Interpret(new(1, "", "denied", false));
        Require(NetworkFirewallCorrelation.Check(networkChecks, incompleteFirewalls).State == CheckState.Unknown,
            "Incomplete firewall profile evidence was correlated");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        try { NetworkFirewallCorrelation.Check(networkChecks, firewallChecks, stop.Token); throw new Exception("Canceled correlation continued"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: active network profiles correlate with matching firewall enablement without selecting a change.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
