using KillerBench.Engine;

internal static class WindowsInstallerErrorChecks
{
    internal static void Run()
    {
        Require(WindowsInstallerErrorCatalog.All.Count >= 45 &&
            WindowsInstallerErrorCatalog.All.Select(entry => entry.Code).Distinct().Count() == WindowsInstallerErrorCatalog.All.Count,
            "Installer catalog is substantial and code identities are unique");
        Require(WindowsInstallerErrorCatalog.All.All(entry => entry.Code >= 0 &&
            !string.IsNullOrWhiteSpace(entry.Symbol) && !string.IsNullOrWhiteSpace(entry.Summary) &&
            !string.IsNullOrWhiteSpace(entry.DiagnosticScope)), "Installer references are complete");
        Require(WindowsInstallerErrorCatalog.Find(1601) is { Symbol: "ERROR_INSTALL_SERVICE_FAILURE", Success: false } service &&
            service.DiagnosticScope.Contains("registration", StringComparison.OrdinalIgnoreCase),
            "Service failure routes to current service and registration evidence");
        var source = WindowsInstallerErrorCatalog.Find(1612) ??
            throw new Exception("Missing source reference is absent");
        Require(source.Symbol == "ERROR_INSTALL_SOURCE_ABSENT" &&
            source.DiagnosticScope.Contains("source", StringComparison.OrdinalIgnoreCase),
            "Missing source routes to source evidence");
        Require(WindowsInstallerErrorCatalog.Find(1618)?.DiagnosticScope.Contains("activity", StringComparison.OrdinalIgnoreCase) == true,
            "Concurrent installation routes to current activity evidence");
        Require(WindowsInstallerErrorCatalog.Find(1641) is { Success: true, RestartRequired: true } &&
            WindowsInstallerErrorCatalog.Find(3010) is { Success: true, RestartRequired: true },
            "Installer restart-success outcomes remain success with explicit restart state");
        Require(WindowsInstallerErrorCatalog.Find(1603) is { Success: false } fatal &&
            fatal.DiagnosticScope.Contains("first", StringComparison.OrdinalIgnoreCase),
            "Generic fatal failure directs review to earlier detailed evidence");
        Require(WindowsInstallerErrorCatalog.Find(9999) is null,
            "Unknown Installer codes remain unknown without guessed guidance");
        var analysis = Investigation.Analyze(new TextEvidence("fixture.log",
            ["MSI (s) (EC:BC) [12:34:56:123]: MainEngineThread is returning 1612"], false));
        Require(analysis.Findings is [{ RuleId: "KB-MSI-1612" } finding] &&
            finding.Title == source.Symbol && finding.Explanation == source.Summary &&
            finding.NextStep.Contains(source.DiagnosticScope, StringComparison.Ordinal),
            "Installer log analysis uses the shared return-code catalog");
        Console.WriteLine("PASS: reusable Windows Installer return-code identities, success and restart states, conservative guidance and unknown-code handling.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
