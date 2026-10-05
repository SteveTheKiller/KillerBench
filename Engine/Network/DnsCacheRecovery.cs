using System.Net.Sockets;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public enum DnsCacheRecoveryOutcome { Blocked, Preview, Verified, Failed, Uncertain, Stopped }

public sealed record DnsCacheRecoveryResult(string Host, DnsCacheRecoveryOutcome Outcome, string Detail,
    DnsLookupResult Selected, DnsLookupResult? Before = null, ToolResult? Flush = null,
    DnsLookupResult? After = null, string? JournalPath = null, bool FlushDispatched = false)
{
    public int GetExitCode() => Outcome switch
    {
        DnsCacheRecoveryOutcome.Preview or DnsCacheRecoveryOutcome.Verified => 0,
        DnsCacheRecoveryOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IDnsCacheRecoveryHost
{
    bool IsAdministrator { get; }
    Task<DnsLookupResult> ResolveAsync(string host, CancellationToken token);
    Task<ToolResult> FlushAsync(CancellationToken token);
}

public interface IDnsCacheRecoveryJournal
{
    Task<string> BeginAsync(DnsLookupResult before, CancellationToken token);
    Task CompleteAsync(DnsCacheRecoveryResult result, CancellationToken token);
}

public static class DnsCacheRecovery
{
    public static RepairStep FlushStep() => new("ipconfig.exe", ["/flushdns"], false);

    public static bool IsEligible(DnsLookupResult evidence) => evidence is not null &&
        evidence.Host == DnsDiagnostics.ValidateHost(evidence.Host) &&
        evidence.State == DnsLookupState.Failed && evidence.Addresses is { Length: 0 } &&
        evidence.ErrorCode is SocketError.HostNotFound or SocketError.NoData;

    public static async Task<DnsCacheRecoveryResult> RunAsync(DnsLookupResult selected, bool execute,
        IDnsCacheRecoveryHost host, IDnsCacheRecoveryJournal journal, IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(journal);
        string target = DnsDiagnostics.ValidateHost(selected.Host);
        token.ThrowIfCancellationRequested();
        if (!IsEligible(selected))
            return new(target, DnsCacheRecoveryOutcome.Blocked,
                "A failed lookup reporting HostNotFound or NoData for an exact hostname is required. Timeouts, routing failures and incomplete evidence do not authorize a cache flush.", selected);
        if (!execute)
            return new(target, DnsCacheRecoveryOutcome.Preview,
                "Eligible for a guarded DNS cache flush. Execution will recheck this exact hostname, flush only the local resolver cache, then resolve the same hostname again. Authoritative records, adapters and DNS settings will not change.", selected);

        Report(progress, "Rechecking " + target + " before changing the local DNS cache.");
        DnsLookupResult before;
        try { before = await host.ResolveAsync(target, token).WaitAsync(token).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (IsProviderFailure(error))
        {
            return new(target, DnsCacheRecoveryOutcome.Blocked,
                "Fresh hostname evidence is unavailable: " + error.Message + ". The local DNS cache was not changed.", selected);
        }
        if (before is null || !SameEligibleTarget(before, target))
            return new(target, DnsCacheRecoveryOutcome.Blocked,
                "The fresh lookup changed, resolved or no longer supplies eligible evidence. The local DNS cache was not changed.", selected, before);
        if (!host.IsAdministrator)
            return new(target, DnsCacheRecoveryOutcome.Blocked,
                "Administrator access is required to flush the local DNS cache. Fresh failure evidence was retained and no setting was changed.", selected, before);

        Report(progress, "Saving a pending recovery record before flushing the local DNS cache.");
        string journalPath = await journal.BeginAsync(before, token).WaitAsync(token).ConfigureAwait(false);
        ToolResult? flush = null;
        DnsCacheRecoveryResult result;
        bool flushDispatched = false;
        try
        {
            token.ThrowIfCancellationRequested();
            Report(progress, "Confirming " + target + " still fails immediately before flushing the local DNS cache.");
            var immediate = await host.ResolveAsync(target, token).WaitAsync(token).ConfigureAwait(false)
                ?? throw new InvalidDataException("The final eligibility provider returned no hostname evidence.");
            if (!SameEligibleTarget(immediate, target))
            {
                result = new(target, DnsCacheRecoveryOutcome.Blocked,
                    "The final lookup changed, resolved or no longer supplies eligible evidence. No cache flush was requested.",
                    selected, before, After: immediate, JournalPath: journalPath);
                await journal.CompleteAsync(result, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Flushing the local DNS cache.");
            flushDispatched = true;
            flush = await host.FlushAsync(token).WaitAsync(token).ConfigureAwait(false)
                ?? throw new InvalidDataException("The flush provider returned no command evidence.");
            if (flush.TimedOut || flush.Truncated || flush.ExitCode != 0 || !string.IsNullOrWhiteSpace(flush.Error))
                result = new(target, DnsCacheRecoveryOutcome.Uncertain,
                    "The cache flush was not confirmed. Review the command evidence and recheck the hostname before another repair.", selected, before, flush, JournalPath: journalPath, FlushDispatched: true);
            else
            {
                Report(progress, "Resolving " + target + " again to verify the result.");
                var after = await host.ResolveAsync(target, token).WaitAsync(token).ConfigureAwait(false)
                    ?? throw new InvalidDataException("The verification provider returned no hostname evidence.");
                result = after.State == DnsLookupState.Resolved && after.Addresses is { Length: > 0 } &&
                    string.Equals(after.Host, target, StringComparison.OrdinalIgnoreCase)
                    ? new(target, DnsCacheRecoveryOutcome.Verified,
                        "The local DNS cache was flushed and the same hostname now resolves. This verifies local lookup recovery only; service reachability was not tested.", selected, before, flush, after, journalPath, true)
                    : new(target, after.State == DnsLookupState.TimedOut ? DnsCacheRecoveryOutcome.Uncertain : DnsCacheRecoveryOutcome.Failed,
                        "The local DNS cache was flushed, but the same hostname did not produce a verified address. Review authoritative records, resolver health and network path before repeating the repair.", selected, before, flush, after, journalPath, true);
            }
        }
        catch (OperationCanceledException error)
        {
            result = new(target, DnsCacheRecoveryOutcome.Stopped, flushDispatched
                ? "DNS cache recovery stopped after the flush request was dispatched. Reconcile the pending operation before another repair. " + error.Message
                : "DNS cache recovery stopped after durable preparation and before any cache flush was requested.",
                selected, before, flush, JournalPath: journalPath, FlushDispatched: flushDispatched);
        }
        catch (Exception error) when (IsProviderFailure(error))
        {
            result = new(target, flushDispatched ? DnsCacheRecoveryOutcome.Uncertain : DnsCacheRecoveryOutcome.Blocked,
                flushDispatched
                    ? "DNS cache recovery was interrupted after dispatch: " + error.Message + ". Reconcile the pending operation before another repair."
                    : "Final DNS cache eligibility could not be established: " + error.Message + ". No cache flush was requested.",
                selected, before, flush, JournalPath: journalPath, FlushDispatched: flushDispatched);
        }
        await journal.CompleteAsync(result, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Report(progress, result.Detail);
        return result;
    }

    private static bool SameEligibleTarget(DnsLookupResult result, string target) =>
        string.Equals(result.Host, target, StringComparison.OrdinalIgnoreCase) && IsEligible(result);

    private static bool IsProviderFailure(Exception error) => error is IOException or InvalidDataException or
        UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException or
        InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception;

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsDnsCacheRecoveryHost : IDnsCacheRecoveryHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<DnsLookupResult> ResolveAsync(string host, CancellationToken token) =>
        DnsDiagnostics.CheckAsync(host, TimeSpan.FromSeconds(10), token: token);
    public Task<ToolResult> FlushAsync(CancellationToken token) =>
        BoundedTool.RunAsync(DnsCacheRecovery.FlushStep(), TimeSpan.FromSeconds(30), token: token);
}

public sealed class DnsCacheRecoveryJournal(string directory) : IDnsCacheRecoveryJournal
{
    private readonly string root = Path.GetFullPath(directory);
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? pendingPath;

    public async Task<string> BeginAsync(DnsLookupResult before, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(before);
        if (!DnsCacheRecovery.IsEligible(before)) throw new InvalidDataException("Eligible fresh hostname evidence is required.");
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (pendingPath is not null) throw new InvalidOperationException("This journal already owns an operation.");
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "dns-cache.pending.json");
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schema = 1, machine = Environment.MachineName, startedUtc = DateTimeOffset.UtcNow, before
            }, Capture.JsonOptions);
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough);
            await stream.WriteAsync(data, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
            pendingPath = path;
            return path;
        }
        finally { gate.Release(); }
    }

    public async Task CompleteAsync(DnsCacheRecoveryResult result, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(result);
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (pendingPath is null || result.JournalPath != pendingPath)
                throw new InvalidOperationException("No matching DNS cache operation belongs to this journal.");
            string completed = Path.Combine(root, "dns-cache-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json");
            await JournalPersistence.WriteAsync(completed, JsonSerializer.Serialize(result, Capture.JsonOptions), token: token).ConfigureAwait(false);
            if (!result.FlushDispatched || result.Outcome is not (DnsCacheRecoveryOutcome.Uncertain or DnsCacheRecoveryOutcome.Stopped))
                File.Move(pendingPath, completed + ".prepared.json");
            pendingPath = null;
        }
        finally { gate.Release(); }
    }
}
