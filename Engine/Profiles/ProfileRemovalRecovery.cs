using System.Text.Json;

namespace KillerBench.Engine.Profiles;

public static class ProfileRemovalRecovery
{
    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc,
        ProfileSnapshot Target, ProfileBackupReceipt Backup, string Journal);

    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true
    };

    public static Task<bool> TryResolveAsync(string folder, CancellationToken token = default) =>
        ResolveAsync(folder, NativeReadyAsync, ProfileAbsence.CheckAsync, TimeSpan.FromSeconds(90), token: token);

    private static Task<bool> NativeReadyAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(false);
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        return Task.FromResult(new System.Security.Principal.WindowsPrincipal(identity)
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator));
    }

    public static Task<bool> TryResolveAsync(string folder, IProfileRemovalHost host, TimeSpan? verificationLimit = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        var limit = verificationLimit ?? TimeSpan.FromSeconds(90);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromSeconds(90)) throw new ArgumentOutOfRangeException(nameof(verificationLimit));
        return ResolveAsync(folder, host.ReadyAsync, host.VerifyAbsentAsync, limit, token: token);
    }

    private static async Task<bool> ResolveAsync(string folder, Func<CancellationToken, Task<bool>> ready,
        Func<ProfileSnapshot, CancellationToken, Task<bool>> verifyAbsent, TimeSpan verificationLimit, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!Directory.Exists(folder)) return true;
        using var gate = new FileStream(Path.Combine(folder, "removal.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(folder, "pending-removal.json");
        if (!File.Exists(pending)) return true;
        ProfileSnapshot target;
        using var stream = RecoveryRecordFiles.OpenLocked(pending);
        try
        {
            if (stream.Length is <= 0 or > 65536) return false;
            using var json = JsonDocument.Parse(stream);
            if (!UniqueFields(json.RootElement)) return false;
            var record = json.RootElement.Deserialize<PendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(record.Journal)) return false;
            string root = Path.GetFullPath(folder);
            string journal = Path.GetFullPath(record.Journal);
            if (!string.Equals(Path.GetDirectoryName(journal), root, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(journal).StartsWith("profile-removal-", StringComparison.OrdinalIgnoreCase) ||
                !journal.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)) return false;
            target = record.Target;
            if (!ProfilePlanning.IsSid(target.Sid) || !ProfilePlanning.IsLocalProfilePath(target.Path) ||
                target.Loaded != false || target.Special != false || target.CurrentAccount || target.LastUsedUtc is null ||
                !ProfileBackupEvidence.Matches(target, record.Backup, record.StartedUtc)) return false;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        { return false; }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(verificationLimit);
        try
        {
            var initialReadiness = ready(deadline.Token);
            if (initialReadiness is null || !await initialReadiness.WaitAsync(deadline.Token).ConfigureAwait(false)) return false;
            deadline.Token.ThrowIfCancellationRequested();
            var absence = verifyAbsent(target, deadline.Token);
            if (absence is null || !await absence.WaitAsync(deadline.Token).ConfigureAwait(false)) return false;
            deadline.Token.ThrowIfCancellationRequested();
            var finalReadiness = ready(deadline.Token);
            if (finalReadiness is null || !await finalReadiness.WaitAsync(deadline.Token).ConfigureAwait(false)) return false;
            deadline.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !token.IsCancellationRequested)
        { return false; }
        token.ThrowIfCancellationRequested();
        RecoveryRecordFiles.Archive(stream, Path.Combine(folder, "resolved-absent-" + Guid.NewGuid().ToString("N") + ".json"));
        return true;
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
}
