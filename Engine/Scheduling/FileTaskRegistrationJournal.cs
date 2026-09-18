using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KillerBench.Engine;

/// <summary>Retains immutable intent and completion files. Existing target records require explicit review.</summary>
public sealed class FileTaskRegistrationJournal(string directory, DateOnly? startDate = null) : ITaskRegistrationJournal
{
    private readonly string root = Path.GetFullPath(directory);
    private readonly SemaphoreSlim gate = new(1, 1);
    private ScheduledTaskPlan? prepared;
    private string? pendingPath;
    private readonly Guid attemptId = Guid.NewGuid();

    public async Task PrepareAsync(ScheduledTaskPlan plan, TaskRegistrationPreflight preflight, CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (prepared is not null) throw new InvalidOperationException("This journal has already prepared an attempt.");
            plan = plan.Validate();
            if (!plan.Identity.Matches(preflight.Target) || !preflight.Eligible || !Complete(preflight.Evidence))
                throw new ArgumentException("An eligible exact-target preflight is required.");
            string path = GetPendingPath(root, plan.Identity);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = startDate.HasValue ? 2 : 1, recordedUtc = DateTimeOffset.UtcNow, state = "Pending",
                machine = Environment.MachineName, attemptId, startDate, plan, preflight
            });
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(root);
            await WriteNewAsync(path, bytes, token).ConfigureAwait(false);
            pendingPath = path;
            prepared = plan;
        }
        finally { gate.Release(); }
    }

    public async Task CompleteAsync(TaskRegistrationVerification verification, CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (prepared is null || pendingPath is null) throw new InvalidOperationException("No prepared attempt belongs to this journal.");
            if (!Complete(verification.Evidence) || verification.Observed is null ||
                !ScheduledTaskPlanJson.Serialize(prepared).SequenceEqual(ScheduledTaskPlanJson.Serialize(verification.Observed)))
                throw new ArgumentException("Completion requires a matching verified definition.");
            await WriteNewAsync(pendingPath + ".verified.json", JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = 1, recordedUtc = DateTimeOffset.UtcNow, state = "DefinitionVerified",
                taskExecuted = false, attemptId, machine = Environment.MachineName, verification
            }), token).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    public async Task AbandonAsync(string detail, CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (prepared is null || pendingPath is null) throw new InvalidOperationException("No prepared attempt belongs to this journal.");
            if (string.IsNullOrWhiteSpace(detail) || detail.Length > 4096 || detail.Any(char.IsControl))
                throw new ArgumentException("A bounded no-dispatch reason is required.", nameof(detail));
            string attemptSuffix = "." + attemptId.ToString("N");
            string resultPath = pendingPath + attemptSuffix + ".not-dispatched.json";
            await WriteNewAsync(resultPath, JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = 1, recordedUtc = DateTimeOffset.UtcNow, state = "NotDispatched",
                taskExecuted = false, attemptId, machine = Environment.MachineName, detail
            }), token).ConfigureAwait(false);
            File.Move(pendingPath, pendingPath + attemptSuffix + ".prepared.json");
            pendingPath = null;
            prepared = null;
        }
        finally { gate.Release(); }
    }

    private static async Task WriteNewAsync(string path, byte[] bytes, CancellationToken token)
    {
        if (bytes.Length > 1024 * 1024) throw new ArgumentException("Task journal evidence exceeds 1 MiB.");
        token.ThrowIfCancellationRequested();
        // A partial record also blocks another attempt. Never overwrite or remove it on failure.
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
            65536, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await file.WriteAsync(bytes, token).ConfigureAwait(false);
        await file.FlushAsync(token).ConfigureAwait(false);
        file.Flush(flushToDisk: true);
    }

    public static string GetPendingPath(string directory, ScheduledTaskIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        string root = Path.GetFullPath(directory);
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.FullName.ToUpperInvariant())));
        return Path.Combine(root, key + ".pending.json");
    }

    private static bool Complete(ToolResult result) => TaskRegistration.Complete(result);
}
