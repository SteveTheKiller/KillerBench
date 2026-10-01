using KillerBench.Engine;

internal static class MsiCacheChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string products, bool complete = true) => new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"products\":[" + products + "]}", "", false);
        const string product = "{\"productCode\":\"{11111111-2222-3333-4444-555555555555}\",\"name\":\"Fixture App\",\"context\":\"Machine\",\"complete\":true,\"packageConfigured\":true,\"packageFile\":\"abc123.msi\",\"packageVisible\":true,\"issue\":null}";
        var visible = MsiCacheDiagnostics.Interpret(Evidence(product));
        Require(visible.Single().State == CheckState.Information && visible.Single().Repair is null && visible.Single().Detail.Contains("abc123.msi"),
            "Visible exact cache registration remains informational");
        var missing = MsiCacheDiagnostics.Interpret(Evidence(product.Replace("\"packageVisible\":true", "\"packageVisible\":false")));
        Require(missing.Single().State == CheckState.Attention && missing.Single().Detail.Contains("not visible"), "Missing cached package requests source review");
        var unconfigured = MsiCacheDiagnostics.Interpret(Evidence(product.Replace("\"packageConfigured\":true,\"packageFile\":\"abc123.msi\",\"packageVisible\":true", "\"packageConfigured\":false,\"packageFile\":null,\"packageVisible\":null")));
        Require(unconfigured.Single().State == CheckState.Attention && unconfigured.Single().Detail.Contains("without a cached LocalPackage"),
            "Missing LocalPackage registration requests source review");
        var partial = MsiCacheDiagnostics.Interpret(Evidence(product.Replace("\"name\":\"Fixture App\",\"context\":\"Machine\",\"complete\":true,\"packageConfigured\":true,\"packageFile\":\"abc123.msi\",\"packageVisible\":true,\"issue\":null",
            "\"name\":\"\",\"context\":\"Machine\",\"complete\":false,\"packageConfigured\":false,\"packageFile\":null,\"packageVisible\":null,\"issue\":\"COMException\"")));
        Require(partial.Single().State == CheckState.Unknown && partial.Single().Detail.Contains("COMException"), "Unavailable per-product properties remain unknown");
        Require(MsiCacheDiagnostics.Interpret(Evidence("")).Single().State == CheckState.Information, "Empty complete inventory remains informational");
        foreach (var invalid in new[]
        {
            MsiCacheDiagnostics.Interpret(Evidence(product, false)),
            MsiCacheDiagnostics.Interpret(Evidence(product + "," + product)),
            MsiCacheDiagnostics.Interpret(Evidence(product.Replace("abc123.msi", "C:\\\\Windows\\\\Installer\\\\abc123.msi"))),
            MsiCacheDiagnostics.Interpret(Evidence(product.Replace("\"context\":\"Machine\"", "\"context\":\"Future\""))),
            MsiCacheDiagnostics.Interpret(new ToolResult(1, "", "denied", false))
        }) Require(invalid.Single().State == CheckState.Unknown && invalid.Single().Repair is null, "Malformed, capped or failed cache evidence remains unknown");
        Console.WriteLine("PASS: bounded Windows Installer cache visibility, missing source evidence and privacy boundaries; no package changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
