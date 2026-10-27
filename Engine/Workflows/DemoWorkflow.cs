namespace KillerBench.Engine.Workflows;

public static class DemoWorkflow
{
    public const string MachineName = "KILLERBENCH-DEMO";
    public static readonly DateTimeOffset ScreenshotStartedUtc = new(2026, 1, 15, 18, 42, 0, TimeSpan.Zero);

    public static WorkflowResult Create(WorkflowKind kind, bool simulateRepair = false,
        DateTimeOffset? startedUtc = null, bool screenshotPreset = false)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var definition = WorkflowCatalog.All.Single(item => item.Kind == kind);
        var checks = new List<WorkflowCheck>
        {
            new("Demo environment", CheckState.Information,
                "Fabricated Windows workstation. No local provider, log, registry key, service, device or file was read.")
        };
        if (screenshotPreset) checks.AddRange(ScreenshotChecks(kind));
        else checks.Add(new(definition.Title, CheckState.Attention, Scenario(kind), Evidence: SyntheticEvidence(kind)));

        string status = "Needs attention";
        if (simulateRepair)
        {
            checks.Add(new("Simulated repair", CheckState.Clear,
                "The selected repair path completed in the demonstration and its synthetic verification passed. No command was executed and no machine state changed."));
            status = "Demo repair simulated";
        }

