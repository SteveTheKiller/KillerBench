using System.Text.Json;

namespace KillerBench.Engine.Windows.Workflows;

public sealed class WindowsRepairHost : IRepairHost
{
    private readonly RepairPlan? selectedPrinterPlan;
    private readonly RepairKind? selectedKind;
    private readonly CancellationToken cancellationToken;
    public WindowsRepairHost() { }
    public WindowsRepairHost(CancellationToken token) => cancellationToken = token;
    internal WindowsRepairHost(RepairKind kind, IReadOnlyList<string>? queues, CancellationToken token)
    {
        selectedKind = kind;
        selectedPrinterPlan = queues is null ? null : kind == RepairKind.PrintQueues
            ? PrintRecovery.Plan(queues) : kind == RepairKind.PrinterOffline
                ? PrinterOfflineRecovery.Plan(queues) : throw new ArgumentException("Selected queues do not apply to this repair.");
        cancellationToken = token;
    }
    public void CheckPrerequisites()
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Repairs require Windows.");
        if (!WindowsState.IsAdministrator()) throw new UnauthorizedAccessException("Run KillerBench as administrator to execute repairs.");
        if (WindowsState.RebootPending()) throw new InvalidOperationException("A pending reboot indicator is present. Restart and reassess before running this repair.");
        if (selectedKind == RepairKind.WindowsRecoveryEnvironment)
            WindowsRecoveryEnable.RequireEligible(WindowsRecoveryDiagnostics.CheckAsync(cancellationToken).GetAwaiter().GetResult());
        if (selectedKind == RepairKind.WmiRepository)
            WmiRecovery.RequireEligible(WmiRecovery.CheckAsync(token: cancellationToken).GetAwaiter().GetResult());
        if (selectedKind == RepairKind.TimeSynchronization)
            TimeRecovery.RequirePlanEligible(TimeRecovery.CheckAsync(null, cancellationToken).GetAwaiter().GetResult());
        if (selectedKind == RepairKind.ComponentCleanup)
            ComponentCleanup.RequireEligible(ComponentCleanup.CheckAsync(token: cancellationToken).GetAwaiter().GetResult());
        if (selectedKind == RepairKind.DefenderSignatures)
            DefenderRecovery.RequireEligible(DefenderRecovery.CheckAsync(token: cancellationToken).GetAwaiter().GetResult());
        if (selectedKind == RepairKind.PrintSpoolDirectory &&
            !PrintSpoolDirectoryRecovery.IsEligible(PrintSpoolDirectoryDiagnostics.Read()))
            throw new InvalidOperationException("Fresh exact missing Windows default print spool directory evidence is required before creation.");
    }
    public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Repairs.Plan(RepairKind.ComponentCleanup).Steps.Any(s => s.Tool == step.Tool && s.Arguments.SequenceEqual(step.Arguments)))
            ComponentCleanup.RequireIdleServicing();
        if (selectedPrinterPlan is not null)
        {
            if (!selectedPrinterPlan.Steps.Any(s => s.Tool == step.Tool && s.Verification == step.Verification && s.Arguments.SequenceEqual(step.Arguments)))
                throw new ArgumentException("Command does not match selected printer scope.");
            CheckPrerequisites();
            return BoundedTool.RunAsync(step, TimeSpan.FromSeconds(45), progress, cancellationToken);
        }
        if (!Enum.GetValues<RepairKind>().SelectMany(k => Repairs.Plan(k).Steps).Any(s => s.Tool == step.Tool && s.Arguments.SequenceEqual(step.Arguments)))
            throw new ArgumentException("Unsupported repair command.");
        if (Repairs.Plan(RepairKind.WindowsInstaller).Steps.Any(s => !s.Verification && s.Tool == step.Tool && s.Arguments.SequenceEqual(step.Arguments)))
            InstallerDiagnostics.RequireReady();
        if (selectedKind is RepairKind.ComponentStore or RepairKind.SystemFiles && !step.Verification)
            return ServicingRepairExecution.RunCommandAsync(selectedKind.Value, step,
                PlatformProviders.CreateServicingRepairCommandHost(), progress, cancellationToken);
        return step.Tool == PrintSpoolDirectoryRecovery.Tool ? PrintSpoolDirectoryRecovery.ExecuteAsync(step, progress, cancellationToken)
            : Repairs.Plan(RepairKind.ComponentCleanup).Steps.Any(s => s.Tool == step.Tool && s.Verification == step.Verification && s.Arguments.SequenceEqual(step.Arguments)) ? ComponentCleanup.ExecuteAsync(step, progress, cancellationToken)
            : Repairs.Plan(RepairKind.DeliveryOptimization).Steps.Any(s => s.Tool == step.Tool && s.Verification == step.Verification && s.Arguments.SequenceEqual(step.Arguments)) ? DeliveryOptimizationRecovery.ExecuteAsync(step, progress, token: cancellationToken)
            : Repairs.Plan(RepairKind.DefenderSignatures).Steps.Any(s => s.Tool == step.Tool && s.Arguments.SequenceEqual(step.Arguments)) ? DefenderRecovery.ExecuteAsync(step, progress, cancellationToken)
            : Repairs.Plan(RepairKind.DhcpLease).Steps.Any(s => s.Tool == step.Tool && s.Arguments.SequenceEqual(step.Arguments)) ? DhcpRecovery.ExecuteAsync(step, progress, cancellationToken)
            : step.Tool == PrintRecovery.Tool ? PrintRecovery.ExecuteAsync(step, progress, cancellationToken)
            : step.Tool == WmiRecovery.Tool ? WmiRecovery.ExecuteAsync(step, progress, cancellationToken)
            : Repairs.Plan(RepairKind.TimeSynchronization).Steps.Any(s => s.Tool == step.Tool && s.Arguments.SequenceEqual(step.Arguments)) ? TimeRecovery.ExecuteAsync(step, progress, cancellationToken)
            : Repairs.Plan(RepairKind.WindowsRecoveryEnvironment).Steps.Any(s => s.Tool == step.Tool && s.Verification == step.Verification && s.Arguments.SequenceEqual(step.Arguments)) ? WindowsRecoveryEnable.ExecuteAsync(step, progress, cancellationToken)
            : step.Tool == "sc.exe" ? ServiceRecovery.ExecuteAsync(step, progress, cancellationToken)
            : BoundedTool.RunAsync(step, TimeSpan.FromHours(1), progress, cancellationToken);
    }
}
