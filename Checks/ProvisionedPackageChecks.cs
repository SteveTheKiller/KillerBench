using KillerBench.Engine;

internal static class ProvisionedPackageChecks
{
    internal static void Run()
    {
        const string row = "{\"name\":\"Fixture\",\"packageName\":\"Fixture_1_neutral_test\",\"version\":\"1.0.0.0\",\"architecture\":\"neutral\"}";
        var raw = new ToolResult(0, "{\"complete\":true,\"packages\":[" + row + "]}", "", false);
        var result = ProvisionedPackages.Interpret(raw);
        var diagnostic = ProvisionedPackages.Describe(result);
        if (diagnostic.Count != 2 || diagnostic[0].Evidence != raw || diagnostic.Any(check => check.State != CheckState.Information || check.Repair is not null))
            throw new Exception("Provisioned diagnostic lost evidence or exposed repair.");
        var incomplete = ProvisionedPackages.Describe(result with { Evidence = raw with { TimedOut = true } });
        if (incomplete.Count != 1 || incomplete[0].State != CheckState.Unknown)
            throw new Exception("Modified provisioned summary bypassed failed evidence.");
        if (!result.Complete || result.Packages.Single().PackageName != "Fixture_1_neutral_test" || result.Evidence != raw)
            throw new Exception("Provisioned package identity or evidence lost.");
        foreach (var invalid in new[] { raw with { TimedOut = true }, raw with { Error = "Denied" }, raw with { Truncated = true },
            raw with { Output = "{}" }, raw with { Output = raw.Output.Replace("[" + row + "]", "[" + row + "," + row + "]") },
            raw with { Output = raw.Output.Replace("\"complete\":true", "\"complete\":true,\"complete\":false") },
            raw with { Output = raw.Output.Replace("\"architecture\":\"neutral\"", "\"architecture\":null") } })
            if (ProvisionedPackages.Interpret(invalid).Complete) throw new Exception("Invalid provisioned evidence claimed completeness.");
        if (!ProvisionedPackages.Interpret(raw with { Output = "{\"complete\":true,\"packages\":[]}" }).Complete)
            throw new Exception("Complete empty provisioned inventory rejected.");
        Console.WriteLine("PASS: provisioned-package identities, incomplete evidence and empty inventory; fixtures only.");
    }
}
