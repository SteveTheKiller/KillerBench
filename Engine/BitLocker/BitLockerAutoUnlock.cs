using System.Text.Json;

namespace KillerBench.Engine.BitLocker;

public sealed record BitLockerAutoUnlockState(string Volume, string Identity, string VolumeType, string VolumeStatus, string LockStatus,
    bool AutoUnlockEnabled, string SystemVolumeStatus, string SystemProtectionStatus, bool Complete);

public enum BitLockerAutoUnlockOutcome { Preview, Blocked, Enabled, NotApplied, Failed, Uncertain, Stopped }

public sealed record BitLockerAutoUnlockResult(string Volume, BitLockerAutoUnlockOutcome Outcome, string Detail,
    BitLockerAutoUnlockState? Before = null, ToolResult? Command = null, BitLockerAutoUnlockState? After = null, string? JournalPath = null)
{
    public string Machine { get; init; } = Environment.MachineName;
    public int GetExitCode() => Outcome switch
    {
        BitLockerAutoUnlockOutcome.Preview or BitLockerAutoUnlockOutcome.Enabled => 0,
        BitLockerAutoUnlockOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IBitLockerAutoUnlockHost
{
    bool IsAdministrator { get; }
    Task<BitLockerAutoUnlockState> InspectAsync(string volume, CancellationToken token);
    Task<ToolResult> EnableAsync(string volume, CancellationToken token);
}

/// <summary>Turns on auto-unlock for one exact encrypted data volume so it unlocks when Windows starts. Windows stores the unlock key on the
/// operating-system volume, so that volume must be fully encrypted with protection on. Each request adds a key protector, so a durable pending
/// record prevents a second request until the first is reconciled.</summary>
public static class BitLockerAutoUnlock
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };
    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc, string Volume, BitLockerAutoUnlockState Before, ToolResult? Command);

    public static string NormalizeVolume(string volume) => BitLockerProtectionResume.NormalizeVolume(volume);

    public static BitLockerAutoUnlockResult Preview(string volume) => new(NormalizeVolume(volume), BitLockerAutoUnlockOutcome.Preview,
        "Preview only. Execution will require an unlocked data volume that is encrypting or fully encrypted, auto-unlock currently off, a fully encrypted operating-system volume with protection on and administrator access, then turn on auto-unlock once and confirm it. Existing protectors and recovery backups are not changed.");

    public static async Task<BitLockerAutoUnlockResult> RunAsync(string volume, bool execute, string journalFolder,
        IBitLockerAutoUnlockHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        volume = NormalizeVolume(volume);
        token.ThrowIfCancellationRequested();
        if (!execute) return Preview(volume);

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "bitlocker-autounlock.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "bitlocker-autounlock.pending.json");
        if (File.Exists(pendingPath))
            return new(volume, BitLockerAutoUnlockOutcome.Blocked, "A prior auto-unlock request needs reconciliation before another can start.", JournalPath: pendingPath);

        Report(progress, "Inspecting " + volume + " and the operating-system volume.");
        BitLockerAutoUnlockState before;
        try { before = await Required(ct => host.InspectAsync(volume, ct), "Auto-unlock inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerAutoUnlockOutcome.Blocked, "Auto-unlock eligibility could not be established: " + error.Message); }
        if (EligibilityBlocker(volume, before) is { } blocker) return new(volume, BitLockerAutoUnlockOutcome.Blocked, blocker, before);
        if (!host.IsAdministrator)
            return new(volume, BitLockerAutoUnlockOutcome.Blocked, "Administrator access is required. Auto-unlock was not turned on.", before);

        var pending = new PendingRecord(1, Environment.MachineName, DateTimeOffset.UtcNow, volume, before, null);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
        string journalPath = Path.Combine(root, "bitlocker-autounlock-" + Guid.NewGuid().ToString("N") + ".json");
        ToolResult? command = null;
        bool dispatched = false;
        BitLockerAutoUnlockResult result;
        try
        {
            Report(progress, "Repeating volume checks immediately before the request.");
            var fresh = await Required(ct => host.InspectAsync(volume, ct), "Final auto-unlock inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
            if (EligibilityBlocker(volume, fresh) is not null || fresh.Identity != before.Identity || !host.IsAdministrator)
            {
                result = new(volume, BitLockerAutoUnlockOutcome.Blocked,
                    "Final volume or administrator evidence no longer authorizes auto-unlock. No request was sent.", before, After: fresh, JournalPath: journalPath);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Turning on auto-unlock for " + volume + ".");
            dispatched = true;
            command = await Required(ct => host.EnableAsync(volume, ct), "Auto-unlock request", TimeSpan.FromMinutes(2), token: token).ConfigureAwait(false);
            await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending with { Command = command }, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
            result = await VerifyAsync(volume, before, command, journalPath, host, progress, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new(volume, BitLockerAutoUnlockOutcome.Stopped,
                dispatched ? "The request stopped after it may have reached Windows. Reconcile before trying again."
                    : "The final check was canceled before any request was sent. Auto-unlock was not turned on.", before, command, JournalPath: journalPath);
        }
        catch (Exception error) when (Unavailable(error))
        {
            result = new(volume, dispatched ? BitLockerAutoUnlockOutcome.Uncertain : BitLockerAutoUnlockOutcome.Blocked,
                dispatched ? "The request was interrupted: " + error.Message + ". Reconcile before trying again."
                    : "Final eligibility was unavailable: " + error.Message + ". No request was sent.", before, command, JournalPath: journalPath);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (!dispatched || result.Outcome is BitLockerAutoUnlockOutcome.Enabled or BitLockerAutoUnlockOutcome.NotApplied or BitLockerAutoUnlockOutcome.Failed)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<BitLockerAutoUnlockResult> ReconcileAsync(string journalFolder, IBitLockerAutoUnlockHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "bitlocker-autounlock.pending.json");
        if (!File.Exists(pendingPath)) return new("C:", BitLockerAutoUnlockOutcome.Blocked, "No pending auto-unlock request was found.");
        using var gate = new FileStream(Path.Combine(root, "bitlocker-autounlock.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        PendingRecord pending;
        try
        {
            await using var stream = new FileStream(pendingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length is <= 0 or > 65_536) throw new InvalidDataException("Pending auto-unlock record size is invalid.");
            pending = await JsonSerializer.DeserializeAsync<PendingRecord>(stream, StrictJson, token).ConfigureAwait(false) ??
                throw new InvalidDataException("Pending auto-unlock record is empty.");
            if (pending.Schema != 1 || !pending.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                NormalizeVolume(pending.Volume) != pending.Volume || !pending.Before.Complete)
                throw new InvalidDataException("Pending auto-unlock record contains invalid evidence.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error) || error is JsonException or ArgumentException)
        {
            return new("C:", BitLockerAutoUnlockOutcome.Uncertain,
                "The interrupted auto-unlock record could not be verified for this computer. It was retained and the request was not repeated.", JournalPath: pendingPath);
        }
        string journalPath = Path.Combine(root, "bitlocker-autounlock-recovered-" + Guid.NewGuid().ToString("N") + ".json");
        var result = await VerifyAsync(pending.Volume, pending.Before, pending.Command, journalPath, host, progress, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome is BitLockerAutoUnlockOutcome.Enabled || result.Outcome == BitLockerAutoUnlockOutcome.NotApplied && pending.Command is not null)
            File.Move(pendingPath, journalPath + ".prepared.json");
        return result;
    }

    public static string? EligibilityBlocker(string volume, BitLockerAutoUnlockState state)
    {
        volume = NormalizeVolume(volume);
        if (!state.Complete || !string.Equals(volume, state.Volume, StringComparison.OrdinalIgnoreCase))
            return "A complete exact selected-volume inventory is required. Auto-unlock was not turned on.";
        if (string.IsNullOrWhiteSpace(state.Identity) || state.Identity.Length > 512 || state.Identity.Any(char.IsControl))
            return "The exact selected volume identity could not be confirmed. Auto-unlock was not turned on.";
        if (state.VolumeType != "Data") return "Auto-unlock applies only to data volumes. The operating-system volume unlocks with its own protectors.";
        if (state.LockStatus != "Unlocked") return "Unlock the selected volume first. Auto-unlock was not turned on.";
        if (state.VolumeStatus is not ("FullyEncrypted" or "EncryptionInProgress"))
            return "The selected volume must be encrypting or fully encrypted before auto-unlock can be turned on.";
        if (state.AutoUnlockEnabled) return "Auto-unlock is already on for this volume. No request is needed.";
        if (state.SystemVolumeStatus != "FullyEncrypted" || state.SystemProtectionStatus != "On")
            return "Windows keeps the auto-unlock key on the operating-system volume, so that volume must be fully encrypted with protection on first.";
        return null;
    }

    private static async Task<BitLockerAutoUnlockResult> VerifyAsync(string volume, BitLockerAutoUnlockState before, ToolResult? command,
        string journalPath, IBitLockerAutoUnlockHost host, IProgress<string>? progress, CancellationToken token)
    {
        Report(progress, "Confirming auto-unlock on " + volume + ".");
        BitLockerAutoUnlockState after;
        try { after = await Required(ct => host.InspectAsync(volume, ct), "Auto-unlock verification", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerAutoUnlockOutcome.Uncertain, "Verification was unavailable: " + error.Message, before, command, JournalPath: journalPath); }
        bool same = after.Complete && after.Identity == before.Identity;
        bool commandComplete = command is not null && command.ExitCode == 0 && !command.TimedOut && !command.Truncated && string.IsNullOrWhiteSpace(command.Error);
        if (same && after.AutoUnlockEnabled)
            return new(volume, BitLockerAutoUnlockOutcome.Enabled,
                "Auto-unlock is on. The volume will unlock when Windows starts. Keep its recovery password backed up for use on another computer.", before, command, after, journalPath);
        if (same && command is null)
            return new(volume, BitLockerAutoUnlockOutcome.Uncertain,
                "Auto-unlock is off, and the record does not prove whether Windows received the request. It was retained and not repeated.", before, command, after, journalPath);
        if (same)
            return new(volume, commandComplete ? BitLockerAutoUnlockOutcome.NotApplied : BitLockerAutoUnlockOutcome.Failed,
                commandComplete ? "Windows completed the request, but auto-unlock is still off. The request was not repeated."
                    : "The request failed and auto-unlock is still off. The request was not repeated.", before, command, after, journalPath);
        return new(volume, BitLockerAutoUnlockOutcome.Uncertain, "The volume after the request could not be matched to this volume.", before, command, after, journalPath);
    }

    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
        System.Security.SecurityException;
    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name, TimeSpan limit, CancellationToken token)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        try { return await pending.WaitAsync(limit, token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence."); }
        catch (TimeoutException) { throw new TimeoutException(name + " exceeded its deadline."); }
    }
    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsBitLockerAutoUnlockHost : IBitLockerAutoUnlockHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();

    public async Task<BitLockerAutoUnlockState> InspectAsync(string volume, CancellationToken token)
    {
        volume = BitLockerAutoUnlock.NormalizeVolume(volume);
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$b=Get-BitLockerVolume -MountPoint '" + volume + "' -ErrorAction Stop;" +
            "$v=Get-Volume -DriveLetter '" + volume[0] + "' -ErrorAction Stop;" +
            "$s=Get-BitLockerVolume -MountPoint $env:SystemDrive -ErrorAction Stop;" +
            "[pscustomobject]@{volume=[string]$b.MountPoint;identity=[string]$v.UniqueId;volumeType=[string]$b.VolumeType;volumeStatus=[string]$b.VolumeStatus;" +
            "lockStatus=[string]$b.LockStatus;autoUnlock=($b.AutoUnlockEnabled -eq $true);systemStatus=[string]$s.VolumeStatus;systemProtection=[string]$s.ProtectionStatus} | ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(result, volume);
    }

    public Task<ToolResult> EnableAsync(string volume, CancellationToken token)
    {
        volume = BitLockerAutoUnlock.NormalizeVolume(volume);
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "Enable-BitLockerAutoUnlock -MountPoint '" + volume + "' -ErrorAction Stop | Out-Null";
        return BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], false), TimeSpan.FromMinutes(2), token: token);
    }

    public static BitLockerAutoUnlockState Interpret(ToolResult result, string volume)
    {
        volume = BitLockerAutoUnlock.NormalizeVolume(volume);
        BitLockerAutoUnlockState Unknown() => new(volume, "", "", "", "", false, "", "", false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            JsonElement root = document.RootElement;
            string[] expected = ["volume", "identity", "volumeType", "volumeStatus", "lockStatus", "autoUnlock", "systemStatus", "systemProtection"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Length ||
                !expected.All(name => root.TryGetProperty(name, out _)) ||
                root.GetProperty("autoUnlock").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Unknown();
            string? identity = root.GetProperty("identity").GetString();
            string? type = root.GetProperty("volumeType").GetString();
            string? status = root.GetProperty("volumeStatus").GetString();
            string? lockStatus = root.GetProperty("lockStatus").GetString();
            string? systemStatus = root.GetProperty("systemStatus").GetString();
            string? systemProtection = root.GetProperty("systemProtection").GetString();
            string[] statuses = ["FullyDecrypted", "FullyEncrypted", "EncryptionInProgress", "DecryptionInProgress", "EncryptionPaused", "DecryptionPaused"];
            if (!string.Equals(root.GetProperty("volume").GetString(), volume, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(identity) || identity.Length > 512 || identity.Any(char.IsControl) ||
                type is not ("Data" or "OperatingSystem") || !statuses.Contains(status) || !statuses.Contains(systemStatus) ||
                lockStatus is not ("Locked" or "Unlocked") || systemProtection is not ("On" or "Off" or "Unknown")) return Unknown();
            return new(volume, identity, type, status!, lockStatus, root.GetProperty("autoUnlock").GetBoolean(), systemStatus!, systemProtection, true);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return Unknown(); }
    }
}
