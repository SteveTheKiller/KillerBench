using System.IO;
using KillerBench.Engine;

internal static class InstallerLoggingChecks
{
    public static void Run()
    {
        var absent = InstallerLoggingDiagnostics.Assess(new(null, null, null));
        Require(absent.State == CheckState.Information && absent.Repair is null && absent.Detail.Contains("not explicitly configured", StringComparison.Ordinal),
            "Absent Installer logging policies remain informational");
        var configured = InstallerLoggingDiagnostics.Assess(new("voicewarmup", 0, 0));
        Require(configured.State == CheckState.Information && configured.Detail.Contains("voicewarmup", StringComparison.Ordinal),
            "Known Installer logging mode letters are retained without changing policy");
        var intensive = InstallerLoggingDiagnostics.Assess(new("voicewarmupx!", 1, 7));
        Require(intensive.State == CheckState.Attention && intensive.Repair is null &&
            intensive.Detail.Contains("slow an installation", StringComparison.Ordinal) &&
            intensive.Detail.Contains("command-line logging bit", StringComparison.Ordinal) &&
            intensive.Detail.Contains("package's MsiLogging", StringComparison.Ordinal),
            "Package logging restriction and detailed debug modes request review without selecting repair");
        foreach (var invalid in new[]
        {
            new InstallerLoggingEnvironment("", null, null), new("ii", null, null), new("voice*", null, null), new(1, null, null),
            new(null, "1", null), new(null, 2, null), new(null, null, "7"), new(null, null, 8)
        }) Require(InstallerLoggingDiagnostics.Assess(invalid).State == CheckState.Unknown,
            "Malformed Installer logging policy cannot claim known behavior");
        foreach (var failure in new Exception[] { new IOException("fixture"), new UnauthorizedAccessException("fixture"),
            new System.Security.SecurityException("fixture"), new InvalidOperationException("fixture") })
            Require(InstallerLoggingDiagnostics.Check(() => throw failure) is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 },
                "Installer logging collection failures retain evidence without changing policy");
        Require(InstallerLoggingDiagnostics.Check(() => null!) is { State: CheckState.Unknown, Repair: null },
            "A missing Installer logging snapshot remains unknown");
        Console.WriteLine("PASS: Installer logging modes, package override and debug exposure; no policy or log changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
