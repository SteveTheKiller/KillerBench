using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record UpdateExecutionOutcome(UpdateSelection Selection, bool Installed, bool RestartRequired, string Detail);
public sealed record UpdateExecutionResult(string Status, string JournalPath, IReadOnlyList<UpdateExecutionOutcome> Outcomes)
{
    public IReadOnlyList<string> Details { get; init; } = [];
    public int SelectedCount { get; init; }
    public int GetExitCode() => Status == "Stopped" ? 130 :
        Status == "Restart required" || Outcomes.Any(o => o.RestartRequired) ? 4 :
        Status == "Completed" && SelectedCount > 0 && Outcomes.Count == SelectedCount &&
        Outcomes.All(o => o.Installed) && Outcomes.Select(o => o.Selection).Distinct().Count() == SelectedCount ? 0 : 3;
    public string Format() => $"UPDATE INSTALLATION / {(Status == "Completed" && GetExitCode() != 0 ? "Verification incomplete" : Status)}\nSelected: {SelectedCount}; verified installed: {Outcomes.Count(o => o.Installed)}; outcomes returned: {Outcomes.Count}\nJournal: {JournalPath}\n\n" +
        string.Join("\n", Details) + "\n\n" + string.Join("\n", Outcomes.Select(o => $"{o.Selection.Id} revision {o.Selection.Revision}\nInstalled: {o.Installed}; restart required: {o.RestartRequired}\n{o.Detail}\n")) +
        "\nNEXT: " + (GetExitCode() == 0 ? "Selected updates verified. This does not establish overall system health." :
            GetExitCode() == 4 ? "Resolve the restart requirement when convenient, then reconcile the pending operation and verify installed state before another attempt." :
            Status == "Blocked" ? "Resolve the listed prerequisites, then search again." :
            "Review the journal and Windows Update. Resolve restart requirements and check the pending operation before another attempt.");
}

public interface IUpdateExecutionHost
{
    Task<string?> ReadinessAsync(CancellationToken token);
    Task<UpdateSearchResult> SearchAsync(UpdateSearchOptions options, CancellationToken token);
    // Providers must bound native execution, preserve the source and verify the exact update.
    Task<UpdateExecutionOutcome> InstallAndVerifyAsync(UpdateSearchOptions search, UpdateSelection selection, UpdateInstallOptions options, CancellationToken token);
}

public static class UpdateExecution
{
    public static async Task<UpdateExecutionResult> RunAsync(UpdateSearchOptions search, IReadOnlyList<UpdateSelection> selections,
        UpdateInstallOptions options, string journalFolder, IUpdateExecutionHost host, IProgress<string>? progress = null, CancellationToken token = default)
        => await RunBoundedAsync(search, selections, options, journalFolder, host, TimeSpan.FromHours(2), progress, token: token).ConfigureAwait(false);

