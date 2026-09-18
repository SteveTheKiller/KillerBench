using KillerBench.Engine;

internal static class MsiSourceChecks
{
    public static void Run()
    {
        ToolResult Evidence(string products, bool complete = true) => new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"products\":[" + products + "]}", "", false);
        const string product = "{\"productKey\":\"01B76DD8676DFCC41B141EA7B6310561\",\"name\":\"Fixture App\",\"packageConfigured\":true,\"packageFile\":\"fixture.msi\",\"lastUsedType\":\"n\",\"networkSources\":2,\"urlSources\":0,\"mediaSources\":1,\"complete\":true,\"issue\":null}";
        var healthy = MsiSourceDiagnostics.Interpret(Evidence(product)).Single();
        Require(healthy.State == CheckState.Information && healthy.Repair is null && healthy.Detail.Contains("source reachability was intentionally not tested", StringComparison.Ordinal),
            "Complete source metadata remains informational without claiming reachability");
        var missingPackage = MsiSourceDiagnostics.Interpret(Evidence(product.Replace("\"packageConfigured\":true,\"packageFile\":\"fixture.msi\"", "\"packageConfigured\":false,\"packageFile\":null"))).Single();
        Require(missingPackage.State == CheckState.Attention && missingPackage.Repair is null && missingPackage.Detail.Contains("missing the registered package filename", StringComparison.Ordinal),
            "Missing package metadata requests review without selecting repair");
        var noSources = MsiSourceDiagnostics.Interpret(Evidence(product.Replace("\"lastUsedType\":\"n\",\"networkSources\":2,\"urlSources\":0,\"mediaSources\":1", "\"lastUsedType\":\"\",\"networkSources\":0,\"urlSources\":0,\"mediaSources\":0"))).Single();
        Require(noSources.State == CheckState.Attention && noSources.Detail.Contains("no registered source category", StringComparison.Ordinal),
            "Missing source categories request review without probing a source");
        var unavailable = MsiSourceDiagnostics.Interpret(Evidence(product.Replace("\"name\":\"Fixture App\",\"packageConfigured\":true,\"packageFile\":\"fixture.msi\",\"lastUsedType\":\"n\",\"networkSources\":2,\"urlSources\":0,\"mediaSources\":1,\"complete\":true,\"issue\":null",
            "\"name\":\"\",\"packageConfigured\":false,\"packageFile\":null,\"lastUsedType\":\"\",\"networkSources\":0,\"urlSources\":0,\"mediaSources\":0,\"complete\":false,\"issue\":\"SecurityException\""))).Single();
        Require(unavailable.State == CheckState.Unknown && unavailable.Repair is null, "Unavailable product source metadata remains unknown");
        Require(MsiSourceDiagnostics.Interpret(Evidence("")).Single().State == CheckState.Information, "An empty complete inventory remains informational");
        foreach (var invalid in new[]
        {
            MsiSourceDiagnostics.Interpret(Evidence(product, false)),
            MsiSourceDiagnostics.Interpret(Evidence(product + "," + product)),
            MsiSourceDiagnostics.Interpret(Evidence(product.Replace("fixture.msi", "C:\\\\source\\\\fixture.msi"))),
            MsiSourceDiagnostics.Interpret(Evidence(product.Replace("\"lastUsedType\":\"n\"", "\"lastUsedType\":\"x\""))),
            MsiSourceDiagnostics.Interpret(Evidence(product.Replace("\"networkSources\":2", "\"networkSources\":257"))),
            MsiSourceDiagnostics.Interpret(new ToolResult(1, "", "denied", false))
        }) Require(invalid.Single().State == CheckState.Unknown && invalid.Single().Repair is null,
            "Malformed, duplicate, excessive or failed source evidence remains unknown");
        Require(!healthy.Detail.Contains("\\\\", StringComparison.Ordinal) && !healthy.Detail.Contains("http", StringComparison.OrdinalIgnoreCase),
            "Reported source summaries exclude paths and URLs");
        Console.WriteLine("PASS: bounded Windows Installer source metadata, missing registration evidence and privacy boundaries; no source changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
