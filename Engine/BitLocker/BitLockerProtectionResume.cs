using System.Text.Json;

namespace KillerBench.Engine;

public sealed record BitLockerProtectionState(string Volume, string Identity, string VolumeStatus,
    string ProtectionStatus, string LockStatus, int EncryptionPercentage,
    IReadOnlyList<Guid> Protectors, bool Complete);

public enum BitLockerProtectionResumeOutcome { Preview, Blocked, Verified, NotApplied, Failed, Uncertain, Stopped }

public sealed record BitLockerProtectionResumeResult(string Volume, BitLockerProtectionResumeOutcome Outcome,
    string Detail, BitLockerProtectionState? Before = null, ToolResult? Command = null,
    BitLockerProtectionState? After = null, string? JournalPath = null)
{
    public int GetExitCode() => Outcome switch
    {
        BitLockerProtectionResumeOutcome.Preview or BitLockerProtectionResumeOutcome.Verified => 0,
        BitLockerProtectionResumeOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IBitLockerProtectionResumeHost
{
    bool IsAdministrator { get; }
    Task<RestartSnapshot> ReadRestartAsync(CancellationToken token);
    Task<BitLockerProtectionState> InspectAsync(string volume, CancellationToken token);
    Task<ToolResult> ResumeAsync(string volume, IProgress<string>? progress, CancellationToken token);
}

public static class BitLockerProtectionResume
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };
    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc, string Volume,
        BitLockerProtectionState Before, ToolResult? Command);

    public static string NormalizeVolume(string volume)
    {
        var options = new BitLockerOptions(volume, BitLockerOperation.ResumeProtection);
        options.Validate();
        return options.Volume.ToUpperInvariant();
    }

    public static Task<BitLockerProtectionResumeResult> PreviewAsync(string volume,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        volume = NormalizeVolume(volume);
        return Task.FromResult(new BitLockerProtectionResumeResult(volume,
            BitLockerProtectionResumeOutcome.Preview,
            "Preview only. Execution will inspect the exact encrypted and unlocked volume, preserve its protector identities, require clear restart evidence and administrator access, save a pending record, resume protection once, then verify protection is on. Encryption, protectors, recovery backup, policy and restart state are not changed by this preview."));
    }

    public static async Task<BitLockerProtectionResumeResult> ReviewAsync(string volume,
        IBitLockerProtectionResumeHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        volume = NormalizeVolume(volume);
        BitLockerProtectionState before;
        try { before = await Required(ct => host.InspectAsync(volume, ct), "BitLocker inspection", token, TimeSpan.FromSeconds(45)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerProtectionResumeOutcome.Blocked, "BitLocker eligibility could not be established: " + error.Message); }
        string? blocker = EligibilityBlocker(volume, before);
        if (blocker is not null) return new(volume, BitLockerProtectionResumeOutcome.Blocked, blocker, before);
        RestartSnapshot restart;
        try { restart = await Required(host.ReadRestartAsync, "Restart inspection", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerProtectionResumeOutcome.Blocked, "Restart readiness could not be established: " + error.Message, before); }
        WorkflowCheck restartCheck = RestartDiagnostics.Assess(restart);
        if (restartCheck.State != CheckState.Clear)
            return new(volume, BitLockerProtectionResumeOutcome.Blocked,
                "Restart readiness must be clear before protection is resumed. " + restartCheck.Detail, before);
        if (!host.IsAdministrator)
            return new(volume, BitLockerProtectionResumeOutcome.Blocked,
                "Administrator access is required before protection resume can be offered.", before);
        return new(volume, BitLockerProtectionResumeOutcome.Preview,
            "Fresh evidence confirms this exact fully encrypted and unlocked volume has suspended protection and can be reviewed for an explicit resume request.", before);
    }

    public static WorkflowCheck ToWorkflowCheck(BitLockerProtectionResumeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        bool available = result.Outcome == BitLockerProtectionResumeOutcome.Preview && result.Before is { Complete: true };
        return new("BitLocker protection resume", available ? CheckState.Repairable :
            result.Before is null ? CheckState.Unknown : CheckState.Attention,
            result.Detail, available ? RepairKind.BitLockerProtectionResume : null);
    }

    public static async Task<BitLockerProtectionResumeResult> RunAsync(string volume, bool execute,
        string journalFolder, IBitLockerProtectionResumeHost host, IProgress<string>? progress = null,
        CancellationToken token = default, TimeSpan? operationTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        volume = NormalizeVolume(volume);
        if (!execute) return await PreviewAsync(volume, token).ConfigureAwait(false);
        TimeSpan limit = operationTimeout ?? TimeSpan.FromMinutes(2);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(operationTimeout));

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "bitlocker-resume.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "bitlocker-resume.pending.json");
        if (File.Exists(pendingPath))
            return new(volume, BitLockerProtectionResumeOutcome.Blocked,
                "A prior BitLocker resume request needs reconciliation before another can start.", JournalPath: pendingPath);

        Report(progress, "Inspecting " + volume + " and its protector identities.");
        BitLockerProtectionState before;
        try { before = await Required(ct => host.InspectAsync(volume, ct), "BitLocker inspection", token, TimeSpan.FromSeconds(45)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerProtectionResumeOutcome.Blocked, "BitLocker eligibility could not be established: " + error.Message); }
        string? blocker = EligibilityBlocker(volume, before);
        if (blocker is not null) return new(volume, BitLockerProtectionResumeOutcome.Blocked, blocker, before);

        Report(progress, "Checking restart readiness.");
        RestartSnapshot restart;
        try { restart = await Required(host.ReadRestartAsync, "Restart inspection", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerProtectionResumeOutcome.Blocked, "Restart readiness could not be established: " + error.Message, before); }
        WorkflowCheck restartCheck = RestartDiagnostics.Assess(restart);
        if (restartCheck.State != CheckState.Clear)
            return new(volume, BitLockerProtectionResumeOutcome.Blocked,
                "Restart readiness must be clear before protection is resumed. " + restartCheck.Detail, before);
        if (!host.IsAdministrator)
            return new(volume, BitLockerProtectionResumeOutcome.Blocked,
                "Administrator access is required. Protection was not resumed.", before);

        Report(progress, "Rechecking " + volume + " and its protector identities immediately before resume.");
        BitLockerProtectionState fresh;
        try { fresh = await Required(ct => host.InspectAsync(volume, ct), "BitLocker preflight inspection", token, TimeSpan.FromSeconds(45)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(volume, BitLockerProtectionResumeOutcome.Blocked, "Fresh BitLocker eligibility could not be established: " + error.Message, before); }
        blocker = EligibilityBlocker(volume, fresh);
        if (blocker is not null)
            return new(volume, BitLockerProtectionResumeOutcome.Blocked,
                "BitLocker eligibility changed before resume. " + blocker, before, After: fresh);
        if (!SameIdentity(before, fresh))
            return new(volume, BitLockerProtectionResumeOutcome.Blocked,
                "The selected volume identity or protector set changed before resume. Protection was not resumed.", before, After: fresh);
        before = fresh;

        var pending = new PendingRecord(1, Environment.MachineName, DateTimeOffset.UtcNow, volume, before, null);
        await JournalPersistence.WriteAsync(pendingPath,
            JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);
        string journalPath = Path.Combine(root, "bitlocker-resume-" + Guid.NewGuid().ToString("N") + ".json");
        ToolResult? command = null;
        bool resumeDispatched = false;
        BitLockerProtectionResumeResult result;
        try
        {
            Report(progress, "Repeating BitLocker protection-resume eligibility immediately before the request.");
            var immediate = await Required(ct => host.InspectAsync(volume, ct), "Final BitLocker inspection",
                token, TimeSpan.FromSeconds(45)).ConfigureAwait(false);
            var immediateRestart = await Required(host.ReadRestartAsync, "Final restart inspection",
                token, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            string? finalBlocker = EligibilityBlocker(volume, immediate);
            bool finalRestartClear = RestartDiagnostics.Assess(immediateRestart).State == CheckState.Clear;
            if (finalBlocker is not null || !SameIdentity(before, immediate) || !finalRestartClear || !host.IsAdministrator)
            {
                string detail = finalBlocker is not null && string.Equals(immediate.ProtectionStatus, "On", StringComparison.Ordinal)
                    ? "BitLocker protection became active after preparation. No resume request was sent."
                    : "Final volume, protector, restart or administrator evidence no longer authorizes protection resume. No request was sent.";
                result = new(volume, BitLockerProtectionResumeOutcome.Blocked, detail,
                    before, After: immediate, JournalPath: journalPath);
                await JournalPersistence.WriteAsync(journalPath,
                    JsonSerializer.Serialize(new
                    {
                        Stage = "FinalEligibility",
                        Initial = new { State = before, Restart = restart },
                        Current = new { State = immediate, Restart = immediateRestart, Administrator = host.IsAdministrator },
                        Result = result
                    }, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }

            Report(progress, "Resuming BitLocker protection on " + volume + ".");
            resumeDispatched = true;
            command = await Required(ct => host.ResumeAsync(volume, progress, ct),
                "BitLocker resume", token, limit).ConfigureAwait(false);
            await JournalPersistence.WriteAsync(pendingPath,
                JsonSerializer.Serialize(pending with { Command = command }, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
            result = await VerifyAsync(volume, before, command, journalPath, host, progress, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new(volume, BitLockerProtectionResumeOutcome.Stopped,
                resumeDispatched
                    ? "The resume request stopped after it may have reached Windows. Protection may have changed; reconcile before trying again."
                    : "The final protection-resume check was canceled before any request was sent. Protection was not changed.",
                before, command, JournalPath: journalPath);
        }
        catch (Exception error) when (Unavailable(error))
        {
            result = new(volume, resumeDispatched ? BitLockerProtectionResumeOutcome.Uncertain : BitLockerProtectionResumeOutcome.Blocked,
                resumeDispatched
                    ? "The resume request was interrupted: " + error.Message + ". Reconcile before trying again."
                    : "Final BitLocker eligibility was unavailable: " + error.Message + ". No resume request was sent.",
                before, command, JournalPath: journalPath);
        }
        await JournalPersistence.WriteAsync(journalPath,
            JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome is BitLockerProtectionResumeOutcome.Verified or BitLockerProtectionResumeOutcome.NotApplied or BitLockerProtectionResumeOutcome.Failed || !resumeDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<BitLockerProtectionResumeResult> ReconcileAsync(string journalFolder,
        IBitLockerProtectionResumeHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "bitlocker-resume.pending.json");
        if (!File.Exists(pendingPath))
            return new("C:", BitLockerProtectionResumeOutcome.Blocked, "No pending BitLocker resume request was found.");
        using var gate = new FileStream(Path.Combine(root, "bitlocker-resume.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        PendingRecord pending;
        try
        {
            pending = await ReadPendingAsync(pendingPath, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error))
        {
            return new("C:", BitLockerProtectionResumeOutcome.Uncertain,
                "The interrupted BitLocker resume record could not be verified for this computer. It was retained and the command was not repeated.",
                JournalPath: pendingPath);
        }
        if (pending.Command is null)
            return new(pending.Volume, BitLockerProtectionResumeOutcome.Uncertain,
                "The pending record does not prove that Windows returned from the request. It was not repeated.",
                pending.Before, pending.Command, JournalPath: pendingPath);
        string journalPath = Path.Combine(root, "bitlocker-resume-recovered-" + Guid.NewGuid().ToString("N") + ".json");
        var result = await VerifyAsync(pending.Volume, pending.Before, pending.Command, journalPath,
            host, progress, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(journalPath,
            JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome is BitLockerProtectionResumeOutcome.Verified or BitLockerProtectionResumeOutcome.NotApplied or BitLockerProtectionResumeOutcome.Failed)
            File.Move(pendingPath, journalPath + ".prepared.json");
        return result;
    }

    private static async Task<PendingRecord> ReadPendingAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 65_536) throw new InvalidDataException("Pending BitLocker resume record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Pending BitLocker resume record contains conflicting fields.");
            var pending = document.RootElement.Deserialize<PendingRecord>(StrictJson) ??
                throw new InvalidDataException("Pending BitLocker resume record is empty.");
            if (pending.Schema != 1 || !pending.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                pending.StartedUtc == default || NormalizeVolume(pending.Volume) != pending.Volume ||
                EligibilityBlocker(pending.Volume, pending.Before) is not null)
                throw new InvalidDataException("Pending BitLocker resume record contains invalid eligibility evidence.");
            return pending;
        }
        catch (JsonException error) { throw new InvalidDataException("Pending BitLocker resume record is invalid.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Pending BitLocker resume record contains an invalid volume.", error); }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    public static string? EligibilityBlocker(string volume, BitLockerProtectionState state)
    {
        volume = NormalizeVolume(volume);
        if (!state.Complete || !string.Equals(volume, state.Volume, StringComparison.OrdinalIgnoreCase))
            return "A complete exact selected-volume inventory is required. Protection was not resumed.";
        if (string.IsNullOrWhiteSpace(state.Identity) || state.Identity.Length > 512 || state.Identity.Any(char.IsControl))
            return "The exact selected volume identity could not be confirmed. Protection was not resumed.";
        if (!string.Equals(state.VolumeStatus, "FullyEncrypted", StringComparison.Ordinal) || state.EncryptionPercentage != 100)
            return "Only a fully encrypted volume at 100 percent is eligible. Conversion settings were not changed.";
        if (!string.Equals(state.ProtectionStatus, "Off", StringComparison.Ordinal))
            return string.Equals(state.ProtectionStatus, "On", StringComparison.Ordinal)
                ? "BitLocker protection is already on. No change is needed."
                : "BitLocker protection state is unrecognized. Protection was not resumed.";
        if (!string.Equals(state.LockStatus, "Unlocked", StringComparison.Ordinal))
            return "The selected volume must be unlocked. Protection was not resumed.";
        if (state.Protectors.Count is < 1 or > 64 || state.Protectors.Any(id => id == Guid.Empty) ||
            state.Protectors.Distinct().Count() != state.Protectors.Count)
            return "A complete unique protector identity set is required. Protection was not resumed.";
        return null;
    }

    private static async Task<BitLockerProtectionResumeResult> VerifyAsync(string volume,
        BitLockerProtectionState before, ToolResult command, string journalPath,
        IBitLockerProtectionResumeHost host, IProgress<string>? progress, CancellationToken token)
    {
        Report(progress, "Verifying protection and protector identities on " + volume + ".");
        BitLockerProtectionState after;
        try { after = await Required(ct => host.InspectAsync(volume, ct), "BitLocker verification", token, TimeSpan.FromSeconds(45)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        {
            return new(volume, BitLockerProtectionResumeOutcome.Uncertain,
                "Windows accepted the request, but verification was unavailable: " + error.Message,
                before, command, JournalPath: journalPath);
        }
        if (!SameIdentity(before, after))
            return new(volume, BitLockerProtectionResumeOutcome.Uncertain,
                "The volume identity or protector set changed during verification. Completion is not established.",
                before, command, after, journalPath);
        bool verified = after.Complete && after.VolumeStatus == "FullyEncrypted" &&
            after.EncryptionPercentage == 100 && after.ProtectionStatus == "On" && after.LockStatus == "Unlocked";
        bool notApplied = after.Complete && after.VolumeStatus == "FullyEncrypted" &&
            after.EncryptionPercentage == 100 && after.ProtectionStatus == "Off" && after.LockStatus == "Unlocked";
        bool commandComplete = Complete(command);
        return new(volume,
            verified ? BitLockerProtectionResumeOutcome.Verified : notApplied && commandComplete ? BitLockerProtectionResumeOutcome.NotApplied : notApplied ? BitLockerProtectionResumeOutcome.Failed : BitLockerProtectionResumeOutcome.Uncertain,
            verified
                ? "BitLocker protection is on for the same fully encrypted volume with the same protector identities. Recovery escrow and a future boot recovery are not verified."
                : notApplied && commandComplete
                    ? "Windows completed the command, but protection remains off on the same volume. The request was not repeated."
                    : notApplied
                    ? "The command failed and fresh exact evidence confirms protection remains off. The request was not repeated."
                    : "Windows completed the command, but the expected protected state was not verified. The request was not repeated.",
            before, command, after, journalPath);
    }

    private static bool SameIdentity(BitLockerProtectionState before, BitLockerProtectionState after) =>
        after.Complete && string.Equals(before.Volume, after.Volume, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(before.Identity, after.Identity, StringComparison.Ordinal) &&
        before.Protectors.Order().SequenceEqual(after.Protectors.Order());

    private static bool Complete(ToolResult result) => result.ExitCode == 0 && !result.TimedOut &&
        !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
        System.Security.SecurityException or OperationCanceledException;
    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan limit)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(limit, token).ConfigureAwait(false) ??
            throw new InvalidDataException(name + " provider returned no evidence.");
    }
    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsBitLockerProtectionResumeHost : IBitLockerProtectionResumeHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();

    public Task<RestartSnapshot> ReadRestartAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(RestartDiagnostics.Read());
    }

    public async Task<BitLockerProtectionState> InspectAsync(string volume, CancellationToken token)
    {
        volume = BitLockerProtectionResume.NormalizeVolume(volume);
        char drive = volume[0];
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$b=Get-BitLockerVolume -MountPoint '" + volume + "' -ErrorAction Stop;" +
            "$v=Get-Volume -DriveLetter '" + drive + "' -ErrorAction Stop;" +
            "[pscustomobject]@{volume=[string]$b.MountPoint;identity=[string]$v.UniqueId;volumeStatus=[string]$b.VolumeStatus;" +
            "protectionStatus=[string]$b.ProtectionStatus;lockStatus=[string]$b.LockStatus;encryptionPercentage=[int]$b.EncryptionPercentage;" +
            "protectors=@($b.KeyProtector | ForEach-Object {[string]$_.KeyProtectorId})} | ConvertTo-Json -Depth 3 -Compress";
        var result = await BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(result, volume);
    }

    public Task<ToolResult> ResumeAsync(string volume, IProgress<string>? progress, CancellationToken token)
    {
        volume = BitLockerProtectionResume.NormalizeVolume(volume);
        string command = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "Resume-BitLocker -MountPoint '" + volume + "' -Confirm:$false -ErrorAction Stop | Out-Null";
        return BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], false),
            TimeSpan.FromMinutes(2), progress, token);
    }

    public static BitLockerProtectionState Interpret(ToolResult result, string volume)
    {
        volume = BitLockerProtectionResume.NormalizeVolume(volume);
        BitLockerProtectionState Unknown() => new(volume, "", "", "", "", -1, [], false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            JsonElement root = document.RootElement;
            string[] expected = ["volume", "identity", "volumeStatus", "protectionStatus", "lockStatus", "encryptionPercentage", "protectors"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Length ||
                !expected.All(name => root.TryGetProperty(name, out _))) return Unknown();
            string? reportedVolume = root.GetProperty("volume").GetString();
            string? identity = root.GetProperty("identity").GetString();
            string? volumeStatus = root.GetProperty("volumeStatus").GetString();
            string? protectionStatus = root.GetProperty("protectionStatus").GetString();
            string? lockStatus = root.GetProperty("lockStatus").GetString();
            int percentage = root.GetProperty("encryptionPercentage").GetInt32();
            JsonElement items = root.GetProperty("protectors");
            if (!string.Equals(reportedVolume, volume, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(identity) || identity.Length > 512 || identity.Any(char.IsControl) ||
                volumeStatus is not ("FullyDecrypted" or "FullyEncrypted" or "EncryptionInProgress" or "DecryptionInProgress" or "EncryptionPaused" or "DecryptionPaused") ||
                protectionStatus is not ("On" or "Off") || lockStatus is not ("Locked" or "Unlocked") ||
                percentage is < 0 or > 100 || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > 64) return Unknown();
            var protectors = new List<Guid>();
            foreach (JsonElement item in items.EnumerateArray())
                if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out Guid id) || id == Guid.Empty || protectors.Contains(id)) return Unknown();
                else protectors.Add(id);
            return new(volume, identity, volumeStatus, protectionStatus, lockStatus, percentage, protectors.AsReadOnly(), true);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return Unknown(); }
    }
}
