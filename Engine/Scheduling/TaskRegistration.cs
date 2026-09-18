namespace KillerBench.Engine;

public sealed record TaskRegistrationPreflight(ScheduledTaskIdentity Target, bool Eligible, string Detail, ToolResult Evidence);
public sealed record TaskRegistrationVerification(ScheduledTaskPlan? Observed, ToolResult Evidence);
public sealed record TaskRegistrationResult(string Status, bool CreateAttempted, string Detail, IReadOnlyList<ToolResult> Evidence);

public interface ITaskRegistrationJournal
{
    // Persist a pending record before returning. Reject an unresolved attempt for this target.
    Task PrepareAsync(ScheduledTaskPlan plan, TaskRegistrationPreflight preflight, CancellationToken token);
    // Close a prepared record when final eligibility fails before creation is dispatched.
    Task AbandonAsync(string detail, CancellationToken token);
    // Persist verified completion; failures must leave the pending record recoverable.
    Task CompleteAsync(TaskRegistrationVerification verification, CancellationToken token);
}

public interface ITaskRegistrationHost
{
    // Verify exact absence, executable identity, account rights and supported scheduler semantics immediately before creation.
    Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan plan, CancellationToken token);
    // Must use create-only semantics, never replace an existing task, and honor bounded cancellation.
    Task<ToolResult> CreateNewAsync(ScheduledTaskPlan plan, CancellationToken token);
    Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token);
}

public static class TaskRegistration
{
    public static async Task<TaskRegistrationResult> RunAsync(ScheduledTaskPlan draft, ITaskRegistrationHost host,
        bool execute = false, CancellationToken token = default, ITaskRegistrationJournal? journal = null,
        TimeSpan? timeout = null, IProgress<string>? progress = null)
    {
        var plan = draft.Validate();
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(90);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromSeconds(90)) throw new ArgumentOutOfRangeException(nameof(timeout));
        var evidence = new List<ToolResult>();
        bool attempted = false;
        bool prepared = false;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(limit);
        TaskRegistrationResult Result(string status, string detail) => new(status, attempted, detail, evidence.AsReadOnly());
        try
        {
            stop.Token.ThrowIfCancellationRequested();
            if (!execute)
            {
                Report(progress, "Registration preview complete. No eligibility probe, journal write or Scheduler request ran.");
                return Result("Preview", "No task created. Fresh eligibility and registration verification are required.");
            }
            if (journal is null) return Result("Blocked", "A recovery journal is required before registration.");
            Report(progress, "Checking the exact task target, account and signed executable.");
            var preflight = await Required(host.CheckAsync(plan, stop.Token), "Preflight", stop.Token).ConfigureAwait(false);
            evidence.Add(preflight.Evidence);
            if (!plan.Identity.Matches(preflight.Target) || !preflight.Eligible || !Complete(preflight.Evidence))
                return Result("Blocked", "Registration preflight did not establish eligibility for the exact target. " + preflight.Detail);
            stop.Token.ThrowIfCancellationRequested();
            Report(progress, "Saving the pending recovery record before task creation.");
            await Required(journal.PrepareAsync(plan, preflight, stop.Token), "Journal preparation", stop.Token).ConfigureAwait(false);
            prepared = true;
            Report(progress, "Repeating exact registration eligibility after preparation.");
            var finalPreflight = await Required(host.CheckAsync(plan, stop.Token), "Final preflight", stop.Token).ConfigureAwait(false);
            evidence.Add(finalPreflight.Evidence);
            if (!plan.Identity.Matches(finalPreflight.Target) || !finalPreflight.Eligible || !Complete(finalPreflight.Evidence))
            {
                await Required(journal.AbandonAsync("Final registration eligibility changed before creation.", CancellationToken.None),
                    "Journal no-dispatch completion", CancellationToken.None).ConfigureAwait(false);
                prepared = false;
                return Result("Blocked", "Final registration eligibility changed before creation. No task creation was dispatched. " + finalPreflight.Detail);
            }
            stop.Token.ThrowIfCancellationRequested();
            Report(progress, "Creating one disabled task definition with create-only semantics.");
            attempted = true;
            var created = await Required(host.CreateNewAsync(plan, stop.Token), "Creation", stop.Token).ConfigureAwait(false);
            evidence.Add(created);
            stop.Token.ThrowIfCancellationRequested();
            if (!Complete(created)) return Result("Unverified", "Creation was attempted but its outcome is incomplete. Inspect the target before retrying.");
            Report(progress, "Reading back the exact disabled task definition.");
            var verification = await Required(host.ReadBackAsync(plan.Identity, stop.Token), "Verification", stop.Token).ConfigureAwait(false);
            evidence.Add(verification.Evidence);
            stop.Token.ThrowIfCancellationRequested();
            if (!Complete(verification.Evidence) || verification.Observed is null ||
                !ScheduledTaskPlanJson.Serialize(plan).SequenceEqual(ScheduledTaskPlanJson.Serialize(verification.Observed)))
                return Result("Unverified", "The registered definition did not verify against the requested plan. No retry or deletion performed.");
            Report(progress, "Saving verified registration evidence.");
            await Required(journal.CompleteAsync(verification, stop.Token), "Journal completion", stop.Token).ConfigureAwait(false);
            Report(progress, "Disabled task definition verified. The task was not run.");
            return Result("Registered", "Created definition verified. The task was not run; action execution is unverified.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && stop.IsCancellationRequested)
        {
            if (prepared && !attempted) await AbandonPreparedAsync(journal!, "Registration timed out before creation.").ConfigureAwait(false);
            return Result(attempted ? "Unverified" : "Timed out", attempted ? "Registration timed out after creation began; the task may exist. Inspect the pending record before retrying." : "Registration timed out before creation. No task creation was dispatched.");
        }
        catch (OperationCanceledException)
        {
            if (prepared && !attempted) await AbandonPreparedAsync(journal!, "Registration was canceled before creation.").ConfigureAwait(false);
            return Result(attempted ? "Unverified" : "Canceled", attempted ? "Registration interrupted after creation began; the task may exist. Inspect before retrying." : "Canceled before creation. No task creation was dispatched.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or
            NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            if (prepared && !attempted) await AbandonPreparedAsync(journal!, "Registration failed before creation: " + error.Message).ConfigureAwait(false);
            return Result(attempted ? "Unverified" : "Blocked", (attempted ? "Creation may have occurred. Inspect the pending record before retrying. " : "Creation was not dispatched. ") + error.Message);
        }
    }

    internal static bool Complete(ToolResult? evidence) => evidence is { ExitCode: 0, Truncated: false, TimedOut: false } && string.IsNullOrWhiteSpace(evidence.Error);

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }

    private static async Task<T> Required<T>(Task<T>? task, string stage, CancellationToken token) where T : class
    {
        if (task is null) throw new InvalidOperationException(stage + " provider returned no task.");
        return await task.WaitAsync(token).ConfigureAwait(false)
            ?? throw new InvalidOperationException(stage + " provider returned no result.");
    }
    private static async Task Required(Task? task, string stage, CancellationToken token)
    {
        if (task is null) throw new InvalidOperationException(stage + " returned no task.");
        await task.WaitAsync(token).ConfigureAwait(false);
    }

    private static async Task AbandonPreparedAsync(ITaskRegistrationJournal journal, string detail)
    {
        try { await Required(journal.AbandonAsync(detail, CancellationToken.None), "Journal no-dispatch completion", CancellationToken.None).ConfigureAwait(false); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or
            NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            // The pending record remains conservative when no-dispatch archival cannot be completed.
        }
    }
}
