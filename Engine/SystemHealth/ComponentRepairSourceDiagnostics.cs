using Microsoft.Win32;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public sealed record ComponentRepairSourceSnapshot(bool Complete,
    object? LocalSourcePath = null, object? UseWindowsUpdate = null,
    object? RepairContentServerSource = null, string? CollectionIssue = null);

public static class ComponentRepairSourceDiagnostics
{
    private const string PolicyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Servicing";

    public static WorkflowCheck Check() => Assess(Read());

    public static ComponentRepairSourceSnapshot Read()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Component repair source collection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var policy = root.OpenSubKey(PolicyPath);
            return new(true, policy?.GetValue("LocalSourcePath", null, RegistryValueOptions.DoNotExpandEnvironmentNames),
                policy?.GetValue("UseWindowsUpdate"), policy?.GetValue("RepairContentServerSource"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, CollectionIssue: error.GetType().Name);
        }
    }

    public static WorkflowCheck Assess(ComponentRepairSourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool validPath = snapshot.LocalSourcePath is null or string { Length: <= 4096 };
        bool validWindowsUpdate = snapshot.UseWindowsUpdate is null or 2;
        bool validRepairServer = snapshot.RepairContentServerSource is null or int and >= 0 and <= 2;
        bool complete = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) &&
            validPath && validWindowsUpdate && validRepairServer;
        string? source = snapshot.LocalSourcePath is string path && !string.IsNullOrWhiteSpace(path) ? path.Trim() : null;
        bool blocksWindowsUpdate = snapshot.UseWindowsUpdate is 2;
        bool directsToWindowsUpdate = snapshot.RepairContentServerSource is 2;
        CheckState state;
        string detail;
        if (!complete)
        {
            state = CheckState.Unknown;
            detail = "Component repair source policy was unavailable or malformed. Recollect policy evidence before starting component-store repair.";
        }
        else if (blocksWindowsUpdate && directsToWindowsUpdate)
        {
            state = CheckState.Attention;
            detail = "Managed component repair policy both forbids Windows Update payload downloads and directs repair content to Windows Update. Resolve the policy conflict before starting component-store repair.";
        }
        else if (blocksWindowsUpdate && source is null)
        {
            state = CheckState.Attention;
            detail = "Managed component repair policy forbids Windows Update payload downloads, but no alternate repair source is configured. Component-store repair is deferred until the controlling policy provides a source.";
        }
        else if (source is not null)
        {
            state = CheckState.Information;
            detail = "Managed component repair policy provides an alternate source. Source reachability, read access, Windows version alignment and required payload presence were not tested.";
        }
        else if (directsToWindowsUpdate)
        {
            state = CheckState.Information;
            detail = "Managed component repair policy directs repair content to Windows Update. Network reachability and payload availability were not tested.";
        }
        else
        {
            state = CheckState.Information;
            detail = "No restrictive component repair source policy was found. Windows chooses the effective repair source when RestoreHealth runs.";
        }
        detail += " Managed policy was preserved. No repair source, update, servicing or registry setting changed.";
        return new("Component repair source", state, detail,
            Evidence: new(complete ? 0 : -1, JsonSerializer.Serialize(snapshot, Capture.JsonOptions),
                snapshot.CollectionIssue ?? "", false));
    }

    public static WorkflowCheck ApplyPrerequisite(WorkflowCheck integrity, WorkflowCheck source)
    {
        ArgumentNullException.ThrowIfNull(integrity);
        ArgumentNullException.ThrowIfNull(source);
        if (integrity.State != CheckState.Repairable || integrity.Repair != RepairKind.ComponentStore ||
            source.State is CheckState.Clear or CheckState.Information)
            return integrity;
        return integrity with
        {
            State = source.State == CheckState.Attention ? CheckState.Attention : CheckState.Unknown,
            Repair = null,
            Detail = integrity.Detail + " " + source.Detail
        };
    }
}
