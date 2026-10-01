namespace KillerBench.Engine.SystemHealth;

public interface IWindowsRecoveryEnableHost
{
    Task<WorkflowCheck> InspectAsync(CancellationToken token);
    Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token);
}

public static class WindowsRecoveryEnable
{
    private static readonly TimeSpan InspectionLimit = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan CommandLimit = TimeSpan.FromMinutes(1);

    public static bool IsEligible(WorkflowCheck? check)
    {
        if (check is not { State: CheckState.Repairable, Repair: RepairKind.WindowsRecoveryEnvironment, Evidence: not null })
            return false;
        var interpreted = WindowsRecoveryDiagnostics.Interpret(check.Evidence);
        return interpreted.State == CheckState.Repairable && interpreted.Repair == RepairKind.WindowsRecoveryEnvironment;
    }

    public static void RequireEligible(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (!IsEligible(check))
            throw new InvalidOperationException("Fresh exact Windows Recovery Environment Disabled evidence is required before enablement. " + check.Detail);
    }

    public static Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress = null, CancellationToken token = default) =>
        ExecuteAsync(step, new WindowsRecoveryEnableHost(), progress, token);

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, IWindowsRecoveryEnableHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(host);
        bool enable = Matches(step, WindowsRecoveryDiagnostics.EnableStep());
        bool verify = Matches(step, WindowsRecoveryDiagnostics.Step());
        if (!enable && !verify)
            throw new ArgumentException("Command does not match the Windows Recovery Environment repair plan.", nameof(step));

        token.ThrowIfCancellationRequested();
        if (verify)
            return await Required(host.RunAsync(step, progress, token), CommandLimit, token,
                "Windows Recovery verification provider").ConfigureAwait(false);

        progress?.Report("Rechecking Windows Recovery Environment eligibility...");
        WorkflowCheck current = await Required(host.InspectAsync(token), InspectionLimit, token,
            "Windows Recovery inspection provider").ConfigureAwait(false);
        if (IsEnabled(current))
        {
            progress?.Report("Windows Recovery Environment is already enabled. No enable command was run.");
            throw new RepairStepSkippedException("Windows Recovery Environment is already enabled. No enable command was run.", current.Evidence!);
        }
        if (!IsEligible(current))
            throw new RepairDispatchDeferredException(
                "Windows Recovery enablement was deferred because fresh exact Disabled evidence was not available. " + current.Detail);

        progress?.Report("Fresh Disabled evidence confirmed. Enabling Windows Recovery Environment...");
        return await Required(host.RunAsync(step, progress, token), CommandLimit, token,
            "Windows Recovery enable provider").ConfigureAwait(false);
    }

    private static bool IsEnabled(WorkflowCheck check)
    {
        if (check is not { State: CheckState.Clear, Repair: null, Evidence: not null }) return false;
        var interpreted = WindowsRecoveryDiagnostics.Interpret(check.Evidence);
        return interpreted.State == CheckState.Clear && interpreted.Repair is null;
    }

    private static bool Matches(RepairStep actual, RepairStep expected) =>
        actual.Tool == expected.Tool && actual.Verification == expected.Verification &&
        actual.Arguments.SequenceEqual(expected.Arguments);

    private static async Task<T> Required<T>(Task<T>? task, TimeSpan limit, CancellationToken token, string provider) where T : class
    {
        if (task is null) throw new InvalidOperationException(provider + " returned no task.");
        return await task.WaitAsync(limit, token).ConfigureAwait(false)
            ?? throw new InvalidOperationException(provider + " returned no evidence.");
    }
}

public sealed class WindowsRecoveryEnableHost : IWindowsRecoveryEnableHost
{
    public Task<WorkflowCheck> InspectAsync(CancellationToken token) => WindowsRecoveryDiagnostics.CheckAsync(token);

    public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token) =>
        BoundedTool.RunAsync(step, step.Verification ? TimeSpan.FromSeconds(20) : TimeSpan.FromMinutes(1), progress, token);
}
