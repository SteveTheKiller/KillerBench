using KillerBench.Engine;

internal static class WindowsSetupErrorChecks
{
    internal static void Run()
    {
        Require(WindowsSetupErrorCatalog.All.Count >= 45 &&
            WindowsSetupErrorCatalog.All.Select(entry => entry.Code).Distinct().Count() == WindowsSetupErrorCatalog.All.Count,
            "Windows Setup reference catalog is substantial and unique");
        Require(WindowsSetupErrorCatalog.SourceUrl.StartsWith("https://learn.microsoft.com/", StringComparison.Ordinal) &&
            WindowsSetupErrorCatalog.All.All(entry => entry.Code != 0 && entry.Symbol.StartsWith("MOSETUP_", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(entry.Meaning) && !string.IsNullOrWhiteSpace(entry.DiagnosticScope)),
            "Every Windows Setup reference retains source, symbol, meaning and diagnostic scope");
        Require(WindowsSetupErrorCatalog.Find(0xC1900208) is { Symbol: "MOSETUP_E_COMPAT_INSTALLREQ_BLOCK", RequiresReview: true } compatibility &&
            compatibility.DiagnosticScope.Contains("CompatData", StringComparison.Ordinal),
            "Compatibility blocks route to retained compatibility evidence");
        Require(WindowsSetupErrorCatalog.Find(0xC190020E)?.DiagnosticScope.Contains("capacity", StringComparison.OrdinalIgnoreCase) == true &&
            WindowsSetupErrorCatalog.Find(0xC1900101)?.DiagnosticScope.Contains("extend code", StringComparison.OrdinalIgnoreCase) == true,
            "Disk-space and rollback results retain specific review routes");
        Require(WindowsSetupErrorCatalog.Find(0xC1900210) is { RequiresReview: false } &&
            WindowsSetupErrorCatalog.Find(0xDEADBEEF) is null,
            "Compatibility scan-only completion is informational and unknown codes remain unresolved");
        Console.WriteLine("PASS: Windows Setup result references map documented modern Setup codes to deterministic diagnostic scopes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