        return new WorkflowResult(kind, MachineName, startedUtc ?? (screenshotPreset ? ScreenshotStartedUtc : DateTimeOffset.UtcNow), status,
            checks.AsReadOnly(), [], "Demo mode: no journal was written.")
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
            "Demo mode: preparing the fabricated workstation...",
            "Demo mode: collecting synthetic " + title + " evidence...",
            "Demo mode: evaluating deterministic rules...",
            "Demo mode: synthetic review items found."
        };
        if (simulateRepair)
        {
            messages.Add("Demo mode: simulating the eligible repair path...");
            messages.Add("Demo mode: synthetic verification passed. No command was executed.");
        }
        return messages.AsReadOnly();
    }

    private static ToolResult SyntheticEvidence(WorkflowKind kind) =>
        new(0, "KILLERBENCH DEMO EVIDENCE\nWorkflow: " + kind + "\nSource: fabricated fixture", "", false);

    private static IReadOnlyList<WorkflowCheck> ScreenshotChecks(WorkflowKind kind) => kind switch
    {
        WorkflowKind.CommonProblems =>
        [
            new("Windows Update readiness", CheckState.Attention, "Synthetic update service is stopped; review its current state before considering recovery."),
            new("System stability", CheckState.Attention, "Two fabricated application failures need correlation with current behavior."),
            new("Restart status", CheckState.Information, "The fabricated workstation has a pending restart."),
            new("Network readiness", CheckState.Clear, "Synthetic adapter and name-resolution checks completed without a finding."),
            new("Storage coverage", CheckState.Unknown, "Synthetic volume health data is unavailable; no healthy verdict is inferred.")
        ],
        WorkflowKind.WindowsUpdate =>
        [
            new("Update service", CheckState.Attention, "The fabricated update service is stopped; this is a demonstration, not a repair recommendation."),
            new("Servicing activity", CheckState.Information, "A synthetic servicing session is waiting for a restart."),
            new("Recent update history", CheckState.Unknown, "The fabricated history source is incomplete, so the outcome remains unknown."),
            new("Update policy", CheckState.Clear, "Synthetic policy inspection found no restriction in the checked scope.")
        ],
        WorkflowKind.Network =>
        [
            new("Adapter configuration", CheckState.Clear, "The fabricated adapter has a usable address and default gateway."),
            new("DNS response", CheckState.Attention, "The synthetic name-resolution check did not receive a usable response."),
            new("Gateway reachability", CheckState.Information, "Synthetic gateway evidence is available but does not prove Internet access."),
            new("Network history", CheckState.Unknown, "The fabricated event source is unavailable; historical coverage is incomplete.")
        ],
        WorkflowKind.Printing =>
        [
            new("Print queue", CheckState.Attention, "The fabricated office queue is offline with two waiting jobs."),
            new("Print Spooler", CheckState.Clear, "The synthetic Print Spooler service is running."),
            new("Default printer", CheckState.Information, "One fabricated default queue is configured; no printer setting changed."),
            new("Physical output", CheckState.Unknown, "A demonstration cannot verify paper output or device hardware.")
        ],
        WorkflowKind.CoreWindowsServices =>
        [
            new("Windows Event Log", CheckState.Clear, "The synthetic EventLog service is running with automatic start."),
            new("Cryptographic Services", CheckState.Attention, "The fabricated CryptSvc service is stopped although its start type is automatic.", RepairKind.CryptographicServices),
            new("Remote Procedure Call", CheckState.Clear, "The synthetic RPC endpoint mapper and DCOM launcher are running."),
            new("Windows Management Instrumentation", CheckState.Information, "The fabricated WMI repository was verified as consistent last week."),
            new("Service dependencies", CheckState.Unknown, "One synthetic dependent service could not be queried, so dependency coverage is incomplete.")
        ],
        WorkflowKind.Defender =>
        [
            new("Real-time protection", CheckState.Clear, "Synthetic real-time protection is enabled and reporting normally."),
            new("Security intelligence", CheckState.Attention, "The fabricated definitions are nine days old; the last update attempt failed."),
            new("Last quick scan", CheckState.Information, "A synthetic quick scan completed three days ago with no detections."),
            new("Tamper protection", CheckState.Clear, "Synthetic tamper protection is on and managed locally."),
            new("Third-party antivirus", CheckState.Unknown, "The fabricated Security Center inventory is incomplete; another provider cannot be ruled out.")
        ],
        WorkflowKind.Depot =>
        [
            new("Browsers", CheckState.Information, "Two fabricated browsers are selected; one is already registered on the demo workstation."),
            new("Productivity applications", CheckState.Attention, "The selected office suite has no matching Click-to-Run registration on the fabricated workstation."),
            new("Communication applications", CheckState.Information, "One synthetic meeting client is selected and not yet present."),
            new("Configuration: power and sleep", CheckState.Information, "The selected group would change synthetic sleep timeouts; it routes to a separately guarded workflow."),
            new("Configuration: privacy defaults", CheckState.Clear, "The fabricated privacy settings already match the selected group."),
            new("Provisioning boundary", CheckState.Information, "This review downloads, installs and changes nothing.")
        ],
        WorkflowKind.Storage =>
        [
            new("System volume free space", CheckState.Attention, "The fabricated C: volume has 11.8 GB free of 476 GB (2.5 percent)."),
            new("Data volume", CheckState.Clear, "The synthetic D: volume has 612 GB free of 931 GB."),
            new("Disk health", CheckState.Unknown, "The fabricated reliability counters are unavailable; no healthy verdict is inferred."),
            new("File system state", CheckState.Clear, "No synthetic volume is marked dirty."),
            new("Largest folders", CheckState.Information, "Synthetic user downloads and a retained update cache account for 64 GB.")
        ],
        WorkflowKind.BitLocker =>
        [
            new("Operating system volume", CheckState.Attention, "Protection on the fabricated C: volume is suspended until the next restart."),
            new("Key protectors", CheckState.Information, "The synthetic volume has TPM and numerical password protectors. No recovery key is present in this fixture."),
            new("Encryption method", CheckState.Clear, "The fabricated volume uses XTS-AES 128 and is fully encrypted."),
            new("Fixed data volume", CheckState.Clear, "The synthetic D: volume is protected with automatic unlock."),
            new("Recovery key backup", CheckState.Unknown, "Directory backup of the fabricated recovery password cannot be confirmed from the demo.")
        ],
        WorkflowKind.Certificates =>
        [
            new("Expiring soon", CheckState.Attention, "The fabricated client authentication certificate CN=demo-vpn expires in 12 days."),
            new("Machine personal store", CheckState.Clear, "Four synthetic machine certificates are valid for more than 90 days."),
            new("User personal store", CheckState.Information, "One synthetic code-signing certificate expires in 74 days."),
            new("Expired certificates", CheckState.Information, "Two fabricated expired certificates remain in the user store; nothing was removed."),
            new("Private key access", CheckState.Unknown, "Key accessibility for one synthetic certificate could not be confirmed.")
        ],
        _ => [new(WorkflowCatalog.All.Single(item => item.Kind == kind).Title, CheckState.Attention,
            Scenario(kind), Evidence: SyntheticEvidence(kind))]
    };

    private static string Scenario(WorkflowKind kind) => kind switch
    {
        WorkflowKind.CommonProblems => "The demonstration found a stopped update service, an old application crash and a pending restart. The evidence is synthetic and is intended to exercise the complete review flow.",
        WorkflowKind.WindowsUpdate => "The demonstration found a stopped update service and servicing activity waiting for a restart.",
        WorkflowKind.SystemFiles => "The demonstration found synthetic protected-file integrity evidence that requires review.",
        WorkflowKind.Network or WorkflowKind.Wireless or WorkflowKind.WiredAuthentication => "The demonstration found a configured adapter without a usable DNS response.",
        WorkflowKind.Printing => "The demonstration found an offline queue with two waiting jobs.",
        WorkflowKind.Installer => "The demonstration found a failed installation with a retained MSI error result.",
        WorkflowKind.Storage or WorkflowKind.StorageSpaces => "The demonstration found a volume with low free space and an incomplete health observation.",
        WorkflowKind.BitLocker => "The demonstration found suspended protection on the Windows volume. No recovery key is present in this fixture.",
        WorkflowKind.Defender => "The demonstration found stale security intelligence while real-time protection remained enabled.",
        WorkflowKind.Certificates => "The demonstration found one certificate approaching expiration.",
        WorkflowKind.ReliabilityHistory or WorkflowKind.Applications or WorkflowKind.HardwareErrors or WorkflowKind.DriverInstallHistory => "The demonstration found recent historical evidence that needs correlation with current behavior.",
        _ when Workflows.SupportsRepair(kind) => "The demonstration found an enabled Windows component that is stopped and eligible for a guarded recovery review.",
        _ => "The demonstration found one synthetic observation that needs review. This workflow does not select an automatic repair."
    };
}
