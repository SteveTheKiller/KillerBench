using System.Text.Json;

namespace KillerBench.Engine.Debloat;

public enum DebloatPowerPlanOutcome { Preview, Blocked, Verified, Failed, Uncertain, Stopped }

public sealed record DebloatPowerPlanChange(string TargetId, string TargetName, string Effects);

public sealed record DebloatPowerPlanChangeResult(DebloatPowerPlanChange Change,
    DebloatPowerPlanOutcome Outcome, string Detail, string? JournalPath = null,
    DebloatPowerPlanEvidence? Before = null, ToolResult? Command = null,
    DebloatPowerPlanEvidence? After = null)
{
    public int GetExitCode() => Outcome switch
    {
        DebloatPowerPlanOutcome.Preview or DebloatPowerPlanOutcome.Verified => 0,
        DebloatPowerPlanOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IDebloatPowerPlanChangeHost
{
    bool IsAdministrator { get; }
    Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token);
    Task<DebloatPowerPlanEvidence> ReadAsync(CancellationToken token);
    Task<ToolResult> ActivateAsync(string planId, CancellationToken token);
}

public static class DebloatPowerPlanExecution
{
    public static DebloatPowerPlanChange CreateHighPerformance() => new(
        WindowsDebloatPowerPlanReader.HighPerformanceId,
        "High performance",
        "Changes the active Windows power plan to the existing High performance registration. It does not create or delete a plan, alter plan settings, restart Windows or change managed policy. Existing power behavior changes immediately.");

    public static async Task<DebloatPowerPlanChangeResult> RunAsync(DebloatPowerPlanChange change,
        bool execute, string journalFolder, IDebloatPowerPlanChangeHost host,
        IProgress<string>? progress = null, CancellationToken token = default,
        TimeSpan? executionTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = executionTimeout ?? TimeSpan.FromSeconds(30);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(2)) throw new ArgumentOutOfRangeException(nameof(executionTimeout));
        if (!Guid.TryParseExact(change.TargetId, "D", out var target) ||
            target.ToString("D") != WindowsDebloatPowerPlanReader.HighPerformanceId ||
            change.TargetName != "High performance")
            return new(change, DebloatPowerPlanOutcome.Blocked, "Only the exact standard High performance plan is supported.");
        token.ThrowIfCancellationRequested();
        if (!execute) return new(change, DebloatPowerPlanOutcome.Preview,
            change.Effects + " Execution repeats the complete power-plan inventory before any change.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "debloat-power-plan.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "debloat-power-plan.pending.json");
        if (File.Exists(pendingPath)) return new(change, DebloatPowerPlanOutcome.Blocked,
            "A prior power-plan change needs reconciliation before another change can start.", pendingPath);

        Report(progress, "Checking managed power policy.");
        var policyTask = host.HasManagedPowerPolicyAsync(token) ??
            throw new InvalidOperationException("Managed power-policy inspection provider returned no task.");
        bool? managed = await policyTask.WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
        if (managed != false) return new(change, DebloatPowerPlanOutcome.Blocked,
            managed == true ? "Managed power policy is present. KillerBench will not override it." :
            "Managed power-policy status is unavailable. The active plan was not changed.");
        Report(progress, "Rechecking the complete power-plan inventory.");
        var before = await Required(host.ReadAsync, "Power-plan inventory", token, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        if (!Eligible(before, change.TargetId, requireActive: false, out string reason))
            return new(change, DebloatPowerPlanOutcome.Blocked, reason, Before: before);
        if (!host.IsAdministrator)
            return new(change, DebloatPowerPlanOutcome.Blocked, "Administrator access is required. The active plan was not changed.", Before: before);

        string journalPath = Path.Combine(root, "debloat-power-plan-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new
        {
            Schema = 1, Machine = Environment.MachineName, StartedUtc = DateTimeOffset.UtcNow,
            Change = change, Before = before, JournalPath = journalPath
        }, Capture.JsonOptions), token).ConfigureAwait(false);
        ToolResult? command = null;
        bool activationDispatched = false;
        DebloatPowerPlanChangeResult result;
        try
        {
            Report(progress, "Confirming final power-plan eligibility.");
            var finalPolicyTask = host.HasManagedPowerPolicyAsync(token) ??
                throw new InvalidOperationException("Managed power-policy inspection provider returned no task.");
            bool? finalManaged = await finalPolicyTask.WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
            var current = await Required(host.ReadAsync, "Final power-plan inventory", token, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            string finalReason = "Final power-plan eligibility changed.";
            if (finalManaged != false || !host.IsAdministrator ||
                !Eligible(current, change.TargetId, requireActive: false, out finalReason) ||
                !SameRegistrations(before, current))
            {
                string detail = finalManaged != false
                    ? "Managed power-policy ownership changed after preparation. The active plan was not changed."
                    : !host.IsAdministrator
                        ? "Administrator access changed after preparation. The active plan was not changed."
                        : !SameRegistrations(before, current)
                            ? "The registered power-plan set changed after preparation. The active plan was not changed."
                            : finalReason + " The active plan was not changed.";
                result = new(change, DebloatPowerPlanOutcome.Blocked, detail, journalPath, before, After: current);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Activating the existing High performance power plan.");
            activationDispatched = true;
            command = await Required(ct => host.ActivateAsync(change.TargetId, ct), "Power-plan activation", token, limit).ConfigureAwait(false);
            if (command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
                result = new(change, DebloatPowerPlanOutcome.Uncertain,
                    "Activation evidence is incomplete. Reconcile the active plan before retrying.", journalPath, before, command);
            else if (command.ExitCode != 0)
                result = new(change, DebloatPowerPlanOutcome.Failed,
                    "Windows did not report a successful power-plan activation. Reconcile the active plan before retrying.", journalPath, before, command);
            else
            {
                Report(progress, "Verifying the active power plan.");
                var after = await Required(host.ReadAsync, "Power-plan verification", token, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                bool verified = Eligible(after, change.TargetId, requireActive: true, out _) &&
                    SameRegistrations(before, after);
                result = new(change, verified ? DebloatPowerPlanOutcome.Verified : DebloatPowerPlanOutcome.Uncertain,
                    verified ? "A fresh complete inventory confirms the exact High performance plan is active and the registered plan set is unchanged."
                        : "The command completed, but fresh evidence did not confirm the requested active plan with the registered plan set unchanged.",
                    journalPath, before, command, after);
            }
        }
        catch (OperationCanceledException)
        {
            result = new(change, DebloatPowerPlanOutcome.Stopped,
                activationDispatched
                    ? "Power-plan activation stopped after command dispatch. The active plan may have changed; reconcile before retrying."
                    : "Final power-plan inspection stopped before command dispatch. The active plan was not changed.",
                journalPath, before, command);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            result = new(change, activationDispatched ? DebloatPowerPlanOutcome.Uncertain : DebloatPowerPlanOutcome.Blocked,
                activationDispatched
                    ? "Power-plan activation was interrupted after command dispatch: " + error.Message + ". Reconcile before retrying."
                    : "Final power-plan inspection was unavailable before command dispatch: " + error.Message + ". The active plan was not changed.",
                journalPath, before, command);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == DebloatPowerPlanOutcome.Verified || !activationDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    private static bool Eligible(DebloatPowerPlanEvidence evidence, string targetId, bool requireActive, out string reason)
    {
        if (!evidence.Complete || evidence.Evidence is not { ExitCode: 0, TimedOut: false, Truncated: false } raw ||
            !string.IsNullOrWhiteSpace(raw.Error) || evidence.HighPerformance is null ||
            !evidence.HighPerformance.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Fresh complete evidence did not identify the exact High performance plan.";
            return false;
        }
        if (requireActive ? !evidence.HighPerformance.Active : evidence.HighPerformance.Active)
        {
            reason = requireActive ? "The exact High performance plan is not active." : "The exact High performance plan is already active.";
            return false;
        }
        reason = "Eligible";
        return true;
    }

    private static bool SameRegistrations(DebloatPowerPlanEvidence before, DebloatPowerPlanEvidence after) =>
        before.Plans.OrderBy(plan => plan.Id, StringComparer.OrdinalIgnoreCase)
            .Select(plan => (plan.Id.ToUpperInvariant(), plan.Name))
            .SequenceEqual(after.Plans.OrderBy(plan => plan.Id, StringComparer.OrdinalIgnoreCase)
                .Select(plan => (plan.Id.ToUpperInvariant(), plan.Name)));

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan limit)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(limit, token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsDebloatPowerPlanChangeHost : IDebloatPowerPlanChangeHost
{
    private readonly WindowsDebloatPowerPlanReader reader = new();
    private readonly WindowsDebloatPowerTimerHost policy = new();
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => policy.HasManagedPowerPolicyAsync(token);
    public Task<DebloatPowerPlanEvidence> ReadAsync(CancellationToken token) => reader.ReadAsync(token);
    public Task<ToolResult> ActivateAsync(string planId, CancellationToken token) =>
        BoundedTool.RunAsync(new("powercfg.exe", ["/setactive", planId], false), TimeSpan.FromSeconds(30), token: token);
}
