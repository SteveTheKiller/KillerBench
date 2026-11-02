namespace KillerBench.Engine.Workflows;

public static class DemoWorkflow
{
    public const string MachineName = "FIELD-WS-014";
    public static readonly DateTimeOffset ScreenshotStartedUtc = new(2026, 11, 2, 18, 42, 0, TimeSpan.Zero);

    public static WorkflowResult Create(WorkflowKind kind, bool simulateRepair = false,
        DateTimeOffset? startedUtc = null, bool screenshotPreset = false)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var definition = WorkflowCatalog.All.Single(item => item.Kind == kind);
        var checks = new List<WorkflowCheck>
        {
            new("Operating system", CheckState.Information,
                "Windows 11 Pro, 64-bit. Workstation joined to the organization.")
        };
        if (screenshotPreset) checks.AddRange(ScreenshotChecks(kind));
        else checks.Add(new(definition.Title, CheckState.Attention, Scenario(kind), Evidence: SyntheticEvidence(kind)));

        string status = "Needs attention";
        if (simulateRepair)
        {
            checks.Add(new("Repair verification", CheckState.Clear,
                "The selected repair completed and follow-up checks passed."));
            for (int i = 0; i < checks.Count; i++)
                if (checks[i].Repair is not null)
                    checks[i] = checks[i] with { State = CheckState.Clear, Detail = "The service is running and its dependencies are available.", Repair = null };
        }

