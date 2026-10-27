using System.Text.Json;

namespace KillerBench.Engine.Updates;

public enum UpdateDataStoreResetOutcome { Preview, Blocked, Verified, Failed, Uncertain, Stopped }

public sealed record UpdateResetService(string Name, bool Complete, bool Exists, bool Running, string StartMode);
public sealed record UpdateStoreSnapshot(bool Complete, string Path, bool Exists);

public sealed record UpdateDataStoreResetResult(UpdateDataStoreResetOutcome Outcome, string Detail,
    string? JournalPath = null, string? RetainedStorePath = null, UpdateSearchResult? Before = null,
    UpdateSearchResult? After = null, IReadOnlyList<UpdateResetService>? Services = null)
{
    public int GetExitCode() => Outcome switch
    {
        UpdateDataStoreResetOutcome.Preview or UpdateDataStoreResetOutcome.Verified => 0,
        UpdateDataStoreResetOutcome.Stopped => 130,
        _ => 3
    };
}

public sealed record UpdateDataStoreResetPendingRecord(int Schema, string OperationId, DateTimeOffset StartedUtc,
    string JournalPath, string OriginalStorePath, string RetainedStorePath, UpdateSearchResult Before,
    IReadOnlyList<UpdateResetService> Services)
{
    public string Machine { get; init; } = "";
}

public interface IUpdateDataStoreResetHost
{
    bool IsAdministrator { get; }
    Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token);
    Task<WorkflowCheck> ReadRestartAsync(CancellationToken token);
    Task<IReadOnlyList<UpdateResetService>> ReadServicesAsync(CancellationToken token);
    Task<UpdateStoreSnapshot> ReadStoreAsync(CancellationToken token);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken token);
    Task<ToolResult> StopServiceAsync(string name, CancellationToken token);
    Task<ToolResult> StartServiceAsync(string name, CancellationToken token);
    Task<ToolResult> RetainStoreAsync(string sourcePath, string retainedPath, CancellationToken token);
    Task<UpdateSearchResult> SearchAsync(UpdateSearchOptions options, CancellationToken token);
}

public enum UpdateDataStoreReconciliationOutcome { NoPendingRecord, Verified, NotChanged, Inconclusive }

public sealed record UpdateDataStoreReconciliationResult(UpdateDataStoreReconciliationOutcome Outcome, string Detail,
    string PendingPath, UpdateDataStoreResetPendingRecord? Pending = null, UpdateSearchResult? Search = null,
    IReadOnlyList<UpdateResetService>? Services = null, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is UpdateDataStoreReconciliationOutcome.NoPendingRecord or
        UpdateDataStoreReconciliationOutcome.Verified or UpdateDataStoreReconciliationOutcome.NotChanged ? 0 : 3;
}

