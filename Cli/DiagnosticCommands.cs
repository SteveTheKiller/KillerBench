using System.Text.Json;
using KillerBench.Engine;

internal static class DiagnosticCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string command = args[0];
        List<CommonDiagnosticScope>? selectedScopes = null;
        if (command == "common-check" && args.Length > 1)
        {
            selectedScopes = [];
            for (int index = 1; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || args[index] != "--scope" || selectedScopes.Count >= 64 ||
                    !Enum.TryParse<CommonDiagnosticScope>(args[index + 1], true, out var scope) ||
                    !Enum.IsDefined(scope) || !scope.ToString().Equals(args[index + 1], StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("Usage: common-check [--scope NAME ...] (read-only; names: " + string.Join(", ", Enum.GetNames<CommonDiagnosticScope>()) + ")");
                    return 2;
                }
                selectedScopes.Add(scope);
            }
        }
        bool allBitsUsers = command == "bits-jobs" && args is [_, "--all-users"];
        bool defenderScanReport = command == "defender-scan-history" && args is [_, "--report"];
        List<string>? crashKnowledgePaths = null;
        bool invalidCrashKnowledge = false;
        if (command == "crash-review" && args.Length > 1)
        {
            crashKnowledgePaths = [];
            for (int index = 1; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || args[index] != "--knowledge" || crashKnowledgePaths.Count >= 32)
                { invalidCrashKnowledge = true; break; }
                crashKnowledgePaths.Add(args[index + 1]);
            }
        }
        string? volume = null;
        bool volumeCommand = command is "volume-capacity" or "volume-dirty" or "ntfs-self-healing";
        if (volumeCommand && args.Length == 2)
        {
            try { volume = StorageDiagnostics.NormalizeVolume(args[1]); }
            catch (ArgumentException) { }
        }
        if (volumeCommand ? volume is null : invalidCrashKnowledge || args.Length != 1 && !allBitsUsers && !defenderScanReport && selectedScopes is null && crashKnowledgePaths is null)
        {
            Console.Error.WriteLine(command == "crash-review" ? "Usage: crash-review [--knowledge PACK_PATH ...] (up to 32 packs; read-only crash and optional exact code-reference review)" : command == "bits-jobs" ? "Usage: bits-jobs [--all-users] (read-only transfer evidence)" : command == "defender-scan-history" ? "Usage: defender-scan-history [--report] (read-only scan timing and overdue review)" : volumeCommand ? $"Usage: {command} DRIVE: (read-only selected-volume evidence; JSON output)" : $"Usage: {command} (read-only diagnostic; JSON evidence; no options)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (command == "storage-reliability")
            {
                var checks = await StorageReliability.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
                return checks.All(check => check.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "storage-jobs")
            {
                Console.Error.WriteLine("Reading aggregate Storage module job state and progress. Target identities and provider payloads are excluded, and no job or storage object will change.");
                var jobs = await StorageJobDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(jobs, Capture.JsonOptions));
                return jobs.State == CheckState.Information ? 0 : 3;
            }
            if (command == "disk-quotas")
            {
                Console.Error.WriteLine("Reading per-volume disk quota settings. User quota rows and identities are excluded, and no quota or volume setting will change.");
                var quotas = await DiskQuotaDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(quotas, Capture.JsonOptions));
                return quotas.State == CheckState.Information ? 0 : 3;
            }
            if (command == "service-triggers")
            {
                Console.Error.WriteLine("Reading installed-service trigger types and actions. Trigger subtype identifiers and payloads are excluded, and no service configuration will change.");
                var inventory = await ServiceInventoryDiagnostics.CheckAsync(stop.Token);
                var triggers = await ServiceTriggerDiagnostics.CheckAsync(inventory, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(triggers, Capture.JsonOptions));
                return triggers.State == CheckState.Information ? 0 : 3;
            }
            if (command == "service-recovery-policy")
            {
                Console.Error.WriteLine("Reading installed-service recovery action types and timing. Recovery commands and reboot messages are excluded, and no service configuration will change.");
                var inventory = await ServiceInventoryDiagnostics.CheckAsync(stop.Token);
                var recovery = await ServiceRecoveryPolicyDiagnostics.CheckAsync(inventory, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(recovery, Capture.JsonOptions));
                return recovery.State == CheckState.Information ? 0 : 3;
            }
            if (command == "service-transitions")
            {
                Console.Error.WriteLine("Reading installed-service state and repeating the observation only when a pending transition is present. No service will be started, stopped, continued, paused, restarted or reconfigured.");
                var inventory = await ServiceInventoryDiagnostics.CheckAsync(stop.Token);
                var transitions = await ServiceTransitionDiagnostics.CheckAsync(inventory, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(transitions, Capture.JsonOptions));
                return transitions.State == CheckState.Information ? 0 : 3;
            }
            if (command == "vpn-profiles")
            {
                Console.Error.WriteLine("Reading current-account VPN profile configuration. Passwords, certificates and keys are excluded, and no connection or setting will change.");
                var profiles = VpnProfileDiagnostics.Describe(await VpnProfileDiagnostics.ReadAsync(stop.Token));
                Console.WriteLine(JsonSerializer.Serialize(profiles, Capture.JsonOptions));
                return profiles.All(check => check.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "winrm-listeners")
            {
                Console.Error.WriteLine("Reading WinRM listener registrations. Certificate thumbprints are excluded, and no listener, firewall rule, trust entry, credential or setting will change.");
                var listeners = WinRmListeners.Describe(await WinRmListeners.ReadAsync(stop.Token));
                Console.WriteLine(JsonSerializer.Serialize(listeners, Capture.JsonOptions));
                return listeners.All(check => check.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "physical-disks")
            {
                Console.Error.WriteLine("Reading current physical-disk provider state. Serial numbers, firmware data and unique hardware identifiers are excluded; no storage operation will run.");
                var checks = await PhysicalDiskDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
                return checks.All(check => check.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "winsock-catalog")
            {
                Console.Error.WriteLine("Reading the native Winsock protocol catalog. Protocol names, DLL paths and namespace providers are excluded, and no catalog or network setting will change.");
                var catalog = await WinsockCatalogDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(catalog, Capture.JsonOptions));
                return catalog.State == CheckState.Information ? 0 : 3;
            }
            if (command == "winsock-namespaces")
            {
                Console.Error.WriteLine("Reading native Winsock namespace-provider registrations. Provider names, DLL paths and query contents are excluded, and no provider or network setting will change.");
                var providers = await WinsockNamespaceDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(providers, Capture.JsonOptions));
                return providers.State == CheckState.Information ? 0 : 3;
            }
            if (command == "winsock-provider-files")
            {
                Console.Error.WriteLine("Reading registered Winsock protocol provider file presence and signatures. Full paths, provider names and signer identities are excluded, and no file, provider or network setting will change.");
                var files = await WinsockProviderFileDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(files, Capture.JsonOptions));
                return files.State == CheckState.Information ? 0 : 3;
            }
            if (command == "winsock-readiness")
            {
                Console.Error.WriteLine("Reading and correlating native Winsock protocol, namespace-provider and provider-file evidence. No provider, catalog, file, resolver or network setting will change.");
                var protocols = await WinsockCatalogDiagnostics.CheckAsync(stop.Token);
                var namespaces = await WinsockNamespaceDiagnostics.CheckAsync(stop.Token);
                var files = await WinsockProviderFileDiagnostics.CheckAsync(stop.Token);
                WorkflowCheck[] checks = [protocols, namespaces, files, WinsockReadinessCorrelation.Check(protocols, namespaces, files, stop.Token)];
                Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
                return checks.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "shadow-storage")
            {
                Console.Error.WriteLine("Reading VSS shadow storage allocation and limits. No association, limit, snapshot or backup data will change.");
                var shadowStorage = await ShadowStorageDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(shadowStorage, Capture.JsonOptions));
                return shadowStorage.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "vss-providers")
            {
                Console.Error.WriteLine("Reading VSS provider registrations and types. Provider and COM class identifiers are excluded, and no registration or setting will change.");
                var providers = await VssProviderDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(providers, Capture.JsonOptions));
                return providers.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "shadow-copies")
            {
                Console.Error.WriteLine("Reading current VSS shadow copy states and attributes. Snapshot, provider, volume, device, machine and path identities are excluded, and no shadow copy will change.");
                var copies = await ShadowCopyDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(copies, Capture.JsonOptions));
                return copies.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "delete-notifications")
            {
                Console.Error.WriteLine("Reading NTFS and ReFS delete-notification configuration. No optimization will run and no setting will change.");
                var deleteNotifications = await DeleteNotificationDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(deleteNotifications, Capture.JsonOptions));
                return deleteNotifications.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "ntfs-self-healing")
            {
                Console.Error.WriteLine("Reading NTFS self-healing configuration. No repair will start and no setting will change.");
                var selfHealing = await NtfsSelfHealingDiagnostics.CheckAsync(volume!, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(selfHealing, Capture.JsonOptions));
                return selfHealing.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "crash-review")
            {
                Console.Error.WriteLine("Reading crash history and dump metadata. No dump contents will be opened.");
                IReadOnlyList<WorkflowCheck> review;
                if (crashKnowledgePaths is null)
                    review = await CrashReview.ReadAsync(stop.Token);
                else
                {
                    var catalogs = new KnowledgeCatalogCollection();
                    foreach (string path in crashKnowledgePaths)
                        catalogs.AddOrReplace(await KnowledgeCatalog.ReadFileAsync(path, stop.Token));
                    review = await CrashReview.ReadWithKnowledgeAsync(catalogs, stop.Token);
                }
                Console.WriteLine(JsonSerializer.Serialize(review, Capture.JsonOptions));
                return review.All(check => check.State is CheckState.Information or CheckState.Clear) ? 0 : 3;
            }
            if (command == "crash-events")
            {
                var history = await CrashEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(history, Capture.JsonOptions));
                return history.State == CheckState.Information ? 0 : 3;
            }
            if (command == "crash-config")
            {
                var configuration = CrashDumpConfiguration.Check(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(configuration, Capture.JsonOptions));
                return configuration.State == CheckState.Information ? 0 : 3;
            }
            if (command == "crash-dumps")
            {
                var inventory = await CrashDumpInventory.ReadAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(inventory, Capture.JsonOptions));
                return inventory.Complete ? 0 : 3;
            }
            if (command == "minidump-analyze")
            {
                if (args.Length != 2) { Console.Error.WriteLine("Usage: minidump-analyze PATH"); return 2; }
                Console.Error.WriteLine("Reading bounded structural minidump metadata. Memory pages, symbols, stacks and full module paths will not be read, and no system setting will change.");
                var finding = await MinidumpAnalyzer.CheckAsync(args[1], stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State == CheckState.Information ? 0 : 3;
            }
            if (command == "backup-events")
            {
                var findings = await BackupEvents.CheckAsync(stop.Token, new DiagnosticProgress());
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "backup-summary")
            {
                Console.Error.WriteLine("Reading the local Windows Backup operation summary. No backup, schedule, catalog, destination or setting will change.");
                var summary = await WindowsBackupSummary.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(summary, Capture.JsonOptions));
                return summary.State == CheckState.Information ? 0 : 3;
            }
            if (command == "file-history-status")
            {
                Console.Error.WriteLine("Reading the current-account File History protection state through the bounded Windows interface. Protected-until time and target details are excluded, and no setting will change.");
                var status = await FileHistoryStatus.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(status, Capture.JsonOptions));
                return status.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "file-history-events")
            {
                Console.Error.WriteLine("Reading privacy-projected File History warning, error and critical event counts. Event payloads and backup configuration will not be read, and no setting will change.");
                var history = await FileHistoryEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(history, Capture.JsonOptions));
                return history.State == CheckState.Information ? 0 : 3;
            }
            if (command == "dns-cache-summary")
            {
                Console.Error.WriteLine("Reading privacy-bounded local DNS cache aggregates. DNS names and record data are excluded, and the cache will not be cleared.");
                var summary = await DnsClientCacheDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(summary, Capture.JsonOptions));
                return summary.State == CheckState.Information ? 0 : 3;
            }
            if (command == "common-check")
            {
                var findings = await CommonDiagnostics.CheckAsync(stop.Token, new DiagnosticProgress(), selectedScopes: selectedScopes);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "printer-ports")
            {
                var findings = await PrinterPortDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "printer-queue-attributes")
            {
                Console.Error.WriteLine("Reading local printer queue operating attributes. No queue, document, share or setting will change.");
                var findings = await PrinterQueueAttributeDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "printer-endpoint-check")
            {
                Console.Error.WriteLine("Connecting only to exact assigned Standard TCP/IP printer endpoints. No discovery scan, SNMP request or print data will be sent.");
                var findings = await PrinterEndpointReachability.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "printer-driver-files")
            {
                Console.Error.WriteLine("Checking registered printer driver files and executable signatures. Full paths are excluded and no driver will change.");
                var findings = await PrinterDriverFileDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "print-extension-files")
            {
                Console.Error.WriteLine("Checking registered print processor and monitor binaries and signatures. Full paths and signer identities are excluded, and no print component will change.");
                var findings = await PrintExtensionFileDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "printer-driver-models")
            {
                Console.Error.WriteLine("Reading printer driver models and package-awareness. No driver, package, queue or policy will change.");
                var finding = await PrinterDriverModelDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "default-printer")
            {
                Console.Error.WriteLine("Reading default-printer selection and automatic-management state. No queue or per-user setting will change.");
                var finding = await DefaultPrinterDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "printer-conditions")
            {
                Console.Error.WriteLine("Reading driver-reported local printer condition codes. No queue, job, port, driver or setting will change.");
                var findings = await PrinterConditionDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(finding => finding.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "route-priority")
            {
                Console.Error.WriteLine("Comparing default-route and interface metrics. No route, interface or metric will change.");
                var finding = await RoutePriorityDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "ip-interface-config")
            {
                Console.Error.WriteLine("Reading IP interface MTU, DHCP, forwarding, discovery and metric configuration. No network setting will change.");
                var finding = await IpInterfaceConfigurationDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "dhcp-leases")
            {
                Console.Error.WriteLine("Reading DHCP lease timestamps, expiry and server evidence. No lease or network setting will change.");
                var finding = await DhcpLeaseDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "dhcp-events")
            {
                Console.Error.WriteLine("Reading recent IPv4 and IPv6 DHCP Client event history. Ordinary summaries exclude network identities and no network setting will change.");
                var findings = await DhcpClientEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(finding => finding.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "dns-client-events")
            {
                Console.Error.WriteLine("Reading recent DNS Client warning and failure history. Ordinary summaries exclude queried names and resolver identities, and no network setting will change.");
                var findings = await DnsClientEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(finding => finding.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "neighbor-cache")
            {
                Console.Error.WriteLine("Reading grouped neighbor reachability states without retaining addresses or link-layer identifiers. No network setting will change.");
                var finding = await NeighborCacheDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "tcp-settings")
            {
                Console.Error.WriteLine("Reading bounded Windows TCP template settings as configuration context. No TCP, adapter, route, firewall, service or policy setting will change.");
                var finding = await TcpSettingDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "network-offload")
            {
                Console.Error.WriteLine("Reading global Windows network offload settings as configuration context. No offload, adapter, TCP, route, firewall, service or policy setting will change.");
                var finding = await NetworkOffloadDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "dns-client-config")
            {
                Console.Error.WriteLine("Reading DNS suffix, registration, devolution and policy evidence. No adapter, resolver or policy setting will change.");
                var finding = await DnsClientConfigurationDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "dns-nrpt-policy")
            {
                Console.Error.WriteLine("Reading effective DNS namespace policy. No namespace, resolver, adapter, route, VPN, DirectAccess, DNSSEC, IPsec or policy setting will change.");
                var finding = await DnsNrptPolicyDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "windows-capabilities")
            {
                Console.Error.WriteLine("Reading online Windows capability states. No capability, payload, source, package, policy or servicing setting will change.");
                var finding = await WindowsCapabilityDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "adapter-bindings")
            {
                Console.Error.WriteLine("Reading IPv4, IPv6, Microsoft network client and file-sharing binding state. No adapter or binding will change.");
                var finding = await AdapterBindingDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "adapter-statistics")
            {
                Console.Error.WriteLine("Reading cumulative network adapter byte, error and discard counters. No adapter or counter will change.");
                var finding = await AdapterStatisticsDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "tcp-port-pressure")
            {
                Console.Error.WriteLine("Reading configured dynamic TCP ranges and aggregate local-port usage. Endpoints and process identities are excluded; no network setting will change.");
                var findings = await TcpPortPressureDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(finding => finding.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "pagefile-check")
            {
                Console.Error.WriteLine("Reading page-file management, allocation and usage. Full paths are excluded and no memory setting will change.");
                var finding = await PageFileDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "reserved-storage")
            {
                Console.Error.WriteLine("Reading reserved-storage state for the online Windows image. No servicing setting will change.");
                var finding = await ReservedStorageDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "storage-sense-policy")
            {
                Console.Error.WriteLine("Reading recognized Storage Sense machine policy. User settings and file identities are excluded; no cleanup or setting change will run.");
                var finding = StorageSensePolicyDiagnostics.Check();
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "storage-sense-task")
            {
                Console.Error.WriteLine("Reading the exact Storage Sense scheduled task state and history. The task will not run and no file or setting will change.");
                var finding = await StorageSenseTaskDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "windows-update-task")
            {
                Console.Error.WriteLine("Reading the exact Windows Update Scheduled Start task state and history. The task will not run and no update, service or setting will change.");
                var finding = await WindowsUpdateScheduledTaskDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "disk-cleanup-task")
            {
                Console.Error.WriteLine("Reading the exact Disk Cleanup scheduled task state and history. The task will not run and no file or setting will change.");
                var finding = await DiskCleanupTaskDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "disk-diagnostic-tasks")
            {
                Console.Error.WriteLine("Reading the exact Windows disk diagnostic task pair. The tasks will not run and no disk, file or setting will change.");
                var findings = await DiskDiagnosticTaskDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "disk-diagnostic")
            {
                Console.Error.WriteLine("Reading and correlating the exact Windows disk diagnostic tasks and event channels. No task, disk, file or setting will change.");
                var findings = await DiskDiagnosticCorrelation.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command is "vitals-check" or "printer-drivers" or "security-check")
            {
                Console.Error.WriteLine(command == "security-check" ? "Reading TPM, Secure Boot, virtualization protection, UAC elevation policy and effective AppLocker collection summaries. No security setting will change." : command == "printer-drivers" ? "Reading queue and driver registrations. No queues or drivers will change." : "Reading Windows and system identity, local Administrators, processor, graphics, physical memory, battery and disk snapshots. No accounts, settings or disks will change.");
                var findings = command == "security-check" ? [.. await SecurityDiagnostics.CheckAsync(stop.Token), .. UacPolicyDiagnostics.Check(), .. await AppLockerPolicyDiagnostics.CheckAsync(stop.Token)] : command == "printer-drivers" ? await PrinterDriverDiagnostics.CheckAsync(stop.Token) : [.. await VitalsDiagnostics.CheckAsync(stop.Token), await MemoryDiagnosticResults.CheckAsync(stop.Token)];
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "uac-policy")
            {
                Console.Error.WriteLine("Reading machine UAC enablement and elevation-prompt policy. Accounts, credentials, tokens and consent history are excluded, and no setting will change.");
                var findings = UacPolicyDiagnostics.Check();
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "applocker-policy")
            {
                Console.Error.WriteLine("Reading effective AppLocker collection modes and aggregate rule counts. Rule identities, paths, publishers, hashes, users and SIDs are excluded, and no policy will change.");
                var findings = await AppLockerPolicyDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "memory-diagnostic-results")
            {
                Console.Error.WriteLine("Reading bounded Windows Memory Diagnostic result history. No test will run or be scheduled and no hardware or setting will change.");
                var finding = await MemoryDiagnosticResults.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "task-check")
            {
                Console.Error.WriteLine("Reading scheduled task inventory and classifying reported states. No tasks will change.");
                var inventory = await ScheduledTaskInventory.ReadAsync(stop.Token);
                stop.Token.ThrowIfCancellationRequested();
                var findings = inventory.Tasks.Select(task => ScheduledTaskDiagnostics.Assess(task)).ToArray();
                Console.WriteLine(JsonSerializer.Serialize(new { inventory.Complete, inventory.Evidence, Tasks = inventory.Tasks, Findings = findings }, Capture.JsonOptions));
                return inventory.Complete && findings.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "event-channels")
            {
                Console.Error.WriteLine("Reading four event channel configurations, sizes and saturation levels. No logging settings or events will change.");
                var channels = await EventLogCapacityDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(channels, Capture.JsonOptions));
                return channels.All(item => item.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "network-profiles")
            {
                Console.Error.WriteLine("Reading current network profile categories and connectivity classifications. Network names are excluded and no settings will change.");
                var profiles = await NetworkProfileDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(profiles, Capture.JsonOptions));
                return profiles.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "ncsi-policy")
            {
                Console.Error.WriteLine("Reading Windows connectivity classification policy. Probe destinations and content are excluded and no settings will change.");
                var finding = await NcsiPolicyDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(finding, Capture.JsonOptions));
                return finding.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "network-firewall")
            {
                Console.Error.WriteLine("Correlating active network assignments with Windows firewall profile enablement. No profile, rule, service or network setting will change.");
                var networks = await NetworkProfileDiagnostics.CheckAsync(stop.Token);
                var firewalls = await FirewallProfiles.CheckAsync(stop.Token);
                var findings = networks.Concat(firewalls).Append(NetworkFirewallCorrelation.Check(networks, firewalls, stop.Token)).ToArray();
                Console.WriteLine(JsonSerializer.Serialize(findings, Capture.JsonOptions));
                return findings.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "firewall-logging")
            {
                Console.Error.WriteLine("Reading Domain, Private and Public Windows Firewall logging configuration. Log paths and contents are excluded, and no setting will change.");
                var logging = await FirewallLoggingDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(logging, Capture.JsonOptions));
                return logging.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "firewall-rules")
            {
                Console.Error.WriteLine("Summarizing effective Windows Firewall rules. Rule identities, applications, ports, addresses and users are excluded, and no setting will change.");
                var summary = await FirewallRuleSummaryDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(summary, Capture.JsonOptions));
                return summary.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "firewall-events")
            {
                Console.Error.WriteLine("Reading bounded Windows Firewall warning, error and critical operational history. Sensitive payload fields remain only in raw evidence, and no firewall or network setting will change.");
                var history = await FirewallEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(history, Capture.JsonOptions));
                return history.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "firewall-service-events")
            {
                Console.Error.WriteLine("Reading exact Base Filtering Engine and Windows Defender Firewall Service Control Manager failure history. Historical records cannot start or restart either service or change firewall policy.");
                var histories = await FirewallServiceFailureEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "network-service-events")
            {
                Console.Error.WriteLine("Reading exact DHCP, DNS, Workstation, Network Location Awareness, Network List and IP Helper Service Control Manager failure history. Historical records cannot start or restart services or change network settings.");
                var histories = await NetworkServiceFailureEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "storage-service-events")
            {
                Console.Error.WriteLine("Reading exact Storage Service, Optimize Drives, Volume Shadow Copy and software shadow provider Service Control Manager failure history. Historical records cannot start or restart services or change storage, snapshots or backup settings.");
                var histories = await StorageServiceFailureEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "backup-service-events")
            {
                Console.Error.WriteLine("Reading exact Volume Shadow Copy, software shadow provider, Windows Backup and Block Level Backup Engine Service Control Manager failure history. Historical records cannot start or restart services or change snapshots, catalogs, destinations or backup settings.");
                var histories = await BackupServiceFailureEvents.CheckAllAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "feature-service-events")
            {
                Console.Error.WriteLine("Reading exact Windows Time, Windows Search, WMI, Windows Event Log and Windows audio Service Control Manager failure history. Historical records cannot start or restart services or change component settings.");
                var histories = await FeatureServiceFailureEvents.CheckAllAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "platform-service-events")
            {
                Console.Error.WriteLine("Reading one bounded Service Control Manager snapshot for exact application deployment, identity, profile and device-support service failures. Historical records cannot start or restart services, change packages, reset data or change policy.");
                var histories = await PlatformServiceFailureEvents.CheckAllAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "infrastructure-service-events")
            {
                Console.Error.WriteLine("Reading one bounded Service Control Manager snapshot for exact connectivity, remote-management, device, notification and core Windows service failures. Historical records cannot start or restart services, reset configuration or change policy.");
                var histories = await InfrastructureServiceFailureEvents.CheckAllAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "smb-shares")
            {
                Console.Error.WriteLine("Reading bounded SMB share inventory. Paths, permissions, sessions, open files and account identities are excluded. No settings will change.");
                var shares = await SmbShareDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(shares, Capture.JsonOptions));
                return shares.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "smb-server-config")
            {
                Console.Error.WriteLine("Reading SMB server protocol and protection configuration. No SMB setting will change.");
                var configuration = await SmbServerConfigurationDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(configuration, Capture.JsonOptions));
                return configuration.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "smb-client-config")
            {
                Console.Error.WriteLine("Reading SMB client signing, guest-access and transport configuration. No SMB setting will change.");
                var configuration = await SmbClientConfigurationDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(configuration, Capture.JsonOptions));
                return configuration.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "smb-connections")
            {
                Console.Error.WriteLine("Reading active SMB protocol negotiation. Server, share and account identities are excluded. No connection or setting will change.");
                var connections = await SmbConnectionDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(connections, Capture.JsonOptions));
                return connections.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "font-registrations")
            {
                Console.Error.WriteLine("Reading bounded machine and current-user font registrations. No fonts, registrations or cache files will change.");
                var fonts = await FontRegistrationDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(fonts, Capture.JsonOptions));
                return fonts.All(item => item.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "proxy-check")
            {
                Console.Error.WriteLine("Reading stored WinHTTP and current-account Internet proxy settings. No network requests or settings changes.");
                stop.Token.ThrowIfCancellationRequested();
                var checks = ProxyDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
                return checks.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "delivery-optimization")
            {
                Console.Error.WriteLine("Reading Delivery Optimization service state. No service, transfer, cache or policy setting will change.");
                var deliveryOptimization = await DeliveryOptimizationDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(deliveryOptimization, Capture.JsonOptions));
                return deliveryOptimization.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "delivery-optimization-activity")
            {
                Console.Error.WriteLine("Reading aggregate current Delivery Optimization activity. File IDs, URLs, cache hosts, peer addresses and caller identities are excluded. No transfer, cache, service or policy setting will change.");
                var activity = await DeliveryOptimizationActivityDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(activity, Capture.JsonOptions));
                return activity.State == CheckState.Information ? 0 : 3;
            }
            if (command == "delivery-optimization-policy")
            {
                Console.Error.WriteLine("Reading Delivery Optimization classic and Policy CSP settings. Group IDs, cache hosts, cache-drive values and provider identities are excluded. No policy or transfer setting will change.");
                stop.Token.ThrowIfCancellationRequested();
                var policy = DeliveryOptimizationPolicyDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(policy, Capture.JsonOptions));
                return policy.State == CheckState.Information ? 0 : 3;
            }
            if (command == "update-policy")
            {
                Console.Error.WriteLine("Reading managed Windows Update policy. No registry, service, source or scan setting will change.");
                stop.Token.ThrowIfCancellationRequested();
                var updatePolicy = UpdatePolicyDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(updatePolicy, Capture.JsonOptions));
                return updatePolicy.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "update-policy-surfaces")
            {
                Console.Error.WriteLine("Identifying classic and Policy CSP Windows Update configuration surfaces. Values and provider identities are excluded, and no policy or registry setting will change.");
                stop.Token.ThrowIfCancellationRequested();
                var policySurfaces = UpdatePolicySurfaceDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(policySurfaces, Capture.JsonOptions));
                return policySurfaces.State == CheckState.Information ? 0 : 3;
            }
            if (command == "update-restart-policy")
            {
                Console.Error.WriteLine("Reading managed update deadlines, grace periods, restart suppression and notification behavior. No policy, registry, update, notification or restart setting will change.");
                stop.Token.ThrowIfCancellationRequested();
                var restartPolicy = UpdateRestartPolicyDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(restartPolicy, Capture.JsonOptions));
                return restartPolicy.State == CheckState.Information ? 0 : 3;
            }
            if (command == "feature-update-policy")
            {
                Console.Error.WriteLine("Reading managed feature update targeting and safeguard policy. No registry, update, safeguard or restart setting will change.");
                stop.Token.ThrowIfCancellationRequested();
                var featurePolicy = FeatureUpdatePolicyDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(featurePolicy, Capture.JsonOptions));
                return featurePolicy.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "component-repair-source")
            {
                Console.Error.WriteLine("Reading managed component repair source policy. No repair source, update, servicing or registry setting will change.");
                stop.Token.ThrowIfCancellationRequested();
                var repairSource = ComponentRepairSourceDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(repairSource, Capture.JsonOptions));
                return repairSource.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "update-cache")
            {
                Console.Error.WriteLine("Measuring bounded Windows Update cache totals. No filename, file content, service, cache, update or policy setting will change.");
                var updateCache = await UpdateCacheDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(updateCache, Capture.JsonOptions));
                return updateCache.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "servicing-logs")
            {
                Console.Error.WriteLine("Inspecting the newest bounded portions of CBS, archived CBS, DISM and applicable Windows Setup logs. Historical entries cannot authorize repair.");
                var servicingLogs = await ServicingLogDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(servicingLogs, Capture.JsonOptions));
                return servicingLogs.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "update-services")
            {
                Console.Error.WriteLine("Reading Windows Update service registrations. No service, source enrollment, policy or scan setting will change.");
                var updateServices = await UpdateServiceDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(updateServices, Capture.JsonOptions));
                return updateServices.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "update-history")
            {
                Console.Error.WriteLine("Reading the newest 100 Windows Update history entries. No update operation will be requested.");
                var updateHistory = await UpdateHistoryDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(updateHistory, Capture.JsonOptions));
                return updateHistory.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "installed-hotfixes")
            {
                Console.Error.WriteLine("Reading a bounded installed Windows hotfix inventory. No update, package or system setting will change.");
                var installedHotfixes = await InstalledHotfixDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(installedHotfixes, Capture.JsonOptions));
                return installedHotfixes.State is CheckState.Clear or CheckState.Information ? 0 : 3;
            }
            if (command == "update-context")
            {
                Console.Error.WriteLine("Reading current elevation and desktop-session context. No token, credential or account identifier will be retained.");
                stop.Token.ThrowIfCancellationRequested();
                var updateContext = UpdateExecutionContextDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(updateContext, Capture.JsonOptions));
                return updateContext.State == CheckState.Information ? 0 : 3;
            }
            if (command == "update-applicability")
            {
                Console.Error.WriteLine("Reading Windows edition, build and architecture. No update source, package or setting will change.");
                stop.Token.ThrowIfCancellationRequested();
                var applicability = UpdateApplicabilityDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(applicability, Capture.JsonOptions));
                return applicability.State == CheckState.Information ? 0 : 3;
            }
            if (command == "restart-check")
            {
                Console.Error.WriteLine("Reading pending restart sources. No restart will be requested or scheduled, and no indicator will be cleared.");
                stop.Token.ThrowIfCancellationRequested();
                var restart = RestartDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(restart, Capture.JsonOptions));
                return restart.State == CheckState.Clear ? 0 : 3;
            }
            if (command == "pending-file-operations")
            {
                Console.Error.WriteLine("Summarizing pending file rename, replacement and deletion operations. Paths and identities are excluded; no registration will be cleared or changed, and no restart will be requested.");
                stop.Token.ThrowIfCancellationRequested();
                var operations = PendingFileOperationDiagnostics.Check();
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(operations, Capture.JsonOptions));
                return operations.State == CheckState.Information ? 0 : 3;
            }
            if (command == "installer-pending-state")
            {
                Console.Error.WriteLine("Correlating privacy-bounded pending file operation counts with current Windows Installer activity. No path or command line will be retained, no process or registration will change, and no restart will be requested.");
                stop.Token.ThrowIfCancellationRequested();
                var operations = PendingFileOperationDiagnostics.Check();
                var activity = await InstallerActivityDiagnostics.CheckAsync(stop.Token);
                var correlation = InstallerPendingOperationCorrelation.Check(operations, activity, stop.Token);
                WorkflowCheck[] checks = [operations, activity, correlation];
                Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
                return checks.All(check => check.State == CheckState.Information) ? 0 : 3;
            }
            if (command == "certificate-chains")
            {
                Console.Error.WriteLine("Building local cache-only trust chains for personal certificates. Revocation and certificate downloads are disabled, and no certificate, trust setting or private key will change.");
                var chains = await CertificateChainDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(chains, Capture.JsonOptions));
                return chains.State == CheckState.Information ? 0 : 3;
            }
            if (command == "licensing-check")
            {
                Console.Error.WriteLine("Reading Windows licensing state. No product key will be collected and no activation will be attempted.");
                var licensing = await WindowsLicensingDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(licensing, Capture.JsonOptions));
                return licensing.State == CheckState.Clear ? 0 : 3;
            }
            if (command == "defender-threats")
            {
                Console.Error.WriteLine("Reading aggregate Microsoft Defender threat history and current active flags. Names, paths, resources, accounts and process details are excluded. No security action will run.");
                var threats = await DefenderThreatSummary.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(threats, Capture.JsonOptions));
                return threats.State == CheckState.Information ? 0 : 3;
            }
            if (command == "defender-scan-history")
            {
                Console.Error.WriteLine("Reading Microsoft Defender quick and full scan timing and overdue status. Threat identities, paths, accounts and file contents are excluded. No scan or security action will run.");
                var history = await DefenderScanHistory.CheckAsync(stop.Token);
                Console.WriteLine(defenderScanReport ? FormatDefenderScanHistory(history) : JsonSerializer.Serialize(history, Capture.JsonOptions));
                return history.State == CheckState.Information ? 0 : 3;
            }
            if (command == "defender-protection")
            {
                Console.Error.WriteLine("Reading Microsoft Defender protection preferences and recognized machine policy value names. Exclusions, protected folders, file contents and policy values are excluded. No security setting will change.");
                var protection = await DefenderProtectionDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(protection, Capture.JsonOptions));
                return protection.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "installer-activity")
            {
                Console.Error.WriteLine("Reading Windows Installer processes and the transaction marker. No command lines will be collected and no process or marker will change.");
                var activity = await InstallerActivityDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(activity, Capture.JsonOptions));
                return activity.State == CheckState.Information ? 0 : 3;
            }
            if (command == "servicing-service-events")
            {
                Console.Error.WriteLine("Reading Service Control Manager failures tied to six exact servicing service identities. No service, update, transfer, cache, source, policy or repair state will change.");
                var histories = await ServicingServiceFailureEvents.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(histories, Capture.JsonOptions));
                return histories.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            if (command == "servicing-service-states")
            {
                Console.Error.WriteLine("Reading current state for six exact Windows Update servicing services. No service, update, transfer, cache, source, policy or startup setting will change.");
                var states = await ServicingServiceStateDiagnostics.CheckAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(states, Capture.JsonOptions));
                return states.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
            }
            Console.Error.WriteLine(command == "bits-jobs" ? "Reading " + (allBitsUsers ? "all-user" : "running-account") + " BITS jobs. No job changes." : command == "vss-writers" ? "Reading reported VSS writer states. No writer reset or snapshot operation." : command == "time-config" ? "Reading Windows Time configuration and policy scope. No synchronization or source changes." : command == "volume-capacity" ? "Reading selected-volume capacity. No files will change." : command == "volume-dirty" ? "Reading the selected volume's dirty bit. No repair or restart will be scheduled." : command == "installer-check"
                ? "Reading Windows Installer policies and engine prerequisites. No settings will change." : command == "installer-security-policy"
                ? "Reading Windows Installer security and patch policy. No settings or packages will change."
                : command == "installer-logging" ? "Reading Windows Installer logging and debug policy. No logging, package or policy setting will change."
                : command == "installer-engine-files" ? "Checking required Windows Installer engine files and Authenticode status. Paths and signer identities are excluded; no file or Installer setting will change."
                : command == "update-engine-files" ? "Checking required Windows Update engine files and Authenticode status. Paths and signer identities are excluded; no file or update setting will change."
                : command == "update-service-files" ? "Checking exact Windows Update service DLL registrations and Authenticode status. Paths and signer identities are excluded; no file, service or registry setting will change."
                : command == "update-service-events" ? "Reading Service Control Manager failures tied to the exact local Windows Update identity. No service, update, cache, source, policy or repair state will change."
                : command == "installer-service-registration" ? "Checking Windows Installer service registration. Raw image paths and account values are excluded; no service or registry setting will change."
                : command == "installer-service-events" ? "Reading Service Control Manager failures tied to the local Windows Installer identity. No service, package, cache, policy or deployment state will change."
                : command == "print-policy" ? "Reading Point and Print restrictions and driver-installation policy. No policy or print setting will change."
                : command == "print-rendering-policy" ? "Reading print rendering and driver-isolation policy. No policy, process, driver, queue, job or print setting will change."
                : command == "print-spooler-events" ? "Reading Service Control Manager failures tied to the local Print Spooler identity. No service, queue, job, driver or print setting will change."
                : "Reading up to 200 historical events from the last seven days. No logging settings will change.");
            stop.Token.ThrowIfCancellationRequested();
            WorkflowCheck check = command switch
            {
                "installer-check" => InstallerDiagnostics.Check(),
                "installer-engine-files" => await InstallerEngineFileDiagnostics.CheckAsync(stop.Token),
                "update-engine-files" => await WindowsUpdateEngineFileDiagnostics.CheckAsync(stop.Token),
                "update-service-files" => await ServiceDllDiagnostics.CheckSelectedAsync("Windows Update service DLLs",
                    ["wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc"], stop.Token),
                "update-service-events" => await WindowsUpdateServiceFailureEvents.CheckAsync(stop.Token),
                "installer-service-registration" => InstallerServiceRegistrationDiagnostics.Check(),
                "installer-security-policy" => InstallerSecurityPolicyDiagnostics.Check(),
                "installer-logging" => InstallerLoggingDiagnostics.Check(),
                "vss-writers" => await VssWriters.CheckAsync(stop.Token),
                "bits-jobs" => await BitsDiagnostics.CheckAsync(allBitsUsers, stop.Token),
                "volume-capacity" => VolumeCapacity.Check(volume!),
                "volume-dirty" => await VolumeDirtyStatus.CheckAsync(volume!, stop.Token),
                "installer-events" => await InstallerEvents.CheckAsync(stop.Token),
                "installer-service-events" => await WindowsInstallerFailureEvents.CheckAsync(stop.Token),
                "print-events" => await PrintServiceEvents.CheckAsync(stop.Token),
                "print-operational-events" => await PrintServiceOperationalEvents.CheckAsync(stop.Token),
                "print-spooler-events" => await PrintSpoolerFailureEvents.CheckAsync(stop.Token),
                "print-policy" => PrintPolicyDiagnostics.Check(),
                "print-rendering-policy" => PrintRenderingPolicyDiagnostics.Check(),
                "service-events" => await ServiceEvents.CheckAsync(stop.Token),
                "storage-events" => await StorageEvents.CheckAsync(stop.Token),
                "update-events" => await WindowsUpdateEvents.CheckAsync(stop.Token),
                "time-events" => await TimeServiceEvents.CheckAsync(stop.Token),
                "time-config" => await TimeConfigurationDiagnostics.CheckAsync(stop.Token),
                "application-events" => await ApplicationEvents.CheckAsync(stop.Token),
                _ => throw new ArgumentException("Unknown diagnostic command.")
            };
            stop.Token.ThrowIfCancellationRequested();
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            // Information means collection completed, not that current health is good.
            return check.State is CheckState.Clear or CheckState.Information ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Diagnostic stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Diagnostic unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static string FormatDefenderScanHistory(WorkflowCheck history)
    {
        ArgumentNullException.ThrowIfNull(history);
        string next = history.State switch
        {
            CheckState.Attention => "Review the intended endpoint-security schedule and ownership. This check did not request a scan.",
            CheckState.Information => "Review the recorded timing with your endpoint-security schedule. No scan is selected.",
            _ => "Repeat this read-only check when Defender scan history is available. Do not infer threat status from incomplete timing evidence."
        };
        return string.Join(Environment.NewLine,
        [
            "MICROSOFT DEFENDER SCAN HISTORY",
            "Outcome: " + history.State,
            "",
            "WHAT WAS FOUND",
            history.Detail,
            "",
            "NEXT STEP",
            next,
            "",
            "SCOPE AND LIMITS",
            "This read-only timing review does not start a scan, alter Defender settings, inspect threat identities, or establish that the computer is free of threats."
        ]);
    }
    private sealed class DiagnosticProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
