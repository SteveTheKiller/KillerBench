using System.Text.Json;

namespace KillerBench.Engine.FileSystem;

public sealed record FileSystemRepairPendingRecord(int Schema, DateTimeOffset StartedUtc,
    FileSystemRepairDraft Draft, WorkflowCheck Before, string JournalPath)
{
    public string Machine { get; init; } = "";
}

public enum FileSystemRepairReconciliationOutcome { NoPendingRecord, AwaitingRestart, Verified, StillDamaged, Inconclusive }

public sealed record FileSystemRepairReconciliationResult(FileSystemRepairReconciliationOutcome Outcome,
    string Detail, string PendingPath, FileSystemRepairPendingRecord? Pending = null,
    StorageVolumeState? VolumeState = null, WorkflowCheck? Current = null, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is FileSystemRepairReconciliationOutcome.NoPendingRecord or
        FileSystemRepairReconciliationOutcome.Verified ? 0 : Outcome == FileSystemRepairReconciliationOutcome.AwaitingRestart ? 4 : 3;
}

public static class FileSystemRepairReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<FileSystemRepairReconciliationResult> RunAsync(string folder,
        IFileSystemRepairHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(folder);
        string pendingPath = Path.Combine(root, "filesystem-repair.pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath)) return new(FileSystemRepairReconciliationOutcome.NoPendingRecord,
            "No interrupted filesystem repair record is present.", pendingPath);
        Directory.CreateDirectory(root);
        await using var operationLock = new FileStream(Path.Combine(root, "filesystem-repair.reconcile.lock"),
            FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        FileSystemRepairPendingRecord pending;
        try
        {
            pending = await ReadAsync(pendingPath, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            System.Security.SecurityException)
        {
            return new(FileSystemRepairReconciliationOutcome.Inconclusive,
                "The pending filesystem repair record is unavailable or invalid: " + error.Message +
                ". The record was retained and no repair was requested.", pendingPath);
        }
        if (pending.Draft.RequiresRestart)
        {
            BootStamp? boot;
            try
            {
                boot = await Required(host.ReadBootAsync, "Windows startup evidence", token, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
                InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
                System.Security.SecurityException)
            {
                return new(FileSystemRepairReconciliationOutcome.Inconclusive,
                    "Windows startup evidence is unavailable: " + error.Message + ". The pending record was retained and no repair was requested.",
                    pendingPath, pending);
            }
            if (boot is null)
                return new(FileSystemRepairReconciliationOutcome.Inconclusive,
                    "Windows startup evidence is unavailable. The pending record was retained and no repair was requested.", pendingPath, pending);
            if (boot.StartedUtc <= pending.StartedUtc)
                return new(FileSystemRepairReconciliationOutcome.AwaitingRestart,
                    "The scheduled system-volume repair is waiting for a newer Windows startup. Restart Windows, then run reconciliation again. No repair was repeated.",
                    pendingPath, pending);
        }
        StorageVolumeState state;
        WorkflowCheck current;
        try
        {
            Report(progress, "Rechecking " + pending.Draft.Volume + " without repeating filesystem repair.");
            state = await Required(ct => host.InspectAsync(pending.Draft.Volume, ct), "Volume inspection",
                token, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            if (!state.Ready || !state.Fixed || !string.Equals(state.FileSystem, "NTFS", StringComparison.OrdinalIgnoreCase))
                return new(FileSystemRepairReconciliationOutcome.Inconclusive,
                    "The exact volume is unavailable or changed. The pending record was retained and no repair was requested.",
                    pendingPath, pending, state);
            current = await Required(ct => host.DiagnoseAsync(pending.Draft.Volume, ct), "Filesystem diagnosis",
                token, TimeSpan.FromMinutes(11)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        {
            return new(FileSystemRepairReconciliationOutcome.Inconclusive,
                "Current filesystem state could not be established: " + error.Message + ". The pending record was retained and no repair was requested.",
                pendingPath, pending);
        }

        bool exact = current.Name.Equals("Filesystem", StringComparison.Ordinal) &&
            current.Detail.StartsWith("Volume: " + pending.Draft.Volume + ".", StringComparison.OrdinalIgnoreCase) &&
            current.Evidence is { TimedOut: false, Truncated: false } evidence && string.IsNullOrWhiteSpace(evidence.Error);
        bool verified = exact && current.State == CheckState.Clear && current.Evidence!.ExitCode == 0;
        bool stillDamaged = exact && current.State == CheckState.Attention && current.Evidence!.ExitCode is 1 or 2 or 3;
        if (!verified && !stillDamaged)
            return new(FileSystemRepairReconciliationOutcome.Inconclusive,
                "Fresh evidence is incomplete or mismatched. The pending record was retained and no repair was requested.",
                pendingPath, pending, state, current);

        var outcome = verified ? FileSystemRepairReconciliationOutcome.Verified : FileSystemRepairReconciliationOutcome.StillDamaged;
        string detail = verified
            ? "A fresh read-only check reported no filesystem problems. The interrupted operation was reconciled without repeating repair. Physical media health remains untested."
            : "The exact volume still reports filesystem errors. Current evidence was archived without repeating repair; review it before another explicit attempt.";
        string archivePath = Path.Combine(root, "filesystem-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".json");
        var result = new FileSystemRepairReconciliationResult(outcome, detail, pendingPath, pending, state, current, archivePath);
        await JournalPersistence.WriteAsync(archivePath, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
        File.Move(pendingPath, archivePath + ".prepared.json");
        Report(progress, detail);
        return result;
    }

    public static async Task<FileSystemRepairPendingRecord> ReadAsync(string path, CancellationToken token = default)
    {
        await using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 262_144) throw new InvalidDataException("Filesystem repair record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Filesystem repair record contains conflicting fields.");
            var record = document.RootElement.Deserialize<FileSystemRepairPendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || string.IsNullOrWhiteSpace(record.Machine) ||
                !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                record.StartedUtc == default || string.IsNullOrWhiteSpace(record.JournalPath) ||
                record.Draft.RequiresRestart == record.Draft.RequiresDismount || !record.Draft.VolumeEligible || record.Draft.Step is null ||
                !FileSystemRepairPlanning.Review(record.Draft, record.Before).ExecutionEligible)
                throw new InvalidDataException("Filesystem repair record is invalid.");
            return record;
        }
        catch (JsonException error) { throw new InvalidDataException("Filesystem repair record is not valid JSON.", error); }
    }

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan limit)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(limit, token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence.");
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
