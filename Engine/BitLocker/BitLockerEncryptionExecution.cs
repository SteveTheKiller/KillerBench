using System.Text.Json;

namespace KillerBench.Engine.BitLocker;

public sealed record BitLockerEncryptionState(string Volume, string Identity, string VolumeStatus,
    string ProtectionStatus, string LockStatus, int EncryptionPercentage, string EncryptionMethod,
    string FileSystem, string VolumeType, bool TpmReady, bool PolicyPresent,
    IReadOnlyList<BitLockerProtector> Protectors, bool Complete);

public enum BitLockerEncryptionOutcome { Preview, Blocked, Started, NotApplied, Failed, Uncertain, Stopped }

public sealed record BitLockerEncryptionResult(string Volume, BitLockerEncryptionOutcome Outcome,
    string Detail, BitLockerEncryptionState? Before = null, RecoveryBackupReceipt? Backup = null,
    ToolResult? Command = null, BitLockerEncryptionState? After = null, string? JournalPath = null)
{
    public int GetExitCode() => Outcome switch
    {
        BitLockerEncryptionOutcome.Preview or BitLockerEncryptionOutcome.Started => 0,
        BitLockerEncryptionOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IBitLockerEncryptionHost
{
    bool IsAdministrator { get; }
    Task<RestartSnapshot> ReadRestartAsync(CancellationToken token);
    Task<BitLockerEncryptionState> InspectAsync(string volume, CancellationToken token);
    Task<RecoveryBackupReceipt> BackupAsync(BitLockerOptions options, Guid protector,
        IProgress<string>? progress, CancellationToken token);
    Task<ToolResult> StartAsync(BitLockerOptions options, IProgress<string>? progress, CancellationToken token);
}

public static class BitLockerEncryptionExecution
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };

    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc,
        string Volume, BitLockerEncryption Encryption, bool UsedSpaceOnly,
        Guid RecoveryProtector, BitLockerEncryptionState Before,
        RecoveryBackupReceipt Backup, ToolResult? Command);

    public static Task<BitLockerEncryptionResult> PreviewAsync(BitLockerOptions options,
        Guid recoveryProtector, CancellationToken token = default)
    {
        ValidateSelection(options, recoveryProtector);
        token.ThrowIfCancellationRequested();
        return Task.FromResult(new BitLockerEncryptionResult(options.Volume.ToUpperInvariant(),
            BitLockerEncryptionOutcome.Preview,
            "Preview only. Execution requires an exact fully decrypted NTFS volume, a TPM protector on an operating-system volume or no TPM protector on a data volume, the selected recovery-password protector, a newly verified recovery backup, clear restart evidence and administrator access. It saves a pending record, repeats every check, starts conversion once and verifies that Windows reports encryption in progress or complete."));
    }

    public static async Task<BitLockerEncryptionResult> ReviewAsync(BitLockerOptions options,
        Guid recoveryProtector, IBitLockerEncryptionHost host, CancellationToken token = default)
    {
        ValidateSelection(options, recoveryProtector);
        ArgumentNullException.ThrowIfNull(host);
        string volume = options.Volume.ToUpperInvariant();
        BitLockerEncryptionState before;
        try { before = await Required(ct => host.InspectAsync(volume, ct), "BitLocker inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerEncryptionOutcome.Blocked, "BitLocker eligibility could not be established: " + error.Message); }
        string? blocker = EligibilityBlocker(options, recoveryProtector, before);
        if (blocker is not null) return new(volume, BitLockerEncryptionOutcome.Blocked, blocker, before);
        RestartSnapshot restart;
        try { restart = await Required(host.ReadRestartAsync, "Restart inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerEncryptionOutcome.Blocked, "Restart readiness could not be established: " + error.Message, before); }
        if (RestartDiagnostics.Assess(restart).State != CheckState.Clear)
            return new(volume, BitLockerEncryptionOutcome.Blocked,
                "Restart readiness must be clear before encryption. " + RestartDiagnostics.Assess(restart).Detail, before);
        if (!host.IsAdministrator)
            return new(volume, BitLockerEncryptionOutcome.Blocked,
                "Administrator access is required before encryption can be offered.", before);
        return new(volume, BitLockerEncryptionOutcome.Preview,
            "Fresh evidence confirms this exact volume and its existing protectors can be reviewed for recovery backup and encryption.", before);
    }

    public static async Task<BitLockerEncryptionResult> RunAsync(BitLockerOptions options,
        Guid recoveryProtector, bool execute, string journalFolder, IBitLockerEncryptionHost host,
        IProgress<string>? progress = null, TimeSpan? operationTimeout = null, CancellationToken token = default)
    {
        ValidateSelection(options, recoveryProtector);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        if (!execute) return await PreviewAsync(options, recoveryProtector, token).ConfigureAwait(false);
        TimeSpan limit = operationTimeout ?? TimeSpan.FromMinutes(5);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(operationTimeout));
        string volume = options.Volume.ToUpperInvariant();
        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "bitlocker-encryption.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "bitlocker-encryption.pending.json");
        if (File.Exists(pendingPath))
            return new(volume, BitLockerEncryptionOutcome.Blocked,
                "A prior BitLocker encryption request needs reconciliation before another can start.", JournalPath: pendingPath);

        Report(progress, "Inspecting " + volume + " and its existing protector identities.");
        BitLockerEncryptionState before;
        try { before = await Required(ct => host.InspectAsync(volume, ct), "BitLocker inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerEncryptionOutcome.Blocked, "BitLocker eligibility could not be established: " + error.Message); }
        string? blocker = EligibilityBlocker(options, recoveryProtector, before);
        if (blocker is not null) return new(volume, BitLockerEncryptionOutcome.Blocked, blocker, before);
        RestartSnapshot restart;
        try { restart = await Required(host.ReadRestartAsync, "Restart inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerEncryptionOutcome.Blocked, "Restart readiness could not be established: " + error.Message, before); }
        if (RestartDiagnostics.Assess(restart).State != CheckState.Clear || !host.IsAdministrator)
            return new(volume, BitLockerEncryptionOutcome.Blocked,
                "Clear restart evidence and administrator access are required. Encryption was not started.", before);

        Report(progress, "Writing and verifying the selected recovery backup before encryption.");
        RecoveryBackupReceipt backup;
        try { backup = await Required(ct => host.BackupAsync(options, recoveryProtector, progress, ct),
            "Recovery backup", TimeSpan.FromMinutes(5), token: token).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error))
        { return new(volume, BitLockerEncryptionOutcome.Blocked, "Recovery backup could not be verified: " + error.Message, before); }
        if (!backup.Verified || backup.ProtectorId != recoveryProtector || backup.Destination != options.Backup ||
            !string.Equals(backup.VolumeIdentity, before.Identity, StringComparison.Ordinal))
            return new(volume, BitLockerEncryptionOutcome.Blocked,
                "Recovery backup was not verified for the exact selected volume, protector and destination. Encryption was not started.", before, backup);

        Report(progress, "Rechecking the volume after recovery backup.");
        BitLockerEncryptionState prepared;
        try { prepared = await Required(ct => host.InspectAsync(volume, ct), "BitLocker post-backup inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerEncryptionOutcome.Blocked, "Post-backup eligibility could not be established: " + error.Message, before, backup); }
        blocker = EligibilityBlocker(options, recoveryProtector, prepared);
        if (blocker is not null || !SameIdentity(before, prepared))
            return new(volume, BitLockerEncryptionOutcome.Blocked,
                "Volume or protector evidence changed after recovery backup. Encryption was not started.", before, backup, After: prepared);

        var pending = new PendingRecord(1, Environment.MachineName, DateTimeOffset.UtcNow,
            volume, options.Encryption, options.UsedSpaceOnly, recoveryProtector, prepared, backup, null);
        await JournalPersistence.WriteAsync(pendingPath,
            JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
        string journalPath = Path.Combine(root, "bitlocker-encryption-" + Guid.NewGuid().ToString("N") + ".json");
        ToolResult? command = null;
        bool dispatched = false;
        BitLockerEncryptionResult result;
        try
        {
            Report(progress, "Repeating encryption eligibility immediately before the Windows request.");
            var immediate = await Required(ct => host.InspectAsync(volume, ct), "Final BitLocker inspection",
                TimeSpan.FromSeconds(45), token:                 token).ConfigureAwait(false);
            var immediateRestart = await Required(host.ReadRestartAsync, "Final restart inspection",
                TimeSpan.FromSeconds(20), token:                 token).ConfigureAwait(false);
            string? finalBlocker = EligibilityBlocker(options, recoveryProtector, immediate);
            if (finalBlocker is not null || !SameIdentity(prepared, immediate) ||
                RestartDiagnostics.Assess(immediateRestart).State != CheckState.Clear || !host.IsAdministrator)
            {
                result = new(volume, BitLockerEncryptionOutcome.Blocked,
                    IsStarted(immediate, options) ? "Encryption began after preparation. No additional request was sent."
                    : "Final volume, protector, restart or administrator evidence no longer authorizes encryption. No request was sent.",
                    prepared, backup, After: immediate, JournalPath: journalPath);
                await FinishNoDispatchAsync(pendingPath, journalPath, result).ConfigureAwait(false);
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Requesting BitLocker encryption on " + volume + " once.");
            dispatched = true;
            command = await Required(ct => host.StartAsync(options, progress, ct),
                "BitLocker encryption request", limit, token: token).ConfigureAwait(false);
            await JournalPersistence.WriteAsync(pendingPath,
                JsonSerializer.Serialize(pending with { Command = command }, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
            result = await VerifyAsync(options, recoveryProtector, prepared, backup, command,
                journalPath, host, progress, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new(volume, BitLockerEncryptionOutcome.Stopped,
                dispatched ? "The encryption request stopped after it may have reached Windows. Reconcile before trying again."
                : "The final encryption check was canceled before any request was sent.",
                prepared, backup, command, JournalPath: journalPath);
        }
        catch (Exception error) when (Unavailable(error))
        {
            result = new(volume, dispatched ? BitLockerEncryptionOutcome.Uncertain : BitLockerEncryptionOutcome.Blocked,
                dispatched ? "The encryption request was interrupted: " + error.Message + ". Reconcile before trying again."
                : "Final encryption eligibility was unavailable: " + error.Message + ". No request was sent.",
                prepared, backup, command, JournalPath: journalPath);
        }
        await JournalPersistence.WriteAsync(journalPath,
            JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome is BitLockerEncryptionOutcome.Started or BitLockerEncryptionOutcome.NotApplied or BitLockerEncryptionOutcome.Failed || !dispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<BitLockerEncryptionResult> ReconcileAsync(string journalFolder,
        IBitLockerEncryptionHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "bitlocker-encryption.pending.json");
        if (!File.Exists(pendingPath))
            return new("C:", BitLockerEncryptionOutcome.Blocked, "No pending BitLocker encryption request was found.");
        using var gate = new FileStream(Path.Combine(root, "bitlocker-encryption.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        PendingRecord pending;
        try { pending = await ReadPendingAsync(pendingPath, token).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error))
        {
            return new("C:", BitLockerEncryptionOutcome.Uncertain,
                "The interrupted encryption record could not be verified for this computer. It was retained and the command was not repeated.",
                JournalPath: pendingPath);
        }
        if (pending.Command is null)
            return new(pending.Volume, BitLockerEncryptionOutcome.Uncertain,
                "The pending record does not prove that Windows returned from the encryption request. It was not repeated.",
                pending.Before, pending.Backup, JournalPath: pendingPath);
        string journalPath = Path.Combine(root, "bitlocker-encryption-recovered-" + Guid.NewGuid().ToString("N") + ".json");
        BitLockerOptions pendingOptions = PendingOptions(pending);
        var result = await VerifyAsync(pendingOptions, pending.RecoveryProtector, pending.Before,
            pending.Backup, pending.Command, journalPath, host, progress, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(journalPath,
            JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome is BitLockerEncryptionOutcome.Started or BitLockerEncryptionOutcome.NotApplied or BitLockerEncryptionOutcome.Failed)
            File.Move(pendingPath, journalPath + ".prepared.json");
        return result;
    }

    public static string? EligibilityBlocker(BitLockerOptions options, Guid recoveryProtector,
        BitLockerEncryptionState state)
    {
        ValidateSelection(options, recoveryProtector);
        string volume = options.Volume.ToUpperInvariant();
        if (!state.Complete || !string.Equals(volume, state.Volume, StringComparison.OrdinalIgnoreCase))
            return "A complete exact selected-volume inventory is required. Encryption was not started.";
        if (string.IsNullOrWhiteSpace(state.Identity) || state.Identity.Length > 512 || state.Identity.Any(char.IsControl))
            return "The exact selected volume identity could not be confirmed. Encryption was not started.";
        bool operatingSystem = state.VolumeType == "OperatingSystem";
        if (state.FileSystem != "NTFS" || !operatingSystem && state.VolumeType != "Data")
            return "This path requires an NTFS operating-system or data volume.";
        if (operatingSystem && !state.TpmReady) return "TPM readiness is required. KillerBench does not initialize or clear the TPM.";
        if (state.PolicyPresent) return "Managed BitLocker policy is present. This local encryption path preserves it and remains unavailable.";
        if (state.VolumeStatus != "FullyDecrypted" || state.EncryptionPercentage != 0 || state.ProtectionStatus != "Off")
            return IsStarted(state, options) ? "Encryption is already in progress or complete. No new request is eligible."
                : "Only a fully decrypted volume at zero percent with protection off is eligible.";
        if (state.LockStatus != "Unlocked") return "The selected volume must be unlocked.";
        if (state.Protectors.Count < (operatingSystem ? 2 : 1) || state.Protectors.Count > 64 ||
            state.Protectors.Select(p => p.Id).Distinct().Count() != state.Protectors.Count)
            return "A complete unique protector inventory is required.";
        if (state.Protectors.Count(p => p.Id == recoveryProtector && p.Type == "RecoveryPassword") != 1)
            return "The selected recovery-password protector is not present exactly once.";
        if (operatingSystem && state.Protectors.Count(p => p.Type == "Tpm") != 1)
            return "Exactly one existing TPM protector is required for this operating-system encryption path.";
        if (!operatingSystem && state.Protectors.Any(p => p.Type.StartsWith("Tpm", StringComparison.Ordinal)))
            return "A data volume cannot carry a TPM protector. The protector inventory was not recognized.";
        if (!string.Equals(state.EncryptionMethod, "None", StringComparison.Ordinal))
            return "The fully decrypted volume reported an unexpected encryption method.";
        return null;
    }

    private static async Task<BitLockerEncryptionResult> VerifyAsync(BitLockerOptions options,
        Guid recoveryProtector, BitLockerEncryptionState before, RecoveryBackupReceipt backup,
        ToolResult command, string journalPath, IBitLockerEncryptionHost host,
        IProgress<string>? progress, CancellationToken token)
    {
        string volume = options.Volume.ToUpperInvariant();
        Report(progress, "Verifying conversion state and protector identities.");
        BitLockerEncryptionState after;
        try { after = await Required(ct => host.InspectAsync(volume, ct), "BitLocker verification", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerEncryptionOutcome.Uncertain, "Encryption verification was unavailable: " + error.Message, before, backup, command, JournalPath: journalPath); }
        if (IsStarted(after, options) && SameIdentity(before, after) &&
            after.Protectors.Count(p => p.Id == recoveryProtector && p.Type == "RecoveryPassword") == 1)
            return new(volume, BitLockerEncryptionOutcome.Started,
                "Windows reports that encryption started or completed with the selected method. The verified recovery protector remains present.",
                before, backup, command, after, journalPath);
        if (command.ExitCode != 0 && SameUnencrypted(before, after))
            return new(volume, BitLockerEncryptionOutcome.NotApplied,
                "Windows returned without starting encryption. The selected volume remains fully decrypted.",
                before, backup, command, after, journalPath);
        if (command.ExitCode == 0 && SameUnencrypted(before, after))
            return new(volume, BitLockerEncryptionOutcome.Failed,
                "Windows returned success but the selected volume still reports fully decrypted. Encryption was not verified.",
                before, backup, command, after, journalPath);
        return new(volume, BitLockerEncryptionOutcome.Uncertain,
            "The selected volume changed, but the requested encryption state was not verified. Do not repeat the request until it is reconciled.",
            before, backup, command, after, journalPath);
    }

    private static bool IsStarted(BitLockerEncryptionState state, BitLockerOptions options) =>
        state.Complete && state.VolumeStatus is "EncryptionInProgress" or "FullyEncrypted" &&
        state.EncryptionPercentage is >= 0 and <= 100 &&
        string.Equals(state.EncryptionMethod, MethodName(options.Encryption), StringComparison.Ordinal);

    private static bool SameUnencrypted(BitLockerEncryptionState before, BitLockerEncryptionState after) =>
        SameIdentity(before, after) && after.Complete && after.VolumeStatus == "FullyDecrypted" &&
        after.EncryptionPercentage == 0 && after.ProtectionStatus == "Off" && after.EncryptionMethod == "None";

    private static bool SameIdentity(BitLockerEncryptionState left, BitLockerEncryptionState right) =>
        left.Complete && right.Complete && string.Equals(left.Volume, right.Volume, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Identity, right.Identity, StringComparison.Ordinal) &&
        left.Protectors.OrderBy(p => p.Id).ThenBy(p => p.Type, StringComparer.Ordinal)
            .SequenceEqual(right.Protectors.OrderBy(p => p.Id).ThenBy(p => p.Type, StringComparer.Ordinal));

    private static void ValidateSelection(BitLockerOptions options, Guid recoveryProtector)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (options.Operation != BitLockerOperation.Encrypt || options.Backup != RecoveryDestination.File || recoveryProtector == Guid.Empty)
            throw new ArgumentException("Select Encrypt, a file recovery destination and an existing recovery-password protector.");
    }

    internal static string MethodName(BitLockerEncryption method) => method switch
    {
        BitLockerEncryption.Aes128 => "Aes128",
        BitLockerEncryption.Aes256 => "Aes256",
        BitLockerEncryption.XtsAes128 => "XtsAes128",
        BitLockerEncryption.XtsAes256 => "XtsAes256",
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static async Task<PendingRecord> ReadPendingAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 131_072) throw new InvalidDataException("Pending encryption record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Pending encryption record contains conflicting fields.");
            var pending = document.RootElement.Deserialize<PendingRecord>(StrictJson) ??
                throw new InvalidDataException("Pending encryption record is empty.");
            BitLockerOptions options = PendingOptions(pending);
            ValidateSelection(options, pending.RecoveryProtector);
            if (pending.Schema != 1 || !pending.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                pending.StartedUtc == default || EligibilityBlocker(options, pending.RecoveryProtector, pending.Before) is not null ||
                !pending.Backup.Verified || pending.Backup.ProtectorId != pending.RecoveryProtector ||
                pending.Backup.Destination != RecoveryDestination.File || pending.Backup.VolumeIdentity != pending.Before.Identity)
                throw new InvalidDataException("Pending encryption record contains invalid evidence.");
            return pending;
        }
        catch (JsonException error) { throw new InvalidDataException("Pending encryption record is invalid.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Pending encryption record contains an invalid selection.", error); }
    }

    private static BitLockerOptions PendingOptions(PendingRecord pending)
    {
        new BitLockerOptions(pending.Volume).Validate();
        if (!Enum.IsDefined(pending.Encryption)) throw new ArgumentException("Pending encryption method is invalid.");
        string otherVolume = char.ToUpperInvariant(pending.Volume[0]) == 'C' ? "D:" : "C:";
        return new(pending.Volume, BitLockerOperation.Encrypt, pending.Encryption, pending.UsedSpaceOnly,
            RecoveryDestination.File, otherVolume + "\\KillerBench-recovery-placeholder.txt");
    }

    private static async Task FinishNoDispatchAsync(string pendingPath, string journalPath,
        BitLockerEncryptionResult result)
    {
        await JournalPersistence.WriteAsync(journalPath,
            JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        File.Move(pendingPath, journalPath + ".prepared.json");
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation,
        string name, TimeSpan timeout, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(timeout);
        try { return await operation(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new TimeoutException(name + " exceeded its deadline."); }
    }

    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception;

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsBitLockerEncryptionHost : IBitLockerEncryptionHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();

    public Task<RestartSnapshot> ReadRestartAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(RestartDiagnostics.Read());
    }

    public async Task<BitLockerEncryptionState> InspectAsync(string volume, CancellationToken token)
    {
        new BitLockerOptions(volume).Validate();
        volume = volume.ToUpperInvariant();
        char drive = volume[0];
        string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$b=@(Get-BitLockerVolume -MountPoint '" + volume + "' -ErrorAction Stop);" +
            "$v=@(Get-Volume -DriveLetter '" + drive + "' -ErrorAction Stop);" +
            "$t=Get-Tpm -ErrorAction Stop;$policy=Test-Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\FVE' -ErrorAction Stop;" +
            "if($b.Count -ne 1 -or $v.Count -ne 1){throw 'Volume not unique'};" +
            "[pscustomobject]@{volume=[string]$b[0].MountPoint;identity=[string]$v[0].UniqueId;volumeStatus=[string]$b[0].VolumeStatus;" +
            "protectionStatus=[string]$b[0].ProtectionStatus;lockStatus=[string]$b[0].LockStatus;encryptionPercentage=[int]$b[0].EncryptionPercentage;" +
            "encryptionMethod=[string]$b[0].EncryptionMethod;fileSystem=[string]$v[0].FileSystemType;volumeType=[string]$b[0].VolumeType;" +
            "tpmReady=[bool]$t.TpmReady;policyPresent=[bool]$policy;protectors=@($b[0].KeyProtector|ForEach-Object{[pscustomobject]@{id=[string]$_.KeyProtectorId;type=[string]$_.KeyProtectorType}})}|ConvertTo-Json -Depth 4 -Compress";
        var result = await BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(result, volume);
    }

    public async Task<RecoveryBackupReceipt> BackupAsync(BitLockerOptions options, Guid protector,
        IProgress<string>? progress, CancellationToken token)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KillerBench", "BitLockerEncryptionBackup");
        var backupOptions = options with { Operation = BitLockerOperation.BackupRecovery };
        var backupHost = new WindowsBitLockerBackupHost();
        if (File.Exists(Path.Combine(folder, "backup.pending.json")))
        {
            var recovery = await BitLockerBackupReconciliation.RunAsync(backupOptions, protector, folder,
                backupHost, progress, token: token).ConfigureAwait(false);
            if (recovery.Outcome != BitLockerBackupReconciliationOutcome.Verified)
                return new("", protector, options.Backup, false);
            string? identity = await backupHost.VolumeIdentityAsync(options.Volume, token).ConfigureAwait(false);
            return new(identity ?? "", protector, options.Backup, !string.IsNullOrWhiteSpace(identity));
        }
        var result = await BitLockerBackupExecution.RunAsync(backupOptions, protector, folder,
            backupHost, progress, token: token).ConfigureAwait(false);
        return result.Receipt ?? new("", protector, options.Backup, false);
    }

    public Task<ToolResult> StartAsync(BitLockerOptions options, IProgress<string>? progress, CancellationToken token)
    {
        options.Validate();
        string method = options.Encryption switch
        {
            BitLockerEncryption.Aes128 => "aes128",
            BitLockerEncryption.Aes256 => "aes256",
            BitLockerEncryption.XtsAes128 => "xts_aes128",
            BitLockerEncryption.XtsAes256 => "xts_aes256",
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        var arguments = new List<string> { "-on", options.Volume.ToUpperInvariant(), "-em", method };
        if (options.UsedSpaceOnly) arguments.Add("-usedspaceonly");
        arguments.Add("-skiphardwaretest");
        return BoundedTool.RunAsync(new RepairStep("manage-bde.exe", [.. arguments], false),
            TimeSpan.FromMinutes(5), progress, token);
    }

    public static BitLockerEncryptionState Interpret(ToolResult result, string volume)
    {
        new BitLockerOptions(volume).Validate();
        volume = volume.ToUpperInvariant();
        BitLockerEncryptionState Unknown() => new(volume, "", "", "", "", -1, "", "", "", false, false, [], false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            JsonElement root = document.RootElement;
            string[] expected = ["volume", "identity", "volumeStatus", "protectionStatus", "lockStatus", "encryptionPercentage", "encryptionMethod", "fileSystem", "volumeType", "tpmReady", "policyPresent", "protectors"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Length ||
                !expected.All(name => root.TryGetProperty(name, out _))) return Unknown();
            string? reportedVolume = root.GetProperty("volume").GetString();
            string? identity = root.GetProperty("identity").GetString();
            string? status = root.GetProperty("volumeStatus").GetString();
            string? protection = root.GetProperty("protectionStatus").GetString();
            string? locked = root.GetProperty("lockStatus").GetString();
            string? method = root.GetProperty("encryptionMethod").GetString();
            string? fileSystem = root.GetProperty("fileSystem").GetString();
            string? volumeType = root.GetProperty("volumeType").GetString();
            bool tpmReady = root.GetProperty("tpmReady").GetBoolean();
            bool policyPresent = root.GetProperty("policyPresent").GetBoolean();
            int percentage = root.GetProperty("encryptionPercentage").GetInt32();
            JsonElement items = root.GetProperty("protectors");
            if (!string.Equals(reportedVolume, volume, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(identity) || identity.Length > 512 || identity.Any(char.IsControl) ||
                status is not ("FullyDecrypted" or "FullyEncrypted" or "EncryptionInProgress" or "DecryptionInProgress" or "EncryptionPaused" or "DecryptionPaused") ||
                protection is not ("On" or "Off") || locked is not ("Locked" or "Unlocked") || percentage is < 0 or > 100 ||
                method is not ("None" or "Aes128" or "Aes256" or "XtsAes128" or "XtsAes256") ||
                fileSystem is null or { Length: > 32 } || volumeType is null or { Length: > 64 } ||
                items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > 64) return Unknown();
            var protectors = new List<BitLockerProtector>();
            foreach (JsonElement item in items.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 2 ||
                    !item.TryGetProperty("id", out var idElement) || !item.TryGetProperty("type", out var typeElement) ||
                    !Guid.TryParse(idElement.GetString(), out Guid id) || id == Guid.Empty) return Unknown();
                string? type = typeElement.GetString();
                if (string.IsNullOrWhiteSpace(type) || type.Length > 64 || type.Any(char.IsControl) || protectors.Any(p => p.Id == id)) return Unknown();
                protectors.Add(new(id, type));
            }
            return new(volume, identity, status, protection, locked, percentage, method, fileSystem, volumeType,
                tpmReady, policyPresent, protectors.AsReadOnly(), true);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { return Unknown(); }
    }
}
