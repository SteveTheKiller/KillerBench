using System.Text.Json;

namespace KillerBench.Engine.Time;

public enum TimeConfigurationRecoveryOutcome
{
    NoPendingRecord,
    Verified,
    StillUnverified,
    InvalidRecord
}

public sealed record TimeConfigurationRecoveryResult(TimeConfigurationRecoveryOutcome Outcome, string Detail,
    string PendingPath, string? ArchivedPath = null, TimeOptions? Options = null)
{
    public bool Resolved => Outcome is TimeConfigurationRecoveryOutcome.NoPendingRecord or TimeConfigurationRecoveryOutcome.Verified;
    public int GetExitCode() => Resolved ? 0 : 3;
}

public static class TimeConfigurationRecovery
{
    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc,
        TimeOptions Options, TimeEnvironment Before, string Journal);

    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static bool HasPendingRecord(string folder) =>
        !string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(Path.GetFullPath(folder), "pending-time.json"));

    public static async Task<TimeConfigurationRecoveryResult> ReconcileAsync(string folder, Func<TimeOptions, CancellationToken, Task<bool>>? verify = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        string root = Path.GetFullPath(folder);
        string pending = Path.Combine(root, "pending-time.json");
        if (!Directory.Exists(root) || !File.Exists(pending)) return new(TimeConfigurationRecoveryOutcome.NoPendingRecord,
            "No pending time-source change was found. No settings changed.", pending);
        using var gate = new FileStream(Path.Combine(root, "time.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (!File.Exists(pending)) return new(TimeConfigurationRecoveryOutcome.NoPendingRecord,
            "No pending time-source change was found. No settings changed.", pending);
        using var stream = RecoveryRecordFiles.OpenLocked(pending);
        TimeOptions options;
        try
        {
            if (stream.Length is <= 0 or > 65536) return Invalid("The pending time-source record has an invalid size.");
            using var json = JsonDocument.Parse(stream);
            if (!Unique(json.RootElement)) return Invalid("The pending time-source record contains duplicate fields.");
            var record = json.RootElement.Deserialize<PendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(record.Journal)) return Invalid("The pending time-source record is incomplete or belongs to another machine.");
            string journal = Path.GetFullPath(record.Journal);
            if (!string.Equals(Path.GetDirectoryName(journal), root, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(journal).StartsWith("time-", StringComparison.OrdinalIgnoreCase) ||
                !journal.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)) return Invalid("The pending time-source journal path is invalid.");
            if (record.Options.Source is not (TimeSourceMode.CustomPeers or TimeSourceMode.DomainHierarchy))
                return Invalid("The pending time-source selection is unsupported.");
            var validated = TimePlanning.Create(record.Options, record.Before);
            if (!validated.ReviewPassed) return Invalid("The recorded time-source prerequisites are invalid or incomplete.");
            options = new(validated.Source, validated.Peers);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        { return Invalid("The pending time-source record could not be validated: " + error.Message); }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromMinutes(3));
        if (!await (verify ?? ((selected, ct) => TimeConfiguration.VerifyAsync(selected, token: ct)))(options, deadline.Token)
            .WaitAsync(deadline.Token).ConfigureAwait(false))
            return new(TimeConfigurationRecoveryOutcome.StillUnverified,
                "Fresh evidence did not verify both the recorded configuration and reported synchronization. The pending record remains and no change was retried.",
                pending, Options: options);
        deadline.Token.ThrowIfCancellationRequested();
        token.ThrowIfCancellationRequested();
        string archived = Path.Combine(root, "resolved-time-" + Guid.NewGuid().ToString("N") + ".json");
        RecoveryRecordFiles.Archive(stream, archived);
        return new(TimeConfigurationRecoveryOutcome.Verified,
            "Fresh evidence verified the recorded configuration and reported synchronization. The pending record was archived and no change was retried.",
            pending, archived, options);

        TimeConfigurationRecoveryResult Invalid(string detail) => new(TimeConfigurationRecoveryOutcome.InvalidRecord,
            detail + " The record remains for review and no change was retried.", pending);
    }

    public static async Task<bool> TryResolveAsync(string folder, Func<TimeOptions, CancellationToken, Task<bool>>? verify = null,
        CancellationToken token = default)
    {
        var result = await ReconcileAsync(folder, verify, token: token).ConfigureAwait(false);
        return result.Resolved;
    }

    private static bool Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(Unique);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && Unique(property.Value));
    }
}
