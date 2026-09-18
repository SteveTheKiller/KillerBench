using Microsoft.Win32;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record InstallerEnvironment(object? DisableMsi, bool InProgressMarker, bool EngineAvailable, object? DisableRollback = null)
{
    public string? InspectedAccount { get; init; }
    public object? UserDisableRollback { get; init; }
    public bool ElevationPolicyCollected { get; init; }
    public object? MachineAlwaysInstallElevated { get; init; }
    public object? UserAlwaysInstallElevated { get; init; }
    public bool ProcessInventoryCollected { get; init; }
    public int? ActiveInstallerProcesses { get; init; }
    public bool SourcePolicyCollected { get; init; }
    public object? MachineSearchOrder { get; init; }
    public object? UserSearchOrder { get; init; }
    public object? MachineDisableMedia { get; init; }
    public object? UserDisableMedia { get; init; }
    public object? MachineDisableBrowse { get; init; }
    public object? UserDisableBrowse { get; init; }
}

public static class InstallerDiagnostics
{
    public static WorkflowCheck Assess(InstallerEnvironment state)
    {
        var details = new List<string>();
        bool attention = false, unknown = false;
        switch (state.DisableMsi)
        {
            case null:
                details.Add("DisableMSI policy is not explicitly configured. Operating-system defaults and other installation policies can still apply.");
                break;
            case int value when value == 0:
                details.Add("DisableMSI does not explicitly restrict installation.");
                break;
            case int value when value == 1:
                attention = true;
                details.Add("DisableMSI restricts unmanaged installations. Managed installation may still be allowed; review the deployment context with the policy owner.");
                break;
            case int value when value == 2:
                attention = true;
                details.Add("DisableMSI disables Windows Installer operations, including repairs. Policy is preserved.");
                break;
            default:
                unknown = true;
                details.Add("DisableMSI has an unrecognized value or type. Policy is preserved.");
                break;
        }
        switch (state.DisableRollback)
        {
            case null:
            case int rollback when rollback == 0:
                details.Add("Machine DisableRollback policy does not explicitly disable rollback. Package settings are not assessed.");
                break;
            case int rollback when rollback == 1:
                attention = true;
                details.Add("Machine DisableRollback policy disables rollback for per-computer installations. Review this with the policy owner before installation or recovery. Policy is preserved.");
                break;
            default:
                unknown = true;
                details.Add("Machine DisableRollback has an unrecognized value or type. Policy is preserved.");
                break;
        }
        if (state.InspectedAccount is { } account)
        {
            details.Add("User policy scope: " + account + ". This is the process account, which may differ from the deployment account.");
            switch (state.UserDisableRollback)
            {
                case null:
                case int rollback when rollback == 0:
                    details.Add("This account's DisableRollback policy does not explicitly disable per-user rollback.");
                    break;
                case int rollback when rollback == 1:
                    attention = true;
                    details.Add("This account's DisableRollback policy disables per-user installation rollback. Review with the policy owner; settings are preserved.");
                    break;
                default:
                    unknown = true;
                    details.Add("This account's DisableRollback value or type is unrecognized. Settings are preserved.");
                    break;
            }
        }
        else details.Add("Per-user rollback policy was not collected.");
        if (state.ElevationPolicyCollected)
        {
            static bool Known(object? value) => value is null or 0 or 1;
            if (!Known(state.MachineAlwaysInstallElevated) || !Known(state.UserAlwaysInstallElevated))
            {
                unknown = true;
                details.Add("AlwaysInstallElevated has an unrecognized value or type. Elevation policy is unconfirmed and preserved.");
            }
            if (state.MachineAlwaysInstallElevated is int machineElevation && machineElevation == 1)
            {
                attention = true;
                details.Add(state.UserAlwaysInstallElevated is int userElevation && userElevation == 1
                    ? "AlwaysInstallElevated is enabled for both machine and inspected account. Windows Installer can run packages with system privileges. Review this security exposure with the policy owner; settings are preserved."
                    : "Machine AlwaysInstallElevated is enabled. Users can enable their per-user setting, so review this security exposure even though both settings were not observed enabled. Policy is preserved.");
            }
            else if (Known(state.MachineAlwaysInstallElevated) && Known(state.UserAlwaysInstallElevated))
                details.Add("Machine AlwaysInstallElevated is not enabled. This policy pair does not grant elevated unmanaged installation for the inspected account; other elevation mechanisms are not assessed.");
        }
        else details.Add("AlwaysInstallElevated policy was not collected.");
        if (state.SourcePolicyCollected)
        {
            ReviewSearchOrder("Machine", state.MachineSearchOrder);
            ReviewSearchOrder("Inspected account", state.UserSearchOrder);
            ReviewRestriction("Machine", "DisableMedia", state.MachineDisableMedia,
                "removable-media installation sources are disabled");
            ReviewRestriction("Inspected account", "DisableMedia", state.UserDisableMedia,
                "removable-media installation sources are disabled");
            ReviewRestriction("Machine", "DisableBrowse", state.MachineDisableBrowse,
                "source browsing is disabled");
            ReviewRestriction("Inspected account", "DisableBrowse", state.UserDisableBrowse,
                "source browsing is disabled");
        }
        else details.Add("Installer source-resolution policy was not collected.");
        if (state.InProgressMarker)
        {
            attention = true;
            details.Add("An Installer InProgress marker exists. It may describe an active or interrupted installation; it is not proof that a process is still running. Wait for the deployment or review its journal. The marker is preserved.");
        }
        if (state.ProcessInventoryCollected)
        {
            if (state.ActiveInstallerProcesses is not int processCount || processCount is < 0 or > 128)
            {
                unknown = true;
                details.Add("Windows Installer process inventory returned an invalid or excessive count. Current activity is unconfirmed and no recovery is selected.");
            }
            else if (processCount > 0)
            {
                attention = true;
                details.Add($"{processCount} msiexec.exe process{(processCount == 1 ? " was" : "es were")} observed. An installation, repair, registration action or suspended client may be active. Wait for it to finish or review the owning deployment before recovery. Process command lines were not collected.");
            }
            else details.Add("No msiexec.exe process was observed at capture time. This snapshot does not prove that no Installer transaction exists.");
        }
        else details.Add("Windows Installer process inventory was not collected.");
        if (!state.EngineAvailable)
        {
            attention = true;
            details.Add("Windows Installer binaries are missing or inaccessible. Run the System files workflow to check protected files; no registration or cached packages were changed.");
        }
        details.Add("This checks known prerequisites only, not a specific package, its source files, or its custom actions.");
        return new("Installer readiness", unknown ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Clear,
            string.Join("\n", details), Evidence: new(0, JsonSerializer.Serialize(state, Capture.JsonOptions), "", false));

        void ReviewSearchOrder(string scope, object? raw)
        {
            if (raw is null) { details.Add(scope + " SearchOrder policy is not explicitly configured."); return; }
            if (raw is not string text || text.Length is < 1 or > 3 || text != text.Trim() ||
                text.Any(character => character is not ('n' or 'm' or 'u' or 'N' or 'M' or 'U')) ||
                text.Select(char.ToLowerInvariant).Distinct().Count() != text.Length)
            { unknown = true; details.Add(scope + " SearchOrder policy has an unrecognized value or type. Policy is preserved."); return; }
            string normalized = text.ToLowerInvariant();
            details.Add(scope + " Installer source search order: " + string.Join(", ", normalized.Select(character => character switch
            { 'n' => "network", 'm' => "removable media", 'u' => "URL", _ => "unknown" })) + ". This is policy order, not proof that a source is reachable.");
        }

        void ReviewRestriction(string scope, string name, object? raw, string effect)
        {
            if (raw is null or 0) { details.Add(scope + " " + name + " policy is not enabled."); return; }
            if (raw is int value && value == 1) { attention = true; details.Add(scope + " " + effect + ". This can explain a source-resolution failure. Review with the policy owner; policy is preserved."); return; }
            unknown = true;
            details.Add(scope + " " + name + " policy has an unrecognized value or type. Policy is preserved.");
        }
    }

