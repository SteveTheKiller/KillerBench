using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record MsiRepairPendingRecord(int Schema, string ProductCode, DateTimeOffset StartedUtc,
    string JournalPath, string LogPath, WorkflowCheck Before)
{
    public string Machine { get; init; } = "";
}

public enum MsiRepairReconciliationOutcome { NoPendingRecord, Verified, NotRepaired, RestartRequired, Inconclusive }

public sealed record MsiRepairReconciliationResult(MsiRepairReconciliationOutcome Outcome, string Detail,
    string PendingPath, MsiRepairPendingRecord? Pending = null, WorkflowCheck? Product = null,
    WorkflowCheck? Activity = null, WorkflowCheck? Restart = null, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is MsiRepairReconciliationOutcome.NoPendingRecord or
        MsiRepairReconciliationOutcome.Verified ? 0 : Outcome == MsiRepairReconciliationOutcome.RestartRequired ? 4 : 3;
}

public static class MsiRepairReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<MsiRepairReconciliationResult> RunAsync(string directory, IMsiRepairHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory);
        string pendingPath = Path.Combine(root, "msi-repair.pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath))
            return new(MsiRepairReconciliationOutcome.NoPendingRecord,
                "No interrupted MSI repair record is present.", pendingPath);

        Directory.CreateDirectory(root);
        await using var operationLock = new FileStream(Path.Combine(root, "msi-repair.reconcile.lock"),
            FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        MsiRepairPendingRecord pending;
        try { pending = await ReadAsync(pendingPath, token).ConfigureAwait(false); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new(MsiRepairReconciliationOutcome.Inconclusive,
                "The pending MSI repair record is unavailable or invalid: " + error.Message +
                ". The record was retained and no Installer provider was queried.", pendingPath);
        }
        Report(progress, "Checking Windows Installer activity without repeating the repair.");
        WorkflowCheck activity;
        WorkflowCheck restart;
        WorkflowCheck product;
        try
        {
            activity = await Required(host.ReadActivityAsync, "Installer activity", token).ConfigureAwait(false);
            if (!MsiRepair.HasIdleInstallerEvidence(activity))
                return new(MsiRepairReconciliationOutcome.Inconclusive,
                    "Windows Installer activity is present or unavailable. The pending record was retained and no repair was requested.",
                    pendingPath, pending, Activity: activity);
            Report(progress, "Checking pending restart state.");
            restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
            if (!MsiRepair.HasCompleteEvidence(restart) || restart.Evidence is not { } restartEvidence)
                return new(MsiRepairReconciliationOutcome.Inconclusive,
                    "Restart evidence is incomplete. The pending record was retained and no repair was requested.",
                    pendingPath, pending, Activity: activity, Restart: restart);
            var interpretedRestart = RestartDiagnostics.Interpret(restartEvidence);
            if (interpretedRestart.State == CheckState.Attention)
                return new(MsiRepairReconciliationOutcome.RestartRequired,
                    "A restart is still pending. The repair remains unverified, the pending record was retained and no repair was requested.",
                    pendingPath, pending, Activity: activity, Restart: restart);
            if (restart.State != CheckState.Clear || interpretedRestart.State != CheckState.Clear)
                return new(MsiRepairReconciliationOutcome.Inconclusive,
                    "Restart state is unavailable. The pending record was retained and no repair was requested.",
                    pendingPath, pending, Activity: activity, Restart: restart);
            Report(progress, "Rechecking exact MSI product " + pending.ProductCode + ".");
            product = await Required(ct => host.ReadProductAsync(pending.ProductCode, ct), "MSI product", token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or InvalidOperationException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new(MsiRepairReconciliationOutcome.Inconclusive,
                "Current MSI state could not be established: " + error.Message + ". The pending record was retained and no repair was requested.",
                pendingPath, pending);
        }

        if (!MsiProductDiagnostics.TryGetReportedState(pending.ProductCode, product, out int state))
            return new(MsiRepairReconciliationOutcome.Inconclusive,
                "Exact product evidence is incomplete or mismatched. The pending record was retained and no repair was requested.",
                pendingPath, pending, product, activity, restart);

        bool verified = state is 3 or 5;
        string detail = verified
            ? "The exact product now reports a local or default installed state. The interrupted repair was reconciled without repeating it. Application functionality and user settings remain untested."
            : "The exact product does not report a verified local or default installed state. Current evidence was archived without repeating the repair; review the MSI log before another explicit attempt.";
        var outcome = verified ? MsiRepairReconciliationOutcome.Verified : MsiRepairReconciliationOutcome.NotRepaired;
        string archivePath = Path.Combine(root, "msi-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".json");
        var result = new MsiRepairReconciliationResult(outcome, detail, pendingPath, pending, product, activity, restart, archivePath);
        await JournalPersistence.WriteAsync(archivePath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: token).ConfigureAwait(false);
        File.Move(pendingPath, archivePath + ".prepared.json");
        Report(progress, detail);
        return result;
    }

    public static async Task<MsiRepairPendingRecord> ReadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        token.ThrowIfCancellationRequested();
        try
        {
            await using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read,
                FileShare.Read, 4096, FileOptions.Asynchronous);
            if (stream.Length is <= 0 or > 262_144) throw new InvalidDataException("MSI repair record size is invalid.");
            var bytes = new byte[checked((int)stream.Length)];
            await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
            ReadOnlyMemory<byte> json = bytes;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) json = json[3..];
            using var document = JsonDocument.Parse(json);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("MSI repair record contains conflicting fields.");
            var record = document.RootElement.Deserialize<MsiRepairPendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(record.JournalPath) || string.IsNullOrWhiteSpace(record.LogPath))
                throw new InvalidDataException("MSI repair record is invalid.");
            string normalized = MsiProductDiagnostics.NormalizeProductCode(record.ProductCode);
            if (!MsiProductDiagnostics.TryGetReportedState(normalized, record.Before, out int state) || state is not (0 or -6))
                throw new InvalidDataException("MSI repair record does not contain eligible exact-product evidence.");
            return record with { ProductCode = normalized };
        }
        catch (JsonException error) { throw new InvalidDataException("MSI repair record is not valid JSON.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("MSI repair record product identity is invalid.", error); }
    }

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