public static class UpdateDataStoreResetReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<UpdateDataStoreReconciliationResult> RunAsync(string directory,
        IUpdateDataStoreResetHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory);
        string pendingPath = Path.Combine(root, "update-store-reset.pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath)) return new(UpdateDataStoreReconciliationOutcome.NoPendingRecord,
            "No interrupted Windows Update data-store reset record is present.", pendingPath);
        Directory.CreateDirectory(root);
        await using var operationLock = new FileStream(Path.Combine(root, "update-store-reset.reconcile.lock"),
            FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        try
        {
            var pending = await ReadAsync(pendingPath, token).ConfigureAwait(false);

            Report(progress, "Checking Installer activity, restart state and update services without repeating the reset.");
            var activity = await Required(host.ReadInstallerActivityAsync, "Installer activity", token).ConfigureAwait(false);
            var restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
            var services = await Required(host.ReadServicesAsync, "Update services", token).ConfigureAwait(false);
            if (activity.State != CheckState.Information || !UpdateDataStoreReset.HasCompleteEvidence(activity) ||
                restart.State != CheckState.Clear || !UpdateDataStoreReset.HasCompleteEvidence(restart) ||
                !ServicesRestored(pending.Services, services))
                return new(UpdateDataStoreReconciliationOutcome.Inconclusive,
                    "Current activity, restart, or service evidence is incomplete or differs from the required restored state. The pending record was retained and no reset was repeated.",
                    pendingPath, pending, Services: services);

            var currentStore = await Required(host.ReadStoreAsync, "Current update store", token).ConfigureAwait(false);
            bool retainedExists = await Required(ct => host.DirectoryExistsAsync(pending.RetainedStorePath, ct),
                "Retained update store", token).ConfigureAwait(false);
            if (currentStore is not { Complete: true, Exists: true } ||
                !Path.GetFullPath(currentStore.Path).Equals(Path.GetFullPath(pending.OriginalStorePath), StringComparison.OrdinalIgnoreCase))
                return new(UpdateDataStoreReconciliationOutcome.Inconclusive,
                    "The expected current SoftwareDistribution directory is not established. The pending record was retained and no reset was repeated.",
                    pendingPath, pending, Services: services);

            UpdateSearchResult? search = null;
            UpdateDataStoreReconciliationOutcome outcome;
            string detail;
            if (!retainedExists && !JournalMayShowRetention(pending.JournalPath))
            {
                outcome = UpdateDataStoreReconciliationOutcome.NotChanged;
                detail = "The original store remains in place, the retained destination is absent, and required services are restored. The reset did not reach store retention and was archived without repeating it.";
            }
            else if (!retainedExists)
                return new(UpdateDataStoreReconciliationOutcome.Inconclusive,
                    "Retained store not found; retention may have occurred because the reset journal records a store-retention step. The pending record was retained and no reset was repeated.",
                    pendingPath, pending, Services: services);
            else
            {
                Report(progress, "Running a fresh Windows Update search to reconcile the retained store.");
                search = await Required(ct => host.SearchAsync(pending.Before.Options, ct), "Windows Update search", token).ConfigureAwait(false);
                bool verified = !search.Partial && search.FailureHResult is null &&
                    search.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } evidence && string.IsNullOrWhiteSpace(evidence.Error);
                if (!verified)
                    return new(UpdateDataStoreReconciliationOutcome.Inconclusive,
                        "The retained prior store and replacement store are present, but a fresh Windows Update search did not complete. The pending record was retained and no reset was repeated.",
                        pendingPath, pending, search, services);
                outcome = UpdateDataStoreReconciliationOutcome.Verified;
                detail = "The prior store remains retained, the replacement store exists, required services are restored, and a fresh Windows Update search completed. No reset was repeated and no update was installed.";
            }

            string archivePath = Path.Combine(root, "update-store-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
                "-" + Guid.NewGuid().ToString("N") + ".json");
            var result = new UpdateDataStoreReconciliationResult(outcome, detail, pendingPath, pending, search, services, archivePath);
            await JournalPersistence.WriteAsync(archivePath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: token).ConfigureAwait(false);
            File.Move(pendingPath, archivePath + ".prepared.json");
            Report(progress, detail);
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new(UpdateDataStoreReconciliationOutcome.Inconclusive,
                "The interrupted reset record or current recovery evidence could not be verified. The pending record was retained and no reset was repeated.",
                pendingPath);
        }
    }

    public static async Task<UpdateDataStoreResetPendingRecord> ReadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        token.ThrowIfCancellationRequested();
        try
        {
            await using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read,
                FileShare.Read, 4096, FileOptions.Asynchronous);
            if (stream.Length is <= 0 or > 1_048_576) throw new InvalidDataException("Update-store reset record size is invalid.");
            var bytes = new byte[checked((int)stream.Length)];
            await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
            ReadOnlyMemory<byte> json = bytes;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) json = json[3..];
            using var document = JsonDocument.Parse(json);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Update-store reset record contains conflicting fields.");
            var record = document.RootElement.Deserialize<UpdateDataStoreResetPendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !record.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || record.OperationId.Length != 32 ||
                !record.OperationId.All(Uri.IsHexDigit) || string.IsNullOrWhiteSpace(record.JournalPath) ||
                !UpdateDataStoreReset.IsEligible(record.Before, out _) || record.Services is null || record.Services.Count != 3)
                throw new InvalidDataException("Update-store reset record is invalid.");
            string original = Path.GetFullPath(record.OriginalStorePath), retained = Path.GetFullPath(record.RetainedStorePath);
            if (!Path.GetFileName(original).Equals("SoftwareDistribution", StringComparison.OrdinalIgnoreCase) ||
                !retained.Equals(original + ".KillerBench." + record.OperationId, StringComparison.OrdinalIgnoreCase) ||
                !Path.IsPathFullyQualified(record.JournalPath)) throw new InvalidDataException("Update-store reset record paths are invalid.");
            return record with { OriginalStorePath = original, RetainedStorePath = retained, JournalPath = Path.GetFullPath(record.JournalPath) };
        }
        catch (JsonException error) { throw new InvalidDataException("Update-store reset record is not valid JSON.", error); }
    }

    // An unreadable journal cannot rule out retention, so it counts as possible retention.
    private static bool JournalMayShowRetention(string journalPath)
    {
        try
        {
            foreach (string line in File.ReadLines(journalPath))
            {
                try
                {
                    using var entry = JsonDocument.Parse(line);
                    if (entry.RootElement.ValueKind == JsonValueKind.Object &&
                        entry.RootElement.TryGetProperty("Stage", out var stage) && stage.ValueKind == JsonValueKind.String &&
                        stage.GetString() == "RetainStore") return true;
                }
                catch (JsonException) { }
            }
            return false;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return true; }
    }

    internal static bool ServicesRestored(IReadOnlyList<UpdateResetService> before, IReadOnlyList<UpdateResetService> after) =>
        before is not null && after is not null && before.Count == 3 && after.Count == 3 &&
        before.All(service => service is { Complete: true, Exists: true } && after.Count(current =>
            current is { Complete: true, Exists: true } && current.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase) &&
            current.Running == service.Running && current.StartMode.Equals(service.StartMode, StringComparison.OrdinalIgnoreCase)) == 1);

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name, CancellationToken token)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public static class UpdateDataStoreReset
{
    private static readonly HashSet<uint> EligibleCodes =
    [
        0x80248003, 0x80248004, 0x80248005, 0x80248006, 0x80248008,
        0x80248009, 0x8024800E, 0x8024801B
    ];
    private static readonly string[] StopOrder = ["bits", "wuauserv", "cryptsvc"];
    private static readonly string[] StartOrder = ["cryptsvc", "bits", "wuauserv"];

    public static bool IsEligible(UpdateSearchResult search, out string reason)
    {
        ArgumentNullException.ThrowIfNull(search);
        if (search.FailureHResult is not int code || !search.Partial || search.Updates.Count != 0 ||
            search.Evidence is not { ExitCode: 0, TimedOut: false, Truncated: false } evidence ||
            !string.IsNullOrWhiteSpace(evidence.Error))
        {
            reason = "A complete current Windows Update search failure is required.";
            return false;
        }
        if (!EligibleCodes.Contains(unchecked((uint)code)))
        {
            reason = "This HRESULT does not identify a structural Windows Update data-store fault that KillerBench can reset.";
            return false;
        }
        reason = "The current search reported a recognized structural Windows Update data-store fault.";
        return true;
    }

    public static WorkflowCheck ToWorkflowCheck(UpdateSearchResult search)
    {
        ArgumentNullException.ThrowIfNull(search);
        if (IsEligible(search, out string reason))
            return new("Windows Update data store", CheckState.Repairable,
                search.Detail + " " + reason + " Guarded data-store recovery is available after review and separate confirmation.",
                RepairKind.UpdateDataStoreReset, search.Evidence);

        bool completeEvidence = search.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } evidence &&
            string.IsNullOrWhiteSpace(evidence.Error);
        if (!completeEvidence || search.Partial && search.FailureHResult is null)
            return new("Windows Update data store", CheckState.Unknown,
                search.Detail + " Complete current search evidence is required before data-store health can be classified. No reset is eligible.",
                Evidence: search.Evidence);
        if (search.FailureHResult is not null)
            return new("Windows Update data store", CheckState.Attention,
                search.Detail + " The reported failure is not eligible for data-store reset. Use its diagnostic scope instead.",
                Evidence: search.Evidence);
        return new("Windows Update data store", CheckState.Clear,
            search.Detail + " The current search did not report a structural data-store failure. No reset is eligible.",
            Evidence: search.Evidence);
    }

    public static async Task<UpdateDataStoreResetResult> RunAsync(UpdateSearchResult failedSearch, bool execute,
        string journalFolder, IUpdateDataStoreResetHost host, IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(failedSearch);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        token.ThrowIfCancellationRequested();
        if (!IsEligible(failedSearch, out string eligibility))
            return new(UpdateDataStoreResetOutcome.Blocked, eligibility + " No service or file was changed.", Before: failedSearch);
        if (!execute)
            return new(UpdateDataStoreResetOutcome.Preview,
                "Execution will recheck Installer activity, restart state, administrator access, three update services and the exact SoftwareDistribution directory. It will stop only services that are currently running, retain the existing store under a unique name, restore only services it stopped, and verify with a fresh search. It will not delete the retained store, change service startup modes, clear BITS jobs, reset security descriptors, change policy, or restart Windows.",
                Before: failedSearch);

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "update-store-reset.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "update-store-reset.pending.json");
        if (File.Exists(pendingPath))
            return new(UpdateDataStoreResetOutcome.Blocked,
                "A prior data-store reset needs reconciliation before another reset can start.", pendingPath, Before: failedSearch);

        Report(progress, "Checking Installer activity and pending restart state.");
        var activity = await Required(host.ReadInstallerActivityAsync, "Installer activity", token).ConfigureAwait(false);
        var restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
        if (activity.State != CheckState.Information || !HasCompleteEvidence(activity) ||
            restart.State != CheckState.Clear || !HasCompleteEvidence(restart))
            return new(UpdateDataStoreResetOutcome.Blocked,
                "Installer activity must be idle and restart evidence must be clear. No service or file was changed.", Before: failedSearch);
        if (!host.IsAdministrator)
            return new(UpdateDataStoreResetOutcome.Blocked,
                "Administrator access is required. No service or file was changed.", Before: failedSearch);

        var services = await Required(host.ReadServicesAsync, "Update services", token).ConfigureAwait(false);
        if (!TryValidateServices(services, out string serviceReason))
            return new(UpdateDataStoreResetOutcome.Blocked, serviceReason + " No service or file was changed.",
                Before: failedSearch, Services: services);
        var store = await Required(host.ReadStoreAsync, "Update store", token).ConfigureAwait(false);
        if (!TryValidateStore(store, out string storeReason))
            return new(UpdateDataStoreResetOutcome.Blocked, storeReason + " No service or file was changed.",
                Before: failedSearch, Services: services);

        Report(progress, "Repeating the same Windows Update search immediately before recovery.");
        var currentFailure = await Required(ct => host.SearchAsync(failedSearch.Options, ct),
            "Current Windows Update search", token).ConfigureAwait(false);
        if (!IsEligible(currentFailure, out _) || currentFailure.FailureHResult != failedSearch.FailureHResult ||
            currentFailure.Options != failedSearch.Options)
            return new(UpdateDataStoreResetOutcome.Blocked,
                "The fresh search no longer reports the same structural data-store failure. No service or file was changed.",
                Before: currentFailure, Services: services);
        failedSearch = currentFailure;

        string operationId = Guid.NewGuid().ToString("N");
        string retainedPath = store.Path + ".KillerBench." + operationId;
        string journalPath = Path.Combine(root, "update-store-reset-" + operationId + ".jsonl");
        var pending = new UpdateDataStoreResetPendingRecord(1, operationId, DateTimeOffset.UtcNow,
            journalPath, store.Path, retainedPath, failedSearch, services) { Machine = Environment.MachineName };
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
        await using var journal = new FileStream(journalPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        async Task RecordAsync(object value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value) + "\n");
            await journal.WriteAsync(bytes, CancellationToken.None).ConfigureAwait(false);
            await journal.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            journal.Flush(true);
        }
        await RecordAsync(new { Stage = "Prepared", Pending = pending, Activity = activity, Restart = restart }).ConfigureAwait(false);

        var stopped = new List<string>();
        bool mutationDispatched = false;
        try
        {
            Report(progress, "Confirming final Windows Update data-store reset eligibility.");
            var finalActivity = await Required(host.ReadInstallerActivityAsync, "Final Installer activity", token).ConfigureAwait(false);
            var finalRestart = await Required(host.ReadRestartAsync, "Final restart state", token).ConfigureAwait(false);
            var finalServices = await Required(host.ReadServicesAsync, "Final update services", token).ConfigureAwait(false);
            var finalStore = await Required(host.ReadStoreAsync, "Final update store", token).ConfigureAwait(false);
            var finalFailure = await Required(ct => host.SearchAsync(failedSearch.Options, ct),
                "Final Windows Update search", token).ConfigureAwait(false);
            bool finalAdministrator = host.IsAdministrator;
            await RecordAsync(new
            {
                Stage = "FinalEligibility",
                Activity = finalActivity,
                Restart = finalRestart,
                Services = finalServices,
                Store = finalStore,
                Search = finalFailure,
                Administrator = finalAdministrator
            }).ConfigureAwait(false);
            string? finalBlocker = FinalEligibilityBlocker(failedSearch, services, store, finalActivity,
                finalRestart, finalServices, finalStore, finalFailure, finalAdministrator);
            if (finalBlocker is not null)
            {
                var blocked = new UpdateDataStoreResetResult(UpdateDataStoreResetOutcome.Blocked,
                    finalBlocker + " No service or file was changed.", journalPath, retainedPath,
                    finalFailure, Services: finalServices);
                await RecordAsync(new { Stage = "Finished", Result = blocked }).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, blocked.Detail);
                return blocked;
            }
            failedSearch = finalFailure;
            services = finalServices;
            store = finalStore;

            foreach (string name in StopOrder)
            {
                if (!services.Single(service => service.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Running) continue;
                Report(progress, "Stopping " + name + ".");
                mutationDispatched = true;
                var command = await Required(ct => host.StopServiceAsync(name, ct), "Stop " + name, token).ConfigureAwait(false);
                await RecordAsync(new { Stage = "StopService", Name = name, Result = command }).ConfigureAwait(false);
                if (!Succeeded(command)) return await RecoverAsync(UpdateDataStoreResetOutcome.Uncertain,
                    "A required service did not stop cleanly. The store was not renamed.", stopped, services, host,
                    failedSearch, journalPath, retainedPath, progress, RecordAsync).ConfigureAwait(false);
                stopped.Add(name);
            }

            Report(progress, "Retaining the existing Windows Update data store.");
            mutationDispatched = true;
            var retained = await Required(ct => host.RetainStoreAsync(store.Path, retainedPath, ct), "Retain update store", token).ConfigureAwait(false);
            await RecordAsync(new { Stage = "RetainStore", Source = store.Path, Destination = retainedPath, Result = retained }).ConfigureAwait(false);
            if (!Succeeded(retained)) return await RecoverAsync(UpdateDataStoreResetOutcome.Uncertain,
                "The existing store could not be retained under its recovery name.", stopped, services, host,
                failedSearch, journalPath, retainedPath, progress, RecordAsync).ConfigureAwait(false);

            await RestoreAsync(stopped, host, progress, RecordAsync).ConfigureAwait(false);
            Report(progress, "Verifying with a fresh Windows Update search.");
            var after = await Required(ct => host.SearchAsync(failedSearch.Options, ct), "Windows Update verification", token).ConfigureAwait(false);
            var afterStore = await Required(host.ReadStoreAsync, "Replacement update store", token).ConfigureAwait(false);
            var afterServices = await Required(host.ReadServicesAsync, "Update services after reset", token).ConfigureAwait(false);
            bool searchVerified = !after.Partial && after.FailureHResult is null && after.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } afterEvidence &&
                string.IsNullOrWhiteSpace(afterEvidence.Error);
            bool storeVerified = afterStore is { Complete: true, Exists: true } &&
                Path.GetFullPath(afterStore.Path).Equals(Path.GetFullPath(store.Path), StringComparison.OrdinalIgnoreCase);
            bool servicesVerified = UpdateDataStoreResetReconciliation.ServicesRestored(services, afterServices);
            var outcome = searchVerified && storeVerified && servicesVerified
                ? UpdateDataStoreResetOutcome.Verified : UpdateDataStoreResetOutcome.Uncertain;
            string detail = outcome == UpdateDataStoreResetOutcome.Verified
                ? "The prior store was retained, required services were restored, a replacement store exists, and a fresh Windows Update search completed. No update was installed. The retained store remains available for review."
                : "The prior store was retained, but replacement-store, service, or fresh-search verification was incomplete. Do not repeat the reset until the pending operation is reconciled.";
            var result = new UpdateDataStoreResetResult(outcome, detail, journalPath, retainedPath, failedSearch, after, afterServices);
            await RecordAsync(new { Stage = "Finished", Result = result }).ConfigureAwait(false);
            if (outcome == UpdateDataStoreResetOutcome.Verified) File.Move(pendingPath, journalPath + ".prepared.json");
            Report(progress, detail);
            return result;
        }
        catch (OperationCanceledException)
        {
            if (!mutationDispatched)
            {
                var stoppedBeforeMutation = new UpdateDataStoreResetResult(UpdateDataStoreResetOutcome.Stopped,
                    "The reset stopped after durable preparation and before any service or file change.",
                    journalPath, retainedPath, failedSearch, Services: services);
                await RecordAsync(new { Stage = "Finished", Result = stoppedBeforeMutation }).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                return stoppedBeforeMutation;
            }
            await RestoreAsync(stopped, host, progress, RecordAsync).ConfigureAwait(false);
            var stoppedAfterMutation = new UpdateDataStoreResetResult(UpdateDataStoreResetOutcome.Stopped,
                "The reset stopped after durable preparation. Service restoration was attempted. Reconcile the pending operation before another reset.",
                journalPath, retainedPath, failedSearch, Services: services);
            await RecordAsync(new { Stage = "Finished", Result = stoppedAfterMutation }).ConfigureAwait(false);
            return stoppedAfterMutation;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            if (!mutationDispatched)
            {
                var blockedBeforeMutation = new UpdateDataStoreResetResult(UpdateDataStoreResetOutcome.Blocked,
                    "Final eligibility could not be established: " + error.Message + ". No service or file was changed.",
                    journalPath, retainedPath, failedSearch, Services: services);
                await RecordAsync(new { Stage = "Finished", Result = blockedBeforeMutation }).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, blockedBeforeMutation.Detail);
                return blockedBeforeMutation;
            }
            await RestoreAsync(stopped, host, progress, RecordAsync).ConfigureAwait(false);
            return new(UpdateDataStoreResetOutcome.Uncertain,
                "The reset was interrupted: " + error.Message + ". Service restoration was attempted. Reconcile the pending operation before another reset.",
                journalPath, retainedPath, failedSearch, Services: services);
        }
    }

    private static string? FinalEligibilityBlocker(UpdateSearchResult before,
        IReadOnlyList<UpdateResetService> services, UpdateStoreSnapshot store,
        WorkflowCheck activity, WorkflowCheck restart, IReadOnlyList<UpdateResetService> currentServices,
        UpdateStoreSnapshot currentStore, UpdateSearchResult currentFailure, bool administrator)
    {
        if (activity.State != CheckState.Information || !HasCompleteEvidence(activity))
            return "Installer activity changed or became incomplete after preparation.";
        if (restart.State != CheckState.Clear || !HasCompleteEvidence(restart))
            return "Restart state changed or became incomplete after preparation.";
        if (!administrator)
            return "Administrator access changed after preparation.";
        if (!TryValidateServices(currentServices, out _))
            return "Required update-service evidence changed or became incomplete after preparation.";
        if (!UpdateDataStoreResetReconciliation.ServicesRestored(services, currentServices))
            return "A required update service changed after preparation.";
        if (!TryValidateStore(currentStore, out _) ||
            !Path.GetFullPath(currentStore.Path).Equals(Path.GetFullPath(store.Path), StringComparison.OrdinalIgnoreCase) ||
            currentStore.Exists != store.Exists)
            return "The exact SoftwareDistribution directory changed after preparation.";
        if (!IsEligible(currentFailure, out _) || currentFailure.FailureHResult != before.FailureHResult ||
            currentFailure.Options != before.Options)
            return "The structural Windows Update data-store failure changed after preparation.";
        return null;
    }

    private static bool TryValidateServices(IReadOnlyList<UpdateResetService> services, out string reason)
    {
        if (services is null || services.Count != 3 || services.Any(service => service is null) ||
            services.Select(service => service.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3 ||
            StopOrder.Any(name => !services.Any(service => service.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
        { reason = "Exact evidence for BITS, Windows Update and Cryptographic Services is required."; return false; }
        if (services.Any(service => !service.Complete || !service.Exists || string.IsNullOrWhiteSpace(service.StartMode)))
        { reason = "One or more required service observations are incomplete or unavailable."; return false; }
        if (services.Any(service => service.StartMode.Equals("Disabled", StringComparison.OrdinalIgnoreCase)))
        { reason = "A required service is disabled. Its startup configuration is preserved."; return false; }
        reason = "Service evidence is complete.";
        return true;
    }

    private static bool TryValidateStore(UpdateStoreSnapshot store, out string reason)
    {
        if (store is null || !store.Complete || !store.Exists || string.IsNullOrWhiteSpace(store.Path))
        { reason = "The current SoftwareDistribution directory is unavailable or incomplete."; return false; }
        string full;
        try { full = Path.GetFullPath(store.Path); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        { reason = "The reported update-store path is invalid."; return false; }
        if (!Path.GetFileName(full).Equals("SoftwareDistribution", StringComparison.OrdinalIgnoreCase))
        { reason = "The reported update-store path is not the exact SoftwareDistribution directory."; return false; }
        reason = "Update-store evidence is complete.";
        return true;
    }

    private static bool Succeeded(ToolResult result) => result is { ExitCode: 0, TimedOut: false, Truncated: false } &&
        string.IsNullOrWhiteSpace(result.Error);

    internal static bool HasCompleteEvidence(WorkflowCheck check) =>
        check.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } evidence &&
        string.IsNullOrWhiteSpace(evidence.Error) && check.EffectiveState == check.State;

    private static async Task RestoreAsync(IReadOnlyCollection<string> stopped, IUpdateDataStoreResetHost host,
        IProgress<string>? progress, Func<object, Task> record)
    {
        foreach (string name in StartOrder.Where(name => stopped.Contains(name, StringComparer.OrdinalIgnoreCase)))
        {
            Report(progress, "Restoring " + name + ".");
            ToolResult result;
            try { result = await Required(ct => host.StartServiceAsync(name, ct), "Start " + name, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception error) { result = new(-1, "", error.Message, false); }
            await record(new { Stage = "RestoreService", Name = name, Result = result }).ConfigureAwait(false);
        }
    }

    private static async Task<UpdateDataStoreResetResult> RecoverAsync(UpdateDataStoreResetOutcome outcome, string detail,
        IReadOnlyCollection<string> stopped, IReadOnlyList<UpdateResetService> services, IUpdateDataStoreResetHost host,
        UpdateSearchResult before, string journalPath, string retainedPath, IProgress<string>? progress,
        Func<object, Task> record)
    {
        await RestoreAsync(stopped, host, progress, record).ConfigureAwait(false);
        var result = new UpdateDataStoreResetResult(outcome,
            detail + " Service restoration was attempted; reconcile the pending operation before retrying.",
            journalPath, retainedPath, before, Services: services);
        await record(new { Stage = "Finished", Result = result }).ConfigureAwait(false);
        return result;
    }

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name, CancellationToken token)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsUpdateDataStoreResetHost : IUpdateDataStoreResetHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();

    public Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token) =>
        InstallerActivityDiagnostics.CheckAsync(token);

    public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token) =>
        Task.FromResult(RestartDiagnostics.Check());

    public async Task<IReadOnlyList<UpdateResetService>> ReadServicesAsync(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Update service inspection requires Windows.");
        var step = new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "$ErrorActionPreference='Stop'; $names=@('bits','wuauserv','cryptsvc'); $rows=@(Get-CimInstance Win32_Service | Where-Object {$names -contains $_.Name} | ForEach-Object {[pscustomobject]@{name=$_.Name;state=$_.State;startMode=$_.StartMode}}); [pscustomobject]@{rows=$rows}|ConvertTo-Json -Depth 3 -Compress"], true);
        var result = await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [new("bits", false, false, false, "Unknown"), new("wuauserv", false, false, false, "Unknown"), new("cryptsvc", false, false, false, "Unknown")];
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 3)
                throw new InvalidDataException("Required service evidence is incomplete.");
            var services = new List<UpdateResetService>(3);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 3 ||
                    !row.TryGetProperty("name", out var nameValue) || !row.TryGetProperty("state", out var stateValue) ||
                    !row.TryGetProperty("startMode", out var modeValue)) throw new InvalidDataException("Required service evidence is malformed.");
                string? name = nameValue.GetString(), state = stateValue.GetString(), mode = modeValue.GetString();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(mode) ||
                    !new[] { "bits", "wuauserv", "cryptsvc" }.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                    state is not ("Running" or "Stopped" or "Paused" or "Start Pending" or "Stop Pending") ||
                    mode is not ("Auto" or "Manual" or "Disabled")) throw new InvalidDataException("Required service evidence is unrecognized.");
                services.Add(new(name, true, true, state == "Running", mode == "Auto" ? "Automatic" : mode));
            }
            return services;
        }
        catch (JsonException error) { throw new InvalidDataException("Required service evidence is not valid JSON.", error); }
    }

    public Task<UpdateStoreSnapshot> ReadStoreAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Update store inspection requires Windows.");
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (string.IsNullOrWhiteSpace(windows)) return Task.FromResult(new UpdateStoreSnapshot(false, "", false));
        string path = Path.Combine(windows, "SoftwareDistribution");
        return Task.FromResult(new UpdateStoreSnapshot(true, path, Directory.Exists(path)));
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(Directory.Exists(Path.GetFullPath(path)));
    }

    public Task<ToolResult> StopServiceAsync(string name, CancellationToken token) => ServiceCommandAsync("stop", name, token);
    public Task<ToolResult> StartServiceAsync(string name, CancellationToken token) => ServiceCommandAsync("start", name, token);

    public Task<ToolResult> RetainStoreAsync(string sourcePath, string retainedPath, CancellationToken token)
    {
        string source = Path.GetFullPath(sourcePath), destination = Path.GetFullPath(retainedPath);
        if (!Path.GetFileName(source).Equals("SoftwareDistribution", StringComparison.OrdinalIgnoreCase) ||
            !destination.StartsWith(source + ".KillerBench.", StringComparison.OrdinalIgnoreCase) ||
            !Path.GetDirectoryName(source)!.Equals(Path.GetDirectoryName(destination), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Update-store retention paths are outside the expected boundary.");
        var step = new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "& { param($source,$destination) if(Test-Path -LiteralPath $destination){throw 'Retention destination already exists.'}; Move-Item -LiteralPath $source -Destination $destination -ErrorAction Stop }", source, destination], true);
        return BoundedTool.RunAsync(step, TimeSpan.FromMinutes(2), token: token);
    }

    public Task<UpdateSearchResult> SearchAsync(UpdateSearchOptions options, CancellationToken token) =>
        UpdateDiscovery.SearchAsync(options, token: token);

    private static Task<ToolResult> ServiceCommandAsync(string operation, string name, CancellationToken token)
    {
        if (operation is not ("start" or "stop") || !new[] { "bits", "wuauserv", "cryptsvc" }.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Select a supported Windows Update service operation.");
        var step = new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "& { param($operation,$name) $service=Get-Service -Name $name -ErrorAction Stop; if($operation -eq 'stop'){if($service.Status -ne 'Stopped'){Stop-Service -InputObject $service -ErrorAction Stop}; $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Stopped,[TimeSpan]::FromSeconds(30))}else{if($service.Status -ne 'Running'){Start-Service -InputObject $service -ErrorAction Stop}; $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Running,[TimeSpan]::FromSeconds(30))} }",
            operation, name], true);
        return BoundedTool.RunAsync(step, TimeSpan.FromSeconds(45), token: token);
    }
}
