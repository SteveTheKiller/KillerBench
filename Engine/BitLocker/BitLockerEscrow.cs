using System.Text.Json;

namespace KillerBench.Engine.BitLocker;

public enum BitLockerEscrowTarget { ActiveDirectory, EntraId }

public sealed record BitLockerEscrowState(string Volume, string Identity, string VolumeStatus,
    IReadOnlyList<BitLockerProtector> Protectors, bool DomainJoined, bool EntraJoined, bool Complete);

public enum BitLockerEscrowOutcome { Preview, Blocked, Accepted, Failed, Uncertain, Stopped }

public sealed record BitLockerEscrowResult(string Volume, BitLockerEscrowTarget Target, Guid Protector,
    BitLockerEscrowOutcome Outcome, string Detail, BitLockerEscrowState? Before = null, ToolResult? Command = null,
    BitLockerEscrowState? After = null, string? JournalPath = null)
{
    public string Machine { get; init; } = Environment.MachineName;
    public int GetExitCode() => Outcome switch
    {
        BitLockerEscrowOutcome.Preview or BitLockerEscrowOutcome.Accepted => 0,
        BitLockerEscrowOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IBitLockerEscrowHost
{
    bool IsAdministrator { get; }
    Task<BitLockerEscrowState> InspectAsync(string volume, CancellationToken token);
    Task<ToolResult> EscrowAsync(string volume, BitLockerEscrowTarget target, Guid protector, CancellationToken token);
}

/// <summary>Backs up one existing recovery-password protector to Active Directory or Microsoft Entra ID. The directory copy cannot be read back from the client, so success means Windows accepted the request for the same protector, never that escrow is proven.</summary>
public static class BitLockerEscrow
{
    private sealed record PendingRecord(int Schema, string Stage, string Outcome, string Machine, DateTimeOffset StartedUtc,
        string Volume, BitLockerEscrowTarget Target, Guid Protector, BitLockerEscrowState Before);

    public static string NormalizeVolume(string volume) => BitLockerProtectionResume.NormalizeVolume(volume);

    public static BitLockerEscrowResult Preview(string volume, BitLockerEscrowTarget target, Guid protector)
    {
        volume = NormalizeVolume(volume);
        ValidateArguments(target, protector);
        return new(volume, target, protector, BitLockerEscrowOutcome.Preview,
            "Preview only. Execution will confirm this exact recovery-password protector on " + volume + ", require " + JoinName(target) +
            " membership and administrator access, repeat those checks immediately before one backup request to " + TargetName(target) +
            ", then confirm the same protector remains. Encryption, protectors, policy and recovery files are not changed. The directory copy itself is not readable from this computer.");
    }

    public static async Task<BitLockerEscrowResult> RunAsync(string volume, BitLockerEscrowTarget target, Guid protector, bool execute,
        string journalFolder, IBitLockerEscrowHost host, IProgress<string>? progress = null, TimeSpan? operationTimeout = null,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        volume = NormalizeVolume(volume);
        ValidateArguments(target, protector);
        token.ThrowIfCancellationRequested();
        if (!execute) return Preview(volume, target, protector);
        TimeSpan limit = operationTimeout ?? TimeSpan.FromMinutes(2);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(operationTimeout));

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "bitlocker-escrow.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        Report(progress, "Inspecting " + volume + ", its protectors and directory membership.");
        BitLockerEscrowState before;
        try { before = await Required(ct => host.InspectAsync(volume, ct), "BitLocker escrow inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, target, protector, BitLockerEscrowOutcome.Blocked, "BitLocker escrow eligibility could not be established: " + error.Message); }
        string? blocker = EligibilityBlocker(volume, target, protector, before);
        if (blocker is not null) return new(volume, target, protector, BitLockerEscrowOutcome.Blocked, blocker, before);
        if (!host.IsAdministrator)
            return new(volume, target, protector, BitLockerEscrowOutcome.Blocked, "Administrator access is required. No backup request was sent.", before);

        string journalPath = Path.Combine(root, "bitlocker-escrow-" + Guid.NewGuid().ToString("N") + ".json");
        ToolResult? command = null;
        bool dispatched = false;
        BitLockerEscrowResult result;
        try
        {
            Report(progress, "Repeating protector and membership checks immediately before the request.");
            var fresh = await Required(ct => host.InspectAsync(volume, ct), "Final BitLocker escrow inspection", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
            if (EligibilityBlocker(volume, target, protector, fresh) is not null || !SameIdentity(before, fresh) || !host.IsAdministrator)
            {
                result = new(volume, target, protector, BitLockerEscrowOutcome.Blocked,
                    "Final volume, protector, membership or administrator evidence no longer authorizes the backup. No request was sent." +
                    (EligibilityBlocker(volume, target, protector, fresh) is { } detail ? " " + detail : ""), before, After: fresh, JournalPath: journalPath);
            }
            else
            {
                // Journal before acting. The outcome below replaces this record at the same path.
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(new PendingRecord(1, "Pending", "Pending", Environment.MachineName,
                    DateTimeOffset.UtcNow, volume, target, protector, before), Capture.JsonOptions), token: token).ConfigureAwait(false);
                Report(progress, "Sending the recovery-password backup request to " + TargetName(target) + ".");
                dispatched = true;
                command = await Required(ct => host.EscrowAsync(volume, target, protector, ct), "BitLocker escrow", limit, token: token).ConfigureAwait(false);
                Report(progress, "Confirming the same protector remains on " + volume + ".");
                BitLockerEscrowState? after = null;
                try { after = await Required(ct => host.InspectAsync(volume, ct), "BitLocker escrow verification", TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false); }
                catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error)) { }
                bool unchanged = after is not null && SameIdentity(before, after) && HasRecoveryPassword(after, protector);
                result = !Complete(command)
                    ? new(volume, target, protector, unchanged ? BitLockerEscrowOutcome.Failed : BitLockerEscrowOutcome.Uncertain,
                        unchanged
                            ? "Windows did not accept the backup request and the protector set is unchanged. Check connectivity to " + TargetName(target) + " and its backup policy before trying again."
                            : "Windows did not accept the backup request and the protector state after the request could not be confirmed.",
                        before, command, after, journalPath)
                    : unchanged
                        ? new(volume, target, protector, BitLockerEscrowOutcome.Accepted,
                            "Windows accepted the backup request to " + TargetName(target) + " for the same protector, which is still present. The directory copy is not readable from this computer; confirm it in " + TargetName(target) + " before relying on it.",
                            before, command, after, journalPath)
                        : new(volume, target, protector, BitLockerEscrowOutcome.Uncertain,
                            "Windows accepted the backup request, but the same protector could not be confirmed afterwards.", before, command, after, journalPath);
            }
        }
        catch (OperationCanceledException)
        {
            result = new(volume, target, protector, BitLockerEscrowOutcome.Stopped,
                dispatched ? "The backup request stopped after it may have reached Windows. Repeating a backup only stores the same recovery information again."
                    : "The final check was canceled before any backup request was sent.", before, command, JournalPath: journalPath);
        }
        catch (Exception error) when (Unavailable(error))
        {
            result = new(volume, target, protector, dispatched ? BitLockerEscrowOutcome.Uncertain : BitLockerEscrowOutcome.Blocked,
                dispatched ? "The backup request was interrupted: " + error.Message
                    : "Final escrow eligibility was unavailable: " + error.Message + ". No backup request was sent.", before, command, JournalPath: journalPath);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        Report(progress, result.Detail);
        return result;
    }

    public static string? EligibilityBlocker(string volume, BitLockerEscrowTarget target, Guid protector, BitLockerEscrowState state)
    {
        volume = NormalizeVolume(volume);
        if (!state.Complete || !string.Equals(volume, state.Volume, StringComparison.OrdinalIgnoreCase))
            return "A complete exact selected-volume inventory is required. No backup request was sent.";
        if (string.IsNullOrWhiteSpace(state.Identity) || state.Identity.Length > 512 || state.Identity.Any(char.IsControl))
            return "The exact selected volume identity could not be confirmed. No backup request was sent.";
        if (state.VolumeStatus is not ("FullyEncrypted" or "EncryptionInProgress" or "EncryptionPaused"))
            return "Only a volume that is encrypted or encrypting has recovery information worth backing up.";
        if (state.Protectors.Count is < 1 or > 64 || state.Protectors.Select(p => p.Id).Distinct().Count() != state.Protectors.Count)
            return "A complete unique protector identity set is required. No backup request was sent.";
        if (!HasRecoveryPassword(state, protector))
            return "The selected protector is not a recovery password on this volume. Only numerical recovery passwords can be backed up to a directory.";
        return target switch
        {
            BitLockerEscrowTarget.ActiveDirectory when !state.DomainJoined => "This computer is not joined to an Active Directory domain.",
            BitLockerEscrowTarget.EntraId when !state.EntraJoined => "This computer is not joined to Microsoft Entra ID.",
            _ => null
        };
    }

    private static bool HasRecoveryPassword(BitLockerEscrowState state, Guid protector) =>
        state.Protectors.Count(p => p.Id == protector) == 1 && state.Protectors.Single(p => p.Id == protector).Type == "RecoveryPassword";
    private static bool SameIdentity(BitLockerEscrowState left, BitLockerEscrowState right) => right.Complete &&
        string.Equals(left.Volume, right.Volume, StringComparison.OrdinalIgnoreCase) && string.Equals(left.Identity, right.Identity, StringComparison.Ordinal) &&
        left.Protectors.Select(p => p.Id + "|" + p.Type).Order(StringComparer.Ordinal).SequenceEqual(right.Protectors.Select(p => p.Id + "|" + p.Type).Order(StringComparer.Ordinal));
    private static void ValidateArguments(BitLockerEscrowTarget target, Guid protector)
    {
        if (!Enum.IsDefined(target)) throw new ArgumentException("Unrecognized escrow target.");
        if (protector == Guid.Empty) throw new ArgumentException("Select an exact recovery-password protector.");
    }
    public static string TargetName(BitLockerEscrowTarget target) => target == BitLockerEscrowTarget.ActiveDirectory ? "Active Directory" : "Microsoft Entra ID";
    private static string JoinName(BitLockerEscrowTarget target) => target == BitLockerEscrowTarget.ActiveDirectory ? "domain" : "Microsoft Entra";
    private static bool Complete(ToolResult result) => result.ExitCode == 0 && !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
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

public sealed class WindowsBitLockerEscrowHost : IBitLockerEscrowHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();

    public async Task<BitLockerEscrowState> InspectAsync(string volume, CancellationToken token)
    {
        volume = BitLockerEscrow.NormalizeVolume(volume);
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$b=Get-BitLockerVolume -MountPoint '" + volume + "' -ErrorAction Stop;" +
            "$v=Get-Volume -DriveLetter '" + volume[0] + "' -ErrorAction Stop;" +
            "$c=Get-CimInstance Win32_ComputerSystem -ErrorAction Stop;" +
            "$d=(& (Join-Path $env:SystemRoot 'System32\\dsregcmd.exe') /status | Out-String);" +
            "[pscustomobject]@{volume=[string]$b.MountPoint;identity=[string]$v.UniqueId;volumeStatus=[string]$b.VolumeStatus;" +
            "domainJoined=[bool]$c.PartOfDomain;entraJoined=[bool]($d -match '(?m)^\\s*AzureAdJoined\\s*:\\s*YES\\s*$');" +
            "protectors=@($b.KeyProtector | ForEach-Object {[pscustomobject]@{id=[string]$_.KeyProtectorId;type=[string]$_.KeyProtectorType}})} | ConvertTo-Json -Depth 4 -Compress";
        var result = await BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(result, volume);
    }

    public Task<ToolResult> EscrowAsync(string volume, BitLockerEscrowTarget target, Guid protector, CancellationToken token)
    {
        volume = BitLockerEscrow.NormalizeVolume(volume);
        string cmdlet = target == BitLockerEscrowTarget.ActiveDirectory ? "Backup-BitLockerKeyProtector" : "BackupToAAD-BitLockerKeyProtector";
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            cmdlet + " -MountPoint '" + volume + "' -KeyProtectorId '" + protector.ToString("B") + "' -ErrorAction Stop | Out-Null";
        return BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], false), TimeSpan.FromMinutes(2), token: token);
    }

    public static BitLockerEscrowState Interpret(ToolResult result, string volume)
    {
        volume = BitLockerEscrow.NormalizeVolume(volume);
        BitLockerEscrowState Unknown() => new(volume, "", "", [], false, false, false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            JsonElement root = document.RootElement;
            string[] expected = ["volume", "identity", "volumeStatus", "domainJoined", "entraJoined", "protectors"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Length ||
                !expected.All(name => root.TryGetProperty(name, out _))) return Unknown();
            string? identity = root.GetProperty("identity").GetString();
            string? status = root.GetProperty("volumeStatus").GetString();
            JsonElement items = root.GetProperty("protectors");
            if (!string.Equals(root.GetProperty("volume").GetString(), volume, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(identity) || identity.Length > 512 || identity.Any(char.IsControl) ||
                status is not ("FullyDecrypted" or "FullyEncrypted" or "EncryptionInProgress" or "DecryptionInProgress" or "EncryptionPaused" or "DecryptionPaused") ||
                root.GetProperty("domainJoined").ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                root.GetProperty("entraJoined").ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > 64) return Unknown();
            var protectors = new List<BitLockerProtector>();
            foreach (JsonElement item in items.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 2 ||
                    !Guid.TryParse(item.GetProperty("id").GetString(), out Guid id) || id == Guid.Empty || protectors.Any(p => p.Id == id)) return Unknown();
                string? type = item.GetProperty("type").GetString();
                if (string.IsNullOrWhiteSpace(type) || type.Length > 64 || type.Any(char.IsControl)) return Unknown();
                protectors.Add(new(id, type));
            }
            return new(volume, identity, status, protectors.AsReadOnly(), root.GetProperty("domainJoined").GetBoolean(), root.GetProperty("entraJoined").GetBoolean(), true);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return Unknown(); }
    }
}
