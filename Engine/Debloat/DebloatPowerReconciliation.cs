using System.Text.Json;

namespace KillerBench.Engine;

public enum DebloatPowerReconciliationOutcome { NoPendingRecord, Verified, CurrentStateDiffers, Inconclusive }

public sealed record DebloatPowerReconciliationResult(DebloatPowerReconciliationOutcome Outcome,
    string Detail, string PendingPath, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is DebloatPowerReconciliationOutcome.NoPendingRecord or
        DebloatPowerReconciliationOutcome.Verified ? 0 : 3;
}

public static class DebloatPowerReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static Task<DebloatPowerReconciliationResult> RunPlanAsync(string folder,
        IDebloatPowerPlanChangeHost host, IProgress<string>? progress = null, CancellationToken token = default) =>
        RunAsync(folder, "debloat-power-plan.pending.json", "power-plan", async (path, ct) =>
        {
            var pending = await ReadAsync<PlanPending>(path, ct).ConfigureAwait(false);
            if (pending.Schema != 1 || !CurrentMachine(pending.Machine) || pending.StartedUtc == default || string.IsNullOrWhiteSpace(pending.JournalPath) ||
                pending.Change != DebloatPowerPlanExecution.CreateHighPerformance())
                throw new InvalidDataException("Pending power-plan record is invalid.");
            Report(progress, "Rechecking the active power plan without repeating activation.");
            var current = await Required(host.ReadAsync, "Power-plan reconciliation", ct, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            if (!current.Complete || current.Evidence is not { ExitCode: 0, TimedOut: false, Truncated: false } evidence ||
                !string.IsNullOrWhiteSpace(evidence.Error) || current.HighPerformance is null ||
                current.HighPerformance.Id != WindowsDebloatPowerPlanReader.HighPerformanceId)
                return (DebloatPowerReconciliationOutcome.Inconclusive,
                    "Current power-plan evidence is incomplete or mismatched. The pending record was retained.");
            return current.HighPerformance.Active
                ? (DebloatPowerReconciliationOutcome.Verified, "Fresh evidence confirms the requested High performance plan is active. The prior attempt was reconciled without repeating it.")
                : (DebloatPowerReconciliationOutcome.CurrentStateDiffers, "Fresh evidence confirms High performance is not active. Current state was archived without repeating activation.");
        }, progress, token);

    public static Task<DebloatPowerReconciliationResult> RunTimersAsync(string folder,
        IDebloatPowerTimerHost host, IProgress<string>? progress = null, CancellationToken token = default) =>
        RunAsync(folder, "debloat-power-timers.pending.json", "power-timers", async (path, ct) =>
        {
            var pending = await ReadAsync<TimerPending>(path, ct).ConfigureAwait(false);
            try { pending.Request.Validate(); }
            catch (ArgumentException error) { throw new InvalidDataException("Pending power-timer request is invalid.", error); }
            if (pending.Schema != 1 || !CurrentMachine(pending.Machine) || pending.StartedUtc == default || string.IsNullOrWhiteSpace(pending.JournalPath) ||
                !Complete(pending.Before)) throw new InvalidDataException("Pending power-timer record is invalid.");
            Report(progress, "Rechecking power timers without repeating any change.");
            var current = await Required(host.ReadAsync, "Power-timer reconciliation", ct, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            if (!Complete(current) || current.ActiveSchemeId != pending.Before.ActiveSchemeId)
                return (DebloatPowerReconciliationOutcome.Inconclusive,
                    "Current power evidence is incomplete or the active plan changed. The pending record was retained.");
            return Matches(current, pending.Request)
                ? (DebloatPowerReconciliationOutcome.Verified, "Fresh evidence confirms all four requested timers. The prior attempt was reconciled without repeating it.")
                : (DebloatPowerReconciliationOutcome.CurrentStateDiffers, "Fresh evidence confirms one or more timers differ from the request. Current state was archived without repeating changes.");
        }, progress, token);

    public static Task<DebloatPowerReconciliationResult> RunHibernationAsync(string folder,
        IDebloatHibernationHost host, IProgress<string>? progress = null, CancellationToken token = default) =>
        RunAsync(folder, "debloat-hibernation.pending.json", "hibernation", async (path, ct) =>
        {
            var pending = await ReadAsync<HibernationPending>(path, ct).ConfigureAwait(false);
            if (pending.Schema != 1 || !CurrentMachine(pending.Machine) || pending.StartedUtc == default || string.IsNullOrWhiteSpace(pending.JournalPath) ||
                !Complete(pending.Before) || pending.Before.HibernationEnabled != true || pending.Before.HibernationFilePresent != true)
                throw new InvalidDataException("Pending hibernation record is invalid.");
            Report(progress, "Rechecking hibernation without repeating the change.");
            var current = await Required(host.ReadAsync, "Hibernation reconciliation", ct, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            if (!Complete(current) || !SamePlanAndTimers(pending.Before, current) ||
                current.HibernationEnabled != current.HibernationFilePresent)
                return (DebloatPowerReconciliationOutcome.Inconclusive,
                    "Current hibernation evidence is incomplete, contradictory or includes other power changes. The pending record was retained.");
            return current.HibernationEnabled == false
                ? (DebloatPowerReconciliationOutcome.Verified, "Fresh evidence confirms hibernation is disabled and hiberfil.sys is absent. The prior attempt was reconciled without repeating it.")
                : (DebloatPowerReconciliationOutcome.CurrentStateDiffers, "Fresh evidence confirms hibernation remains enabled. Current state was archived without repeating the change.");
        }, progress, token);

    private static async Task<DebloatPowerReconciliationResult> RunAsync(string folder, string fileName,
        string archiveName, Func<string, CancellationToken, Task<(DebloatPowerReconciliationOutcome Outcome, string Detail)>> inspect,
        IProgress<string>? progress, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(inspect);
        string root = Path.GetFullPath(folder);
        string pendingPath = Path.Combine(root, fileName);
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath)) return new(DebloatPowerReconciliationOutcome.NoPendingRecord,
            "No pending " + archiveName + " record is present.", pendingPath);
        Directory.CreateDirectory(root);
        await using var operationLock = new FileStream(Path.Combine(root, archiveName + ".reconcile.lock"),
            FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        (DebloatPowerReconciliationOutcome Outcome, string Detail) inspected;
        try { inspected = await inspect(pendingPath, token).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            return new(DebloatPowerReconciliationOutcome.Inconclusive,
                "Current state could not be established: " + error.Message + " The pending record was retained.", pendingPath);
        }
        if (inspected.Outcome == DebloatPowerReconciliationOutcome.Inconclusive)
            return new(inspected.Outcome, inspected.Detail, pendingPath);
        string archivePath = Path.Combine(root, "debloat-" + archiveName + "-reconciliation-" +
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json");
        var result = new DebloatPowerReconciliationResult(inspected.Outcome, inspected.Detail, pendingPath, archivePath);
        await JournalPersistence.WriteAsync(archivePath, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
        File.Move(pendingPath, archivePath + ".prepared.json");
        Report(progress, inspected.Detail);
        return result;
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 262_144) throw new InvalidDataException("Pending DEBLOAT power record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Pending DEBLOAT power record contains conflicting fields.");
            return document.RootElement.Deserialize<T>(StrictJson) ?? throw new InvalidDataException("Pending DEBLOAT power record is empty.");
        }
        catch (JsonException error) { throw new InvalidDataException("Pending DEBLOAT power record is not valid JSON.", error); }
    }

    private static bool Complete(DebloatPowerEvidence value) => value.Complete &&
        Guid.TryParseExact(value.ActiveSchemeId, "D", out _) && value.SleepAcSeconds.HasValue &&
        value.SleepDcSeconds.HasValue && value.DisplayAcSeconds.HasValue && value.DisplayDcSeconds.HasValue &&
        value.HibernationEnabled.HasValue && value.HibernationFilePresent.HasValue;
    private static bool Matches(DebloatPowerEvidence value, DebloatPowerRequest request) =>
        value.SleepAcSeconds == request.SleepAcMinutes * 60 && value.SleepDcSeconds == request.SleepDcMinutes * 60 &&
        value.DisplayAcSeconds == request.DisplayAcMinutes * 60 && value.DisplayDcSeconds == request.DisplayDcMinutes * 60;
    private static bool SamePlanAndTimers(DebloatPowerEvidence left, DebloatPowerEvidence right) =>
        left.ActiveSchemeId == right.ActiveSchemeId && left.SleepAcSeconds == right.SleepAcSeconds &&
        left.SleepDcSeconds == right.SleepDcSeconds && left.DisplayAcSeconds == right.DisplayAcSeconds &&
        left.DisplayDcSeconds == right.DisplayDcSeconds;
    private static bool CurrentMachine(string machine) =>
        string.Equals(machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
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

    private sealed record PlanPending(int Schema, string Machine, DateTimeOffset StartedUtc, DebloatPowerPlanChange Change,
        DebloatPowerPlanEvidence Before, string JournalPath);
    private sealed record TimerPending(int Schema, string Machine, DateTimeOffset StartedUtc, DebloatPowerRequest Request,
        DebloatPowerEvidence Before, string JournalPath);
    private sealed record HibernationPending(int Schema, string Machine, DateTimeOffset StartedUtc, string Effects,
        DebloatPowerEvidence Before, string JournalPath);
}
