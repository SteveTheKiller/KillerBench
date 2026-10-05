using System.Text.Json;

namespace KillerBench.Engine.Debloat;

public enum DebloatHibernationOutcome { Preview, Blocked, Verified, Uncertain, Stopped }

public sealed record DebloatHibernationResult(DebloatHibernationOutcome Outcome, string Detail,
    string? JournalPath = null, DebloatPowerEvidence? Before = null, ToolResult? Command = null,
    DebloatPowerEvidence? After = null)
{
    public int GetExitCode() => Outcome switch
    {
        DebloatHibernationOutcome.Preview or DebloatHibernationOutcome.Verified => 0,
        DebloatHibernationOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IDebloatHibernationHost
{
    bool IsAdministrator { get; }
    Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token);
    Task<DebloatPowerEvidence> ReadAsync(CancellationToken token);
    Task<ToolResult> DisableAsync(CancellationToken token);
}

public static class DebloatHibernationExecution
{
    public const string Effects = "Disables Windows hibernation and requests removal of hiberfil.sys. Fast Startup can also become unavailable. It does not change the active power plan, sleep or display timers, managed policy or restart Windows. Re-enabling hibernation later can recreate the file.";

    public static async Task<DebloatHibernationResult> RunAsync(bool execute, string journalFolder,
        IDebloatHibernationHost host, IProgress<string>? progress = null, TimeSpan? commandTimeout = null,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = commandTimeout ?? TimeSpan.FromSeconds(30);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(2)) throw new ArgumentOutOfRangeException(nameof(commandTimeout));
        token.ThrowIfCancellationRequested();
        if (!execute) return new(DebloatHibernationOutcome.Preview,
            Effects + " Execution repeats current state and managed-policy checks before any change.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "debloat-hibernation.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "debloat-hibernation.pending.json");
        if (File.Exists(pendingPath)) return new(DebloatHibernationOutcome.Blocked,
            "A prior hibernation change needs reconciliation before another change can start.", pendingPath);

        Report(progress, "Checking managed power policy.");
        var policyTask = host.HasManagedPowerPolicyAsync(token) ??
            throw new InvalidOperationException("Managed power-policy inspection provider returned no task.");
        bool? managed = await policyTask.WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
        if (managed != false) return new(DebloatHibernationOutcome.Blocked,
            managed == true ? "Managed power policy is present. KillerBench will not override it." :
            "Managed power-policy status is unavailable. Hibernation was not changed.");
        Report(progress, "Rechecking hibernation and active-plan state.");
        var before = await Required(host.ReadAsync, "Hibernation inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        if (!Complete(before)) return new(DebloatHibernationOutcome.Blocked,
            "Fresh complete power and hibernation evidence is required before disabling hibernation.", Before: before);
        if (before.HibernationEnabled == false && before.HibernationFilePresent == false)
            return new(DebloatHibernationOutcome.Blocked, "Hibernation is already disabled and the hibernation file is absent.", Before: before);
        if (before.HibernationEnabled != true || before.HibernationFilePresent != true)
            return new(DebloatHibernationOutcome.Blocked,
                "Hibernation state and hibernation-file evidence are contradictory. No change was made.", Before: before);
        if (!host.IsAdministrator) return new(DebloatHibernationOutcome.Blocked,
            "Administrator access is required. Hibernation was not changed.", Before: before);

        string journalPath = Path.Combine(root, "debloat-hibernation-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new
        {
            Schema = 1, Machine = Environment.MachineName, StartedUtc = DateTimeOffset.UtcNow,
            Effects, Before = before, JournalPath = journalPath
        }, Capture.JsonOptions), token: token).ConfigureAwait(false);
        ToolResult? command = null;
        bool disableDispatched = false;
        DebloatHibernationResult result;
        try
        {
            Report(progress, "Confirming final hibernation eligibility.");
            var finalPolicyTask = host.HasManagedPowerPolicyAsync(token) ??
                throw new InvalidOperationException("Managed power-policy inspection provider returned no task.");
            bool? finalManaged = await finalPolicyTask.WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
            var current = await Required(host.ReadAsync, "Final hibernation inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
            if (finalManaged != false || !host.IsAdministrator || !Complete(current) ||
                current.HibernationEnabled != true || current.HibernationFilePresent != true ||
                !SamePowerState(before, current))
            {
                string detail = finalManaged != false
                    ? "Managed power-policy ownership changed after preparation. Hibernation was not changed."
                    : !host.IsAdministrator
                        ? "Administrator access changed after preparation. Hibernation was not changed."
                        : !Complete(current)
                            ? "Complete power and hibernation evidence became unavailable after preparation. Hibernation was not changed."
                            : current.HibernationEnabled == false && current.HibernationFilePresent == false
                                ? "Hibernation became disabled after preparation. No command was required."
                                : "The hibernation, active-plan, or timer state changed after preparation. Hibernation was not changed.";
                result = new(DebloatHibernationOutcome.Blocked, detail, journalPath, before, After: current);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Disabling Windows hibernation.");
            disableDispatched = true;
            command = await Required(host.DisableAsync, "Hibernation command", limit, token: token).ConfigureAwait(false);
            if (command.ExitCode != 0 || command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
                result = new(DebloatHibernationOutcome.Uncertain,
                    "The command did not provide complete success evidence. Hibernation may have changed; reconcile before retrying.",
                    journalPath, before, command);
            else
            {
                Report(progress, "Verifying hibernation and preserved power settings.");
                var after = await Required(host.ReadAsync, "Hibernation verification", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
                bool verified = Complete(after) && after.HibernationEnabled == false && after.HibernationFilePresent == false &&
                    SamePlanAndTimers(before, after);
                result = new(verified ? DebloatHibernationOutcome.Verified : DebloatHibernationOutcome.Uncertain,
                    verified ? "Fresh evidence confirms hibernation is disabled, hiberfil.sys is absent, and the active plan and timers are unchanged."
                        : "The command completed, but fresh evidence did not confirm the requested state with the active plan and timers preserved.",
                    journalPath, before, command, after);
            }
        }
        catch (OperationCanceledException)
        {
            result = new(DebloatHibernationOutcome.Stopped,
                disableDispatched
                    ? "Hibernation disablement stopped after command dispatch. Hibernation may have changed; reconcile before retrying."
                    : "Final hibernation inspection stopped before command dispatch. Hibernation was not changed.",
                journalPath, before, command);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            result = new(disableDispatched ? DebloatHibernationOutcome.Uncertain : DebloatHibernationOutcome.Blocked,
                disableDispatched
                    ? "Hibernation configuration was interrupted after command dispatch: " + error.Message + ". Reconcile before retrying."
                    : "Final hibernation inspection was unavailable before command dispatch: " + error.Message + ". Hibernation was not changed.",
                journalPath, before, command);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == DebloatHibernationOutcome.Verified || !disableDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    private static bool Complete(DebloatPowerEvidence value) => value.Complete &&
        Guid.TryParseExact(value.ActiveSchemeId, "D", out _) && value.SleepAcSeconds.HasValue &&
        value.SleepDcSeconds.HasValue && value.DisplayAcSeconds.HasValue && value.DisplayDcSeconds.HasValue &&
        value.HibernationEnabled.HasValue && value.HibernationFilePresent.HasValue;

    private static bool SamePlanAndTimers(DebloatPowerEvidence before, DebloatPowerEvidence after) =>
        before.ActiveSchemeId == after.ActiveSchemeId && before.SleepAcSeconds == after.SleepAcSeconds &&
        before.SleepDcSeconds == after.SleepDcSeconds && before.DisplayAcSeconds == after.DisplayAcSeconds &&
        before.DisplayDcSeconds == after.DisplayDcSeconds;

    private static bool SamePowerState(DebloatPowerEvidence before, DebloatPowerEvidence current) =>
        SamePlanAndTimers(before, current) && before.HibernationEnabled == current.HibernationEnabled &&
        before.HibernationFilePresent == current.HibernationFilePresent;

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        TimeSpan limit, CancellationToken token)
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

public sealed class WindowsDebloatHibernationHost : IDebloatHibernationHost
{
    private readonly WindowsDebloatPowerReader reader = new();
    private readonly WindowsDebloatPowerTimerHost policy = new();
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => policy.HasManagedPowerPolicyAsync(token);
    public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token) => reader.ReadAsync(token);
    public Task<ToolResult> DisableAsync(CancellationToken token) =>
        BoundedTool.RunAsync(new("powercfg.exe", ["/hibernate", "off"], false), TimeSpan.FromSeconds(30), token: token);
}
