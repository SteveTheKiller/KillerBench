using System.Text.Json;

namespace KillerBench.Engine.Updates;

public interface IUpdateReconciliationHost
{
    Task<bool> IsInstalledAndReadyAsync(UpdateSearchOptions search, UpdateSelection selection, CancellationToken token);
}

public sealed record UpdateRecoveryResult(bool Resolved, string Detail);

public static class UpdateReconciliation
{
    public static async Task<bool> TryResolveAsync(string folder, IUpdateReconciliationHost host, TimeSpan? verificationLimit = null,
        CancellationToken token = default) => (await ResolveAsync(folder, host, verificationLimit, token: token).ConfigureAwait(false)).Resolved;

    public static async Task<UpdateRecoveryResult> ResolveAsync(string folder, IUpdateReconciliationHost host, TimeSpan? verificationLimit = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        var limit = verificationLimit ?? TimeSpan.FromMinutes(10);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(verificationLimit));
        token.ThrowIfCancellationRequested();
        if (!Directory.Exists(folder)) return new(true, "No pending update record exists.");
        using var gate = new FileStream(Path.Combine(folder, "execution.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(folder, "pending.json");
        if (!File.Exists(pending)) return new(true, "No pending update record exists.");
        if (!OperatingSystem.IsWindows()) return new(false, "Pending Windows update recovery requires Windows. The record was preserved.");
        using var providerGate = UpdateProviderLease.Acquire(folder);
        using var stream = RecoveryRecordFiles.OpenLocked(pending);
        UpdateSelection target;
        UpdateSearchOptions search;
        try
        {
            if (stream.Length > 16384) return InvalidRecord();
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                document.RootElement.EnumerateObject().Count(p => p.NameEquals("Machine")) != 1 ||
                document.RootElement.EnumerateObject().Count(p => p.NameEquals("Target")) != 1 ||
                document.RootElement.EnumerateObject().Count(p => p.NameEquals("Search")) != 1) return InvalidRecord();
            if (!string.Equals(document.RootElement.GetProperty("Machine").GetString(), Environment.MachineName,
                StringComparison.OrdinalIgnoreCase)) return InvalidRecord();
            search = document.RootElement.GetProperty("Search").Deserialize<UpdateSearchOptions>() ?? throw new InvalidDataException();
            search.Criteria();
            var value = document.RootElement.GetProperty("Target");
            if (value.ValueKind != JsonValueKind.Object ||
                value.EnumerateObject().Count(p => p.NameEquals("Id")) != 1 ||
                value.EnumerateObject().Count(p => p.NameEquals("Revision")) != 1) return InvalidRecord();
            target = new(value.GetProperty("Id").GetGuid(), value.GetProperty("Revision").GetInt32());
            if (target.Id == Guid.Empty || target.Revision < 0) return InvalidRecord();
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException) { return InvalidRecord(); }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        try
        {
            var verification = host.IsInstalledAndReadyAsync(search, target, deadline.Token)
                ?? throw new InvalidOperationException("Update recovery returned no verification task.");
            if (!await verification.WaitAsync(deadline.Token).ConfigureAwait(false))
                return new(false, "The exact update revision was not verified installed and ready. Review Windows Update and restart requirements. The pending record was preserved.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return new(false, "Update recovery verification timed out. The pending record was preserved; no installation or retry was requested."); }
        token.ThrowIfCancellationRequested();
        // Retain the original prepared record as evidence; only a verified installed state clears the guard.
        RecoveryRecordFiles.Archive(stream, Path.Combine(folder, "resolved-installed-" + Guid.NewGuid().ToString("N") + ".json"));
        return new(true, "The exact update revision was verified installed and ready. Its pending record was archived; no installation or retry was requested.");
    }

    private static UpdateRecoveryResult InvalidRecord() => new(false,
        "The pending update record is invalid, ambiguous or oversized. It was preserved. Review the saved journal before another installation attempt.");
}
