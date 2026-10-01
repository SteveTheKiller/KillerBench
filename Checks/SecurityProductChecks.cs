using KillerBench.Engine;

internal static class SecurityProductChecks
{
    internal static void Run()
    {
        static ToolResult Evidence(string json) => new(0, json, "", false);
        const string json = "{\"complete\":true,\"products\":[{\"name\":\"Fixture Security\",\"id\":\"{11111111-2222-3333-4444-555555555555}\",\"state\":397568,\"timestamp\":\"2026-09-14T12:00:00Z\"}]}";
        var inventory = SecurityProductDiagnostics.Interpret(Evidence(json));
        var checks = SecurityProductDiagnostics.Describe(inventory);
        Require(inventory.Complete && inventory.Products.Single().ProductState == 397568 && checks.Count == 2 &&
            checks.All(check => check.Repair is null) && checks[0].Evidence == inventory.Evidence &&
            checks[0].Detail.Contains("without interpreting protection health", StringComparison.Ordinal),
            "Security Center registrations retain raw state without invented health or repair eligibility");
        var empty = SecurityProductDiagnostics.Interpret(Evidence("{\"complete\":true,\"products\":[]}"));
        Require(empty.Complete && empty.Products.Count == 0 && SecurityProductDiagnostics.Describe(empty)[0].State == CheckState.Information,
            "Empty complete registration evidence remains informational rather than a product-health claim");
        foreach (var evidence in new[]
        {
            Evidence(json.Replace("11111111-2222-3333-4444-555555555555", "00000000-0000-0000-0000-000000000000")),
            Evidence(json.Replace("397568", "16777216")),
            Evidence(json.Replace("Fixture Security", "bad\u0001name")),
            Evidence("{\"complete\":true,\"complete\":false,\"products\":[]}"),
            Evidence("not json"),
            Evidence(json) with { ExitCode = 1 },
            Evidence(json) with { Error = "unavailable" },
            Evidence(json) with { TimedOut = true },
            Evidence(json) with { Truncated = true }
        })
        {
            var incomplete = SecurityProductDiagnostics.Interpret(evidence);
            Require(!incomplete.Complete && SecurityProductDiagnostics.Describe(incomplete)[0].State == CheckState.Unknown,
                "Malformed and incomplete Security Center evidence cannot claim product coverage");
        }
        Console.WriteLine("PASS: Security Center product identities, raw provider state, empty inventory and incomplete evidence; no security settings changed.");
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