    public static async Task<UpdateExecutionResult> RunBoundedAsync(UpdateSearchOptions search, IReadOnlyList<UpdateSelection> selections,
        UpdateInstallOptions options, string journalFolder, IUpdateExecutionHost host, TimeSpan installationLimit,
        IProgress<string>? progress = null, TimeSpan? prerequisiteLimit = null, CancellationToken token = default)
    {
        if (installationLimit <= TimeSpan.Zero || installationLimit > TimeSpan.FromHours(2))
            throw new ArgumentOutOfRangeException(nameof(installationLimit));
        var prerequisiteTimeout = prerequisiteLimit ?? TimeSpan.FromMinutes(10);
        if (prerequisiteTimeout <= TimeSpan.Zero || prerequisiteTimeout > TimeSpan.FromMinutes(10))
            throw new ArgumentOutOfRangeException(nameof(prerequisiteLimit));
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(selections);
        token.ThrowIfCancellationRequested();
        search.Criteria();
        var targets = selections.Take(501).ToArray();
        if (targets.Length is < 1 or > 500 || targets.Any(s => s.Id == Guid.Empty || s.Revision < 0) || targets.Distinct().Count() != targets.Length)
            throw new ArgumentException("Select unique update identities and revisions before execution.");
        Directory.CreateDirectory(journalFolder);
        // Shared by callers using this journal folder. A prepared record survives unexpected termination.
        using var gate = new FileStream(Path.Combine(journalFolder, "execution.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(journalFolder, "pending.json");
        if (File.Exists(pending)) throw new InvalidOperationException("A prior update operation needs reconciliation before another installation can start.");
        string journal = Path.Combine(journalFolder, "updates-" + Guid.NewGuid().ToString("N") + ".jsonl");
        using var stream = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        var outcomes = new List<UpdateExecutionOutcome>();
        var details = new List<string>();
        void Record(object value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value) + "\n");
            stream.Write(bytes); stream.Flush(true);
        }
        string status = "Completed";
        bool preparedWithoutDispatch = false;
        bool installationDispatched = false;
        void ArchivePrepared()
        {
            if (!preparedWithoutDispatch || !File.Exists(pending)) return;
            using var record = RecoveryRecordFiles.OpenLocked(pending);
            RecoveryRecordFiles.Archive(record, journal + ".prepared.json");
            preparedWithoutDispatch = false;
        }
        Record(new { Stage = "Started", Search = search, Options = options, Targets = targets });
        try
        {
            Report(progress, "Checking selected updates against a fresh search...");
            var discovery = await PrerequisiteAsync(t => host.SearchAsync(search, t), "Update search", prerequisiteTimeout, token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Update search returned no result.");
            token.ThrowIfCancellationRequested();
            if (discovery.Options != search)
                throw new InvalidOperationException("Update provider returned a different software or driver scope. Search again with the requested options.");
            var plan = UpdatePlanning.Create(discovery, targets, options);
            Record(new { Stage = "Plan", Plan = plan });
            if (!plan.Eligible)
            {
                status = "Blocked";
                details.AddRange(plan.Updates.SelectMany(u => u.Blockers.Select(b => u.Update.Title + ": " + b)));
            }
            else foreach (var target in targets)
            {
                preparedWithoutDispatch = false;
                installationDispatched = false;
                token.ThrowIfCancellationRequested();
                Report(progress, "Checking installation prerequisites...");
                string? blocker = await PrerequisiteAsync(host.ReadinessAsync, "Update readiness", prerequisiteTimeout, token).ConfigureAwait(false);
                if (blocker is not null) { details.Add(blocker); Record(new { Stage = "Blocked", Detail = blocker }); status = "Blocked"; break; }
                token.ThrowIfCancellationRequested();
                using (var prepared = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                {
                    JsonSerializer.Serialize(prepared, new
                    {
                        Machine = Environment.MachineName, Journal = journal, Search = search,
                        Target = target, PreparedUtc = DateTimeOffset.UtcNow
                    });
                    prepared.Flush(true);
                }
                preparedWithoutDispatch = true;
                Record(new { Stage = "Prepared", Target = target });
                using (var preparedRecord = RecoveryRecordFiles.OpenLocked(pending))
                {
                    string? finalBlocker = await PrerequisiteAsync(host.ReadinessAsync, "Final update readiness", prerequisiteTimeout, token).ConfigureAwait(false);
                    var finalDiscovery = await PrerequisiteAsync(t => host.SearchAsync(search, t), "Final update search", prerequisiteTimeout, token).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("Final update search returned no result.");
                    string? scopeBlocker = finalDiscovery.Options == search
                        ? null : "Update provider returned a different software or driver scope.";
                    UpdateInstallPlan? finalPlan = scopeBlocker is null
                        ? UpdatePlanning.Create(finalDiscovery, [target], options) : null;
                    Record(new { Stage = "FinalEligibility", Target = target, Blocker = finalBlocker, ScopeBlocker = scopeBlocker, Plan = finalPlan });
                    if (finalBlocker is not null || scopeBlocker is not null || finalPlan?.Eligible != true)
                    {
                        if (finalBlocker is not null) details.Add(finalBlocker);
                        if (scopeBlocker is not null) details.Add(scopeBlocker);
                        if (finalPlan is not null)
                            details.AddRange(finalPlan.Updates.SelectMany(update => update.Blockers.Select(blocker => update.Update.Title + ": " + blocker)));
                        status = "Blocked";
                        RecoveryRecordFiles.Archive(preparedRecord, journal + ".prepared.json");
                        preparedWithoutDispatch = false;
                        break;
                    }
                }
                if (status == "Blocked") break;
                Report(progress, $"Installing selected update {target.Id}, revision {target.Revision}...");
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                using var installationStop = CancellationTokenSource.CreateLinkedTokenSource(token);
                installationStop.CancelAfter(installationLimit);
                installationDispatched = true;
                var installation = UpdateProviderLease.Start(journalFolder,
                    () => host.InstallAndVerifyAsync(search, target, options, installationStop.Token)).WaitAsync(installationLimit, token);
                while (!installation.IsCompleted)
                {
                    await Task.WhenAny(installation, Task.Delay(TimeSpan.FromSeconds(20))).ConfigureAwait(false);
                    if (!installation.IsCompleted)
                        Report(progress, $"Selected update {target.Id} is still running ({elapsed.Elapsed.TotalSeconds:F0} seconds). Waiting for Windows and verification; completion is not yet confirmed.");
                }
                var outcome = await installation.ConfigureAwait(false);
                if (outcome.Selection != target) throw new InvalidOperationException("Update provider returned a different identity or revision.");
                outcomes.Add(outcome);
                Record(new { Stage = "Result", Outcome = outcome });
                Report(progress, $"Update {target.Id}: {outcome.Detail}" + (outcome.RestartRequired ? " A restart is required." : ""));
                if (!outcome.Installed) { status = "Needs review"; break; }
                if (outcome.RestartRequired) { status = "Restart required"; break; }
                File.Delete(pending);
                preparedWithoutDispatch = false;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            if (!installationDispatched) ArchivePrepared();
            status = "Stopped";
            details.Add(installationDispatched ? "Operation stopped. The prepared update remains pending until verified." : "Operation stopped before installation dispatch. No update request was sent.");
        }
        catch (OperationCanceledException e)
        {
            if (!installationDispatched) ArchivePrepared();
            status = installationDispatched ? "Needs review" : "Blocked";
            details.Add(installationDispatched ? "Update provider stopped before verification: " + e.Message : "Final update eligibility stopped before installation dispatch. No update request was sent.");
            Record(new { Stage = "Error", Detail = details[^1] });
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException or
            System.Security.SecurityException or System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException or TimeoutException)
        {
            bool finalEligibilityFailure = preparedWithoutDispatch && !installationDispatched;
            if (finalEligibilityFailure) ArchivePrepared();
            status = finalEligibilityFailure ? "Blocked" : "Needs review";
            details.Add(e.Message);
            Record(new { Stage = "Error", Detail = e.Message });
        }
        var result = new UpdateExecutionResult(status, journal, outcomes.AsReadOnly()) { Details = details.AsReadOnly(), SelectedCount = targets.Length };
        Record(new { Stage = "Finished", Result = result });
        return result;
    }

    private static async Task<T> PrerequisiteAsync<T>(Func<CancellationToken, Task<T>> read, string stage, TimeSpan limit, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        try
        {
            var task = read(deadline.Token) ?? throw new InvalidOperationException(stage + " returned no task.");
            return await task.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new TimeoutException(stage + " timed out. Installation was not dispatched for this target."); }
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        // A display failure must not interrupt verification or durable outcome recording.
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}