        return new WorkflowResult(kind, MachineName, startedUtc ?? (screenshotPreset ? ScreenshotStartedUtc : DateTimeOffset.UtcNow), status,
            checks.AsReadOnly(), [], "Not saved")
        {
            PlannedRecipeIds = []
        };
    }

    public static IReadOnlyList<string> Progress(WorkflowKind kind, bool simulateRepair)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        string title = WorkflowCatalog.All.Single(item => item.Kind == kind).Title;
        var messages = new List<string>
        {
            "Preparing workstation checks...",
            "Collecting " + title + " evidence...",
            "Evaluating findings...",
            "Review items found."
        };
        if (simulateRepair)
        {
            messages.Add("Starting the selected repair...");
            messages.Add("Repair verification completed.");
        }
        return messages.AsReadOnly();
    }

    private static ToolResult SyntheticEvidence(WorkflowKind kind) =>
        new(0, "Computer: " + MachineName + "\nWorkflow: " + kind + "\n" + Scenario(kind), "", false);

    private static IReadOnlyList<WorkflowCheck> ScreenshotChecks(WorkflowKind kind) => kind switch
    {
        WorkflowKind.CommonProblems =>
        [
            new("Windows Update readiness", CheckState.Attention, "Update service is stopped; review its current state before considering recovery."),
            new("System stability", CheckState.Attention, "Two application failures need correlation with current behavior."),
            new("Restart status", CheckState.Information, "The workstation has a pending restart."),
            new("Network readiness", CheckState.Clear, "Adapter and name-resolution checks completed without a finding."),
            new("Storage coverage", CheckState.Unknown, "Volume health data is unavailable; no healthy verdict is inferred.")
        ],
        WorkflowKind.WindowsUpdate =>
        [
            new("Update service", CheckState.Attention, "The update service is stopped; review service readiness before recovery."),
            new("Servicing activity", CheckState.Information, "A servicing session is waiting for a restart."),
            new("Recent update history", CheckState.Unknown, "The history source is incomplete, so the outcome remains unknown."),
            new("Update policy", CheckState.Clear, "Policy inspection found no restriction in the checked scope.")
        ],
        WorkflowKind.Network =>
        [
            new("Adapter configuration", CheckState.Clear, "The adapter has a usable address and default gateway."),
            new("DNS response", CheckState.Attention, "The name-resolution check did not receive a usable response."),
            new("Gateway reachability", CheckState.Information, "Gateway evidence is available but does not prove Internet access."),
            new("Network history", CheckState.Unknown, "The event source is unavailable; historical coverage is incomplete.")
        ],
        WorkflowKind.Printing =>
        [
            new("Print queue", CheckState.Attention, "The office queue is offline with two waiting jobs."),
            new("Print Spooler", CheckState.Clear, "The Print Spooler service is running."),
            new("Default printer", CheckState.Information, "One default queue is configured; no printer setting changed."),
            new("Physical output", CheckState.Unknown, "Paper output and device hardware have not been verified.")
        ],
        WorkflowKind.CoreWindowsServices =>
        [
            new("Windows Event Log", CheckState.Clear, "The EventLog service is running with automatic start."),
            new("Cryptographic Services", CheckState.Attention, "The CryptSvc service is stopped although its start type is automatic.", RepairKind.CryptographicServices),
            new("Remote Procedure Call", CheckState.Clear, "The RPC endpoint mapper and DCOM launcher are running."),
            new("Windows Management Instrumentation", CheckState.Information, "The WMI repository was verified as consistent last week."),
            new("Service dependencies", CheckState.Unknown, "One dependent service could not be queried, so dependency coverage is incomplete.")
        ],
        WorkflowKind.Defender =>
        [
            new("Real-time protection", CheckState.Clear, "Real-time protection is enabled and reporting normally."),
            new("Security intelligence", CheckState.Attention, "The definitions are nine days old; the last update attempt failed."),
            new("Last quick scan", CheckState.Information, "A quick scan completed three days ago with no detections."),
            new("Tamper protection", CheckState.Clear, "Tamper protection is on and managed locally."),
            new("Third-party antivirus", CheckState.Unknown, "The Security Center inventory is incomplete; another provider cannot be ruled out.")
        ],
        WorkflowKind.Depot =>
        [
            new("Browsers", CheckState.Information, "Two browsers are selected; one is already registered on the workstation."),
            new("Productivity applications", CheckState.Attention, "The selected office suite has no matching Click-to-Run registration on the workstation."),
            new("Communication applications", CheckState.Information, "One meeting client is selected and not yet present."),
            new("Configuration: power and sleep", CheckState.Information, "The selected group would change sleep timeouts; it routes to a separately guarded workflow."),
            new("Configuration: privacy defaults", CheckState.Clear, "The privacy settings already match the selected group."),
            new("Provisioning boundary", CheckState.Information, "This review downloads, installs and changes nothing.")
        ],
        WorkflowKind.Storage =>
        [
            new("System volume free space", CheckState.Attention, "The C: volume has 11.8 GB free of 476 GB (2.5 percent)."),
            new("Data volume", CheckState.Clear, "The D: volume has 612 GB free of 931 GB."),
            new("Disk health", CheckState.Unknown, "The reliability counters are unavailable; no healthy verdict is inferred."),
            new("File system state", CheckState.Clear, "No volume is marked dirty."),
            new("Largest folders", CheckState.Information, "User downloads and a retained update cache account for 64 GB.")
        ],
        WorkflowKind.BitLocker =>
        [
            new("Operating system volume", CheckState.Attention, "Protection on the C: volume is suspended until the next restart."),
            new("Key protectors", CheckState.Information, "The volume has TPM and numerical password protectors. Recovery passwords are omitted from this report."),
            new("Encryption method", CheckState.Clear, "The volume uses XTS-AES 128 and is fully encrypted."),
            new("Fixed data volume", CheckState.Clear, "The D: volume is protected with automatic unlock."),
            new("Recovery key backup", CheckState.Unknown, "Directory backup of the recovery password could not be confirmed.")
        ],
        WorkflowKind.Certificates =>
        [
            new("Expiring soon", CheckState.Attention, "The client authentication certificate CN=staff-vpn expires in 12 days."),
            new("Machine personal store", CheckState.Clear, "Four machine certificates are valid for more than 90 days."),
            new("User personal store", CheckState.Information, "One code-signing certificate expires in 74 days."),
            new("Expired certificates", CheckState.Information, "Two expired certificates remain in the user store; nothing was removed."),
            new("Private key access", CheckState.Unknown, "Key accessibility for one certificate could not be confirmed.")
        ],
        _ => [new(WorkflowCatalog.All.Single(item => item.Kind == kind).Title, CheckState.Attention,
            Scenario(kind), Evidence: SyntheticEvidence(kind))]
    };

    private static string Scenario(WorkflowKind kind) => kind switch
    {
        WorkflowKind.CommonProblems => "A stopped update service, an old application crash and a pending restart. Review the current findings before choosing a repair.",
        WorkflowKind.WindowsUpdate => "A stopped update service and servicing activity waiting for a restart.",
        WorkflowKind.SystemFiles => "Protected-file integrity evidence that requires review.",
        WorkflowKind.Network or WorkflowKind.Wireless or WorkflowKind.WiredAuthentication => "A configured adapter without a usable DNS response.",
        WorkflowKind.Printing => "An offline queue with two waiting jobs.",
        WorkflowKind.Installer => "A failed installation with a retained MSI error result.",
        WorkflowKind.Storage or WorkflowKind.StorageSpaces => "A volume with low free space and an incomplete health observation.",
        WorkflowKind.BitLocker => "Suspended protection on the Windows volume. Recovery passwords are omitted from this report.",
        WorkflowKind.Defender => "Stale security intelligence while real-time protection remained enabled.",
        WorkflowKind.Certificates => "One certificate approaching expiration.",
        WorkflowKind.ReliabilityHistory or WorkflowKind.Applications or WorkflowKind.HardwareErrors or WorkflowKind.DriverInstallHistory => "Recent historical evidence that needs correlation with current behavior.",
        _ when Workflows.SupportsRepair(kind) => "An enabled Windows component that is stopped and eligible for a guarded recovery review.",
        _ => "One observation that needs review. This workflow does not select an automatic repair."
    };
}
