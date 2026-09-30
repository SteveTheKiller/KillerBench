using System.IO;
using KillerBench.Engine;

internal static class BeamPlanningChecks
{
    internal static void Run(string scratch)
    {
        string exe = Path.Combine(scratch, "beam-test.exe");
        File.WriteAllBytes(exe, [0x4D, 0x5A, 0, 0, 0, 0, 0, 0]);
        var options = new BeamOptions(BeamSourceKind.LocalFile, exe, BeamInstallerType.Exe,
            "Support Agent", "Example Publisher", "/silent",
            ["PC-002", "pc-001", "PC-001", "192.0.2.10"], "example.onmicrosoft.com");
        var review = BeamPlanning.ReviewAsync(options).GetAwaiter().GetResult();
        Require(review.SourceVerified && review.Ready && !review.ExecutionSupported,
            "A matching local installer header produces a ready review without execution");
        Require(review.Plan.TargetHostnames.SequenceEqual(["pc-001", "PC-002"], StringComparer.OrdinalIgnoreCase) &&
            review.Plan.IgnoredIpTargets.SequenceEqual(["192.0.2.10"]),
            "Targets are deduplicated and IP entries are separated from Intune hostnames");
        Require(review.Plan.InstallContext == "SYSTEM" && review.Plan.AssignmentIntent.Contains("Required") &&
            review.Plan.InstallCommand.Contains("/silent") && review.Plan.UninstallCommand.Contains("No automatic"),
            "The review exposes Intune deployment execution consequences and the EXE uninstall limitation");
        Require(review.Checks.All(check => check.Repair is null) && review.Format().Contains("Execution available: No"),
            "Intune deployment review never offers tenant or installer execution");

        var url = options with { SourceKind = BeamSourceKind.HttpsUrl,
            Source = "https://downloads.example.test/setup.msi?token=secret", InstallerType = BeamInstallerType.Msi,
            InstallSwitches = "" };
        var urlReview = BeamPlanning.ReviewAsync(url).GetAwaiter().GetResult();
        Require(urlReview.Ready && !urlReview.SourceVerified && !urlReview.Plan.SourceDisplay.Contains("secret") &&
            urlReview.Plan.InstallCommand.Contains("/qn"),
            "HTTPS reviews redact query data and apply the source script's default MSI switch");
        try
        {
            BeamPlanning.ReviewAsync(options with { Source = "relative.exe" }).GetAwaiter().GetResult();
            throw new Exception("Relative Intune deployment source accepted.");
        }
        catch (ArgumentException) { }
        try
        {
            BeamPlanning.ReviewAsync(options with { TargetHostnames = ["192.0.2.10"] }).GetAwaiter().GetResult();
            throw new Exception("IP-only Intune deployment target list accepted.");
        }
        catch (ArgumentException) { }
        try
        {
            BeamPlanning.ReviewAsync(url with { Source = "https://downloads.example.test/setup%0A.msi" }).GetAwaiter().GetResult();
            throw new Exception("Encoded control character accepted in Intune deployment installer name.");
        }
        catch (ArgumentException) { }
        try
        {
            BeamPlanning.ReviewAsync(url with { Source = "https://downloads.example.test/setup.exe" }).GetAwaiter().GetResult();
            throw new Exception("Conflicting Intune deployment URL extension accepted.");
        }
        catch (ArgumentException) { }
        Console.WriteLine("PASS: Intune deployment source, package, target and tenant review; no deployment available.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
