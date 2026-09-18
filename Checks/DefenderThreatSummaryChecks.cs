using KillerBench.Engine;

internal static class DefenderThreatSummaryChecks
{
    internal static void Run()
    {
        var evidence = new ToolResult(0, "{\"complete\":true,\"observed\":3,\"retained\":3,\"active\":1,\"executed\":1,\"capped\":false,\"severity\":[{\"id\":1,\"count\":1},{\"id\":5,\"count\":2}]}", "", false);
        var attention = DefenderThreatSummary.Interpret(evidence);
        Require(attention.State == CheckState.Attention && attention.Repair is null && attention.Evidence == evidence &&
            attention.Detail.Contains("1 currently active") && attention.Detail.Contains("Severity IDs: 1=1, 5=2") &&
            attention.Detail.Contains("Threat names, identifiers, paths, resources, accounts and process details were not collected") &&
            attention.Detail.Contains("No scan, quarantine, removal, exclusion, protection or signature action was selected"),
            "Aggregate threat history reports bounded counts and current active flags without sensitive details or actions");
        var empty = DefenderThreatSummary.Interpret(evidence with { Output = "{\"complete\":true,\"observed\":0,\"retained\":0,\"active\":0,\"executed\":0,\"capped\":false,\"severity\":[]}" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("does not prove remediation"),
            "An empty complete inventory remains informational");
        var capped = DefenderThreatSummary.Interpret(evidence with { Output = "{\"complete\":true,\"observed\":1001,\"retained\":1000,\"active\":0,\"executed\":0,\"capped\":true,\"severity\":[{\"id\":2,\"count\":1000}]}" });
        Require(capped.State == CheckState.Unknown && capped.Detail.Contains("coverage is incomplete"), "A capped inventory cannot establish complete current state");
        foreach (string output in new[]
        {
            "{}", "not JSON", "{\"complete\":false,\"observed\":0,\"retained\":0,\"active\":0,\"executed\":0,\"capped\":false,\"severity\":[]}",
            "{\"complete\":true,\"observed\":1,\"retained\":1,\"active\":2,\"executed\":0,\"capped\":false,\"severity\":[{\"id\":1,\"count\":1}]}",
            "{\"complete\":true,\"observed\":1,\"retained\":1,\"active\":0,\"executed\":0,\"capped\":false,\"severity\":[{\"id\":1,\"count\":1},{\"id\":1,\"count\":1}]}",
            "{\"complete\":true,\"observed\":0,\"retained\":0,\"active\":0,\"executed\":0,\"capped\":false,\"severity\":[],\"observed\":0}"
        }) Require(DefenderThreatSummary.Interpret(evidence with { Output = output }).State == CheckState.Unknown, "Malformed or inconsistent threat evidence fails closed");
        foreach (var bad in new[] { evidence with { ExitCode = 1 }, evidence with { Error = "provider error" }, evidence with { Truncated = true }, evidence with { TimedOut = true } })
            Require(DefenderThreatSummary.Interpret(bad).State == CheckState.Unknown, "Incomplete native threat evidence fails closed");
        Require(DefenderThreatSummary.QueryStep().Arguments.Last().Contains("Select-Object -First 1001") &&
            !DefenderThreatSummary.QueryStep().Arguments.Last().Contains("ThreatName") && !DefenderThreatSummary.QueryStep().Arguments.Last().Contains("Resources"),
            "Native query is bounded and excludes sensitive threat fields");
        Console.WriteLine("PASS: Defender threat history counts, current active flags, privacy boundary, incomplete evidence and no security action.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
