using System.Text.Json;

namespace KillerBench.Engine;

public sealed record DnsCachePendingRecord(int Schema, DateTimeOffset StartedUtc, DnsLookupResult Before)
{
    public string Machine { get; init; } = "";
}

public enum DnsCacheReconciliationOutcome { NoPendingRecord, Resolved, StillFailing, Inconclusive }

public sealed record DnsCacheReconciliationResult(DnsCacheReconciliationOutcome Outcome, string Detail,
    string PendingPath, DnsCachePendingRecord? Pending = null, DnsLookupResult? Current = null,
    string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is DnsCacheReconciliationOutcome.NoPendingRecord or
        DnsCacheReconciliationOutcome.Resolved ? 0 : 3;
}

public static class DnsCacheReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<DnsCacheReconciliationResult> RunAsync(string directory,
        Func<string, CancellationToken, Task<DnsLookupResult>> resolve, CancellationToken token = default,
        IProgress<string>? progress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(resolve);
        string root = Path.GetFullPath(directory);
        string pendingPath = Path.Combine(root, "dns-cache.pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath))
            return new(DnsCacheReconciliationOutcome.NoPendingRecord,
                "No interrupted DNS cache recovery record is present.", pendingPath);

        Directory.CreateDirectory(root);
        string lockPath = Path.Combine(root, "dns-cache.reconcile.lock");
        await using var operationLock = new FileStream(lockPath, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 1, FileOptions.DeleteOnClose);
        DnsCachePendingRecord pending;
        try
        {
            pending = await ReadAsync(pendingPath, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or InvalidOperationException or System.Security.SecurityException)
        {
            return new(DnsCacheReconciliationOutcome.Inconclusive,
                "The interrupted DNS cache record could not be verified for this computer. It was retained and no cache flush was requested.",
                pendingPath);
        }
        Report(progress, "Rechecking " + pending.Before.Host + " without repeating the cache flush.");
        DnsLookupResult current;
        try
        {
            current = await resolve(pending.Before.Host, token).WaitAsync(token).ConfigureAwait(false)
                ?? throw new InvalidDataException("The resolver returned no hostname evidence.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or InvalidOperationException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new(DnsCacheReconciliationOutcome.Inconclusive,
                "The hostname could not be rechecked: " + error.Message + ". The pending record was retained and no cache flush was requested.",
                pendingPath, pending);
        }
        if (!string.Equals(current.Host, pending.Before.Host, StringComparison.OrdinalIgnoreCase))
            return new(DnsCacheReconciliationOutcome.Inconclusive,
                "The resolver returned evidence for a different hostname. The pending record was retained and no cache flush was requested.",
                pendingPath, pending, current);

        bool resolved = current.State == DnsLookupState.Resolved && current.Addresses is { Length: > 0 };
        bool completeFailure = DnsCacheRecovery.IsEligible(current);
        if (!resolved && !completeFailure)
            return new(DnsCacheReconciliationOutcome.Inconclusive,
                "The current lookup is incomplete or reports a different failure class. The pending record was retained and no cache flush was requested.",
                pendingPath, pending, current);

        var outcome = resolved ? DnsCacheReconciliationOutcome.Resolved : DnsCacheReconciliationOutcome.StillFailing;
        string detail = resolved
            ? "The exact hostname now resolves. The interrupted operation was reconciled without repeating the cache flush. Service reachability remains untested."
            : "The exact hostname still reports HostNotFound or NoData. The interrupted operation was reconciled without repeating the cache flush; review authoritative records and resolver health.";
        string archivePath = Path.Combine(root, "dns-cache-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".json");
        var result = new DnsCacheReconciliationResult(outcome, detail, pendingPath, pending, current, archivePath);
        await JournalPersistence.WriteAsync(archivePath, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
        File.Move(pendingPath, archivePath + ".prepared.json");
        Report(progress, detail);
        return result;
    }

    public static async Task<DnsCachePendingRecord> ReadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        token.ThrowIfCancellationRequested();
        try
        {
            await using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read,
                FileShare.Read, 4096, FileOptions.Asynchronous);
            if (stream.Length is <= 0 or > 65_536) throw new InvalidDataException("DNS cache recovery record size is invalid.");
            var bytes = new byte[checked((int)stream.Length)];
            await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
            ReadOnlyMemory<byte> json = bytes;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) json = json[3..];
            using var document = JsonDocument.Parse(json);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("DNS cache recovery record contains conflicting fields.");
            var record = document.RootElement.Deserialize<DnsCachePendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !record.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || !DnsCacheRecovery.IsEligible(record.Before))
                throw new InvalidDataException("DNS cache recovery record is invalid.");
            return record;
        }
        catch (JsonException error) { throw new InvalidDataException("DNS cache recovery record is not valid JSON.", error); }
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
