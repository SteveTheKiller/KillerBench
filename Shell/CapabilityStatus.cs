using System.Windows;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private static IReadOnlyList<RepairCapability> CapabilitiesForWorkflow(WorkflowKind kind)
    {
        var ids = RecipeCatalog.ForWorkflow(kind).Select(recipe => recipe.Id).ToHashSet(StringComparer.Ordinal);
        string[] commands = kind switch
        {
            WorkflowKind.SystemFiles => ["repair-sfc"],
            WorkflowKind.ComponentCleanup => ["repair-dism"],
            WorkflowKind.Storage => ["storage-repair", "volume-optimize"],
            WorkflowKind.SystemRestore => ["restore-point"],
            WorkflowKind.Defender => ["defender-scan"],
            WorkflowKind.WindowsUpdate => ["update-store-reset"],
            WorkflowKind.AvailableUpdates => ["update-plan"],
            WorkflowKind.BackgroundTransfers => ["bits-retry"],
            WorkflowKind.Search => ["search-index-rebuild"],
            WorkflowKind.EventLog => ["event-channel-enable"],
            WorkflowKind.Installer => ["installer-registration-repair", "msi-repair"],
            WorkflowKind.ScheduledTasks => ["task-register"],
            WorkflowKind.Mace => ["mace-package-removal"],
            WorkflowKind.Network => ["dns-cache", "dhcp-renew"],
            WorkflowKind.ComputerName => ["name-rename"],
            WorkflowKind.Printing => ["printer-repair", "print-job-cancel", "default-printer-set"],
            WorkflowKind.TimeService => ["time-configure"],
            WorkflowKind.Profiles => ["profiles"],
            WorkflowKind.Orca => ["orca-cache-reset"],
            WorkflowKind.Cleanup => ["cleanup", "previous-windows"],
            WorkflowKind.Privacy => ["privacy-clipboard-history", "privacy-clipboard-history-undo", "privacy-setting", "privacy-setting-undo"],
            WorkflowKind.BitLocker => ["bitlocker-backup", "bitlocker-resume", "bitlocker-escrow", "bitlocker-protector-add", "bitlocker-decrypt", "bitlocker-auto-unlock", "bitlocker-encrypt"],
            WorkflowKind.FirewallServices => ["firewall-enable"],
            WorkflowKind.Debloat => ["debloat-high-performance", "debloat-power-timers", "debloat-hibernation"],
            _ => []
        };
        ids.UnionWith(commands.Select(command => "command." + command));
        return WindowsRepairCapabilities.All.Where(capability => ids.Contains(capability.Id)).ToArray();
    }

    private static string NativeCapabilityDetails(WorkflowKind kind)
    {
        var capabilities = CapabilitiesForWorkflow(kind);
        return capabilities.Count == 0
            ? Controls.LocalizedText.Format("Str_NativeLab_Format", Controls.LocalizedText.Get("Str_NativeLab_DiagnosticOnly"))
            : string.Join("\n", capabilities.Select(capability => Controls.LocalizedText.Format("Str_NativeLab_Format",
                capability.Id + ": " + Controls.LocalizedText.Get("Str_NativeLab_" + capability.Status))));
    }

    private void RefreshNativeCapabilityStatus()
    {
        if (NativeCapabilityStatus is null || MainWorkflow?.SelectedItem is not WorkflowDefinition workflow) return;
        var capabilities = CapabilitiesForWorkflow(workflow.Kind);
        string[] labels = capabilities.Where(capability => capability.Status != Engine.Validation.NativeCapabilityStatus.DiagnosticOnly)
            .Select(capability => Controls.LocalizedText.Get("Str_NativeLab_" + capability.Status)).Distinct(StringComparer.Ordinal).ToArray();
        NativeCapabilityStatus.Text = Controls.LocalizedText.Format("Str_NativeLab_Format", labels.Length == 0
            ? Controls.LocalizedText.Get("Str_NativeLab_DiagnosticOnly") : string.Join("; ", labels));
        NativeCapabilityStatus.ToolTip = NativeCapabilityDetails(workflow.Kind);
        NativeCapabilityStatus.Visibility = Visibility.Visible;
    }

    private static string NativeCapabilityLabel(string id) => Controls.LocalizedText.Format("Str_NativeLab_Format",
        Controls.LocalizedText.Get("Str_NativeLab_" + (WindowsRepairCapabilities.Find(id)?.Status ?? Engine.Validation.NativeCapabilityStatus.NotYetVerified)));

    private static string FocusedRepairPreview(RepairKind kind) => Repairs.Preview(kind) + "\n\n" +
        NativeCapabilityLabel(kind == RepairKind.SystemFiles ? "command.repair-sfc" : "command.repair-dism");
}
