using KillerBench.Engine;

internal static class DeviceProblemCodeChecks
{
    public static void Run()
    {
        Require(DeviceProblemCodeCatalog.All.Count == 42 && DeviceProblemCodeCatalog.All.Select(entry => entry.Code).Distinct().Count() == 42,
            "Device problem catalog retains every currently documented non-reserved code plus clear state");
        Require(DeviceProblemCodeCatalog.Find(28) is { Symbol: "CM_PROB_FAILED_INSTALL" } missing &&
            missing.Summary.Contains("not installed", StringComparison.Ordinal) && missing.NextAction.Contains("approved vendor", StringComparison.Ordinal),
            "Missing-driver guidance retains the formal identity and bounded next action");
        Require(DeviceProblemCodeCatalog.Find(14).RestartMayResolve && DeviceProblemCodeCatalog.Find(56).RestartMayResolve &&
            !DeviceProblemCodeCatalog.Find(43).RestartMayResolve,
            "Restart metadata is limited to codes whose state can be transitional");
        var unknown = DeviceProblemCodeCatalog.Find(999);
        Require(unknown.Code == 999 && unknown.Symbol == "CM_PROB_UNKNOWN" && unknown.NextAction.Contains("numeric code", StringComparison.Ordinal),
            "Unknown future codes remain visible without invented remediation");
        Require(DeviceProblemCodeCatalog.TryParse("28", out uint decimalCode) && decimalCode == 28 &&
            DeviceProblemCodeCatalog.TryParse("0x1C", out uint hexCode) && hexCode == 28 &&
            !DeviceProblemCodeCatalog.TryParse("-1", out _) && !DeviceProblemCodeCatalog.TryParse(" 28", out _) &&
            !DeviceProblemCodeCatalog.TryParse("0x", out _) && !DeviceProblemCodeCatalog.TryParse("99999999999", out _),
            "CLI code parsing accepts bounded unsigned decimal and hexadecimal identities only");
        Console.WriteLine("PASS: reusable Device Manager problem-code identities and conservative guidance cover documented codes and unknown future values.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
