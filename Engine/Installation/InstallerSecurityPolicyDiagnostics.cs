using Microsoft.Win32;
using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record InstallerSecurityPolicyEnvironment(
    object? DisableUserInstalls,
    object? DisablePatch,
    object? DisablePatchUninstall,
    object? DisableLuaPatching,
    object? SafeForScripting,
    object? EnableUserControl,
    object? AllowLockdownBrowse,
    object? AllowLockdownMedia,
    object? TransformsSecure,
    object? AllowLockdownPatch = null,
    object? DisableAutomaticApplicationShutdown = null,
    object? DisableFlyWeightPatching = null,
    object? DisableSharedComponent = null,
    object? EnforceUpgradeComponentRules = null,
    object? EnableAdminTsRemote = null,
    object? LimitSystemRestoreCheckpointing = null,
    object? MaxPatchCacheSize = null,
    object? MsiDisableEmbeddedUi = null,
    object? WinHttpAutoLogonLevel = null);

public static class InstallerSecurityPolicyDiagnostics
{
    public static WorkflowCheck Check(Func<InstallerSecurityPolicyEnvironment>? collect = null)
    {
        try
        {
            var state = (collect ?? Collect)();
            return state is null ? Unknown("Installer security policy collector returned no observation.") : Assess(state);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return Unknown("Windows Installer security and patch policy could not be read. Managed policy remains preserved.",
                new(-1, "", error.GetType().Name + ": " + error.Message, false));
        }
    }

