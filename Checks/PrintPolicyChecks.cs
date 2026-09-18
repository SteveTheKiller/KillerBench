using System.IO;
using KillerBench.Engine;

internal static class PrintPolicyChecks
{
    internal static void Run()
    {
        var absent = PrintPolicyDiagnostics.Assess(new(Empty(), Empty(), null));
        Require(absent is { State: CheckState.Information, Repair: null, Evidence: not null } &&
            absent.Detail.Contains("No policy, registry value, queue, driver, connection, credential or print setting changed"),
            "Absent print policy remains descriptive and non-mutating");
        Require(!absent.Evidence!.Output.Contains("server.example", StringComparison.OrdinalIgnoreCase),
            "Print policy evidence contains no server identities");

        var secured = PrintPolicyDiagnostics.Assess(new(
            Empty() with { Restricted = 1, TrustedServers = 1, TrustedServerCount = 2, InForest = 1,
                PackagePointAndPrintOnly = 1, PackagePointAndPrintServerList = 1, ApprovedPackageServerCount = 2 },
            Empty(), 1));
        Require(secured.State == CheckState.Information && secured.Detail.Contains("requires an administrator") &&
            secured.Detail.Contains("2 entries") && secured.Repair is null,
            "Recognized restrictions explain blocked installation without weakening policy");

        foreach (var exposed in new[]
        {
            new PrintPolicyEnvironment(Empty(), Empty(), 0),
            new PrintPolicyEnvironment(Empty() with { NoWarningNoElevationOnInstall = 1 }, Empty(), null),
            new PrintPolicyEnvironment(Empty() with { UpdatePromptSettings = 2 }, Empty(), null),
            new PrintPolicyEnvironment(Empty() with { TrustedServers = 1 }, Empty(), null),
            new PrintPolicyEnvironment(Empty() with { PackagePointAndPrintServerList = 1 }, Empty(), null)
        })
            Require(PrintPolicyDiagnostics.Assess(exposed) is { State: CheckState.Attention, Repair: null },
                "Unsafe prompt behavior or an empty enforced server list requests review without selecting repair");

        foreach (var invalid in new[]
        {
            new PrintPolicyEnvironment(Empty() with { Restricted = 2 }, Empty(), null),
            new PrintPolicyEnvironment(Empty() with { UpdatePromptSettings = 3 }, Empty(), null),
            new PrintPolicyEnvironment(Empty() with { TrustedServerCount = -1 }, Empty(), null),
            new PrintPolicyEnvironment(Empty() with { ApprovedPackageServerCount = 257 }, Empty(), null),
            new PrintPolicyEnvironment(Empty(), Empty(), "1")
        })
            Require(PrintPolicyDiagnostics.Assess(invalid).State == CheckState.Unknown,
                "Malformed print policy cannot claim known behavior");

        foreach (var failure in new Exception[] { new IOException("fixture"), new UnauthorizedAccessException("fixture"),
            new System.Security.SecurityException("fixture"), new InvalidOperationException("fixture") })
            Require(PrintPolicyDiagnostics.Check(() => throw failure) is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 },
                "Print policy collection failures retain evidence without changing policy");
        Require(PrintPolicyDiagnostics.Check(() => null!) is { State: CheckState.Unknown, Repair: null },
            "A missing print policy snapshot remains unknown");
        Console.WriteLine("PASS: Point and Print restrictions, approved-server counts, prompt exposure and privacy; no policy or print changes.");
    }

    private static PrintPolicyScope Empty() => new(null, null, null, null, null, 0, null, null, 0);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
