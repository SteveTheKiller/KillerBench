using KillerBench.Engine;

internal static class MsiProductChecks
{
    private const string Product = "{11111111-2222-3333-4444-555555555555}";

    internal static void Run()
    {
        Require(MsiProductDiagnostics.NormalizeProductCode(Product.ToLowerInvariant()) == Product,
            "MSI product code normalization retains the exact braced identity");
        Require(new WindowsWorkflowHost().MsiProductCode is null &&
            new WindowsWorkflowHost { MsiProductCode = Product.ToLowerInvariant() }.MsiProductCode == Product,
            "Installer workflow keeps exact product inspection optional and normalized");
        var inventory = new InstalledApplicationInventory([
            new("Registry64", Product.ToLowerInvariant(), "Fixture App", "2.0", "Fixture"),
            new("Registry32", Product, "Duplicate Fixture App", "1.0", "Fixture"),
            new("Registry64", "not-an-msi-code", "Ordinary registration", null, null)
        ], true, new(0, "{}", "", false));
        var candidates = MsiProductDiagnostics.SelectCandidates(inventory);
        Require(candidates.Count == 1 && candidates[0].ProductCode == Product && candidates[0].Name == "Fixture App" &&
            candidates[0].View == "Registry64" && candidates[0].ToString().Contains("2.0", StringComparison.Ordinal),
            "MSI picker accepts exact product-code registrations, normalizes identity and prefers the 64-bit duplicate registry view");
        foreach (string invalid in new[] { "", "11111111-2222-3333-4444-555555555555", "{11111111-2222-3333-4444-55555555555Z}", Product + " extra" })
        {
            bool rejected = false;
            try { MsiProductDiagnostics.NormalizeProductCode(invalid); }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "Invalid MSI product code is rejected before collection");
        }
        foreach (int state in Enumerable.Range(-7, 13))
        {
            var check = MsiProductDiagnostics.Interpret(Product, Result(state));
            CheckState expected = state is -6 or -5 or -4 or 0 ? CheckState.Attention : state is -3 or -2 ? CheckState.Unknown : CheckState.Information;
            Require(check.State == expected && check.Repair is null && check.Evidence is not null && check.Detail.Contains($"({state})"),
                "Every documented Installer product state remains read-only and explicitly classified");
        }
        foreach (string invalid in new[]
        {
            "{}", "{\"productCode\":\"" + Product + "\",\"state\":6}",
            "{\"productCode\":\"{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}\",\"state\":3}",
            "{\"productCode\":\"" + Product + "\",\"state\":3,\"state\":2}"
        })
            Require(MsiProductDiagnostics.Interpret(Product, new(0, invalid, "", false)).State == CheckState.Unknown,
                "Malformed, mismatched or duplicate MSI product evidence remains unknown");
        foreach (var failure in new[] { Result(3) with { ExitCode = 5 }, Result(3) with { TimedOut = true }, new ToolResult(0, "{}", "provider error", false), new ToolResult(0, "{}", "", true) })
            Require(MsiProductDiagnostics.Interpret(Product, failure).State == CheckState.Unknown, "Failed MSI product collection cannot pass");
        string command = string.Join(" ", MsiProductDiagnostics.Step(Product).Arguments);
        Require(command.Contains(Product, StringComparison.Ordinal) && command.Contains("ProductState", StringComparison.Ordinal) &&
            !command.Contains("InstallProduct", StringComparison.OrdinalIgnoreCase) && !command.Contains("Reinstall", StringComparison.OrdinalIgnoreCase),
            "MSI product query uses the exact read-only ProductState API");
        Console.WriteLine("PASS: exact MSI product identity, all product states, malformed evidence and read-only query planning.");
    }

    private static ToolResult Result(int state) => new(0, "{\"productCode\":\"" + Product + "\",\"state\":" + state + "}", "", false);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
