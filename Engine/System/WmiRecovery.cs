namespace KillerBench.Engine;

public interface IWmiRecoveryHost
{
    Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token);
}

public static class WmiRecovery
{
    public const string Tool = @"wbem\winmgmt.exe";
    public static RepairStep Step(bool verification) => new(Tool,
        [verification ? "/verifyrepository" : "/salvagerepository"], verification);

    public static WorkflowCheck Interpret(ToolResult result)
    {
        if (result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new("WMI repository", CheckState.Unknown, "Repository evidence is incomplete. Windows may still be completing work after a timeout. No salvage selected; collect a complete result before retrying.", Evidence: result);
        var verdicts = result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Equals("WMI repository is consistent", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("WMI repository is inconsistent", StringComparison.OrdinalIgnoreCase)).ToArray();
        bool consistent = verdicts.Length == 1 && verdicts[0].Equals("WMI repository is consistent", StringComparison.OrdinalIgnoreCase);
        bool inconsistent = verdicts.Length == 1 && verdicts[0].Equals("WMI repository is inconsistent", StringComparison.OrdinalIgnoreCase);
        if (consistent && result.ExitCode == 0)
            return new("WMI repository", CheckState.Clear, "Repository consistency verified. Individual providers and management applications are not tested.", Evidence: result);
        // ERROR_INTERNAL_DB_CORRUPTION can be returned as a Win32 code or HRESULT.
        if (inconsistent && result.ExitCode is 1358 or unchecked((int)0x8007054E))
            return new("WMI repository", CheckState.Repairable, "Repository inconsistency confirmed. Salvage rebuilds it and merges readable content; unreadable provider registrations may need reinstalling. No repository reset is used.", RepairKind.WmiRepository, result);
        return new("WMI repository", CheckState.Unknown, "Repository consistency could not be established. No salvage selected. Review the recorded output.", Evidence: result);
    }

    public static bool IsEligible(WorkflowCheck? check)
    {
        if (check is not { State: CheckState.Repairable, Repair: RepairKind.WmiRepository, Evidence: not null }) return false;
        var interpreted = Interpret(check.Evidence);
        return interpreted.State == CheckState.Repairable && interpreted.Repair == RepairKind.WmiRepository;
    }

    public static void RequireEligible(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (!IsEligible(check))
            throw new InvalidOperationException("Fresh exact inconsistent WMI repository evidence is required before salvage. " + check.Detail);
    }

    public static Task<WorkflowCheck> CheckAsync(IProgress<string>? progress = null, CancellationToken token = default) =>
        CheckAsync(new WindowsWmiRecoveryHost(), progress, token);

    public static async Task<WorkflowCheck> CheckAsync(IWmiRecoveryHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        return Interpret(await RunBounded(Step(true), host, progress, TimeSpan.FromMinutes(1), token).ConfigureAwait(false));
    }

    internal static Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress, CancellationToken token = default) =>
        ExecuteAsync(step, new WindowsWmiRecoveryHost(), progress, token);

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, IWmiRecoveryHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(host);
        bool salvage = Matches(step, Step(false));
        bool verify = Matches(step, Step(true));
        if (!salvage && !verify) throw new ArgumentException("Command does not match the WMI repository repair plan.", nameof(step));
        if (verify) return await RunBounded(step, host, progress, TimeSpan.FromMinutes(1), token).ConfigureAwait(false);

        progress?.Report("Rechecking WMI repository consistency before salvage...");
        WorkflowCheck current = await CheckAsync(host, progress, token).ConfigureAwait(false);
        if (current is { State: CheckState.Clear, Repair: null, Evidence: not null } && Interpret(current.Evidence).State == CheckState.Clear)
        {
            progress?.Report("The WMI repository is now consistent. No salvage command was run.");
            throw new RepairStepSkippedException("The WMI repository is now consistent. No salvage command was run.", current.Evidence);
        }
        if (!IsEligible(current))
            throw new RepairDispatchDeferredException(
                "WMI salvage was deferred because fresh exact inconsistent evidence was not available. " + current.Detail);

        progress?.Report("Fresh repository inconsistency confirmed. Starting bounded WMI salvage...");
        return await RunBounded(step, host, progress, TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
    }

    private static bool Matches(RepairStep actual, RepairStep expected) =>
        actual.Tool == expected.Tool && actual.Verification == expected.Verification && actual.Arguments.SequenceEqual(expected.Arguments);

    private static async Task<ToolResult> RunBounded(RepairStep step, IWmiRecoveryHost host,
        IProgress<string>? progress, TimeSpan limit, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(limit);
        try
        {
            var pending = host.RunAsync(step, progress, timeout.Token)
                ?? throw new InvalidOperationException("WMI provider returned no command task.");
            return await pending.WaitAsync(limit + TimeSpan.FromSeconds(1), token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("WMI provider returned no command evidence.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new(-1, "", "WMI operation timed out. The service may still be completing work; repository state is unverified. Do not immediately repeat salvage.", false) { TimedOut = true };
        }
        catch (TimeoutException)
        {
            timeout.Cancel();
            return new(-1, "", "WMI operation timed out. The service may still be completing work; repository state is unverified. Do not immediately repeat salvage.", false) { TimedOut = true };
        }
    }
}

public sealed class WindowsWmiRecoveryHost : IWmiRecoveryHost
{
    public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token) =>
        NativeTool.RunAsync(step.Tool, step.Arguments, progress: progress, token: token);
}
