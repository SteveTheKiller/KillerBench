using System.Text.Json;

namespace KillerBench.Engine.BitLocker;

public sealed record BitLockerDecryptionState(string Volume, string Identity, string VolumeStatus, string LockStatus,
    string EncryptionMethod, bool OperatingSystemVolume, int AutoUnlockVolumes, bool Complete);

public enum BitLockerDecryptionOutcome { Preview, Blocked, Started, NotApplied, Failed, Uncertain, Stopped }

public sealed record BitLockerDecryptionResult(string Volume, BitLockerDecryptionOutcome Outcome, string Detail,
    BitLockerDecryptionState? Before = null, ToolResult? Command = null, BitLockerDecryptionState? After = null, string? JournalPath = null)
{
    public string Machine { get; init; } = Environment.MachineName;
    public int GetExitCode() => Outcome switch
    {
        BitLockerDecryptionOutcome.Preview or BitLockerDecryptionOutcome.Started => 0,
        BitLockerDecryptionOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IBitLockerDecryptionHost
{
    bool IsAdministrator { get; }
    Task<RestartSnapshot> ReadRestartAsync(CancellationToken token);
    Task<BitLockerDecryptionState> InspectAsync(string volume, CancellationToken token);
    Task<ToolResult> DecryptAsync(string volume, CancellationToken token);
}

/// <summary>Starts decryption of one exact fully encrypted volume, for example to re-encrypt it with a required method. Decryption removes protection
/// while it runs and afterwards, so it needs its own confirmation, and an operating-system volume is refused while other drives auto-unlock from it.
/// A durable pending record prevents a second request until the first is reconciled.</summary>
public static class BitLockerDecryption
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };
    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc, string Volume, BitLockerDecryptionState Before, ToolResult? Command);

    public static string NormalizeVolume(string volume) => BitLockerProtectionResume.NormalizeVolume(volume);

    public static BitLockerDecryptionResult Preview(string volume) => new(NormalizeVolume(volume), BitLockerDecryptionOutcome.Preview,
        "Preview only. Execution will require a fully encrypted, unlocked volume, no drives that auto-unlock from an operating-system volume, clear restart evidence, administrator access and a separate decryption confirmation, then start decryption once and confirm it began. The volume is unprotected while decryption runs and afterwards until it is encrypted again. Protectors and recovery backups are not changed.");

    public static async Task<BitLockerDecryptionResult> RunAsync(string volume, bool execute, bool confirmUnprotected, string journalFolder,
        IBitLockerDecryptionHost host, IProgress<string>? progress = null, TimeSpan? operationTimeout = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        volume = NormalizeVolume(volume);
        token.ThrowIfCancellationRequested();
        if (!execute) return Preview(volume);
        if (!confirmUnprotected)
            return new(volume, BitLockerDecryptionOutcome.Blocked, "Decryption leaves the volume unprotected. Confirm that separately before it can start.");
        TimeSpan limit = operationTimeout ?? TimeSpan.FromMinutes(2);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(operationTimeout));

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "bitlocker-decrypt.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "bitlocker-decrypt.pending.json");
        if (File.Exists(pendingPath))
            return new(volume, BitLockerDecryptionOutcome.Blocked, "A prior decryption request needs reconciliation before another can start.", JournalPath: pendingPath);

        Report(progress, "Inspecting " + volume + " and auto-unlock drives.");
        BitLockerDecryptionState before;
        try { before = await Required(ct => host.InspectAsync(volume, ct), "Decryption inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerDecryptionOutcome.Blocked, "Decryption eligibility could not be established: " + error.Message); }
        if (EligibilityBlocker(volume, before) is { } blocker) return new(volume, BitLockerDecryptionOutcome.Blocked, blocker, before);
        RestartSnapshot restart;
        try { restart = await Required(host.ReadRestartAsync, "Restart inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerDecryptionOutcome.Blocked, "Restart readiness could not be established: " + error.Message, before); }
        if (RestartDiagnostics.Assess(restart) is { State: not CheckState.Clear } restartCheck)
            return new(volume, BitLockerDecryptionOutcome.Blocked, "Restart readiness must be clear before decryption starts. " + restartCheck.Detail, before);
        if (!host.IsAdministrator)
            return new(volume, BitLockerDecryptionOutcome.Blocked, "Administrator access is required. Decryption was not started.", before);

        var pending = new PendingRecord(1, Environment.MachineName, DateTimeOffset.UtcNow, volume, before, null);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
        string journalPath = Path.Combine(root, "bitlocker-decrypt-" + Guid.NewGuid().ToString("N") + ".json");
        ToolResult? command = null;
        bool dispatched = false;
        BitLockerDecryptionResult result;
        try
        {
            Report(progress, "Repeating volume, auto-unlock and restart checks immediately before the request.");
            var fresh = await Required(ct => host.InspectAsync(volume, ct), "Final decryption inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
            var freshRestart = await Required(host.ReadRestartAsync, "Final restart inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
            if (EligibilityBlocker(volume, fresh) is not null || fresh.Identity != before.Identity ||
                RestartDiagnostics.Assess(freshRestart).State != CheckState.Clear || !host.IsAdministrator)
            {
                result = new(volume, BitLockerDecryptionOutcome.Blocked,
                    "Final volume, auto-unlock, restart or administrator evidence no longer authorizes decryption. No request was sent.", before, After: fresh, JournalPath: journalPath);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Starting decryption of " + volume + ".");
            dispatched = true;
            command = await Required(ct => host.DecryptAsync(volume, ct), "Decryption request", limit, token: token).ConfigureAwait(false);
            await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending with { Command = command }, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
            result = await VerifyAsync(volume, before, command, journalPath, host, progress, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new(volume, BitLockerDecryptionOutcome.Stopped,
                dispatched ? "The request stopped after it may have reached Windows. Reconcile before trying again."
                    : "The final check was canceled before any request was sent. Decryption was not started.", before, command, JournalPath: journalPath);
        }
        catch (Exception error) when (Unavailable(error))
        {
            result = new(volume, dispatched ? BitLockerDecryptionOutcome.Uncertain : BitLockerDecryptionOutcome.Blocked,
                dispatched ? "The request was interrupted: " + error.Message + ". Reconcile before trying again."
                    : "Final eligibility was unavailable: " + error.Message + ". No request was sent.", before, command, JournalPath: journalPath);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (!dispatched || result.Outcome is BitLockerDecryptionOutcome.Started or BitLockerDecryptionOutcome.NotApplied or BitLockerDecryptionOutcome.Failed)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<BitLockerDecryptionResult> ReconcileAsync(string journalFolder, IBitLockerDecryptionHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "bitlocker-decrypt.pending.json");
        if (!File.Exists(pendingPath)) return new("C:", BitLockerDecryptionOutcome.Blocked, "No pending decryption request was found.");
        using var gate = new FileStream(Path.Combine(root, "bitlocker-decrypt.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        PendingRecord pending;
        try
        {
            await using var stream = new FileStream(pendingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length is <= 0 or > 65_536) throw new InvalidDataException("Pending decryption record size is invalid.");
            pending = await JsonSerializer.DeserializeAsync<PendingRecord>(stream, StrictJson, token).ConfigureAwait(false) ??
                throw new InvalidDataException("Pending decryption record is empty.");
            if (pending.Schema != 1 || !pending.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                NormalizeVolume(pending.Volume) != pending.Volume || !pending.Before.Complete)
                throw new InvalidDataException("Pending decryption record contains invalid evidence.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error) || error is JsonException or ArgumentException)
        {
            return new("C:", BitLockerDecryptionOutcome.Uncertain,
                "The interrupted decryption record could not be verified for this computer. It was retained and the request was not repeated.", JournalPath: pendingPath);
        }
        string journalPath = Path.Combine(root, "bitlocker-decrypt-recovered-" + Guid.NewGuid().ToString("N") + ".json");
        var result = await VerifyAsync(pending.Volume, pending.Before, pending.Command, journalPath, host, progress, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome is BitLockerDecryptionOutcome.Started or BitLockerDecryptionOutcome.NotApplied)
            File.Move(pendingPath, journalPath + ".prepared.json");
        return result;
    }

    public static string? EligibilityBlocker(string volume, BitLockerDecryptionState state)
    {
        volume = NormalizeVolume(volume);
        if (!state.Complete || !string.Equals(volume, state.Volume, StringComparison.OrdinalIgnoreCase))
            return "A complete exact selected-volume inventory is required. Decryption was not started.";
        if (string.IsNullOrWhiteSpace(state.Identity) || state.Identity.Length > 512 || state.Identity.Any(char.IsControl))
            return "The exact selected volume identity could not be confirmed. Decryption was not started.";
        if (state.LockStatus != "Unlocked") return "The selected volume must be unlocked. Decryption was not started.";
        if (state.VolumeStatus != "FullyEncrypted")
            return state.VolumeStatus is "FullyDecrypted" or "DecryptionInProgress" or "DecryptionPaused"
                ? "The volume is already decrypted or decrypting. No request is needed."
                : "Only a fully encrypted volume can start decryption. Let the current conversion finish first.";
        if (state.OperatingSystemVolume && state.AutoUnlockVolumes > 0)
            return state.AutoUnlockVolumes + " drive(s) unlock automatically from this operating-system volume. Turn off auto-unlock on them first so they keep a working unlock method.";
        return null;
    }

    private static async Task<BitLockerDecryptionResult> VerifyAsync(string volume, BitLockerDecryptionState before, ToolResult? command,
        string journalPath, IBitLockerDecryptionHost host, IProgress<string>? progress, CancellationToken token)
    {
        Report(progress, "Confirming decryption state on " + volume + ".");
        BitLockerDecryptionState after;
        try { after = await Required(ct => host.InspectAsync(volume, ct), "Decryption verification", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerDecryptionOutcome.Uncertain, "Verification was unavailable: " + error.Message, before, command, JournalPath: journalPath); }
        bool same = after.Complete && after.Identity == before.Identity;
        bool commandComplete = command is not null && command.ExitCode == 0 && !command.TimedOut && !command.Truncated && string.IsNullOrWhiteSpace(command.Error);
        if (same && after.VolumeStatus is "DecryptionInProgress" or "FullyDecrypted")
            return new(volume, BitLockerDecryptionOutcome.Started,
                after.VolumeStatus == "FullyDecrypted" ? "The volume is fully decrypted and unprotected. Encrypt it again with the required method."
                    : "Decryption is running and the volume is unprotected. Encrypt it again with the required method once it finishes.", before, command, after, journalPath);
        if (same && after.VolumeStatus == "FullyEncrypted")
            return new(volume, commandComplete ? BitLockerDecryptionOutcome.NotApplied : BitLockerDecryptionOutcome.Failed,
                commandComplete ? "Windows completed the request, but the volume is still fully encrypted. The request was not repeated."
                    : "The request failed and the volume is still fully encrypted. The request was not repeated.", before, command, after, journalPath);
        return new(volume, BitLockerDecryptionOutcome.Uncertain, "The volume state after the request could not be matched to this volume.", before, command, after, journalPath);
    }

    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
        System.Security.SecurityException or OperationCanceledException;
    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name, TimeSpan limit, CancellationToken token)
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

public sealed class WindowsBitLockerDecryptionHost : IBitLockerDecryptionHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();

    public Task<RestartSnapshot> ReadRestartAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(RestartDiagnostics.Read());
    }

    public async Task<BitLockerDecryptionState> InspectAsync(string volume, CancellationToken token)
    {
        volume = BitLockerDecryption.NormalizeVolume(volume);
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$b=Get-BitLockerVolume -MountPoint '" + volume + "' -ErrorAction Stop;" +
            "$v=Get-Volume -DriveLetter '" + volume[0] + "' -ErrorAction Stop;" +
            "$a=@(Get-BitLockerVolume -ErrorAction Stop | Where-Object { $_.AutoUnlockEnabled -eq $true }).Count;" +
            "[pscustomobject]@{volume=[string]$b.MountPoint;identity=[string]$v.UniqueId;volumeStatus=[string]$b.VolumeStatus;lockStatus=[string]$b.LockStatus;" +
            "encryptionMethod=[string]$b.EncryptionMethod;operatingSystem=([string]$b.VolumeType -eq 'OperatingSystem');autoUnlock=[int]$a} | ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(result, volume);
    }

    public Task<ToolResult> DecryptAsync(string volume, CancellationToken token)
    {
        volume = BitLockerDecryption.NormalizeVolume(volume);
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "Disable-BitLocker -MountPoint '" + volume + "' -ErrorAction Stop | Out-Null";
        return BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], false), TimeSpan.FromMinutes(2), token: token);
    }

    public static BitLockerDecryptionState Interpret(ToolResult result, string volume)
    {
        volume = BitLockerDecryption.NormalizeVolume(volume);
        BitLockerDecryptionState Unknown() => new(volume, "", "", "", "", false, 0, false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            JsonElement root = document.RootElement;
            string[] expected = ["volume", "identity", "volumeStatus", "lockStatus", "encryptionMethod", "operatingSystem", "autoUnlock"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Length ||
                !expected.All(name => root.TryGetProperty(name, out _)) ||
                root.GetProperty("operatingSystem").ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.GetProperty("autoUnlock").TryGetInt32(out int autoUnlock) || autoUnlock is < 0 or > 64) return Unknown();
            string? identity = root.GetProperty("identity").GetString();
            string? status = root.GetProperty("volumeStatus").GetString();
            string? lockStatus = root.GetProperty("lockStatus").GetString();
            string? method = root.GetProperty("encryptionMethod").GetString();
            if (!string.Equals(root.GetProperty("volume").GetString(), volume, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(identity) || identity.Length > 512 || identity.Any(char.IsControl) ||
                status is not ("FullyDecrypted" or "FullyEncrypted" or "EncryptionInProgress" or "DecryptionInProgress" or "EncryptionPaused" or "DecryptionPaused") ||
                lockStatus is not ("Locked" or "Unlocked") || method is null || method.Length > 32 || method.Any(char.IsControl)) return Unknown();
            return new(volume, identity, status, lockStatus, method, root.GetProperty("operatingSystem").GetBoolean(), autoUnlock, true);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return Unknown(); }
    }
}
