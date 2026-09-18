namespace KillerBench.Engine;

public static class DemoWorkflow
{
    public const string MachineName = "KILLERBENCH-DEMO";

    public static WorkflowResult Create(WorkflowKind kind, bool simulateRepair = false,
        DateTimeOffset? startedUtc = null)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var definition = WorkflowCatalog.All.Single(item => item.Kind == kind);
        var checks = new List<WorkflowCheck>
        {
            new("Demo environment", CheckState.Information,
                "Fabricated Windows workstation. No local provider, log, registry key, service, device or file was read."),
            new(definition.Title, CheckState.Attention, Scenario(kind), Evidence: SyntheticEvidence(kind))
        };

        string status = "Needs attention";
        if (simulateRepair)
        {
            checks.Add(new("Simulated repair", CheckState.Clear,
                "The selected repair path completed in the demonstration and its synthetic verification passed. No command was executed and no machine state changed."));
            status = "Demo repair simulated";
        }

        return new WorkflowResult(kind, MachineName, startedUtc ?? DateTimeOffset.UtcNow, status,
            checks.AsReadOnly(), Array.Empty<RepairResult>(), "Demo mode: no journal was written.")
        {
            PlannedRecipeIds = Array.Empty<string>()
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
            "Demo mode: one review item found."
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
