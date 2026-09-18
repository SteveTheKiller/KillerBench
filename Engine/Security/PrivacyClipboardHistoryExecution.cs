using System.Text.Json;
using Microsoft.Win32;

namespace KillerBench.Engine;

public enum PrivacyChangeOutcome { Preview, Blocked, Verified, Uncertain, Stopped }

public sealed record PrivacyClipboardHistoryEvidence(bool Complete, bool UserValuePresent, int? UserValue,
    bool PolicyValuePresent, int? PolicyValue, string Detail);

public sealed record PrivacyClipboardHistoryResult(PrivacyChangeOutcome Outcome, bool RequestedEnabled,
    string Detail, string? JournalPath = null, PrivacyClipboardHistoryEvidence? Before = null,
    ToolResult? Command = null, PrivacyClipboardHistoryEvidence? After = null)
{
    public string Machine { get; init; } = Environment.MachineName;
    public int GetExitCode() => Outcome switch
    {
        PrivacyChangeOutcome.Preview or PrivacyChangeOutcome.Verified => 0,
        PrivacyChangeOutcome.Stopped => 130,
        _ => 3
    };
}

public enum PrivacyChangeReconciliationOutcome { NoPendingRecord, Verified, CurrentStateDiffers, Inconclusive }

public sealed record PrivacyClipboardHistoryReconciliationResult(PrivacyChangeReconciliationOutcome Outcome,
    string Detail, string PendingPath, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is PrivacyChangeReconciliationOutcome.NoPendingRecord or
        PrivacyChangeReconciliationOutcome.Verified ? 0 : 3;
}

public interface IPrivacyClipboardHistoryHost
{
    Task<PrivacyClipboardHistoryEvidence> ReadAsync(CancellationToken token);
    Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token);
}

