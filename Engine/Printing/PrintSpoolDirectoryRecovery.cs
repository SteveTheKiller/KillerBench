using System.Text.Json;

namespace KillerBench.Engine;

public interface IPrintSpoolDirectoryRecoveryHost
{
    PrintSpoolDirectorySnapshot Read();
    void CreateDefault();
}

public static class PrintSpoolDirectoryRecovery
{
    public const string Tool = "KillerBench.PrintSpoolDirectory";

    public static RepairStep Step(bool verification) => new(Tool,
        [verification ? "verify-default" : "create-default"], verification);

    public static bool IsEligible(PrintSpoolDirectorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot is
        {
            Complete: true,
            UsedWindowsDefault: true,
            Exists: false,
            ReparsePoint: null,
            DriveType: DriveType.Fixed,
            AvailableBytes: >= 1073741824,
            CollectionIssue: null
        } && IsExactDefault(snapshot.ResolvedPath) && IsExactDefault(snapshot.ConfiguredPath);
    }

    public static Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress,
        CancellationToken token) => ExecuteAsync(step, new WindowsPrintSpoolDirectoryRecoveryHost(), progress, token);

    public static Task<ToolResult> ExecuteAsync(RepairStep step, IPrintSpoolDirectoryRecoveryHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(host);
        if (step.Tool != Tool || step.Arguments is not { Length: 1 } ||
            step.Verification != (step.Arguments[0] == "verify-default") ||
            step.Arguments[0] is not ("create-default" or "verify-default"))
            throw new ArgumentException("Unsupported print spool directory operation.", nameof(step));
        token.ThrowIfCancellationRequested();
        if (!step.Verification)
        {
            var current = host.Read();
            var evidence = Evidence(current);
            if (Verify(evidence))
                throw new RepairStepSkippedException(
                    "The exact Windows default print spool directory now exists. No directory creation was run.", evidence);
            if (!IsEligible(current))
                throw new RepairDispatchDeferredException(
                    "Print spool directory creation was deferred because fresh exact missing-directory evidence was not available. " +
                    PrintSpoolDirectoryDiagnostics.Interpret(current).Detail);
            progress?.Report("Creating the missing Windows default print spool directory...");
            host.CreateDefault();
        }
        token.ThrowIfCancellationRequested();
        return Task.FromResult(Evidence(host.Read()));
    }

    public static bool Verify(ToolResult result)
    {
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated ||
            !string.IsNullOrWhiteSpace(result.Error)) return false;
        try
        {
            var snapshot = JsonSerializer.Deserialize<PrintSpoolDirectorySnapshot>(result.Output, Capture.JsonOptions);
            return snapshot is
            {
                Complete: true,
                UsedWindowsDefault: true,
                Exists: true,
                ReparsePoint: false,
                DriveType: DriveType.Fixed,
                AvailableBytes: >= 1073741824,
                CollectionIssue: null
            } && IsExactDefault(snapshot.ResolvedPath) && IsExactDefault(snapshot.ConfiguredPath);
        }
        catch (JsonException) { return false; }
    }

    public static async Task<PrintSpoolDirectoryReconciliation> ReconcileAsync(RepairRecoveryStore store,
        Func<PrintSpoolDirectorySnapshot> read, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(read);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return new(PrintSpoolDirectoryReconciliationOutcome.NoPending,
            "No pending print spool directory repair was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return new(PrintSpoolDirectoryReconciliationOutcome.Inconclusive, "Pending repair evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.PrintSpoolDirectory))
            return new(PrintSpoolDirectoryReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.PrintSpoolDirectory))
            return new(PrintSpoolDirectoryReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId ||
            currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return new(PrintSpoolDirectoryReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted directory operation can be reconciled; the guard remains active.");
        token.ThrowIfCancellationRequested();
        PrintSpoolDirectorySnapshot current;
        try { current = read(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        { return new(PrintSpoolDirectoryReconciliationOutcome.Inconclusive, "Current directory evidence is unavailable; the guard remains active."); }
        if (Verify(new(0, JsonSerializer.Serialize(current, Capture.JsonOptions), "", false)))
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return new(PrintSpoolDirectoryReconciliationOutcome.Verified,
                "The interrupted operation is reconciled. The exact Windows default spool directory exists and creation was not repeated.");
        }
        if (IsEligible(current))
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return new(PrintSpoolDirectoryReconciliationOutcome.NotApplied,
                "The interrupted operation is reconciled. The directory is still absent, so no change was verified and creation was not repeated in this run.");
        }
        return new(PrintSpoolDirectoryReconciliationOutcome.Inconclusive,
            "Current directory evidence differs from both the requested and original eligible states; the guard remains active.");
    }

    private static string DefaultPath() => Path.GetFullPath(Path.Combine(Environment.SystemDirectory, "spool", "PRINTERS"));

    private static ToolResult Evidence(PrintSpoolDirectorySnapshot snapshot) =>
        new(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);

    private static bool IsExactDefault(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try { return string.Equals(Path.GetFullPath(path), DefaultPath(), StringComparison.OrdinalIgnoreCase); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }
}

public sealed class WindowsPrintSpoolDirectoryRecoveryHost : IPrintSpoolDirectoryRecoveryHost
{
    public PrintSpoolDirectorySnapshot Read() => PrintSpoolDirectoryDiagnostics.Read();
    public void CreateDefault() => Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Environment.SystemDirectory, "spool", "PRINTERS")));
}

public enum PrintSpoolDirectoryReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record PrintSpoolDirectoryReconciliation(PrintSpoolDirectoryReconciliationOutcome Outcome, string Detail);
