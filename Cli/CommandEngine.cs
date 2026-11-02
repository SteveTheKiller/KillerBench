using System.Text.Json;
using KillerBench.Engine;

namespace KillerBench.Cli;

public static class CommandEngine
{
    public static string[] CommandNames =>
    [
        "capabilities", "pack", "symptom", "validate-native", "servicing-monitor", "task-definition",
        "restore-point", "restore-point-recover", "defender-scan", "defender-scan-recover", "diagnostic-scopes",
        "workflows", "workflow-options", "validation-plan", "validation-record", "validation-coverage",
        "validation-report", "workflow-journal", "workflow-report", "investigate-batch", "knowledge-compare",
        "knowledge-update", "knowledge-status", "knowledge-activate", "knowledge-active", "knowledge-recover",
        "crash-events", "crash-review", "storage-reliability", "storage-jobs", "disk-quotas",
        "service-triggers", "service-recovery-policy", "service-transitions", "vpn-profiles", "winrm-listeners",
        "physical-disks", "shadow-storage", "vss-providers", "shadow-copies", "delete-notifications",
        "ntfs-self-healing", "crash-dumps", "minidump-analyze", "crash-config", "installed-apps-compare",
        "app-folder-review", "startup-apps", "browser-extensions", "duplicate-files", "large-files",
        "backup-events", "backup-summary", "file-history-status", "file-history-events", "dns-cache-summary",
        "vss-writers", "bits-jobs", "bits-job-review", "bits-retry", "bits-retry-recover",
        "app-packages", "app-package-review", "provisioned-packages", "installed-apps", "update-search",
        "third-party-updates", "third-party-update", "third-party-update-recover", "update-store-reset", "update-store-reset-recover",
        "search-index-rebuild", "search-index-rebuild-recover", "previous-windows", "previous-windows-recover", "device-check",
        "device-code", "msi-code", "update-code", "update-followup", "setup-code",
        "event-channel-enable", "event-channel-enable-recover", "ncsi-policy", "event-channels", "task-check",
        "vitals-check", "default-printer", "printer-conditions", "printer-queue-attributes", "printer-drivers",
        "printer-driver-files", "print-extension-files", "printer-driver-models", "printer-ports", "printer-endpoint-check",
        "security-check", "network-profiles", "network-firewall", "firewall-logging", "firewall-rules",
        "firewall-events", "firewall-service-events", "network-service-events", "storage-service-events", "backup-service-events",
        "feature-service-events", "platform-service-events", "infrastructure-service-events", "dns-client-config", "dns-client-events",
        "dns-nrpt-policy", "windows-capabilities", "route-priority", "ip-interface-config", "dhcp-leases",
        "dhcp-events", "neighbor-cache", "adapter-bindings", "adapter-statistics", "tcp-port-pressure",
        "tcp-settings", "network-offload", "winsock-catalog", "winsock-namespaces", "winsock-provider-files",
        "winsock-readiness", "pagefile-check", "reserved-storage", "storage-sense-policy", "storage-sense-task",
        "windows-update-task", "disk-cleanup-task", "disk-diagnostic-tasks", "disk-diagnostic", "smb-shares",
        "smb-server-config", "smb-client-config", "smb-connections", "font-registrations", "common-check",
        "service-check", "service-binaries", "service-path-quoting", "service-logons", "service-startup-config",
        "service-dependencies", "service-dlls", "msi-product", "msi-repair", "msi-repair-recover",
        "installer-registration-repair", "installer-registration-repair-recover", "msi-cache", "msi-sources", "installer-check",
        "installer-engine-files", "installer-service-registration", "installer-security-policy", "installer-logging", "installer-activity",
        "installer-pending-state", "installer-events", "installer-service-events", "print-events", "print-operational-events",
        "print-spooler-events", "print-policy", "print-rendering-policy", "volume-capacity", "volume-dirty",
        "proxy-check", "service-events", "storage-events", "servicing-logs", "servicing-service-states",
        "servicing-service-events", "update-events", "update-engine-files", "update-service-files", "update-service-events",
        "time-events", "time-config", "application-events", "delivery-optimization", "delivery-optimization-activity",
        "delivery-optimization-policy", "update-cache", "update-policy", "update-policy-surfaces", "update-restart-policy",
        "feature-update-policy", "component-repair-source", "update-services", "update-history", "installed-hotfixes",
        "update-context", "update-applicability", "restart-check", "pending-file-operations", "certificate-chains",
        "licensing-check", "defender-threats", "defender-scan-history", "defender-protection", "defender-overview",
        "uac-policy", "applocker-policy", "memory-diagnostic-results", "task-journal", "task-plan",
        "task-xml", "task-register", "task-register-recover", "task-inventory", "schedule-preview",
        "foxit-inventory", "mace-review", "mace-package-plan", "mace-package-recover", "tcp-check",
        "gateway-check", "ip-addresses", "wmi-provider", "dns-check", "dns-cache",
        "dns-cache-recover", "dhcp-check", "dhcp-renew", "dhcp-renew-recover", "name-review",
        "name-rename", "name-rename-recover", "storage-check", "storage-repair-plan", "storage-repair-review",
        "storage-repair", "storage-repair-recover", "volume-optimize", "volume-optimize-recover", "win32-code",
        "knowledge", "knowledge-info", "knowledge-set", "knowledge-log", "knowledge-search",
        "driver-reference", "mac-vendor", "pci-vendor", "usb-vendor", "uefi-vendor",
        "bluetooth-company", "printer-check", "printer-list", "printer-repair", "print-job-list",
        "print-job-cancel", "print-job-cancel-recover", "default-printer-set", "default-printer-set-recover", "time-plan",
        "time-verify", "time-recover", "time-configure", "profiles", "orca-review",
        "orca-cache-reset", "orca-cache-reset-recover", "depot-review", "depot-verify-installer", "debloat-review", "debloat-recover",
        "cleanup", "privacy-plan", "privacy-clipboard-history", "privacy-clipboard-history-recover", "privacy-clipboard-history-undo",
        "privacy-setting", "privacy-setting-recover", "privacy-setting-undo", "bitlocker-plan", "bitlocker-protectors",
        "bitlocker-backup", "bitlocker-backup-recover", "bitlocker-resume", "bitlocker-resume-recover", "bitlocker-escrow",
        "bitlocker-protector-add", "bitlocker-protector-recover", "bitlocker-decrypt", "bitlocker-decrypt-recover", "bitlocker-auto-unlock",
        "bitlocker-auto-unlock-recover", "bitlocker-encrypt", "bitlocker-encrypt-recover", "firewall-enable", "firewall-enable-recover",
        "update-plan", "update-recover", "recipes", "workflow", "full-scan",
        "repair", "scan", "open", "investigate", "analyze"
    ];

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length > 0 && args[0] == "capabilities")
        {
            if (args.Length > 2 || (args.Length == 2 && args[1] != "--json"))
            { Console.Error.WriteLine("Usage: capabilities [--json] (supported command names only; no diagnostics or repairs)"); return 2; }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                SchemaVersion = 1,
                Version = typeof(CommandEngine).Assembly.GetName().Version?.ToString() ?? "unknown",
                Commands = CommandNames.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
            }, Capture.JsonOptions));
            return 0;
        }
        WindowsEngine.Register();
        if (args.Length > 0 && args[0] is not ("workflow" or "symptom" or "pack"))
            CapabilityOutput.WriteLabels(WindowsRepairCapabilities.ForCommand(args));
        if (args.Length > 0 && args[0] == "pack") return await PackCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "symptom") return await SymptomCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "validate-native") return await ValidateNativeCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "servicing-monitor") return await ServicingMonitorCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "task-definition") return await TaskDefinitionCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "restore-point" or "restore-point-recover") return await SystemRestoreCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "defender-scan" or "defender-scan-recover") return await DefenderScanCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "diagnostic-scopes")
        {
            if (args.Length > 2 || (args.Length == 2 && args[1] != "--all"))
            { Console.Error.WriteLine("Usage: diagnostic-scopes [--all] (list recommended outcome groups or every read-only diagnostic area)"); return 2; }
            var areas = args.Length == 2 ? CommonDiagnosticCatalog.All : CommonDiagnosticCatalog.SummaryAreas;
            Console.WriteLine(JsonSerializer.Serialize(areas, Capture.JsonOptions));
            return 0;
        }
        if (args.Length > 0 && args[0] == "workflows")
        {
            if (args.Length != 1) { Console.Error.WriteLine("Usage: workflows (list installed workflow definitions; no scan or repair)"); return 2; }
            Console.WriteLine(JsonSerializer.Serialize(WorkflowCatalog.All, Capture.JsonOptions));
            return 0;
        }
        if (args.Length > 0 && args[0] == "workflow-options") return WorkflowOptionCommands.Run(args);
        if (args.Length > 0 && args[0] == "validation-plan") return ValidationPlanCommands.Run(args);
        if (args.Length > 0 && args[0] is "validation-record" or "validation-coverage") return await ValidationEvidenceCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "validation-report") return await ValidationReportCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "workflow-journal") return await WorkflowJournalCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "workflow-report") return await WorkflowReportCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "investigate-batch") return await EvidenceBatchCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "knowledge-compare") return await KnowledgeCompareCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "knowledge-update" or "knowledge-status") return await KnowledgeUpdateCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "knowledge-activate" or "knowledge-active" or "knowledge-recover") return await KnowledgeActivationCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "crash-events") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "crash-review") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "storage-reliability") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "storage-jobs") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "disk-quotas") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "service-triggers" or "service-recovery-policy" or "service-transitions") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "vpn-profiles" or "winrm-listeners") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "physical-disks") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "shadow-storage") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "vss-providers") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "shadow-copies") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "delete-notifications") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "ntfs-self-healing") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "crash-dumps") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "minidump-analyze") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "crash-config") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "installed-apps-compare") return await ApplicationCompareCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "app-folder-review") return ApplicationFolderCommands.Run(args);
        if (args.Length > 0 && args[0] == "startup-apps") return await StartupApplicationCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "browser-extensions") return BrowserExtensionCommands.Run(args);
        if (args.Length > 0 && args[0] == "duplicate-files") return await DuplicateFileCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "large-files") return await LargeFileCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "backup-events") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "backup-summary") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "file-history-status") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "file-history-events") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "dns-cache-summary") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "vss-writers") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "bits-jobs") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "bits-job-review") return await BitsReviewCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "bits-retry" or "bits-retry-recover") return await BitsRetryCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "app-packages" or "app-package-review" or "provisioned-packages" or "installed-apps") return await AppPackageCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "update-search") return await UpdateSearchCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "third-party-updates" or "third-party-update" or "third-party-update-recover")
        {
            if (!ReleaseFeatures.ThirdPartyUpdates) { Console.Error.WriteLine(ReleaseFeatures.ThirdPartyUpdatesNotice); return 2; }
            return await ThirdPartyUpdateCommands.RunAsync(args);
        }
        if (args.Length > 0 && args[0] is "update-store-reset" or "update-store-reset-recover") return await UpdateDataStoreCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "search-index-rebuild" or "search-index-rebuild-recover") return await SearchIndexCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "previous-windows" or "previous-windows-recover") return await PreviousWindowsCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "device-check") return await DeviceCheckCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "device-code") return DeviceProblemCodeCommands.Run(args);
        if (args.Length > 0 && args[0] == "msi-code") return WindowsInstallerCodeCommands.Run(args);
        if (args.Length > 0 && args[0] == "update-code") return WindowsUpdateCodeCommands.Run(args);
        if (args.Length > 0 && args[0] == "update-followup") return await WindowsUpdateCodeCommands.FollowupAsync(args);
        if (args.Length > 0 && args[0] == "setup-code") return WindowsSetupCodeCommands.Run(args);
        if (args.Length > 0 && args[0] is "event-channel-enable" or "event-channel-enable-recover") return await EventChannelCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "ncsi-policy") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "event-channels" or "task-check" or "vitals-check" or "default-printer" or "printer-conditions" or "printer-queue-attributes" or "printer-driver-files" or "print-extension-files" or "printer-driver-models" or "printer-ports" or "printer-endpoint-check" or "security-check" or "network-profiles" or "network-firewall" or "firewall-logging" or "firewall-rules" or "firewall-events" or "firewall-service-events" or "network-service-events" or "storage-service-events" or "backup-service-events" or "feature-service-events" or "platform-service-events" or "infrastructure-service-events" or "dns-client-config" or "dns-client-events" or "dns-nrpt-policy" or "windows-capabilities" or "route-priority" or "ip-interface-config" or "dhcp-leases" or "dhcp-events" or "neighbor-cache" or "adapter-bindings" or "adapter-statistics" or "tcp-port-pressure" or "tcp-settings" or "network-offload" or "winsock-catalog" or "winsock-namespaces" or "winsock-provider-files" or "winsock-readiness" or "pagefile-check" or "reserved-storage" or "storage-sense-policy" or "storage-sense-task" or "windows-update-task" or "disk-cleanup-task" or "disk-diagnostic-tasks" or "disk-diagnostic" or "smb-shares" or "smb-server-config" or "smb-client-config" or "smb-connections" or "font-registrations" or "common-check") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-check") return await ServiceCheckCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-binaries") return await ServiceBinaryCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-path-quoting") return await ServicePathQuotingCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-logons") return await ServiceLogonCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-startup-config") return await ServiceStartupConfigurationCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-dependencies") return await ServiceDependencyConfigurationCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-dlls") return await ServiceDllCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "msi-product" or "msi-repair" or "msi-repair-recover") return await MsiProductCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "installer-registration-repair" or "installer-registration-repair-recover") return await InstallerRegistrationRepairCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "msi-cache") return await MsiCacheCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "msi-sources") return await MsiSourceCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "installer-check" or "installer-engine-files" or "installer-service-registration" or "installer-security-policy" or "installer-logging" or "installer-activity" or "installer-pending-state" or "installer-events" or "installer-service-events" or "print-events" or "print-operational-events" or "print-spooler-events" or "print-policy" or "print-rendering-policy" or "volume-capacity" or "volume-dirty" or "proxy-check" or "servicing-logs" or "servicing-service-states" or "servicing-service-events" or "update-events" or "update-engine-files" or "update-service-files" or "update-service-events" or "time-events" or "time-config" or "application-events" or "delivery-optimization" or "delivery-optimization-activity" or "delivery-optimization-policy" or "update-cache" or "update-policy" or "update-policy-surfaces" or "update-restart-policy" or "feature-update-policy" or "component-repair-source" or "update-services" or "update-history" or "installed-hotfixes" or "update-context" or "update-applicability" or "restart-check" or "pending-file-operations" or "certificate-chains" or "licensing-check" or "defender-threats" or "defender-scan-history" or "defender-protection" or "defender-overview" or "uac-policy" or "applocker-policy" or "memory-diagnostic-results") return await DiagnosticCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "task-journal") return await TaskJournalCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "task-plan") return await TaskPlanCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "task-xml") return await TaskPlanCommands.ExportXmlAsync(args);
        if (args.Length > 0 && args[0] == "task-register") return await TaskPlanCommands.RegisterAsync(args);
        if (args.Length > 0 && args[0] == "task-register-recover") return await TaskPlanCommands.RecoverRegistrationAsync(args);
        if (args.Length > 0 && args[0] == "task-inventory") return await TaskCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "schedule-preview") return ScheduleCommands.Run(args);
        if (args.Length > 0 && args[0] == "foxit-inventory") return await FoxitCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "mace-review") return await MaceCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "mace-package-plan") return await MaceCommands.RunPackagePlanAsync(args);
        if (args.Length > 0 && args[0] == "mace-package-recover") return await MaceCommands.RecoverPackageRemovalAsync(args);
        if (args.Length > 0 && args[0] == "tcp-check") return await TcpCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "gateway-check") return await GatewayCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "ip-addresses") return await IpAddressCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "service-events") return await ServiceCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "storage-events") return await StorageEventCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "wmi-provider") return await WmiProviderCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "printer-drivers") return await PrinterDriverCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "dns-check") return await DnsCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "dns-cache" or "dns-cache-recover") return await DnsCacheCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "dhcp-check" or "dhcp-renew" or "dhcp-renew-recover") return await DhcpCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "name-review" or "name-rename" or "name-rename-recover") return await ComputerNameCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "storage-check" or "storage-repair-plan" or "storage-repair-review" or "storage-repair" or "storage-repair-recover" or "volume-optimize" or "volume-optimize-recover") return await StorageCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "win32-code") return WindowsErrorCommands.Run(args);
        if (args.Length > 0 && args[0] is "knowledge" or "knowledge-info" or "knowledge-set" or "knowledge-log" or "knowledge-search" or "driver-reference" or "mac-vendor" or "pci-vendor" or "usb-vendor" or "uefi-vendor" or "bluetooth-company") return await KnowledgeCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "printer-check" or "printer-list" or "printer-repair" or
            "print-job-list" or "print-job-cancel" or "print-job-cancel-recover" or
            "default-printer-set" or "default-printer-set-recover") return await PrinterCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "time-plan" or "time-verify" or "time-recover" or "time-configure") return await TimeCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "profiles") return await ProfileCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "orca-review" or "orca-cache-reset" or "orca-cache-reset-recover") return await OrcaCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "depot-review") return await DepotCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "depot-verify-installer") return await DepotInstallerCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "debloat-review") return await DebloatCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "debloat-recover") return await DebloatRecoveryCommands.RunAsync(args);

        if (args.Length > 0 && args[0] == "cleanup") return CleanupCommands.Run(args);
        if (args.Length > 0 && args[0] == "privacy-plan") return PrivacyCommands.Run(args);
        if (args.Length > 0 && args[0] is "privacy-clipboard-history" or "privacy-clipboard-history-recover" or "privacy-clipboard-history-undo" or "privacy-setting" or "privacy-setting-recover" or "privacy-setting-undo") return await PrivacyChangeCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "bitlocker-plan") return await BitLockerCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "bitlocker-protectors" or "bitlocker-backup" or "bitlocker-backup-recover") return await BitLockerBackupCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "bitlocker-resume" or "bitlocker-resume-recover") return await BitLockerProtectionCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "bitlocker-escrow") return await BitLockerEscrowCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "bitlocker-protector-add" or "bitlocker-protector-recover") return await BitLockerProtectorCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "bitlocker-decrypt" or "bitlocker-decrypt-recover") return await BitLockerDecryptionCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "bitlocker-auto-unlock" or "bitlocker-auto-unlock-recover") return await BitLockerAutoUnlockCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "bitlocker-encrypt" or "bitlocker-encrypt-recover") return await BitLockerEncryptionCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "firewall-enable" or "firewall-enable-recover") return await FirewallProfileCommands.RunAsync(args);
        if (args.Length > 0 && args[0] is "update-plan" or "update-recover") return await UpdateCommands.RunAsync(args);

        if (args.Length > 0 && args[0] == "recipes")
        {
            var workflow = args.Length == 2 ? WorkflowCatalog.Find(args[1]) : null;
            if (args.Length != 1 && (args.Length != 2 || workflow is null))
            {
                Console.Error.WriteLine("Usage: recipes [WORKFLOW] (definitions only; no scan or repair)");
                return 2;
            }
            var recipes = workflow is null ? RecipeCatalog.All : RecipeCatalog.ForWorkflow(workflow.Kind);
            var nodes = JsonSerializer.SerializeToNode(recipes, Capture.JsonOptions)!.AsArray();
            foreach (var node in nodes)
                if (node is not null && WindowsRepairCapabilities.Find(node["id"]!.GetValue<string>()) is { } capability)
                    node["nativeCapability"] = JsonSerializer.SerializeToNode(capability, Capture.JsonOptions);
            Console.WriteLine(nodes.ToJsonString(Capture.JsonOptions));
            return 0;
        }
        if (args.Length > 0 && args[0] == "workflow") return await WorkflowCommands.RunAsync(args);
        if (args.Length > 0 && args[0] == "full-scan") return await WorkflowCommands.RunAsync(["workflow", "common-problems", .. args.Skip(1)]);
        if (args.Length > 0 && args[0] == "repair") return await RepairCommands.RunAsync(args);

        if (args.Length > 0 && args[0] is "scan" or "open") return await CaptureCommands.RunAsync(args);

        if (args.Length == 0 || args is ["help"] or ["--help"] or ["-h"])
        {
            Console.WriteLine("KillerBench command-line Windows maintenance workbench. Diagnostics are read-only by default; every machine change requires --execute and its command-specific safeguards.");
            Console.WriteLine("killerbench workflow-options [WORKFLOW [--set KEY=VALUE ...]] (show or validate host-neutral controls; no scan or repair)");
            Console.WriteLine("killerbench backup-events (VSS and volsnap history; JSON evidence; no changes)");
            Console.WriteLine("killerbench servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task] (read-only Servicing activity snapshots)");
            Console.WriteLine("killerbench setup-code DECIMAL_OR_0xHEX (documented Windows Setup result-code reference; no machine inspection or repair)");
            Console.WriteLine("killerbench backup-summary (local Windows Backup operation summary; no backup changes)");
            Console.WriteLine("killerbench file-history-status (current-account File History protection state; no changes)");
            Console.WriteLine("killerbench file-history-events (privacy-projected File History warning and error counts; no changes)");
            Console.WriteLine("killerbench crash-dumps (default Windows dump-file metadata; no content analysis or changes)");
            Console.WriteLine("killerbench minidump-analyze PATH (bounded structural minidump metadata; no memory pages, symbols or changes)");
            Console.WriteLine("killerbench crash-config (stored Windows dump mode and paths; no changes)");
            Console.WriteLine("killerbench crash-events (recent system error reports; no diagnosis or changes)");
            Console.WriteLine("killerbench crash-review [--knowledge PACK_PATH ...] (up to 32 packs; combined crash, shutdown, bounded minidump structure and optional exact bug-check reference review; no changes)");
            Console.WriteLine("killerbench vss-writers (reported writer states; English output interpretation; no reset)");
            Console.WriteLine("killerbench bits-jobs [--all-users] (BITS job states, owners and byte counts; no transfer changes)");
            Console.WriteLine("killerbench bits-job-review JOB_GUID [--all-users] [--observe] (exact transfer review or two-snapshot observation; no retry)");
            Console.WriteLine("killerbench bits-retry JOB_GUID [--execute] (preview by default; explicit exact-job retry, no application-completion claim)");
            Console.WriteLine("killerbench bits-retry-recover JOB_GUID (two read-only snapshots; archives stable outcomes without another retry)");
            Console.WriteLine("killerbench update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID] (JSON discovery and matches; no installation)");
            if (ReleaseFeatures.ThirdPartyUpdates)
            {
                Console.WriteLine("killerbench third-party-updates (read-only Microsoft.WinGet.Client application update discovery; no application changes)");
                Console.WriteLine("killerbench third-party-update ID SOURCE VERSION [--accept-agreements] [--execute] (preview by default; one exact version only)");
                Console.WriteLine("killerbench third-party-update-recover (verify a pending exact version without retrying the update)");
            }
            Console.WriteLine("killerbench update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute] (fresh-search gated; preview by default; retains the old store)");
            Console.WriteLine("killerbench update-store-reset-recover (reconcile a prepared store reset without repeating it)");
            Console.WriteLine("killerbench search-index-rebuild [--execute] (preview by default; retains the prior index and verifies fresh state)");
            Console.WriteLine("killerbench search-index-rebuild-recover (read-only reconciliation without repeating repair commands)");
            Console.WriteLine("killerbench previous-windows [--execute --backup-confirmed] (preview by default; permanently removes Windows rollback capability through DISM)");
            Console.WriteLine("killerbench previous-windows-recover (read-only reconciliation without repeating rollback removal)");
            Console.WriteLine("killerbench component-repair-source (managed DISM repair-source policy and conflicts; no changes)");
            Console.WriteLine("killerbench task-inventory (read-only task identities and states; JSON output)");
            Console.WriteLine("killerbench task-definition TASK_FOLDER TASK_NAME (read-only exact task XML; unavailable does not mean absent)");
            Console.WriteLine("killerbench task-journal RECORD.json [--completion COMPLETION.json] (historical recovery review; never authorizes retry)");
            Console.WriteLine("killerbench workflow-journal FILE.json (historical contents; exit 0 means read successfully, not healthy)");
            Console.WriteLine("killerbench workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite] (historical report only; no commands run)");
            Console.WriteLine("killerbench task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature] (validate and review a task draft; no registration)");
            Console.WriteLine("killerbench task-xml PLAN.json YYYY-MM-DD (disabled review XML to stdout; local-zone calendar schedules only; no registration)");
            Console.WriteLine("killerbench task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt] (preview by default; disabled create-only Local System definition)");
            Console.WriteLine("killerbench task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt] (read-only exact-definition reconciliation; never retries or runs a task)");
            Console.WriteLine("killerbench storage-reliability (read-only temperature, wear and I/O counters; JSON; no repair)");
            Console.WriteLine("killerbench storage-jobs (read-only aggregate Storage module job state and progress; JSON; no changes)");
            Console.WriteLine("killerbench disk-quotas (read-only per-volume quota defaults and state; user identities excluded; no changes)");
            Console.WriteLine("killerbench physical-disks (read-only current health, operational, media, bus and usage state; JSON; no repair)");
            Console.WriteLine("killerbench shadow-storage (read-only VSS differential-area allocation and pressure; JSON; no resize or snapshot action)");
            Console.WriteLine("killerbench vss-providers (read-only VSS provider names, versions and types; identifiers excluded; no registration changes)");
            Console.WriteLine("killerbench shadow-copies (read-only current VSS snapshot states and attributes; identities excluded; no snapshot changes)");
            Console.WriteLine("killerbench delete-notifications (read-only NTFS and ReFS delete-notification configuration; JSON; no optimization or setting change)");
            Console.WriteLine("killerbench ntfs-self-healing DRIVE: (read-only selected-volume online NTFS repair configuration; no repair or setting change)");
            Console.WriteLine("killerbench schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last] (preview only)");
            Console.WriteLine("killerbench foxit-inventory (read-only machine product inventory; JSON output)");
            Console.WriteLine("killerbench mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json] (review only; no removal)");
            Console.WriteLine("killerbench mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json] (fresh exact eligibility preview; no removal provider)");
            Console.WriteLine("killerbench mace-package-recover (freshly reconcile one interrupted app package-removal record; never repeats removal)");
            Console.WriteLine("killerbench <analyze|investigate> <text-log-path> [--json] (analyze local text evidence)");
            Console.WriteLine("killerbench investigate-batch FILE [FILE ...] (up to 12 files; JSON Lines; exit 3 for failed or partial imports)");
            Console.WriteLine("killerbench app-packages [--all-users] (registered Appx/MSIX inventory only; no removal)");
            Console.WriteLine("killerbench provisioned-packages (packages staged for new users in local Windows; no removal)");
            Console.WriteLine("killerbench installed-apps [--current-user] (uninstall registrations and capability flags; no removal)");
            Console.WriteLine("killerbench installed-apps-compare BEFORE.json AFTER.json (saved inventory registration differences; no machine changes)");
            Console.WriteLine("killerbench app-folder-review FOLDER_NAME [--report] (fixed-root exact folder review; no deletion)");
            Console.WriteLine("killerbench startup-apps (startup registrations and boot or logon task review; no disablement)");
            Console.WriteLine("killerbench browser-extensions (Chrome, Edge, Brave and Firefox extension identity review; no removal)");
            Console.WriteLine("killerbench workflow app-packages [--all-users] [--include-provisioned] [--json] (inventory only)");
            Console.WriteLine("killerbench app-package-review FULL_NAME [--all-users] (exact target and dependent packages; no removal)");
            Console.WriteLine("investigate also accepts a saved .evtx archive (Windows only; newest 200 records, no date filter).");
            Console.WriteLine("investigate accepts exported .xml Events documents (first 200 records in file order; no date filter).");
            Console.WriteLine("killerbench knowledge PACK_PATH CONTEXT CODE (local reference lookup; no repair execution)");
            Console.WriteLine("killerbench knowledge-info PACK_PATH (pack metadata and per-context entry counts; no changes)");
            Console.WriteLine("killerbench knowledge-compare BEFORE_JSON AFTER_JSON (reference changes only; no pack installation)");
            Console.WriteLine("killerbench knowledge-status CONFIG_JSON STORAGE_ROOT (verify every configured installed database; no download or activation)");
            Console.WriteLine("killerbench knowledge-update CONFIG_JSON STORAGE_ROOT (fetch and atomically activate signed database candidates from configured HTTPS endpoints)");
            Console.WriteLine("killerbench knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY (verified local activation; no download)");
            Console.WriteLine("killerbench knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION (verify active local pack; no changes)");
            Console.WriteLine("killerbench knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION (select newest verified retained pack; no download)");
            Console.WriteLine("killerbench knowledge-search PACK_PATH CONTEXT TEXT (up to 50 reference text matches; no repair execution)");
            Console.WriteLine("killerbench knowledge-log PACK_PATH CONTEXT LOG_PATH (attach contextual references to observed codes)");
            Console.WriteLine("killerbench knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...] (resolve one exact identity across up to 32 packs; conflicts are never selected)");
            Console.WriteLine("killerbench driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD (reference candidates only)");
            Console.WriteLine("killerbench mac-vendor PACK_PATH MAC_ADDRESS (longest-prefix local pack lookup; no network request)");
            Console.WriteLine("killerbench pci-vendor PACK_PATH PCI_HARDWARE_ID (exact local pack lookup; no network request or driver selection)");
            Console.WriteLine("killerbench usb-vendor PACK_PATH USB_HARDWARE_ID (exact local pack lookup; no network request or driver selection)");
            Console.WriteLine("killerbench uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID (exact local UEFI registry lookup; no network request or driver selection)");
            Console.WriteLine("killerbench bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID (exact local Bluetooth SIG assignment lookup; no network request)");
            Console.WriteLine("killerbench win32-code DECIMAL_OR_0xHEX (offline installed-Windows message lookup)");
            Console.WriteLine("killerbench device-code DECIMAL_OR_0xHEX (offline Device Manager problem-code reference)");
            Console.WriteLine("killerbench recipes [WORKFLOW] (list installed definitions or workflow-specific dependencies and limits; no execution)");
            Console.WriteLine("killerbench validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW] (read-only disposable-VM scenario plan; no diagnostics or repairs)");
            Console.WriteLine("killerbench validation-record RECORD.json (validate one bounded native VM evidence record; no diagnostics or repairs)");
            Console.WriteLine("killerbench validation-coverage RECORD.json [...] (summarize current-version native VM evidence; no diagnostics or repairs)");
            Console.WriteLine("killerbench validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite] (local current-version VM coverage report)");
            Console.WriteLine("killerbench workflows (list installed workflow keys and titles; no scan or repair)");
            Console.WriteLine("killerbench capabilities [--json] (versioned command discovery; no diagnostics or repairs)");
            Console.WriteLine("killerbench dhcp-check (read-only missing or expired DHCP lease eligibility; JSON evidence, no renewal)");
            Console.WriteLine("killerbench dhcp-renew [--execute] (preview by default; exact missing or expired connected physical DHCP leases only)");
            Console.WriteLine("killerbench dhcp-renew-recover (read-only reconciliation without requesting another lease)");
            Console.WriteLine("killerbench dns-cache-summary (privacy-bounded local DNS cache status and record-type counts; no changes)");
            Console.WriteLine("killerbench service-check NAME [NAME ...] | service-check --all (read-only supported service states and dependency evidence)");
            Console.WriteLine("killerbench service-binaries (read-only local service binary presence and Authenticode status; paths excluded and nonlocal paths not contacted)");
            Console.WriteLine("killerbench service-path-quoting (read-only service executable path quoting review; full paths and arguments excluded)");
            Console.WriteLine("killerbench service-logons (read-only service account-class and desktop-interaction review; account identities excluded)");
            Console.WriteLine("killerbench service-startup-config (read-only startup-mode coherence review; no service changes)");
            Console.WriteLine("killerbench service-dependencies (read-only direct dependency integrity review; no service changes)");
            Console.WriteLine("killerbench service-dlls (read-only registered service DLL presence and Authenticode status; paths excluded and nonlocal paths not contacted)");
            Console.WriteLine("killerbench volume-capacity DRIVE: (read-only available space with JSON evidence)");
            Console.WriteLine("killerbench volume-dirty DRIVE: (read-only dirty-bit status; no repair or restart)");
            Console.WriteLine("killerbench proxy-check (read-only stored WinHTTP and current-account Internet proxy evidence)");
            Console.WriteLine("killerbench orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json] (review only; no Outlook changes)");
            Console.WriteLine("killerbench orca-cache-reset --mode New|Classic|Both [--execute] (preview by default; retains exact running-account caches under recovery names)");
            Console.WriteLine("killerbench orca-cache-reset-recover (verify interrupted cache retention without repeating a move)");
            Console.WriteLine("killerbench depot-review [application and configuration selections] [--json] (review only; no provisioning)");
            Console.WriteLine("killerbench depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH (read-only Authenticode and vendor-signer check; nothing is run)");
            Console.WriteLine("killerbench debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json] (review only; no system changes)");
            Console.WriteLine("killerbench debloat-recover power-plan|power-timers|hibernation --folder PATH [--json] (reconcile pending evidence without repeating changes; resolved local records may be archived)");
            Console.WriteLine("killerbench delivery-optimization (read-only Delivery Optimization service state; JSON evidence)");
            Console.WriteLine("killerbench delivery-optimization-activity (privacy-bounded current job counts and byte totals; no transfer identities or changes)");
            Console.WriteLine("killerbench delivery-optimization-policy (read-only classic and Policy CSP settings; sensitive values and provider identities excluded)");
            Console.WriteLine("killerbench update-cache (bounded read-only SoftwareDistribution cache totals; no filenames)");
            Console.WriteLine("killerbench servicing-logs (bounded newest CBS, plain-text and compressed CBS archives, DISM and Windows Setup log analysis; no repair)");
            Console.WriteLine("killerbench update-policy (read-only managed Windows Update policy; JSON evidence)");
            Console.WriteLine("killerbench update-policy-surfaces (read-only classic and Policy CSP configuration-surface evidence; values and provider identities excluded)");
            Console.WriteLine("killerbench update-restart-policy (read-only compliance deadlines, grace periods, restart suppression and notification behavior)");
            Console.WriteLine("killerbench feature-update-policy (read-only target release, deferral, pause and safeguard policy; JSON evidence)");
            Console.WriteLine("killerbench update-engine-files (read-only native Windows Update engine presence, version and signature evidence)");
            Console.WriteLine("killerbench update-service-files (read-only exact Windows Update service DLL registration and signature evidence)");
            Console.WriteLine("killerbench update-service-events (read-only exact Windows Update service failure history)");
            Console.WriteLine("killerbench servicing-service-events (read-only exact Windows Update servicing-chain failure histories)");
            Console.WriteLine("killerbench servicing-service-states (read-only current state for the six-service Windows Update servicing chain)");
            Console.WriteLine("killerbench update-services (read-only Windows Update service registrations; JSON evidence)");
            Console.WriteLine("killerbench update-history (read-only newest 100 Windows Update operation outcomes; JSON evidence)");
            Console.WriteLine("killerbench installed-hotfixes (read-only bounded installed Windows hotfix inventory; JSON evidence)");
            Console.WriteLine("killerbench memory-diagnostic-results (read-only bounded Windows Memory Diagnostic history; JSON evidence)");
            Console.WriteLine("killerbench update-context (read-only current elevation and desktop-session evidence; no account identifiers)");
            Console.WriteLine("killerbench update-applicability (read-only Windows edition, build and architecture evidence)");
            Console.WriteLine("killerbench restart-check (read-only pending restart sources; JSON evidence)");
            Console.WriteLine("killerbench pending-file-operations (read-only privacy-bounded pending file operation summary; JSON evidence)");
            Console.WriteLine("killerbench installer-pending-state (read-only pending file operation and Installer activity correlation; JSON evidence)");
            Console.WriteLine("killerbench certificate-chains (read-only local cache-only chain trust for personal certificates; no revocation or downloads)");
            Console.WriteLine("killerbench licensing-check (read-only Windows activation state without product keys; JSON evidence)");
            Console.WriteLine("killerbench service-events | storage-events | time-events (read-only recent event history with JSON evidence)");
            Console.WriteLine("killerbench time-config (read-only Windows Time mode, peers and policy evidence)");
            Console.WriteLine("killerbench application-events (read-only recent Application errors, grouped counts and JSON evidence)");
            Console.WriteLine("killerbench event-channels (read-only state, retention, size and saturation for four diagnostic channels)");
            Console.WriteLine("killerbench event-channel-enable CHANNEL [--execute] (preview by default; enable one exact unmanaged diagnostic channel and preserve other settings)");
            Console.WriteLine("killerbench event-channel-enable-recover (read-only reconciliation without repeating the enable command)");
            Console.WriteLine("killerbench task-check (read-only task inventory, classified findings and JSON evidence)");
            Console.WriteLine("killerbench security-check (read-only TPM, Secure Boot, virtualization protections and UAC policy; JSON evidence)");
            Console.WriteLine("killerbench uac-policy (read-only UAC enablement and elevation-prompt policy; no account or credential data)");
            Console.WriteLine("killerbench network-profiles (read-only current profile categories and connectivity classifications; network names excluded)");
            Console.WriteLine("killerbench ncsi-policy (read-only Windows connectivity classification policy; probe destinations excluded)");
            Console.WriteLine("killerbench network-firewall (correlate active network assignments with matching firewall profile enablement)");
            Console.WriteLine("killerbench firewall-logging (read Domain, Private and Public traffic logging configuration without opening logs)");
            Console.WriteLine("killerbench firewall-rules (read privacy-bounded effective rule and policy-source counts)");
            Console.WriteLine("killerbench firewall-events (read bounded Windows Firewall warning, error and critical operational history)");
            Console.WriteLine("killerbench firewall-service-events (read exact BFE and Windows Defender Firewall Service Control Manager failure history)");
            Console.WriteLine("killerbench network-service-events (read exact DHCP, DNS, Workstation, NLA, Network List and IP Helper failure history)");
            Console.WriteLine("killerbench storage-service-events (read exact Storage Service, Optimize Drives, VSS and software shadow provider failure history)");
            Console.WriteLine("killerbench backup-service-events (read exact VSS, software shadow provider, Windows Backup and block-level engine failure history)");
            Console.WriteLine("killerbench feature-service-events (read exact Time, Search, WMI, Event Log and audio service failure history)");
            Console.WriteLine("killerbench platform-service-events (read one bounded exact application, identity, profile and device-support service failure snapshot)");
            Console.WriteLine("killerbench infrastructure-service-events (read one bounded exact connectivity, remote-management, notification and core service failure snapshot)");
            Console.WriteLine("killerbench smb-shares (read-only bounded SMB share inventory; paths, permissions, sessions and accounts excluded)");
            Console.WriteLine("killerbench smb-server-config (read-only SMB protocol, signing, encryption and multichannel configuration)");
            Console.WriteLine("killerbench smb-client-config (read-only SMB signing, guest-access and transport configuration)");
            Console.WriteLine("killerbench smb-connections (read-only active protocol negotiation; remote identities excluded)");
            Console.WriteLine("killerbench font-registrations (read-only bounded machine and current-user registration and file-presence check)");
            Console.WriteLine("killerbench defender-threats [--report] (aggregate threat history, current active flags and severity IDs; sensitive details excluded; no action)");
            Console.WriteLine("killerbench defender-overview [--report] (read-only protection, scan timing and aggregate threat overview; no action)");
            Console.WriteLine("killerbench defender-scan-history [--report] (read-only quick and full scan timing and overdue status; sensitive details excluded; no scan)");
            Console.WriteLine("killerbench defender-scan Quick|Full [--execute] [--report] (preview by default; one explicit guarded Microsoft Defender scan)");
            Console.WriteLine("killerbench defender-scan-recover [--report] (read-only reconciliation without starting another scan)");
            Console.WriteLine("killerbench defender-protection [--report] (read-only protection preferences and recognized policy surfaces; sensitive details excluded; no changes)");
            Console.WriteLine("killerbench applocker-policy (read-only effective collection modes and aggregate rule counts; rule details excluded; no changes)");
            Console.WriteLine("killerbench common-check [--scope NAME ...] (all diagnostic areas by default; selected areas only when specified; read-only JSON)");
            Console.WriteLine("killerbench service-triggers (read-only installed-service trigger summary; trigger payloads excluded; no changes)");
            Console.WriteLine("killerbench service-recovery-policy (read-only installed-service recovery action summary; commands and reboot messages excluded)");
            Console.WriteLine("killerbench service-transitions (read-only repeated observation of pending service states; no service action)");
            Console.WriteLine("killerbench vpn-profiles (read-only current-account VPN profile configuration; credentials excluded)");
            Console.WriteLine("killerbench winrm-listeners (read-only WinRM listener registration summary; certificate thumbprints excluded)");
            Console.WriteLine("killerbench diagnostic-scopes [--all] (list 11 recommended outcome groups, or all 49 read-only areas; no collection)");
            Console.WriteLine("killerbench vitals-check (read-only Windows, hardware, account, resource, battery and physical-disk snapshots; JSON evidence)");
            Console.WriteLine("killerbench printer-drivers (read-only queue/driver registration matching; JSON evidence)");
            Console.WriteLine("killerbench default-printer (read-only default selection and automatic-management setting; JSON evidence)");
            Console.WriteLine("killerbench printer-conditions (read-only local queue condition codes; no physical verification or changes)");
            Console.WriteLine("killerbench printer-queue-attributes (read-only local queue spooling, retention and sharing attributes)");
            Console.WriteLine("killerbench printer-driver-files (read-only registered file presence and executable signature summary; full paths excluded)");
            Console.WriteLine("killerbench print-extension-files (read-only print processor and monitor binary presence and signature summary; full paths excluded)");
            Console.WriteLine("killerbench printer-driver-models (read-only version 3, version 4 and package-awareness summary; JSON evidence)");
            Console.WriteLine("killerbench printer-ports (read-only queue/port registration matching; JSON evidence)");
            Console.WriteLine("killerbench printer-endpoint-check (bounded connections to exact assigned Standard TCP/IP printer endpoints; no discovery, SNMP or print data)");
            Console.WriteLine("killerbench route-priority (read-only effective default-route metric comparison; JSON evidence)");
            Console.WriteLine("killerbench adapter-bindings (read-only core protocol binding summary; JSON evidence)");
            Console.WriteLine("killerbench adapter-statistics (read-only adapter byte, error and discard counters; JSON evidence)");
            Console.WriteLine("killerbench tcp-port-pressure (read-only dynamic TCP ranges and aggregate local-port usage; endpoints and processes excluded)");
            Console.WriteLine("killerbench pagefile-check (read-only page-file management, allocation and usage summary; full paths excluded)");
            Console.WriteLine("killerbench reserved-storage (read-only online Windows reserved-storage state; JSON evidence)");
            Console.WriteLine("killerbench storage-sense-policy (read-only recognized machine cleanup policy; user settings and file identities excluded)");
            Console.WriteLine("killerbench storage-sense-task (read-only exact task state and runtime history; no task execution or cleanup)");
            Console.WriteLine("killerbench windows-update-task (read-only exact Scheduled Start task state and runtime history; no task execution or update operation)");
            Console.WriteLine("killerbench disk-cleanup-task (read-only exact task state and runtime history; no task execution or cleanup)");
            Console.WriteLine("killerbench disk-diagnostic-tasks (read-only exact collector and resolver task history; no task or disk action)");
            Console.WriteLine("killerbench disk-diagnostic (correlate exact disk diagnostic tasks and event history; no task or disk action)");
            Console.WriteLine("killerbench device-check <CATEGORY> [--knowledge PACK_PATH ...] (read-only device evidence with optional exact driver references from up to 32 packs)");
            Console.WriteLine("killerbench installer-check (read-only Installer policy and engine readiness; JSON evidence)");
            Console.WriteLine("killerbench msi-code NONNEGATIVE_DECIMAL (documented Windows Installer return-code reference; no machine inspection or repair)");
            Console.WriteLine("killerbench update-code DECIMAL_OR_0xHEX (documented Windows Update result-code reference; no machine inspection or repair)");
            Console.WriteLine("killerbench update-followup DECIMAL_OR_0xHEX (fresh read-only diagnostics selected by a documented Windows Update result code)");
            Console.WriteLine("killerbench installer-engine-files (read-only required Installer file presence and Authenticode summary; JSON evidence)");
            Console.WriteLine("killerbench installer-service-registration (read-only Windows Installer service registration summary; JSON evidence)");
            Console.WriteLine("killerbench installer-registration-repair [--execute] (preview by default; exact service-registration defect only; never unregisters the engine)");
            Console.WriteLine("killerbench installer-registration-repair-recover (recheck interrupted registration without repeating commands)");
            Console.WriteLine("killerbench installer-security-policy (read-only Installer security and patch policy; JSON evidence)");
            Console.WriteLine("killerbench installer-logging (read-only Installer logging and debug policy; JSON evidence)");
            Console.WriteLine("killerbench installer-activity (read-only Installer process and transaction-marker evidence; JSON evidence)");
            Console.WriteLine("killerbench msi-product {PRODUCT-CODE} (read-only exact MSI registration state; JSON evidence)");
            Console.WriteLine("killerbench msi-repair {PRODUCT-CODE} [--execute] (preview by default; exact broken or bad-configuration product only)");
            Console.WriteLine("killerbench msi-repair-recover (recheck an interrupted exact-product repair without repeating it)");
            Console.WriteLine("killerbench msi-cache (read-only registered Windows Installer cache visibility; JSON evidence)");
            Console.WriteLine("killerbench msi-sources (read-only privacy-bounded registered Windows Installer source metadata; JSON evidence)");
            Console.WriteLine("killerbench installer-events (read-only MSI history from the last seven days; JSON evidence)");
            Console.WriteLine("killerbench installer-service-events (read-only Service Control Manager failures tied to the local Windows Installer identity; JSON evidence)");
            Console.WriteLine("killerbench print-events (read-only PrintService Admin history from the last seven days; JSON evidence)");
            Console.WriteLine("killerbench print-operational-events (read-only PrintService Operational channel state and available history; JSON evidence)");
            Console.WriteLine("killerbench print-spooler-events (read-only Service Control Manager failures tied to the local Print Spooler identity; JSON evidence)");
            Console.WriteLine("killerbench print-policy (read-only Point and Print restrictions and driver-installation policy; JSON evidence)");
            Console.WriteLine("killerbench print-rendering-policy (read-only rendering and driver-isolation policy; JSON evidence)");
            Console.WriteLine("killerbench printer-drivers (read-only queue/driver registration inventory; JSON evidence)");
            Console.WriteLine("killerbench dns-check HOST [--timeout-seconds 1..60] (bounded system name resolution; JSON result)");
            Console.WriteLine("killerbench dns-client-config (read-only suffix, registration, devolution and policy evidence; JSON result)");
            Console.WriteLine("killerbench dns-client-events (read-only privacy-bounded DNS Client warning and failure history; JSON result)");
            Console.WriteLine("killerbench dns-nrpt-policy (read-only effective DNS namespace policy evidence; JSON result)");
            Console.WriteLine("killerbench ip-interface-config (read-only IP interface configuration evidence; JSON result)");
            Console.WriteLine("killerbench dhcp-leases (read-only lease timestamp, expiry and server evidence; JSON result)");
            Console.WriteLine("killerbench dhcp-events (read-only IPv4 and IPv6 DHCP Client event history; JSON result)");
            Console.WriteLine("killerbench neighbor-cache (read-only privacy-bounded neighbor reachability evidence; JSON result)");
            Console.WriteLine("killerbench tcp-settings (read-only Windows TCP template configuration evidence; JSON result)");
            Console.WriteLine("killerbench network-offload (read-only global Windows network offload configuration; JSON result)");
            Console.WriteLine("killerbench winsock-catalog (read-only native Winsock protocol catalog structure; JSON result)");
            Console.WriteLine("killerbench winsock-namespaces (read-only native Winsock namespace-provider structure; JSON result)");
            Console.WriteLine("killerbench winsock-provider-files (read-only protocol provider file presence and signatures; JSON result)");
            Console.WriteLine("killerbench winsock-readiness (read-only native Winsock protocol, namespace-provider and provider-file readiness; JSON results)");
            Console.WriteLine("killerbench windows-capabilities (read-only online Windows capability states; JSON result)");
            Console.WriteLine("killerbench dns-cache HOST [--execute] (preview by default; guarded local cache flush with exact-host verification)");
            Console.WriteLine("killerbench dns-cache-recover (recheck an interrupted cache operation without repeating the flush)");
            Console.WriteLine("killerbench tcp-check HOST PORT [--timeout-seconds 1..60] (bounded connection check; no application data sent)");
            Console.WriteLine("killerbench gateway-check (bounded ICMP checks of exact configured default next hops; no address scan)");
            Console.WriteLine("killerbench ip-addresses (read-only current IP address state, origin and conflict review; JSON evidence)");
            Console.WriteLine("killerbench service-events (read-only service failure history; JSON including raw evidence)");
            Console.WriteLine("killerbench storage-events (read-only storage history; JSON including raw evidence)");
            Console.WriteLine("killerbench update-events (read-only Windows Update Client history from the last seven days; JSON including raw evidence)");
            Console.WriteLine("killerbench wmi-provider (read-only local operating-system provider query; JSON evidence)");
            Console.WriteLine("killerbench workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json] (optional selected-target diagnosis included in the journal)");
            Console.WriteLine("killerbench workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json] (optional exact product state included in the journal)");
            Console.WriteLine("killerbench workflow storage [--volume D:] [--json] (journaled read-only filesystem check)");
            Console.WriteLine("killerbench name-review PROPOSED_NAME (read-only identity and proposed-name review)");
            Console.WriteLine("killerbench name-rename PROPOSED_NAME [--execute] (preview by default; standalone workgroup PCs only; never restarts Windows)");
            Console.WriteLine("killerbench name-rename-recover (verify a pending rename without repeating it)");
            Console.WriteLine("killerbench workflow computer-name --name PROPOSED_NAME [--json] (journaled review only)");
            Console.WriteLine("killerbench storage-check DRIVE_LETTER (read-only fixed NTFS check; JSON results and live progress)");
            Console.WriteLine("killerbench storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] (preview only; no repair or scheduling)");
            Console.WriteLine("killerbench storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] (collect and bind current evidence; no repair or scheduling)");
            Console.WriteLine("killerbench storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute] (preview by default; execution requires backup confirmation; system volumes schedule at restart)");
            Console.WriteLine("killerbench storage-repair-recover (verify a completed or scheduled filesystem repair without repeating it)");
            Console.WriteLine("killerbench volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute] (read-only analysis by default; exact healthy volume and compatible media required)");
            Console.WriteLine("killerbench volume-optimize-recover (verify a pending completed operation without repeating it)");
            Console.WriteLine("killerbench restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute] (preview by default; creates one point after fresh checks)");
            Console.WriteLine("killerbench restore-point-recover (verify a pending request without creating another point)");
            Console.WriteLine("killerbench printer-list (discover local queues without changing settings)");
            Console.WriteLine("killerbench printer-repair --queue NAME [--queue NAME ...] [--execute] (preview by default; clearing offline mode can release waiting jobs)");
            Console.WriteLine("killerbench printer-check --queue NAME [--queue NAME ...] (read-only selected local queue settings)");
            Console.WriteLine("killerbench print-job-list (list current visible jobs without changing them)");
            Console.WriteLine("killerbench print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH] (preview by default; exact Error or Blocked job only)");
            Console.WriteLine("killerbench print-job-cancel-recover [--folder PATH] (recheck an interrupted cancellation without repeating it)");
            Console.WriteLine("killerbench default-printer-set QUEUE [--execute] [--folder PATH] (preview by default; missing or conflicting default only)");
            Console.WriteLine("killerbench default-printer-set-recover [--folder PATH] (recheck an interrupted selection without repeating it)");
            Console.WriteLine("killerbench time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...] (preview only)");
            Console.WriteLine("killerbench time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute] (preview by default; custom mode requires peers)");
            Console.WriteLine("killerbench time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...] (read-only; custom mode requires peers)");
            Console.WriteLine("killerbench time-recover --folder PATH (verify and archive a pending record; never retry changes)");
            Console.WriteLine("killerbench firewall-enable --profile Domain|Private|Public [--profile ...] [--execute] (preview by default; unmanaged local profiles only)");
            Console.WriteLine("killerbench firewall-enable-recover (inspect a pending change without repeating it)");
            Console.WriteLine("killerbench profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT] (read-only review; verification requires one selected SID)");
            Console.WriteLine("killerbench profiles --select SID --create-backup-artifact DESTINATION (compare an existing copied backup and create its fixed artifact; does not copy or remove a profile)");
            Console.WriteLine("killerbench profiles --select SID --backup-to NEW_DESTINATION [--execute] (preview first; guarded native copy requires explicit execution and an absent local NTFS destination)");
            Console.WriteLine("killerbench profiles --recover (verify recorded absence and retain recovery evidence; never retries deletion)");
            Console.WriteLine("killerbench profiles --recover-backup (verify and archive a pending backup artifact; never retries copying)");
            Console.WriteLine("killerbench privacy-plan --select Camera --select ClipboardSync (review selections only; no settings changed)");
            Console.WriteLine("killerbench privacy-clipboard-history on|off [--execute] (preview by default; exact current-user value with policy preservation)");
            Console.WriteLine("killerbench privacy-clipboard-history-recover (recheck an interrupted change without repeating it)");
            Console.WriteLine("killerbench privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute] (guarded restoration of the prior value)");
            Console.WriteLine("killerbench privacy-setting KEY on|off [--execute] (preview by default; policy-preserving current-user change; invalid keys list supported choices)");
            Console.WriteLine("killerbench privacy-setting-recover KEY (recheck an interrupted change without repeating it)");
            Console.WriteLine("killerbench privacy-setting-undo KEY VERIFIED_JOURNAL [--execute] (guarded exact-value restoration)");
            Console.WriteLine("killerbench bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId (options preview only)");
            Console.WriteLine("killerbench bitlocker-protectors DRIVE: (protector identities and types only; no recovery passwords)");
            Console.WriteLine("killerbench bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute] (preview by default; new recovery backup file only)");
            Console.WriteLine("killerbench bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH (verify an interrupted backup without writing it again)");
            Console.WriteLine("killerbench bitlocker-resume DRIVE: [--execute] (preview by default; exact fully encrypted suspended volume only)");
            Console.WriteLine("killerbench bitlocker-resume-recover (verify pending protection state without repeating the request)");
            Console.WriteLine("killerbench bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute] (preview by default; backs up an existing recovery password to Active Directory or Microsoft Entra ID)");
            Console.WriteLine("killerbench bitlocker-protector-add DRIVE: tpm|recovery [--execute] (preview by default; adds one TPM or recovery-password protector; the password is never printed)");
            Console.WriteLine("killerbench bitlocker-protector-recover (verify an interrupted protector request without repeating it)");
            Console.WriteLine("killerbench bitlocker-decrypt DRIVE: [--execute --confirm-unprotected] (preview by default; starts decryption, for example before re-encrypting with a required method)");
            Console.WriteLine("killerbench bitlocker-decrypt-recover (verify an interrupted decryption request without repeating it)");
            Console.WriteLine("killerbench bitlocker-auto-unlock DRIVE: [--execute] (preview by default; turns on auto-unlock for an encrypted data volume)");
            Console.WriteLine("killerbench bitlocker-auto-unlock-recover (verify an interrupted auto-unlock request without repeating it)");
            Console.WriteLine("killerbench bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute] (preview by default; requires existing TPM and recovery protectors)");
            Console.WriteLine("killerbench bitlocker-encrypt-recover (verify pending conversion state without repeating the request)");
            Console.WriteLine("killerbench cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute] (preview by default; Recycle Bin is all-or-nothing and age does not apply)");
            Console.WriteLine("killerbench full-scan [--recipes ID[,ID]] [--report OUTPUT.html|OUTPUT.txt] [--json] (all diagnostic areas; diagnostic-only)");
            Console.WriteLine("killerbench full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json] (focused diagnostic journal; no repairs)");
            Console.WriteLine("killerbench workflow <" + WorkflowCatalog.CommandChoices + "> [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]");
            Console.WriteLine("killerbench workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json] (run a validated read-only shared option plan)");
            Console.WriteLine("killerbench workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json] (focused diagnostic journal; no repairs)");
            Console.WriteLine("Defender options: --skip-tpm --skip-secure-boot --skip-virtualization (signature checks remain included)");
            Console.WriteLine("Available updates: workflow updates [--drivers] [--no-software] [--json] (discovery only)");
            Console.WriteLine("killerbench update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute] (repeat --select; preview by default)");
            Console.WriteLine("killerbench update-recover (check a pending update; never installs or retries it)");
            Console.WriteLine("killerbench repair <dism|sfc> [--execute] (preview by default)");
            Console.WriteLine("killerbench scan --output <new-file.kbench> [--json]\nkillerbench open <file.kbench> [--json]");
            Console.WriteLine("Scan collects Windows Update evidence locally. No repairs. Ctrl+C stops; completed sources are saved.");
            return 0;
        }
        if (args.Length is < 2 or > 3 || args[0] is not ("analyze" or "investigate") || (args.Length == 3 && args[2] != "--json"))
        {
            Console.Error.WriteLine("Usage: killerbench <analyze|investigate> <text-log-path> [--json]");
            return 2;
        }
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        try
        {
            if (args[0] == "investigate")
            {
                var report = await Investigation.AnalyzeFileAsync(args[1], cancellation.Token);
                if (args.Length == 3) Console.WriteLine(Investigation.ToJson(report));
                else
                {
                    Console.WriteLine($"Source: {report.Source}\nFormat: {report.Format}; {report.ParsedLines} parsed entries; {report.Findings.Count} findings");
                    foreach (var finding in report.Findings)
                        Console.WriteLine($"\n{finding.RuleId} v{finding.RuleVersion}: {finding.Title}\n{finding.Certainty}; {finding.Occurrences} occurrences\n{finding.Explanation}\nNext: {finding.NextStep}\nLines: {string.Join(", ", finding.Evidence.Select(e => e.LineNumber))}\nReference: {finding.ReferenceUrl}");
                    if (report.Partial) Console.WriteLine("PARTIAL: a source or analysis limit was reached.");
                    if (report.Findings.Count == 0) Console.WriteLine("No supported rule matched. This is not a health verdict.");
                    Console.WriteLine(report.Limitation);
                }
                return report.Partial ? 3 : 0;
            }
            var result = await LogAnalyzer.AnalyzeFileAsync(args[1], cancellation.Token);
            if (args.Length == 3)
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            else
            {
                Console.WriteLine($"Source: {result.Source}");
                Console.WriteLine($"{result.LinesAnalyzed} lines analyzed; {result.Observations.Count} distinct code observations.");
                foreach (var code in result.Observations)
                    Console.WriteLine($"{code.Code}: {code.Occurrences} occurrences; lines {string.Join(", ", code.Evidence.Select(e => e.LineNumber))}");
                if (result.SourceTruncated || result.ObservationsTruncated) Console.WriteLine("PARTIAL: input or observation limit reached.");
                Console.WriteLine(result.Limitation);
            }
            return result.SourceTruncated || result.ObservationsTruncated ? 3 : 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Analysis canceled or exceeded 15 seconds."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Console.Error.WriteLine("Cannot analyze evidence: " + error.Message); return 1; }
    }
}