public static class PrivacyClipboardHistoryExecution
{
    public const string Effects = "Changes Clipboard history for the account running KillerBench. Turning it off does not clear existing clipboard history. Clipboard sync is a separate setting. Machine policy is preserved.";
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<PrivacyClipboardHistoryResult> RunAsync(bool requestedEnabled, bool execute,
        string journalFolder, IPrivacyClipboardHistoryHost host, IProgress<string>? progress = null,
        CancellationToken token = default, TimeSpan? commandTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = ValidateTimeout(commandTimeout);
        token.ThrowIfCancellationRequested();
        if (!execute) return new(PrivacyChangeOutcome.Preview, requestedEnabled,
            Effects + " Execution repeats the current-value and policy checks before changing the setting.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "privacy-clipboard-history.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "privacy-clipboard-history.pending.json");
        if (File.Exists(pendingPath)) return new(PrivacyChangeOutcome.Blocked, requestedEnabled,
            "A prior Clipboard history change needs reconciliation before another change can start.", pendingPath);

        Report(progress, "Checking Clipboard history and policy.");
        var before = await Required(host.ReadAsync, "Clipboard history inspection", token, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        string? blocker = Blocker(before);
        if (blocker is not null) return new(PrivacyChangeOutcome.Blocked, requestedEnabled, blocker, Before: before);
        if (before.UserValuePresent && before.UserValue == (requestedEnabled ? 1 : 0))
            return new(PrivacyChangeOutcome.Blocked, requestedEnabled,
                "Clipboard history already has the requested current-user value. No change was made.", Before: before);

        string journalPath = Path.Combine(root, "privacy-clipboard-history-" + Guid.NewGuid().ToString("N") + ".json");
        var pending = new PrivacyClipboardHistoryPending(2, Environment.MachineName, DateTimeOffset.UtcNow,
            true, requestedEnabled ? 1 : 0, before, journalPath);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);

        ToolResult? command = null;
        bool writeDispatched = false;
        PrivacyClipboardHistoryResult result;
        try
        {
            Report(progress, "Confirming final Clipboard history eligibility.");
            var current = await Required(host.ReadAsync, "Clipboard history final inspection", token,
                TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            int target = requestedEnabled ? 1 : 0;
            if (Blocker(current) is not null || !Equivalent(current, before) ||
                current.UserValuePresent && current.UserValue == target)
            {
                string detail = Blocker(current) is not null
                    ? "Clipboard history value or policy evidence changed after preparation. No write was made."
                    : current.UserValuePresent && current.UserValue == target
                        ? "Clipboard history reached the requested value after preparation. No write was required."
                        : "Clipboard history changed after preparation. The later value was preserved.";
                result = new(PrivacyChangeOutcome.Blocked, requestedEnabled, detail, journalPath, before, After: current);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, requestedEnabled ? "Enabling Clipboard history." : "Disabling Clipboard history.");
            writeDispatched = true;
            command = await Required(ct => host.WriteUserValueAsync(requestedEnabled, ct), "Clipboard history change", token, limit).ConfigureAwait(false);
            if (!Successful(command))
                result = Uncertain(requestedEnabled, journalPath, before, command,
                    "The setting change did not provide complete success evidence. Clipboard history may have changed; reconcile before retrying.");
            else
            {
                Report(progress, "Verifying Clipboard history and policy.");
                var after = await Required(host.ReadAsync, "Clipboard history verification", token, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                bool verified = Blocker(after) is null && after.UserValuePresent && after.UserValue == (requestedEnabled ? 1 : 0);
                result = new(verified ? PrivacyChangeOutcome.Verified : PrivacyChangeOutcome.Uncertain, requestedEnabled,
                    verified ? "Fresh evidence confirms the requested Clipboard history value and no machine policy was changed."
                        : "The setting command completed, but fresh evidence did not confirm the requested value with policy still absent.",
                    journalPath, before, command, after);
            }
        }
        catch (OperationCanceledException)
        {
            result = new(PrivacyChangeOutcome.Stopped, requestedEnabled,
                writeDispatched
                    ? "The operation stopped after the registry write may have been dispatched. Reconcile before retrying."
                    : "The final Clipboard history check was canceled before any registry write was dispatched.",
                journalPath, before, command);
        }
        catch (Exception error) when (Expected(error))
        {
            result = writeDispatched
                ? Uncertain(requestedEnabled, journalPath, before, command,
                    "The Clipboard history change was interrupted: " + error.Message + ". Reconcile before retrying.")
                : new(PrivacyChangeOutcome.Blocked, requestedEnabled,
                    "Final Clipboard history evidence was unavailable: " + error.Message + ". No registry write was dispatched.",
                    journalPath, before, command);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == PrivacyChangeOutcome.Verified || !writeDispatched) File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<PrivacyClipboardHistoryResult> UndoAsync(string completedJournalPath,
        string journalFolder, IPrivacyClipboardHistoryHost host, IProgress<string>? progress = null,
        CancellationToken token = default, TimeSpan? commandTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(completedJournalPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = ValidateTimeout(commandTimeout);
        token.ThrowIfCancellationRequested();
        var prior = await ReadCompletedAsync(completedJournalPath, token).ConfigureAwait(false);
        if (prior.Outcome != PrivacyChangeOutcome.Verified || !CurrentMachine(prior.Machine) || prior.Before is null ||
            prior.After is null || Blocker(prior.Before) is not null || Blocker(prior.After) is not null ||
            !prior.After.UserValuePresent || prior.After.UserValue != (prior.RequestedEnabled ? 1 : 0))
            return new(PrivacyChangeOutcome.Blocked, prior.RequestedEnabled,
                "Only a complete verified Clipboard history change can be undone.", completedJournalPath);

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "privacy-clipboard-history.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "privacy-clipboard-history.pending.json");
        if (File.Exists(pendingPath)) return new(PrivacyChangeOutcome.Blocked, prior.RequestedEnabled,
            "A prior Clipboard history change needs reconciliation before undo can start.", pendingPath);

        Report(progress, "Checking Clipboard history before undo.");
        var current = await Required(host.ReadAsync, "Clipboard history undo inspection", token, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        string? blocker = Blocker(current);
        if (blocker is not null) return new(PrivacyChangeOutcome.Blocked, prior.RequestedEnabled, blocker, Before: current);
        if (!current.UserValuePresent || current.UserValue != (prior.RequestedEnabled ? 1 : 0))
            return new(PrivacyChangeOutcome.Blocked, prior.RequestedEnabled,
                "Clipboard history no longer matches the value written by this journal. Undo will not overwrite a later change.", Before: current);

        string journalPath = Path.Combine(root, "privacy-clipboard-history-undo-" + Guid.NewGuid().ToString("N") + ".json");
        var pending = new PrivacyClipboardHistoryPending(2, Environment.MachineName, DateTimeOffset.UtcNow,
            prior.Before.UserValuePresent, prior.Before.UserValue, current, journalPath);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);
        ToolResult? command = null;
        bool writeDispatched = false;
        PrivacyClipboardHistoryResult result;
        try
        {
            Report(progress, "Confirming final Clipboard history undo eligibility.");
            var finalCurrent = await Required(host.ReadAsync, "Clipboard history final undo inspection", token,
                TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            if (Blocker(finalCurrent) is not null || !Equivalent(finalCurrent, current))
            {
                result = new(PrivacyChangeOutcome.Blocked, prior.RequestedEnabled,
                    "Clipboard history or policy changed after undo preparation. The later state was preserved.",
                    journalPath, current, After: finalCurrent);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Restoring the previous Clipboard history value.");
            bool? restore = prior.Before.UserValuePresent ? prior.Before.UserValue == 1 : null;
            writeDispatched = true;
            command = await Required(ct => host.WriteUserValueAsync(restore, ct), "Clipboard history undo", token, limit).ConfigureAwait(false);
            if (!Successful(command))
                result = Uncertain(prior.RequestedEnabled, journalPath, current, command,
                    "Undo did not provide complete success evidence. The current value is uncertain; reconcile before retrying.");
            else
            {
                var after = await Required(host.ReadAsync, "Clipboard history undo verification", token, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                bool restored = Blocker(after) is null && after.UserValuePresent == prior.Before.UserValuePresent &&
                    (!after.UserValuePresent || after.UserValue == prior.Before.UserValue);
                result = new(restored ? PrivacyChangeOutcome.Verified : PrivacyChangeOutcome.Uncertain, prior.RequestedEnabled,
                    restored ? "Fresh evidence confirms the prior Clipboard history value was restored."
                        : "Undo completed, but fresh evidence did not confirm the exact prior value.",
                    journalPath, current, command, after);
            }
        }
        catch (OperationCanceledException)
        {
            result = new(PrivacyChangeOutcome.Stopped, prior.RequestedEnabled,
                writeDispatched ? "Undo stopped after the registry write may have been dispatched. Reconcile before retrying."
                    : "The final Clipboard history undo check was canceled before any registry write was dispatched.",
                journalPath, current, command);
        }
        catch (Exception error) when (Expected(error))
        {
            result = writeDispatched
                ? Uncertain(prior.RequestedEnabled, journalPath, current, command,
                    "Clipboard history undo was interrupted: " + error.Message + ". Reconcile before retrying.")
                : new(PrivacyChangeOutcome.Blocked, prior.RequestedEnabled,
                    "Final Clipboard history undo evidence was unavailable: " + error.Message + ". No registry write was dispatched.",
                    journalPath, current, command);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == PrivacyChangeOutcome.Verified || !writeDispatched) File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<PrivacyClipboardHistoryReconciliationResult> ReconcileAsync(string journalFolder,
        IPrivacyClipboardHistoryHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "privacy-clipboard-history.pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath)) return new(PrivacyChangeReconciliationOutcome.NoPendingRecord,
            "No pending Clipboard history change is present.", pendingPath);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "privacy-clipboard-history.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        PrivacyClipboardHistoryPending pending;
        try
        {
            pending = await ReadPendingAsync(pendingPath, token).ConfigureAwait(false);
            if (pending.Schema != 2 || !CurrentMachine(pending.Machine) || pending.StartedUtc == default ||
                string.IsNullOrWhiteSpace(pending.JournalPath) || Blocker(pending.Before) is not null ||
                pending.TargetValuePresent && pending.TargetValue is not (0 or 1) ||
                !pending.TargetValuePresent && pending.TargetValue is not null)
                throw new InvalidDataException("Pending Clipboard history record is invalid.");
            Report(progress, "Rechecking Clipboard history without repeating the change.");
            var current = await Required(host.ReadAsync, "Clipboard history reconciliation", token, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            if (Blocker(current) is not null)
                return new(PrivacyChangeReconciliationOutcome.Inconclusive,
                    "Current Clipboard history or policy evidence is incomplete or managed. The pending record was retained.", pendingPath);
            PrivacyChangeReconciliationOutcome outcome;
            string detail;
            if (current.UserValuePresent == pending.TargetValuePresent &&
                (!current.UserValuePresent || current.UserValue == pending.TargetValue))
            {
                outcome = PrivacyChangeReconciliationOutcome.Verified;
                detail = "Fresh evidence confirms the requested Clipboard history value. The prior attempt was reconciled without repeating it.";
            }
            else if (Equivalent(current, pending.Before))
            {
                outcome = PrivacyChangeReconciliationOutcome.CurrentStateDiffers;
                detail = "Fresh evidence confirms the original Clipboard history value remains. The prior attempt was not applied and was archived without repeating it.";
            }
            else return new(PrivacyChangeReconciliationOutcome.Inconclusive,
                "Current Clipboard history differs from both the requested and original values. The pending record was retained.", pendingPath);

            string archivePath = Path.Combine(root, "privacy-clipboard-history-reconciliation-" +
                DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json");
            var result = new PrivacyClipboardHistoryReconciliationResult(outcome, detail, pendingPath, archivePath);
            await JournalPersistence.WriteAsync(archivePath, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Move(pendingPath, archivePath + ".prepared.json");
            Report(progress, detail);
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Expected(error))
        {
            return new(PrivacyChangeReconciliationOutcome.Inconclusive,
                "Current state could not be established: " + error.Message + " The pending record was retained.", pendingPath);
        }
    }

    private static string? Blocker(PrivacyClipboardHistoryEvidence evidence)
    {
        if (!evidence.Complete || evidence.UserValuePresent && evidence.UserValue is not (0 or 1) ||
            evidence.PolicyValuePresent && evidence.PolicyValue is not (0 or 1))
            return "Complete supported Clipboard history evidence is required before a change.";
        return evidence.PolicyValuePresent
            ? "Machine policy controls Clipboard history. KillerBench will not override it."
            : null;
    }

    private static async Task<PrivacyClipboardHistoryResult> ReadCompletedAsync(string path, CancellationToken token)
        => await ReadAsync<PrivacyClipboardHistoryResult>(path, token).ConfigureAwait(false);

    private static async Task<PrivacyClipboardHistoryPending> ReadPendingAsync(string path, CancellationToken token)
    {
        string full = Path.GetFullPath(path);
        await using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 262_144) throw new InvalidDataException("Clipboard history journal size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Clipboard history journal contains conflicting fields.");
            int schema = document.RootElement.GetProperty("schema").GetInt32();
            if (schema == 2)
                return document.RootElement.Deserialize<PrivacyClipboardHistoryPending>(StrictJson)
                    ?? throw new InvalidDataException("Clipboard history pending journal is empty.");
            if (schema != 1) throw new InvalidDataException("Clipboard history pending journal schema is unsupported.");
            if (document.RootElement.TryGetProperty("requestedEnabled", out _))
            {
                var legacy = document.RootElement.Deserialize<LegacyPrivacyClipboardHistoryPending>(StrictJson)
                    ?? throw new InvalidDataException("Clipboard history pending journal is empty.");
                return new(2, legacy.Machine, legacy.StartedUtc, true, legacy.RequestedEnabled ? 1 : 0,
                    legacy.Before, legacy.JournalPath);
            }
            var undo = document.RootElement.Deserialize<LegacyPrivacyClipboardHistoryUndoPending>(StrictJson)
                ?? throw new InvalidDataException("Clipboard history undo pending journal is empty.");
            return new(2, undo.Machine, undo.StartedUtc, undo.Original.UserValuePresent,
                undo.Original.UserValue, undo.BeforeUndo, undo.JournalPath);
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException)
        { throw new InvalidDataException("Clipboard history pending journal is not valid.", error); }
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken token)
    {
        string full = Path.GetFullPath(path);
        await using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 262_144) throw new InvalidDataException("Clipboard history journal size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Clipboard history journal contains conflicting fields.");
            return document.RootElement.Deserialize<T>(StrictJson)
                ?? throw new InvalidDataException("Clipboard history journal is empty.");
        }
        catch (JsonException error) { throw new InvalidDataException("Clipboard history journal is not valid JSON.", error); }
    }

    private static bool Equivalent(PrivacyClipboardHistoryEvidence left, PrivacyClipboardHistoryEvidence right) =>
        left.UserValuePresent == right.UserValuePresent && (!left.UserValuePresent || left.UserValue == right.UserValue) &&
        left.PolicyValuePresent == right.PolicyValuePresent && (!left.PolicyValuePresent || left.PolicyValue == right.PolicyValue);
    private static bool CurrentMachine(string machine) =>
        string.Equals(machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase);

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static PrivacyClipboardHistoryResult Uncertain(bool requested, string path,
        PrivacyClipboardHistoryEvidence before, ToolResult? command, string detail) =>
        new(PrivacyChangeOutcome.Uncertain, requested, detail, path, before, command);
    private static bool Successful(ToolResult result) => result.ExitCode == 0 && !result.TimedOut &&
        !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
    private static TimeSpan ValidateTimeout(TimeSpan? value)
    {
        TimeSpan limit = value ?? TimeSpan.FromSeconds(15);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(1)) throw new ArgumentOutOfRangeException(nameof(value));
        return limit;
    }
    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan limit)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(limit, token).ConfigureAwait(false)
            ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }
    private static bool Expected(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException;
    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }

    private sealed record PrivacyClipboardHistoryPending(int Schema, string Machine, DateTimeOffset StartedUtc,
        bool TargetValuePresent, int? TargetValue, PrivacyClipboardHistoryEvidence Before, string JournalPath);
    private sealed record LegacyPrivacyClipboardHistoryPending(int Schema, string Machine, DateTimeOffset StartedUtc,
        bool RequestedEnabled, PrivacyClipboardHistoryEvidence Before, string JournalPath);
    private sealed record LegacyPrivacyClipboardHistoryUndoPending(int Schema, string Machine, DateTimeOffset StartedUtc,
        string SourceJournalPath, PrivacyClipboardHistoryEvidence Original, PrivacyClipboardHistoryEvidence BeforeUndo,
        string JournalPath);
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class WindowsPrivacyClipboardHistoryHost : IPrivacyClipboardHistoryHost
{
    private const string UserPath = @"Software\Microsoft\Clipboard";
    private const string UserName = "EnableClipboardHistory";
    private const string PolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\System";
    private const string PolicyName = "AllowClipboardHistory";

    public Task<PrivacyClipboardHistoryEvidence> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(new PrivacyClipboardHistoryEvidence(false, false, null, false, null, "Windows is required."));
        try
        {
            var user = Read(RegistryHive.CurrentUser, UserPath, UserName);
            var policy = Read(RegistryHive.LocalMachine, PolicyPath, PolicyName);
            bool complete = user.Supported && policy.Supported;
            return Task.FromResult(new PrivacyClipboardHistoryEvidence(complete, user.Present, user.Value,
                policy.Present, policy.Value, complete ? "Exact current-user and machine-policy values were read." : "A value has an unsupported type or range."));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Task.FromResult(new PrivacyClipboardHistoryEvidence(false, false, null, false, null,
                "Clipboard history evidence is unavailable: " + error.Message));
        }
    }

    public Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            if (enabled.HasValue)
            {
                using var key = root.CreateSubKey(UserPath, writable: true);
                key.SetValue(UserName, enabled.Value ? 1 : 0, RegistryValueKind.DWord);
                key.Flush();
            }
            else
            {
                using var key = root.OpenSubKey(UserPath, writable: true);
                key?.DeleteValue(UserName, throwOnMissingValue: false);
                key?.Flush();
            }
            return Task.FromResult(new ToolResult(0, "Current-user Clipboard history value written.", "", false));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return Task.FromResult(new ToolResult(-1, "", "Clipboard history write failed: " + error.Message, false)); }
    }

    private static (bool Present, int? Value, bool Supported) Read(RegistryHive hive, string path, string name)
    {
        using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = root.OpenSubKey(path, writable: false);
        object? raw = key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (raw is null) return (false, null, true);
        return raw is int value && value is 0 or 1 && key!.GetValueKind(name) == RegistryValueKind.DWord
            ? (true, value, true) : (true, null, false);
    }
}