    public static WorkflowCheck Assess(InstallerSecurityPolicyEnvironment state)
    {
        ArgumentNullException.ThrowIfNull(state);
        bool attention = false, unknown = false;
        var details = new List<string>();
        EvaluateRestriction("DisableUserInstalls", state.DisableUserInstalls,
            "Per-user Windows Installer operations are blocked and per-user registrations can be ignored.");
        EvaluateRestriction("DisablePatch", state.DisablePatch,
            "Windows Installer patch application is blocked by machine policy.");
        EvaluateRestriction("DisablePatchUninstall", state.DisablePatchUninstall,
            "Windows Installer patch removal is blocked by machine policy.");
        EvaluateRestriction("DisableLUAPatching", state.DisableLuaPatching,
            "Non-administrator vendor-signed patching is blocked by machine policy.");
        EvaluateExposure("SafeForScripting", state.SafeForScripting,
            "Web-script installation safety prompts can be suppressed. Review this exposure with the policy owner.");
        EvaluateExposure("EnableUserControl", state.EnableUserControl,
            "User-controlled public properties can cross into elevated managed installations. Review this exposure with the policy owner.");
        EvaluateExposure("AllowLockdownBrowse", state.AllowLockdownBrowse,
            "Non-administrator source browsing can be allowed during elevated installations. Review this exposure with the policy owner.");
        EvaluateExposure("AllowLockdownMedia", state.AllowLockdownMedia,
            "Non-administrator removable-media sources can be allowed during elevated installations. Review this exposure with the policy owner.");
        EvaluateExposure("AllowLockdownPatch", state.AllowLockdownPatch,
            "Non-administrators can in some cases apply minor patches with elevated privileges. Review this exposure with the policy owner.");
        EvaluateRestriction("DisableAutomaticApplicationShutdown", state.DisableAutomaticApplicationShutdown,
            "Windows Installer does not use Restart Manager application shutdown and falls back to FilesInUse handling.");
        EvaluateConfiguration("DisableFlyWeightPatching", state.DisableFlyWeightPatching,
            "Patch optimization is disabled for Windows Installer transactions.");
        EvaluateRestriction("DisableSharedComponent", state.DisableSharedComponent,
            "Windows Installer shared-component behavior is disabled for packages on this machine.");
        EvaluateRestriction("EnforceUpgradeComponentRules", state.EnforceUpgradeComponentRules,
            "Upgrade component rules are enforced for small updates and minor upgrades and can reject incompatible packages.");
        EvaluateConfiguration("EnableAdminTSRemote", state.EnableAdminTsRemote,
            "Administrator installations from a Terminal Services client session are enabled.");
        EvaluateRestriction("LimitSystemRestoreCheckpointing", state.LimitSystemRestoreCheckpointing,
            "Windows Installer creation of System Restore checkpoints is disabled.");
        EvaluateConfiguration("MsiDisableEmbeddedUI", state.MsiDisableEmbeddedUi,
            "Embedded user-interface handlers are disabled for Windows Installer packages.");

        switch (state.TransformsSecure)
        {
            case null:
            case int value when value == 0:
                details.Add("TransformsSecure is not explicitly enabled. Windows Installer and package behavior can still determine transform storage.");
                break;
            case int value when value == 1:
                details.Add("TransformsSecure requests secure local caching for transforms.");
                break;
            default:
                unknown = true;
                details.Add("TransformsSecure has an unrecognized value or type. Transform storage behavior is unconfirmed.");
                break;
        }

        switch (state.MaxPatchCacheSize)
        {
            case null:
                details.Add("MaxPatchCacheSize is not explicitly configured. Windows Installer uses its default baseline-cache limit.");
                break;
            case int value when value == 0:
                attention = true;
                details.Add("MaxPatchCacheSize is 0%. New patch baseline files are not cached, which can affect later servicing source requirements. Policy is preserved.");
                break;
            case int value when value is >= 1 and <= 100:
                details.Add("MaxPatchCacheSize limits new patch baseline files to " + value + "% of disk space.");
                break;
            default:
                unknown = true;
                details.Add("MaxPatchCacheSize has an unrecognized value or type. Patch baseline caching is unconfirmed.");
                break;
        }

        switch (state.WinHttpAutoLogonLevel)
        {
            case null:
                details.Add("WinHttpAutoLogonLevel is not explicitly configured. Windows Installer uses the medium automatic-logon level by default.");
                break;
            case string value when value.Equals("L", StringComparison.OrdinalIgnoreCase):
                attention = true;
                details.Add("WinHttpAutoLogonLevel permits default credentials for all Installer WinHTTP requests. Review source trust and credential exposure with the policy owner.");
                break;
            case string value when value.Equals("M", StringComparison.OrdinalIgnoreCase):
                details.Add("WinHttpAutoLogonLevel permits default credentials only for local-intranet Installer requests.");
                break;
            case string value when value.Equals("H", StringComparison.OrdinalIgnoreCase):
                details.Add("WinHttpAutoLogonLevel prevents automatic use of default credentials for Installer requests.");
                break;
            default:
                unknown = true;
                details.Add("WinHttpAutoLogonLevel has an unrecognized value or type. Installer credential forwarding behavior is unconfirmed.");
                break;
        }

        details.Add("This policy snapshot does not prove package health or authorize a policy change. No policy, package, cache or Installer setting changed.");
        return new("Installer security and patch policy", unknown ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Information,
            string.Join("\n", details), Evidence: new(0, JsonSerializer.Serialize(state, Capture.JsonOptions), "", false));

        void EvaluateRestriction(string name, object? value, string enabledDetail) => Evaluate(name, value, enabledDetail);
        void EvaluateExposure(string name, object? value, string enabledDetail) => Evaluate(name, value, enabledDetail);
        void EvaluateConfiguration(string name, object? value, string enabledDetail)
        {
            switch (value)
            {
                case null:
                case int number when number == 0:
                    details.Add(name + " is not explicitly enabled by machine policy.");
                    break;
                case int number when number == 1:
                    details.Add(name + " is enabled. " + enabledDetail + " Policy is preserved.");
                    break;
                default:
                    unknown = true;
                    details.Add(name + " has an unrecognized value or type. Behavior is unconfirmed and policy is preserved.");
                    break;
            }
        }
        void Evaluate(string name, object? value, string enabledDetail)
        {
            switch (value)
            {
                case null:
                case int number when number == 0:
                    details.Add(name + " is not explicitly enabled by machine policy.");
                    break;
                case int number when number == 1:
                    attention = true;
                    details.Add(name + " is enabled. " + enabledDetail + " Policy is preserved.");
                    break;
                default:
                    unknown = true;
                    details.Add(name + " has an unrecognized value or type. Behavior is unconfirmed and policy is preserved.");
                    break;
            }
        }
    }

    private static InstallerSecurityPolicyEnvironment Collect()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Installer policy checks require Windows.");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var policy = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Installer");
        return new(policy?.GetValue("DisableUserInstalls"), policy?.GetValue("DisablePatch"),
            policy?.GetValue("DisablePatchUninstall"), policy?.GetValue("DisableLUAPatching"),
            policy?.GetValue("SafeForScripting"), policy?.GetValue("EnableUserControl"),
            policy?.GetValue("AllowLockdownBrowse"), policy?.GetValue("AllowLockdownMedia"),
            policy?.GetValue("TransformsSecure"), policy?.GetValue("AllowLockdownPatch"),
            policy?.GetValue("DisableAutomaticApplicationShutdown"), policy?.GetValue("DisableFlyWeightPatching"),
            policy?.GetValue("DisableSharedComponent"), policy?.GetValue("EnforceUpgradeComponentRules"),
            policy?.GetValue("EnableAdminTSRemote"), policy?.GetValue("LimitSystemRestoreCheckpointing"),
            policy?.GetValue("MaxPatchCacheSize"), policy?.GetValue("MsiDisableEmbeddedUI"),
            policy?.GetValue("WinHttpAutoLogonLevel"));
    }

    private static WorkflowCheck Unknown(string detail, ToolResult? evidence = null) =>
        new("Installer security and patch policy", CheckState.Unknown,
            detail + " No policy, package, cache or Installer setting changed.", Evidence: evidence);
}
