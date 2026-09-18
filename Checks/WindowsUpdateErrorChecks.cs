using KillerBench.Engine;

internal static class WindowsUpdateErrorChecks
{
    internal static void Run()
    {
        Require(WindowsUpdateErrorCatalog.All.Count >= 80 && WindowsUpdateErrorCatalog.All.Select(entry => entry.Code).Distinct().Count() == WindowsUpdateErrorCatalog.All.Count,
            "Windows Update reference codes are substantial and unique");
        Require(WindowsUpdateErrorCatalog.SourceUrl.StartsWith("https://learn.microsoft.com/", StringComparison.Ordinal) &&
            WindowsUpdateErrorCatalog.Find(0x80240034u) is { Symbol: "WU_E_DOWNLOAD_FAILED" },
            "Unsigned direct lookup retains the documented source and exact HRESULT identity");
        foreach (var entry in WindowsUpdateErrorCatalog.All)
            Require(entry.Code != 0 && !string.IsNullOrWhiteSpace(entry.Symbol) && !string.IsNullOrWhiteSpace(entry.Meaning) && !string.IsNullOrWhiteSpace(entry.DiagnosticScope),
                "Every Windows Update reference has a code, symbol, meaning and diagnostic scope");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0x80240022)) is { Symbol: "WU_E_ALL_UPDATES_FAILED" } allFailed &&
            allFailed.DiagnosticScope.Contains("CBS", StringComparison.Ordinal), "All-updates-failed routes to individual and CBS evidence");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0x8024401B))?.DiagnosticScope.Contains("proxy", StringComparison.OrdinalIgnoreCase) == true,
            "Proxy authentication errors route to proxy diagnostics");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0x80240034)) is { Symbol: "WU_E_DOWNLOAD_FAILED" } download &&
            download.DiagnosticScope.Contains("BITS", StringComparison.Ordinal), "Download failures route to transfer and connectivity evidence");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0x800F081F))?.DiagnosticScope.Contains("component", StringComparison.OrdinalIgnoreCase) == true,
            "Missing servicing sources route to component evidence");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0x8024402C)) is { Symbol: "WU_E_PT_WINHTTP_NAME_NOT_RESOLVED" } dns &&
            dns.DiagnosticScope.Contains("DNS", StringComparison.Ordinal), "Name-resolution failures route to network and proxy evidence");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0x80246008))?.DiagnosticScope.Contains("BITS", StringComparison.Ordinal) == true &&
            WindowsUpdateErrorCatalog.Find(unchecked((int)0x8024D009))?.DiagnosticScope.Contains("policy", StringComparison.OrdinalIgnoreCase) == true,
            "BITS connection and Agent self-update failures have specific review scopes");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0x80248003)) is { Symbol: "WU_E_DS_TABLEMISSING" } tableMissing &&
            tableMissing.DiagnosticScope.Contains("do not reset", StringComparison.OrdinalIgnoreCase) &&
            WindowsUpdateErrorCatalog.Find(unchecked((int)0x80241003))?.DiagnosticScope.Contains("policy", StringComparison.OrdinalIgnoreCase) == true,
            "Data store and Windows Installer families retain conservative diagnostic routes");
        Require(WindowsUpdateErrorCatalog.Find(unchecked((int)0xDEADBEEF)) is null && WindowsUpdateErrorCatalog.Find(0) is null,
            "Unknown and zero HRESULTs remain raw evidence without invented references");
        Console.WriteLine("PASS: Windows Update error references map common HRESULTs to deterministic diagnostic scopes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