    public static void RequireReady(Func<InstallerEnvironment>? collect = null)
    {
        var readiness = Check(collect);
        if (readiness.State != CheckState.Clear)
            throw new InvalidOperationException("Windows Installer recovery deferred: " + readiness.Detail);
    }

    public static WorkflowCheck Check(Func<InstallerEnvironment>? collect = null)
    {
        try
        {
            var state = (collect ?? Collect)();
            return state is null
                ? new("Installer readiness", CheckState.Unknown, "Installer collector returned no observation. Policy and engine readiness are unconfirmed; no changes selected.")
                : Assess(state);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return new("Installer readiness", CheckState.Unknown,
                "Installer policy or engine state could not be collected. Other diagnostic sources can still be inspected. Managed settings are preserved; no changes selected.",
                Evidence: new(-1, "", error.GetType().Name + ": " + error.Message, false));
        }
    }

    private static InstallerEnvironment Collect()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Installer checks require Windows.");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var policy = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Installer");
        using var progress = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress");
        using var userRoot = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using var userPolicy = userRoot.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Installer");
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var installerProcesses = System.Diagnostics.Process.GetProcessesByName("msiexec");
        int activeInstallerProcesses = installerProcesses.Length;
        foreach (var process in installerProcesses) process.Dispose();
        string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        return new(policy?.GetValue("DisableMSI"), progress is not null,
            File.Exists(Path.Combine(system, "msiexec.exe")) && File.Exists(Path.Combine(system, "msi.dll")), policy?.GetValue("DisableRollback"))
        { InspectedAccount = identity.Name, UserDisableRollback = userPolicy?.GetValue("DisableRollback"),
            ElevationPolicyCollected = true, MachineAlwaysInstallElevated = policy?.GetValue("AlwaysInstallElevated"),
            UserAlwaysInstallElevated = userPolicy?.GetValue("AlwaysInstallElevated"), ProcessInventoryCollected = true,
            ActiveInstallerProcesses = activeInstallerProcesses, SourcePolicyCollected = true,
            MachineSearchOrder = policy?.GetValue("SearchOrder"), UserSearchOrder = userPolicy?.GetValue("SearchOrder"),
            MachineDisableMedia = policy?.GetValue("DisableMedia"), UserDisableMedia = userPolicy?.GetValue("DisableMedia"),
            MachineDisableBrowse = policy?.GetValue("DisableBrowse"), UserDisableBrowse = userPolicy?.GetValue("DisableBrowse") };
    }
}
