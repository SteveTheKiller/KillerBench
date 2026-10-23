using System.IO;
using KillerBench.Engine;

internal static class FirewallRuleSummaryChecks
{
    public static void Run()
    {
        var complete = FirewallRuleSummaryDiagnostics.Interpret(Result(
            "{\"complete\":true,\"total\":120,\"enabled\":80,\"disabled\":40,\"inAllow\":30,\"inBlock\":10,\"outAllow\":35,\"outBlock\":5,\"local\":50,\"groupPolicy\":40,\"dynamic\":10,\"system\":20,\"other\":0}"));
        Require(complete.State == CheckState.Information && complete.Detail.Contains("120 total") &&
            complete.Detail.Contains("Group Policy 40") && complete.Repair is null,
            "Complete aggregate remains informational and read only");

        var future = FirewallRuleSummaryDiagnostics.Interpret(Result(
            "{\"complete\":true,\"total\":4,\"enabled\":3,\"disabled\":1,\"inAllow\":1,\"inBlock\":1,\"outAllow\":1,\"outBlock\":0,\"local\":1,\"groupPolicy\":1,\"dynamic\":0,\"system\":1,\"other\":1}"));
        Require(future.State == CheckState.Attention && future.Detail.Contains("unrecognized policy source"),
            "Future policy source requests review without inventing ownership");

        foreach (string output in new[]
        {
            "{\"complete\":true,\"total\":3,\"enabled\":3,\"disabled\":1,\"inAllow\":1,\"inBlock\":1,\"outAllow\":1,\"outBlock\":0,\"local\":1,\"groupPolicy\":1,\"dynamic\":0,\"system\":1,\"other\":0}",
            "{\"complete\":true,\"total\":3,\"enabled\":3,\"disabled\":0,\"inAllow\":1,\"inBlock\":1,\"outAllow\":0,\"outBlock\":0,\"local\":1,\"groupPolicy\":1,\"dynamic\":0,\"system\":1,\"other\":0}",
            "{\"complete\":true,\"total\":3,\"enabled\":3,\"disabled\":0,\"inAllow\":1,\"inBlock\":1,\"outAllow\":1,\"outBlock\":0,\"local\":1,\"groupPolicy\":1,\"dynamic\":0,\"system\":0,\"other\":0}",
            "{\"complete\":true,\"total\":-1,\"enabled\":0,\"disabled\":0,\"inAllow\":0,\"inBlock\":0,\"outAllow\":0,\"outBlock\":0,\"local\":0,\"groupPolicy\":0,\"dynamic\":0,\"system\":0,\"other\":0}",
            "{\"complete\":false,\"total\":0,\"enabled\":0,\"disabled\":0,\"inAllow\":0,\"inBlock\":0,\"outAllow\":0,\"outBlock\":0,\"local\":0,\"groupPolicy\":0,\"dynamic\":0,\"system\":0,\"other\":0}",
            "{\"complete\":true,\"total\":0,\"enabled\":0,\"disabled\":0,\"inAllow\":0,\"inBlock\":0,\"outAllow\":0,\"outBlock\":0,\"local\":0,\"groupPolicy\":0,\"dynamic\":0,\"system\":0,\"other\":0,\"names\":[\"private rule\"]}",
            "not-json"
        })
            Require(FirewallRuleSummaryDiagnostics.Interpret(Result(output)).State == CheckState.Unknown,
                "Malformed or privacy-expanded evidence fails closed");

        foreach (var evidence in new[]
        {
            new ToolResult(1, "", "denied", false),
            new ToolResult(0, "{}", "", true),
            new ToolResult(0, "{}", "", false) { TimedOut = true }
        })
            Require(FirewallRuleSummaryDiagnostics.Interpret(evidence).State == CheckState.Unknown,
                "Failed, truncated or timed-out collection remains unknown");

        string source = File.ReadAllText(CheckSource.Find(Path.Combine("Engine", "Network", "FirewallRuleSummaryDiagnostics.cs")));
        foreach (string excluded in new[] { "DisplayName", "DisplayGroup", "Program", "LocalPort", "RemotePort", "LocalAddress", "RemoteAddress", "Owner", "Package" })
            Require(!source.Contains("Select-Object " + excluded, StringComparison.Ordinal), "Native query excludes " + excluded);
        Require(!source.Contains("Set-NetFirewall", StringComparison.OrdinalIgnoreCase) &&
            !source.Contains("New-NetFirewall", StringComparison.OrdinalIgnoreCase) &&
            !source.Contains("Remove-NetFirewall", StringComparison.OrdinalIgnoreCase),
            "Native query contains no firewall mutation command");

        Console.WriteLine("PASS: Windows Firewall rule aggregates, policy sources, consistency, privacy and failures remain bounded and read only.");
    }

    private static ToolResult Result(string output) => new(0, output, "", false);
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
