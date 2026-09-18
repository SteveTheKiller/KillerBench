using KillerBench.Engine;

internal static class NetworkProfileChecks
{
    public static void Run()
    {
        var evidence = new ToolResult(0,
            "{\"profiles\":[{\"InterfaceAlias\":\"Ethernet\",\"InterfaceIndex\":4,\"NetworkCategory\":\"Private\",\"IPv4Connectivity\":\"Internet\",\"IPv6Connectivity\":\"NoTraffic\"},{\"InterfaceAlias\":\"Wi-Fi\",\"InterfaceIndex\":7,\"NetworkCategory\":\"Public\",\"IPv4Connectivity\":\"LocalNetwork\",\"IPv6Connectivity\":\"Disconnected\"}]}",
            "", false);
        var checks = NetworkProfileDiagnostics.Interpret(evidence);
        Require(checks.Count == 2 && checks[0].State == CheckState.Information && checks[1].State == CheckState.Attention,
            "Private and Public discovery contexts remain distinct");
        Require(checks.All(check => check.Evidence == evidence && check.Detail.Contains("No network settings are changed")),
            "Profile findings retain evidence and preservation scope");
        Require(checks[1].Detail.Contains("Review the network's trust") && !checks[1].Detail.Contains("change it to Private"),
            "Public profiles prompt trust review without prescribing a profile change");

        var empty = NetworkProfileDiagnostics.Interpret(new(0, "{\"profiles\":[]}", "", false)).Single();
        Require(empty.State == CheckState.Information && empty.Detail.Contains("no active network", StringComparison.OrdinalIgnoreCase),
            "No current profile is explicit discovery context");

        string duplicate = evidence.Output.Replace("\"InterfaceIndex\":7", "\"InterfaceIndex\":4");
        Require(NetworkProfileDiagnostics.Interpret(evidence with { Output = duplicate }).Single().State == CheckState.Unknown,
            "Duplicate interface identity cannot pass validation");
        foreach (string invalid in new[]
        {
            "{\"profiles\":[{\"InterfaceAlias\":\"Ethernet\",\"InterfaceIndex\":4,\"NetworkCategory\":\"Unknown\",\"IPv4Connectivity\":\"Internet\",\"IPv6Connectivity\":\"NoTraffic\"}]}",
            "{\"profiles\":[{\"InterfaceAlias\":\"bad\\nname\",\"InterfaceIndex\":4,\"NetworkCategory\":\"Private\",\"IPv4Connectivity\":\"Internet\",\"IPv6Connectivity\":\"NoTraffic\"}]}",
            "{\"profiles\":{}}",
            "{}",
            "not json"
        })
            Require(NetworkProfileDiagnostics.Interpret(new(0, invalid, "", false)).Single().State == CheckState.Unknown,
                "Malformed profile evidence remains unknown");

        string records = string.Join(",", Enumerable.Range(1, 33).Select(index =>
            $"{{\"InterfaceAlias\":\"Interface {index}\",\"InterfaceIndex\":{index},\"NetworkCategory\":\"Private\",\"IPv4Connectivity\":\"Internet\",\"IPv6Connectivity\":\"NoTraffic\"}}"));
        Require(NetworkProfileDiagnostics.Interpret(new(0, "{\"profiles\":[" + records + "]}", "", false)).Single().State == CheckState.Unknown,
            "Capped profile coverage cannot pass as complete");
        foreach (var incomplete in new[]
        {
            evidence with { ExitCode = 1 }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Error = "failure" }
        })
            Require(NetworkProfileDiagnostics.Interpret(incomplete).Single().State == CheckState.Unknown,
                "Incomplete collection remains unknown");

        Console.WriteLine("PASS: network profile discovery context, strict evidence validation and bounded coverage.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
