using System.IO;
using KillerBench.Engine;

internal static class InstallerSecurityPolicyChecks
{
    public static void Run()
    {
        var absent = InstallerSecurityPolicyDiagnostics.Assess(Empty());
        Require(absent is { State: CheckState.Information, Repair: null } &&
            absent.Detail.Contains("No policy, package, cache or Installer setting changed", StringComparison.Ordinal),
            "Absent Installer security policies remain informational and non-mutating");

        var secureTransforms = InstallerSecurityPolicyDiagnostics.Assess(Empty() with { TransformsSecure = 1 });
        Require(secureTransforms.State == CheckState.Information &&
            secureTransforms.Detail.Contains("secure local caching", StringComparison.Ordinal),
            "Secure transform caching is described without claiming package health");
        var configured = InstallerSecurityPolicyDiagnostics.Assess(Empty() with
        {
            DisableFlyWeightPatching = 1, EnableAdminTsRemote = 1, MsiDisableEmbeddedUi = 1,
            MaxPatchCacheSize = 15, WinHttpAutoLogonLevel = "M"
        });
        Require(configured.State == CheckState.Information && configured.Detail.Contains("15%", StringComparison.Ordinal) &&
            configured.Detail.Contains("local-intranet", StringComparison.Ordinal),
            "Recognized Installer compatibility and cache settings remain descriptive");

        foreach (var enabled in EnabledPolicies())
            Require(InstallerSecurityPolicyDiagnostics.Assess(enabled) is { State: CheckState.Attention, Repair: null },
                "Enabled Installer restriction or security exposure requests review without selecting repair");

        foreach (var invalid in InvalidPolicies())
            Require(InstallerSecurityPolicyDiagnostics.Assess(invalid).State == CheckState.Unknown,
                "Malformed Installer security policy cannot claim known behavior");

        foreach (var failure in new Exception[] { new IOException("fixture"), new UnauthorizedAccessException("fixture"),
            new System.Security.SecurityException("fixture"), new InvalidOperationException("fixture") })
            Require(InstallerSecurityPolicyDiagnostics.Check(() => throw failure) is
                { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 },
                "Installer security policy collection failures retain evidence without changing policy");
        Require(InstallerSecurityPolicyDiagnostics.Check(() => null!) is { State: CheckState.Unknown, Repair: null },
            "A missing Installer security policy snapshot remains unknown");
        Console.WriteLine("PASS: Installer patch restrictions and security exposures; no policy or package changes.");
    }

    private static InstallerSecurityPolicyEnvironment Empty() => new(null, null, null, null, null, null, null, null, null);

    private static IEnumerable<InstallerSecurityPolicyEnvironment> EnabledPolicies()
    {
        yield return Empty() with { DisableUserInstalls = 1 };
        yield return Empty() with { DisablePatch = 1 };
        yield return Empty() with { DisablePatchUninstall = 1 };
        yield return Empty() with { DisableLuaPatching = 1 };
        yield return Empty() with { SafeForScripting = 1 };
        yield return Empty() with { EnableUserControl = 1 };
        yield return Empty() with { AllowLockdownBrowse = 1 };
        yield return Empty() with { AllowLockdownMedia = 1 };
        yield return Empty() with { AllowLockdownPatch = 1 };
        yield return Empty() with { DisableAutomaticApplicationShutdown = 1 };
        yield return Empty() with { DisableSharedComponent = 1 };
        yield return Empty() with { EnforceUpgradeComponentRules = 1 };
        yield return Empty() with { LimitSystemRestoreCheckpointing = 1 };
        yield return Empty() with { MaxPatchCacheSize = 0 };
        yield return Empty() with { WinHttpAutoLogonLevel = "L" };
    }

    private static IEnumerable<InstallerSecurityPolicyEnvironment> InvalidPolicies()
    {
        yield return Empty() with { DisablePatch = 2 };
        yield return Empty() with { DisableUserInstalls = "1" };
        yield return Empty() with { SafeForScripting = -1 };
        yield return Empty() with { TransformsSecure = "enabled" };
        yield return Empty() with { DisableAutomaticApplicationShutdown = 2 };
        yield return Empty() with { MaxPatchCacheSize = 101 };
        yield return Empty() with { WinHttpAutoLogonLevel = "Low" };
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
