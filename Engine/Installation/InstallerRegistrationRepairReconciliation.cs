using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record InstallerRegistrationRepairPendingRecord(int Schema, string Machine,
    DateTimeOffset StartedUtc, string JournalPath, WorkflowCheck Before, WorkflowCheck EngineFiles);

public enum InstallerRegistrationRepairReconciliationOutcome
{
    NoPendingRecord,
    Verified,
    NotRepaired,
    RestartRequired,
    Inconclusive
}

public sealed record InstallerRegistrationRepairReconciliationResult(
    InstallerRegistrationRepairReconciliationOutcome Outcome, string Detail, string PendingPath,
    InstallerRegistrationRepairPendingRecord? Pending = null, WorkflowCheck? Registration = null,
    WorkflowCheck? Activity = null, WorkflowCheck? Restart = null, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome switch
    {
        InstallerRegistrationRepairReconciliationOutcome.NoPendingRecord or
            InstallerRegistrationRepairReconciliationOutcome.Verified => 0,
        InstallerRegistrationRepairReconciliationOutcome.RestartRequired => 4,
        _ => 3
    };
}

public static class InstallerRegistrationRepairReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<InstallerRegistrationRepairReconciliationResult> RunAsync(string directory,
        IInstallerRegistrationRepairHost host, IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory);
        string pendingPath = Path.Combine(root, "installer-registration-repair.pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath))
            return new(InstallerRegistrationRepairReconciliationOutcome.NoPendingRecord,
                "No interrupted Installer registration repair record is present.", pendingPath);

        Directory.CreateDirectory(root);
        await using var operationLock = new FileStream(
            Path.Combine(root, "installer-registration-repair.reconcile.lock"), FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        InstallerRegistrationRepairPendingRecord pending;
        try
        {
            pending = await ReadAsync(pendingPath, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or
            UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new(InstallerRegistrationRepairReconciliationOutcome.Inconclusive,
                "The pending Installer registration repair record is unavailable or invalid: " +
                error.Message + ". The record was retained and no registration command was requested.",
                pendingPath);
        }

        WorkflowCheck activity;
        WorkflowCheck restart;
        WorkflowCheck registration;
        try
        {
            Report(progress, "Checking Windows Installer activity without repeating registration.");
            activity = await Required(host.ReadActivityAsync, "Installer activity", token).ConfigureAwait(false);
            if (!MsiRepair.HasIdleInstallerEvidence(activity))
                return new(InstallerRegistrationRepairReconciliationOutcome.Inconclusive,
                    "Windows Installer activity is present or unavailable. The pending record was retained and no registration command was requested.",
                    pendingPath, pending, Activity: activity);

            Report(progress, "Checking pending restart state.");
            restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
            if (!InstallerRegistrationRepair.HasCompleteEvidence(restart) || restart.Evidence is not { } restartEvidence)
                return new(InstallerRegistrationRepairReconciliationOutcome.Inconclusive,
                    "Restart evidence is incomplete. The pending record was retained and no registration command was requested.",
                    pendingPath, pending, Activity: activity, Restart: restart);
            var interpretedRestart = RestartDiagnostics.Interpret(restartEvidence);
            if (interpretedRestart.State == CheckState.Attention)
                return new(InstallerRegistrationRepairReconciliationOutcome.RestartRequired,
                    "A restart is still pending. Registration remains unverified, the pending record was retained and no command was requested.",
                    pendingPath, pending, Activity: activity, Restart: restart);
            if (restart.State != CheckState.Clear || interpretedRestart.State != CheckState.Clear)
                return new(InstallerRegistrationRepairReconciliationOutcome.Inconclusive,
                    "Restart state is unavailable. The pending record was retained and no registration command was requested.",
                    pendingPath, pending, Activity: activity, Restart: restart);

            Report(progress, "Rechecking exact Windows Installer service registration.");
            registration = await Required(host.ReadRegistrationAsync, "Installer service registration", token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or
            UnauthorizedAccessException or NotSupportedException or InvalidOperationException or
            TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return new(InstallerRegistrationRepairReconciliationOutcome.Inconclusive,
                "Current Installer registration state could not be established: " + error.Message +
                ". The pending record was retained and no registration command was requested.",
                pendingPath, pending);
        }

        if (!InstallerRegistrationRepair.HasExactRegistrationEvidence(registration, CheckState.Information) &&
            !InstallerRegistrationRepair.HasExactRegistrationEvidence(registration, CheckState.Attention))
            return new(InstallerRegistrationRepairReconciliationOutcome.Inconclusive,
                "Exact Installer registration evidence is incomplete. The pending record was retained and no command was requested.",
                pendingPath, pending, registration, activity, restart);

        bool verified = registration.State == CheckState.Information;
        string detail = verified
            ? "The exact Windows Installer service registration now matches the expected configuration. The interrupted operation was reconciled without repeating registration. Package installation and application behavior remain untested."
            : "The exact Windows Installer service registration still needs attention. Current evidence was archived without repeating registration; review it before another explicit attempt.";
        var outcome = verified
            ? InstallerRegistrationRepairReconciliationOutcome.Verified
            : InstallerRegistrationRepairReconciliationOutcome.NotRepaired;
        string archivePath = Path.Combine(root, "installer-registration-reconciliation-" +
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" +
            Guid.NewGuid().ToString("N") + ".json");
        var result = new InstallerRegistrationRepairReconciliationResult(outcome, detail, pendingPath,
            pending, registration, activity, restart, archivePath);
        await JournalPersistence.WriteAsync(archivePath,
            JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
        File.Move(pendingPath, archivePath + ".prepared.json");
        Report(progress, detail);
        return result;
    }

    public static async Task<InstallerRegistrationRepairPendingRecord> ReadAsync(string path,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        token.ThrowIfCancellationRequested();
        try
        {
            await using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open,
                FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            if (stream.Length is <= 0 or > 262_144)
                throw new InvalidDataException("Installer registration repair record size is invalid.");
            var bytes = new byte[checked((int)stream.Length)];
            await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
            ReadOnlyMemory<byte> json = bytes;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                json = json[3..];
            using var document = JsonDocument.Parse(json);
            if (!UniqueFields(document.RootElement))
                throw new InvalidDataException("Installer registration repair record contains conflicting fields.");
            var record = document.RootElement.Deserialize<InstallerRegistrationRepairPendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(record.JournalPath) ||
                !InstallerRegistrationRepair.HasExactRegistrationEvidence(record.Before, CheckState.Attention) ||
                !InstallerRegistrationRepair.TrustedEngineFiles(record.EngineFiles))
                throw new InvalidDataException("Installer registration repair record is invalid or ineligible.");
            return record;
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Installer registration repair record is not valid JSON.", error);
        }
    }

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation,
        string name, CancellationToken token)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(token).ConfigureAwait(false) ??
            throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
            return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property =>
            names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try
        {
            progress?.Report(message);
        }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException)
        {
        }
    }
}
