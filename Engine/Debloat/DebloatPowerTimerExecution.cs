using System.Text.Json;

namespace KillerBench.Engine.Debloat;

public enum DebloatPowerTimerOutcome { Preview, Blocked, Verified, Uncertain, Stopped }

public enum DebloatPowerTimerSetting { SleepAc, SleepDc, DisplayAc, DisplayDc }

public sealed record DebloatPowerTimerCommand(DebloatPowerTimerSetting Setting, uint Minutes, ToolResult Result);

public sealed record DebloatPowerTimerResult(DebloatPowerRequest Request, DebloatPowerTimerOutcome Outcome,
    string Detail, string? JournalPath = null, DebloatPowerEvidence? Before = null,
    IReadOnlyList<DebloatPowerTimerCommand>? Commands = null, DebloatPowerEvidence? After = null)
{
    public int GetExitCode() => Outcome switch
    {
        DebloatPowerTimerOutcome.Preview or DebloatPowerTimerOutcome.Verified => 0,
        DebloatPowerTimerOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IDebloatPowerTimerHost
{
    bool IsAdministrator { get; }
    Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token);
    Task<DebloatPowerEvidence> ReadAsync(CancellationToken token);
    Task<ToolResult> ApplyAsync(DebloatPowerTimerSetting setting, uint minutes, CancellationToken token);
}

public static class DebloatPowerTimerExecution
{
    public const string Effects = "Changes sleep and display idle timers on the currently active Windows power plan for AC and battery power. Zero means Never. It does not change the active plan, hibernation, policy, other power settings or restart Windows. Power behavior changes immediately.";

    public static async Task<DebloatPowerTimerResult> RunAsync(DebloatPowerRequest request, bool execute,
        string journalFolder, IDebloatPowerTimerHost host, IProgress<string>? progress = null,
        CancellationToken token = default, TimeSpan? commandTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = commandTimeout ?? TimeSpan.FromSeconds(30);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(2)) throw new ArgumentOutOfRangeException(nameof(commandTimeout));
        token.ThrowIfCancellationRequested();
        if (!execute) return new(request, DebloatPowerTimerOutcome.Preview,
            Effects + " Execution repeats current values and managed-policy checks before any change.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "debloat-power-timers.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "debloat-power-timers.pending.json");
        if (File.Exists(pendingPath)) return new(request, DebloatPowerTimerOutcome.Blocked,
            "A prior power-timer change needs reconciliation before another change can start.", pendingPath);

        Report(progress, "Checking managed power policy.");
        var policyTask = host.HasManagedPowerPolicyAsync(token) ??
            throw new InvalidOperationException("Managed power-policy inspection provider returned no task.");
        bool? managed = await policyTask.WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
        if (managed != false) return new(request, DebloatPowerTimerOutcome.Blocked,
            managed == true ? "Managed power policy is present. KillerBench will not override it." :
            "Managed power-policy status is unavailable. No power timer was changed.");
        Report(progress, "Rechecking current power timers.");
        var before = await Required(host.ReadAsync, "Power-timer inspection", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        if (!Complete(before)) return new(request, DebloatPowerTimerOutcome.Blocked,
            "Fresh complete active-plan and timer evidence is required before changing power timers.", Before: before);
        if (Matches(before, request)) return new(request, DebloatPowerTimerOutcome.Blocked,
            "All four power timers already match the requested values.", Before: before);
        if (!host.IsAdministrator) return new(request, DebloatPowerTimerOutcome.Blocked,
            "Administrator access is required. No power timer was changed.", Before: before);

        string journalPath = Path.Combine(root, "debloat-power-timers-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new
        {
            Schema = 1, Machine = Environment.MachineName, StartedUtc = DateTimeOffset.UtcNow,
            Request = request, Before = before, JournalPath = journalPath
        }, Capture.JsonOptions), token).ConfigureAwait(false);
        var commands = new List<DebloatPowerTimerCommand>();
        bool commandDispatched = false;
        DebloatPowerTimerResult result;
        try
        {
            Report(progress, "Confirming final power-timer eligibility.");
            var finalPolicyTask = host.HasManagedPowerPolicyAsync(token) ??
                throw new InvalidOperationException("Managed power-policy inspection provider returned no task.");
            bool? finalManaged = await finalPolicyTask.WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
            var current = await Required(host.ReadAsync, "Final power-timer inspection", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            if (finalManaged != false || !host.IsAdministrator || !Complete(current) ||
                !SamePowerState(before, current) || Matches(current, request))
            {
                string detail = finalManaged != false
                    ? "Managed power-policy ownership changed after preparation. No power timer was changed."
                    : !host.IsAdministrator
                        ? "Administrator access changed after preparation. No power timer was changed."
                        : !Complete(current)
                            ? "Complete power evidence became unavailable after preparation. No power timer was changed."
                            : Matches(current, request)
                                ? "All four power timers reached the requested values after preparation. No command was required."
                                : "The active plan, timers, or hibernation state changed after preparation. No power timer was changed.";
                result = new(request, DebloatPowerTimerOutcome.Blocked, detail, journalPath, before,
                    commands.AsReadOnly(), current);
                await SaveAsync(result, journalPath).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            foreach (var (timerSetting, timerMinutes) in Settings(request))
            {
                Report(progress, "Applying " + Label(timerSetting) + ": " + timerMinutes + " minute(s).");
                commandDispatched = true;
                var command = await Required(ct => host.ApplyAsync(timerSetting, timerMinutes, ct),
                    Label(timerSetting), token, limit).ConfigureAwait(false);
                commands.Add(new(timerSetting, timerMinutes, command));
                if (command.ExitCode != 0 || command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
                {
                    result = new(request, DebloatPowerTimerOutcome.Uncertain,
                        "A power-timer command did not provide complete success evidence. Some values may have changed; reconcile before retrying.",
                        journalPath, before, commands.AsReadOnly());
                    await SaveAsync(result, journalPath).ConfigureAwait(false);
                    Report(progress, result.Detail);
                    return result;
                }
            }
            Report(progress, "Verifying all four power timers.");
            var after = await Required(host.ReadAsync, "Power-timer verification", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            bool verified = Complete(after) && before.ActiveSchemeId == after.ActiveSchemeId &&
                before.HibernationEnabled == after.HibernationEnabled &&
                before.HibernationFilePresent == after.HibernationFilePresent && Matches(after, request);
            result = new(request, verified ? DebloatPowerTimerOutcome.Verified : DebloatPowerTimerOutcome.Uncertain,
                verified ? "Fresh evidence confirms all four requested timers on the same active power plan with hibernation unchanged."
                    : "Commands completed, but fresh evidence did not confirm all requested timers on the same active plan with hibernation unchanged.",
                journalPath, before, commands.AsReadOnly(), after);
        }
        catch (OperationCanceledException)
        {
            result = new(request, DebloatPowerTimerOutcome.Stopped,
                commandDispatched
                    ? "Power-timer configuration stopped after command dispatch. Some timers may have changed; reconcile before retrying."
                    : "Final power-timer inspection stopped before command dispatch. No power timer was changed.",
                journalPath, before, commands.AsReadOnly());
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            result = new(request, commandDispatched ? DebloatPowerTimerOutcome.Uncertain : DebloatPowerTimerOutcome.Blocked,
                commandDispatched
                    ? "Power-timer configuration was interrupted after command dispatch: " + error.Message + ". Reconcile before retrying."
                    : "Final power-timer inspection was unavailable before command dispatch: " + error.Message + ". No power timer was changed.",
                journalPath, before, commands.AsReadOnly());
        }
        await SaveAsync(result, journalPath).ConfigureAwait(false);
        if (result.Outcome == DebloatPowerTimerOutcome.Verified || !commandDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    private static bool Complete(DebloatPowerEvidence value) => value.Complete &&
        Guid.TryParseExact(value.ActiveSchemeId, "D", out _) && value.SleepAcSeconds.HasValue &&
        value.SleepDcSeconds.HasValue && value.DisplayAcSeconds.HasValue && value.DisplayDcSeconds.HasValue &&
        value.HibernationEnabled.HasValue && value.HibernationFilePresent.HasValue;

    private static bool Matches(DebloatPowerEvidence value, DebloatPowerRequest request) =>
        value.SleepAcSeconds == checked(request.SleepAcMinutes * 60) &&
        value.SleepDcSeconds == checked(request.SleepDcMinutes * 60) &&
        value.DisplayAcSeconds == checked(request.DisplayAcMinutes * 60) &&
        value.DisplayDcSeconds == checked(request.DisplayDcMinutes * 60);

    private static bool SamePowerState(DebloatPowerEvidence before, DebloatPowerEvidence current) =>
        before.ActiveSchemeId == current.ActiveSchemeId && before.SleepAcSeconds == current.SleepAcSeconds &&
        before.SleepDcSeconds == current.SleepDcSeconds && before.DisplayAcSeconds == current.DisplayAcSeconds &&
        before.DisplayDcSeconds == current.DisplayDcSeconds && before.HibernationEnabled == current.HibernationEnabled &&
        before.HibernationFilePresent == current.HibernationFilePresent;

    private static IEnumerable<(DebloatPowerTimerSetting Setting, uint Minutes)> Settings(DebloatPowerRequest request)
    {
        yield return (DebloatPowerTimerSetting.SleepAc, request.SleepAcMinutes);
        yield return (DebloatPowerTimerSetting.SleepDc, request.SleepDcMinutes);
        yield return (DebloatPowerTimerSetting.DisplayAc, request.DisplayAcMinutes);
        yield return (DebloatPowerTimerSetting.DisplayDc, request.DisplayDcMinutes);
    }

    private static string Label(DebloatPowerTimerSetting setting) => setting switch
    {
        DebloatPowerTimerSetting.SleepAc => "AC sleep timeout",
        DebloatPowerTimerSetting.SleepDc => "battery sleep timeout",
        DebloatPowerTimerSetting.DisplayAc => "AC display timeout",
        DebloatPowerTimerSetting.DisplayDc => "battery display timeout",
        _ => throw new ArgumentOutOfRangeException(nameof(setting))
    };

    private static Task SaveAsync(DebloatPowerTimerResult result, string path) =>
        JournalPersistence.WriteAsync(path, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None);

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

public sealed class WindowsDebloatPowerTimerHost : IDebloatPowerTimerHost
{
    private readonly WindowsDebloatPowerReader reader = new();
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token) => reader.ReadAsync(token);

    public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult<bool?>(null);
        try
        {
            using var groupPolicy = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Power\PowerSettings");
            using var policyManager = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Power");
            return Task.FromResult<bool?>(groupPolicy is not null || policyManager is not null);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Task.FromResult<bool?>(null);
        }
    }

    public Task<ToolResult> ApplyAsync(DebloatPowerTimerSetting setting, uint minutes, CancellationToken token)
    {
        string option = setting switch
        {
            DebloatPowerTimerSetting.SleepAc => "/change standby-timeout-ac",
            DebloatPowerTimerSetting.SleepDc => "/change standby-timeout-dc",
            DebloatPowerTimerSetting.DisplayAc => "/change monitor-timeout-ac",
            DebloatPowerTimerSetting.DisplayDc => "/change monitor-timeout-dc",
            _ => throw new ArgumentOutOfRangeException(nameof(setting))
        };
        return BoundedTool.RunAsync(new("powercfg.exe", [option, minutes.ToString(System.Globalization.CultureInfo.InvariantCulture)], false),
            TimeSpan.FromSeconds(30), token: token);
    }
}
