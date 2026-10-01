using System.Text.Json;

namespace KillerBench.Engine.Services;

/// <summary>Retains one retry attempt per job until a separate read-only reconciliation archives it.</summary>
public sealed class BitsRetryJournal(string directory) : IBitsRetryJournal
{
    private readonly string root = Path.GetFullPath(directory);
    private Guid? active;
    private ToolResult? activeEvidence;
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task BeginAsync(Guid jobId, BitsInventoryResult before, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(before);
        if (jobId == Guid.Empty) throw new ArgumentException("An exact job ID is required.", nameof(jobId));
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (active is not null) throw new InvalidOperationException("This journal already owns an attempt.");
            token.ThrowIfCancellationRequested();
            before = BitsDiagnostics.Parse(before.Evidence, token);
            var selected = before.Jobs.SingleOrDefault(job => job.Id == jobId);
            if (!before.Complete || before.Scope != "CurrentAccount" || selected is null ||
                string.IsNullOrWhiteSpace(selected.Owner) || selected.ErrorCode is null || selected.BytesTransferred is null ||
                selected.State is not ("Error" or "TransientError"))
                throw new InvalidDataException("Complete current-account evidence for the selected errored job is required.");
            Directory.CreateDirectory(root);
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(new
            {
                jobId, machine = Environment.MachineName, started = DateTimeOffset.UtcNow, before
            }, Capture.JsonOptions);
            // Exclusive creation also blocks competing processes and preserves corrupt or interrupted records.
            await PersistAsync(Path.Combine(root, jobId.ToString("N") + ".pending.json"), data, token).ConfigureAwait(false);
            active = jobId;
            activeEvidence = before.Evidence;
        }
        finally { gate.Release(); }
    }

    public async Task CompleteAsync(BitsRetryResult result, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(result);
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (active != result.JobId) throw new InvalidOperationException("No matching attempt belongs to this journal.");
            if (result.Before?.Evidence != activeEvidence || !Enum.IsDefined(result.Outcome))
                throw new InvalidDataException("Result does not match the prepared evidence or has an unknown outcome.");
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(result, Capture.JsonOptions);
            await PersistAsync(Path.Combine(root, result.JobId.ToString("N") + ".result.json"), data, token).ConfigureAwait(false);
            // A request result is not reconciliation. Keep the pending guard for every outcome.
            active = null;
            activeEvidence = null;
        }
        finally { gate.Release(); }
    }

    public async Task AbandonAsync(BitsRetryResult result, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(result);
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (active != result.JobId) throw new InvalidOperationException("No matching attempt belongs to this journal.");
            if (result.Before?.Evidence != activeEvidence || result.Request is not null ||
                result.Outcome is not (BitsRetryOutcome.Blocked or BitsRetryOutcome.Stopped))
                throw new InvalidDataException("Only an exact no-request result can abandon retry preparation.");
            string archive = Path.Combine(root, "bits-retry-no-request-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
                "-" + Guid.NewGuid().ToString("N") + ".json");
            await PersistAsync(archive, JsonSerializer.SerializeToUtf8Bytes(result, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Move(Path.Combine(root, result.JobId.ToString("N") + ".pending.json"), archive + ".prepared.json");
            active = null;
            activeEvidence = null;
        }
        finally { gate.Release(); }
    }

    private static async Task PersistAsync(string path, byte[] data, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(data, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }
}
