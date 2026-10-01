using System.Text.RegularExpressions;

namespace KillerBench.Engine.Cleanup;

public interface IComponentCleanupHost
{
    Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token);
}

public static partial class ComponentCleanup
{
    [GeneratedRegex(@"^[ \t]*Component Store Cleanup Recommended[ \t]*:[ \t]*(Yes|No)[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RecommendationRegex();

    public static RepairStep Step(bool verification) => new("dism.exe",
        ["/Online", "/Cleanup-Image", verification ? "/AnalyzeComponentStore" : "/StartComponentCleanup", "/NoRestart", "/English"], verification);

    public static WorkflowCheck Interpret(ToolResult result)
    {
        var unknown = new WorkflowCheck("Component-store cleanup", CheckState.Unknown,
            "Cleanup recommendation was not verified. No maintenance selected.", Evidence: result);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error) ||
            !result.Output.Contains("The operation completed successfully.", StringComparison.OrdinalIgnoreCase)) return unknown;
        var matches = RecommendationRegex().Matches(result.Output);
        if (matches.Count != 1) return unknown;
        bool recommended = matches[0].Groups[1].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
        return new("Component-store cleanup", recommended ? CheckState.Repairable : CheckState.Clear,
            recommended ? "Windows recommends component-store cleanup. Optional maintenance removes superseded components; this is not a corruption diagnosis." : "Windows no longer recommends component-store cleanup. This does not measure reclaimed space or overall system health.",
            recommended ? RepairKind.ComponentCleanup : null, result);
    }

    public static void RequireIdleServicing()
    {
        foreach (string name in new[] { "dism", "TiWorker" })
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(name);
            bool active = processes.Length > 0;
            foreach (var process in processes) process.Dispose();
            if (active) throw new InvalidOperationException("Windows servicing is already running. Wait for it to finish before component-store maintenance.");
        }
    }

    public static bool IsEligible(WorkflowCheck? check)
    {
        if (check is not { State: CheckState.Repairable, Repair: RepairKind.ComponentCleanup, Evidence: not null }) return false;
        var interpreted = Interpret(check.Evidence);
        return interpreted.State == CheckState.Repairable && interpreted.Repair == RepairKind.ComponentCleanup;
    }

    public static void RequireEligible(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (!IsEligible(check))
            throw new InvalidOperationException("Fresh exact component-store cleanup recommendation evidence is required before maintenance. " + check.Detail);
    }

    public static Task<WorkflowCheck> CheckAsync(IProgress<string>? progress = null, CancellationToken token = default) =>
        CheckAsync(new WindowsComponentCleanupHost(), progress, token);

    public static async Task<WorkflowCheck> CheckAsync(IComponentCleanupHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        return Interpret(await RunBounded(Step(true), host, progress, token).ConfigureAwait(false));
    }

    public static Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress = null, CancellationToken token = default) =>
        ExecuteAsync(step, new WindowsComponentCleanupHost(), progress, token);

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, IComponentCleanupHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(host);
        bool cleanup = Matches(step, Step(false));
        bool verify = Matches(step, Step(true));
        if (!cleanup && !verify)
            throw new ArgumentException("Command does not match the component-store cleanup plan.", nameof(step));
        if (verify) return await RunBounded(step, host, progress, token).ConfigureAwait(false);

        progress?.Report("Rechecking the component-store cleanup recommendation...");
        var current = await CheckAsync(host, progress, token).ConfigureAwait(false);
        if (current is { State: CheckState.Clear, Repair: null, Evidence: not null } && Interpret(current.Evidence).State == CheckState.Clear)
        {
            progress?.Report("Windows no longer recommends component-store cleanup. No cleanup command was run.");
            throw new RepairStepSkippedException("Windows no longer recommends component-store cleanup. No cleanup command was run.", current.Evidence);
        }
        if (!IsEligible(current))
            throw new RepairDispatchDeferredException(
                "Component-store cleanup was deferred because a fresh exact recommendation was not available. " + current.Detail);

        RequireIdleServicing();
        progress?.Report("Fresh cleanup recommendation confirmed. Starting bounded component-store maintenance...");
        return await RunBounded(step, host, progress, token).ConfigureAwait(false);
    }

    private static bool Matches(RepairStep actual, RepairStep expected) =>
        actual.Tool == expected.Tool && actual.Verification == expected.Verification && actual.Arguments.SequenceEqual(expected.Arguments);

    private static async Task<ToolResult> RunBounded(RepairStep step, IComponentCleanupHost host,
        IProgress<string>? progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var pending = host.RunAsync(step, progress, token)
            ?? throw new InvalidOperationException("Component cleanup provider returned no command task.");
        return await pending.WaitAsync(TimeSpan.FromHours(1), token).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Component cleanup provider returned no command evidence.");
    }
}

public sealed class WindowsComponentCleanupHost : IComponentCleanupHost
{
    public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token) =>
        BoundedTool.RunAsync(step, TimeSpan.FromHours(1), progress, token);
}
