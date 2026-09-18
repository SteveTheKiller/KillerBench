using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record PrintPolicyScope(
    object? Restricted,
    object? TrustedServers,
    object? InForest,
    object? NoWarningNoElevationOnInstall,
    object? UpdatePromptSettings,
    int TrustedServerCount,
    object? PackagePointAndPrintOnly,
    object? PackagePointAndPrintServerList,
    int ApprovedPackageServerCount);

public sealed record PrintPolicyEnvironment(
    PrintPolicyScope Machine,
    PrintPolicyScope CurrentUser,
    object? RestrictDriverInstallationToAdministrators);

public static class PrintPolicyDiagnostics
{
    public static WorkflowCheck Check(Func<PrintPolicyEnvironment>? collect = null)
    {
        try
        {
            var state = (collect ?? Collect)();
            return state is null ? Unknown("Print policy collector returned no observation.") : Assess(state);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return Unknown("Point and Print policy could not be read. Managed settings remain preserved.",
                new(-1, "", error.GetType().Name + ": " + error.Message, false));
        }
    }

    public static WorkflowCheck Assess(PrintPolicyEnvironment state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(state.Machine);
        ArgumentNullException.ThrowIfNull(state.CurrentUser);
        bool attention = false, unknown = false;
        var details = new List<string>();
        EvaluateScope("Machine", state.Machine);
        EvaluateScope("Current user", state.CurrentUser);
        switch (state.RestrictDriverInstallationToAdministrators)
        {
            case null:
                details.Add("Administrator-only printer driver installation is not explicitly configured by machine policy. Current Windows defaults and other policy can still require elevation.");
                break;
            case int value when value == 1:
                details.Add("Machine policy requires an administrator to install printer drivers. This can explain a blocked non-administrator connection or driver update.");
                break;
            case int value when value == 0:
                attention = true;
                details.Add("Machine policy explicitly allows non-administrators to install printer drivers. Review this security exposure with the policy owner.");
                break;
            default:
                unknown = true;
                details.Add("RestrictDriverInstallationToAdministrators has an unrecognized value or type.");
                break;
        }
        details.Add("This snapshot can explain connection, installation and update behavior, but it does not prove a queue, driver or server is faulty. Approved server identities are excluded from evidence. No policy, registry value, queue, driver, connection, credential or print setting changed.");
        return new("Point and Print policy", unknown ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Information,
            string.Join("\n", details), Evidence: new(0, JsonSerializer.Serialize(state, Capture.JsonOptions), "", false));

        void EvaluateScope(string label, PrintPolicyScope scope)
        {
            bool? restricted = Flag(label + " Restricted", scope.Restricted);
            bool? trusted = Flag(label + " TrustedServers", scope.TrustedServers);
            bool? forest = Flag(label + " InForest", scope.InForest);
            bool? packageOnly = Flag(label + " PackagePointAndPrintOnly", scope.PackagePointAndPrintOnly);
            bool? packageList = Flag(label + " PackagePointAndPrintServerList", scope.PackagePointAndPrintServerList);
            Prompt(label + " new-driver prompt", scope.NoWarningNoElevationOnInstall, 1);
            Prompt(label + " updated-driver prompt", scope.UpdatePromptSettings, 1, 2);
            if (scope.TrustedServerCount is < 0 or > 256)
            {
                unknown = true;
                details.Add(label + " trusted-server list is malformed or exceeds the 256-entry bound.");
            }
            else
            {
                details.Add(label + " trusted-server list contains " + scope.TrustedServerCount + " entr" + (scope.TrustedServerCount == 1 ? "y." : "ies."));
                if (trusted == true && scope.TrustedServerCount == 0)
                {
                    attention = true;
                    details.Add(label + " policy requires trusted servers but no trusted server entry was found. This can block printer installation.");
                }
            }
            if (scope.ApprovedPackageServerCount is < 0 or > 256)
            {
                unknown = true;
                details.Add(label + " approved package-server count is invalid or exceeds the 256-entry bound.");
            }
            else
            {
                details.Add(label + " approved package-server list contains " + scope.ApprovedPackageServerCount + " entr" + (scope.ApprovedPackageServerCount == 1 ? "y." : "ies."));
                if (packageList == true && scope.ApprovedPackageServerCount == 0)
                {
                    attention = true;
                    details.Add(label + " policy restricts printer connections to approved servers but no approved package server entry was found. This can block printer installation.");
                }
            }
            if (restricted == true)
                details.Add(label + " Point and Print restrictions are enabled. Trusted-server and forest restrictions can intentionally block unapproved connections.");
            if (forest == true)
                details.Add(label + " policy limits Point and Print discovery or connections to the current forest.");
            if (packageOnly == true)
                details.Add(label + " policy allows package-aware Point and Print connections only.");
        }

        bool? Flag(string name, object? value)
        {
            switch (value)
            {
                case null:
                    details.Add(name + " is not explicitly configured.");
                    return null;
                case int number when number is 0 or 1:
                    details.Add(name + " is " + (number == 1 ? "enabled." : "disabled."));
                    return number == 1;
                default:
                    unknown = true;
                    details.Add(name + " has an unrecognized value or type.");
                    return null;
            }
        }

        void Prompt(string name, object? value, params int[] insecureValues)
        {
            switch (value)
            {
                case null:
                    details.Add(name + " behavior is not explicitly configured.");
                    break;
                case int number when number is >= 0 and <= 2:
                    if (insecureValues.Contains(number))
                    {
                        attention = true;
                        details.Add(name + " suppresses elevation or warning behavior. Review this security exposure with the policy owner.");
                    }
                    else
                        details.Add(name + " requires warning and elevation behavior.");
                    break;
                default:
                    unknown = true;
                    details.Add(name + " has an unrecognized value or type.");
                    break;
            }
        }
    }

    private static PrintPolicyEnvironment Collect()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Print policy checks require Windows.");
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        var machineScope = ReadScope(machine);
        var userScope = ReadScope(user);
        using var pointAndPrint = machine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Printers\PointAndPrint");
        return new(machineScope, userScope, pointAndPrint?.GetValue("RestrictDriverInstallationToAdministrators"));
    }

    [SupportedOSPlatform("windows")]
    private static PrintPolicyScope ReadScope(RegistryKey root)
    {
        using var pointAndPrint = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Printers\PointAndPrint");
        using var package = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Printers\PackagePointAndPrint");
        using var list = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Printers\PackagePointAndPrint\ListofServers");
        string[] names = list?.GetValueNames() ?? [];
        int trustedServerCount = CountServerList(pointAndPrint?.GetValue("ServerList"));
        return new(pointAndPrint?.GetValue("Restricted"), pointAndPrint?.GetValue("TrustedServers"),
            pointAndPrint?.GetValue("InForest"), pointAndPrint?.GetValue("NoWarningNoElevationOnInstall"),
            pointAndPrint?.GetValue("UpdatePromptSettings"), trustedServerCount, package?.GetValue("PackagePointAndPrintOnly"),
            package?.GetValue("PackagePointAndPrintServerList"), names.Length);
    }

    private static int CountServerList(object? value)
    {
        if (value is null) return 0;
        if (value is not string text || text.Length > 8192) return -1;
        var entries = text.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return entries.Length > 256 || entries.Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Length ? -1 : entries.Length;
    }

    private static WorkflowCheck Unknown(string detail, ToolResult? evidence = null) =>
        new("Point and Print policy", CheckState.Unknown,
            detail + " No policy, registry value, queue, driver, connection, credential or print setting changed.", Evidence: evidence);
}
