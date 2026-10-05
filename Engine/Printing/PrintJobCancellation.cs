using System.ComponentModel;
using System.Text.Json;

namespace KillerBench.Engine.Printing;

public enum PrintJobCancellationOutcome
{
    Preview,
    Blocked,
    Verified,
    NotApplied,
    Uncertain,
    Stopped,
    NoPending
}

public sealed record PrintJobCancellationResult(string QueueName, uint JobId,
    PrintJobCancellationOutcome Outcome, string Detail, PrintJobInventory? Before = null,
    ToolResult? Command = null, PrintJobInventory? After = null, string? RecordPath = null)
{
    public int GetExitCode() => Outcome switch
    {
        PrintJobCancellationOutcome.Preview or PrintJobCancellationOutcome.Verified or
            PrintJobCancellationOutcome.NoPending => 0,
        PrintJobCancellationOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IPrintJobCancellationHost
{
    Task<PrintJobInventory> InspectAsync(CancellationToken token);
    Task<ToolResult> CancelAsync(PrintJobSnapshot expected, CancellationToken token);
}

public sealed record PrintJobCancellationPendingRecord(int Schema, DateTimeOffset StartedUtc,
    string QueueName, uint JobId, PrintJobInventory Before)
{
    public string Machine { get; init; } = "";
}

public static class PrintJobCancellation
{
    private const string PendingName = "print-job-cancel.pending.json";
    private const uint EligibleMask = 2u | 512u;
    private const uint ActiveOrTerminalMask = 4u | 8u | 16u | 128u | 256u;
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static bool HasPendingRecord(string directory) =>
        !string.IsNullOrWhiteSpace(directory) && File.Exists(Path.Combine(Path.GetFullPath(directory), PendingName));

    public static bool IsCandidate(PrintJobSnapshot job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return ValidJob(job) &&
            (job.StatusMask & ActiveOrTerminalMask) == 0 && (job.StatusMask & EligibleMask) != 0;
    }

    public static WorkflowCheck Assess(PrintJobInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var evidence = new ToolResult(0, JsonSerializer.Serialize(inventory, Capture.JsonOptions), "", false);
        if (!Complete(inventory))
            return new("Selected print-job cancellation", CheckState.Unknown,
                "Complete exact print-job evidence is required before cancellation candidates can be identified. No job is eligible.",
                Evidence: evidence);
        int candidates = inventory.Jobs.Count(IsCandidate);
        if (candidates == 0)
            return new("Selected print-job cancellation", CheckState.Information,
                "No visible print job is currently reported as Error or Blocked while inactive. No cancellation is eligible.",
                Evidence: evidence);
        return new("Selected print-job cancellation", CheckState.Repairable,
            candidates + " exact Error or Blocked print job candidate(s) are visible. Choose one exact job and separately confirm cancellation in the guarded Printing controls. Automatic repair cannot cancel a job.",
            RepairKind.PrintJobCancellation, evidence);
    }

    public static async Task<PrintJobCancellationResult> RunAsync(string queueName, uint jobId, bool execute,
        string directory, IPrintJobCancellationHost host, IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        string queue = ValidateQueue(queueName);
        if (jobId == 0) throw new ArgumentException("Choose a nonzero print job ID.", nameof(jobId));
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        token.ThrowIfCancellationRequested();
        if (!execute) return new(queue, jobId, PrintJobCancellationOutcome.Preview,
            "Execution will require three matching complete inventories for one exact Error or Blocked job, record durable preparation, cancel only that job, and verify its removal while every other observed job remains present. No queue purge, restart, printer change or automatic repair is included.");

        string root = Path.GetFullPath(directory);
        Directory.CreateDirectory(root);
        using var operationLock = new FileStream(Path.Combine(root, "print-job-cancel.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(root, PendingName);
        if (File.Exists(pending)) return new(queue, jobId, PrintJobCancellationOutcome.Blocked,
            "A prior print-job cancellation needs reconciliation before another cancellation can start.", RecordPath: pending);

        Report(progress, "Inspecting the exact selected print job.");
        PrintJobInventory first = await InspectAsync(host, token).ConfigureAwait(false);
        if (!Eligible(first, queue, jobId, out PrintJobSnapshot? selected, out string blocker))
            return new(queue, jobId, PrintJobCancellationOutcome.Blocked, blocker, first);
        Report(progress, "Repeating print-job evidence before preparation.");
        PrintJobInventory second = await InspectAsync(host, token).ConfigureAwait(false);
        if (!Same(first, second) || !Eligible(second, queue, jobId, out selected, out _))
            return new(queue, jobId, PrintJobCancellationOutcome.Blocked,
                "The print-job inventory or selected status changed during the fresh recheck. No job was canceled.", second);

        var prepared = new PrintJobCancellationPendingRecord(1, DateTimeOffset.UtcNow, queue, jobId, second)
        { Machine = Environment.MachineName };
        await WriteNewAsync(pending, JsonSerializer.SerializeToUtf8Bytes(prepared, Capture.JsonOptions), token).ConfigureAwait(false);
        ToolResult? command = null;
        bool dispatched = false;
        try
        {
            Report(progress, "Confirming final print-job eligibility.");
            PrintJobInventory final = await InspectAsync(host, token).ConfigureAwait(false);
            if (!Same(second, final) || !Eligible(final, queue, jobId, out selected, out _))
                return await FinishAsync(new(queue, jobId, PrintJobCancellationOutcome.Blocked,
                    "The print-job inventory or selected status changed after preparation. No job was canceled.",
                    second, After: final, RecordPath: pending), root, true).ConfigureAwait(false);
            Report(progress, "Canceling only the exact selected print job.");
            dispatched = true;
            command = await (host.CancelAsync(selected!, token) ??
                throw new InvalidDataException("The print provider returned no cancellation task."))
                .WaitAsync(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
            if (!Successful(command))
                return await FinishAsync(new(queue, jobId, PrintJobCancellationOutcome.Uncertain,
                    "Windows did not return complete cancellation success evidence. Reconcile the pending record before another attempt.",
                    second, command, RecordPath: pending), root, false).ConfigureAwait(false);
            Report(progress, "Verifying the selected job is gone and other observed jobs remain.");
            PrintJobInventory after = await InspectAsync(host, token).ConfigureAwait(false);
            bool removed = Target(after, queue, jobId) is null;
            bool retained = OtherIdentities(second, queue, jobId).All(identity =>
                after.Jobs.Any(job => string.Equals(job.Name, identity, StringComparison.OrdinalIgnoreCase)));
            bool verified = Complete(after) && removed && retained;
            bool notApplied = Same(second, after);
            return await FinishAsync(new(queue, jobId,
                verified ? PrintJobCancellationOutcome.Verified : notApplied ? PrintJobCancellationOutcome.NotApplied : PrintJobCancellationOutcome.Uncertain,
                verified
                    ? "The exact selected print job is absent and every other observed job remains present. No queue purge, restart or printer setting change was requested. Physical printing was not tested."
                    : notApplied
                        ? "Windows accepted the request, but the complete print-job inventory is unchanged. No cancellation was verified."
                        : "The selected job or another observed job could not be verified after the request. The result remains unresolved.",
                second, command, after, pending), root, verified || notApplied).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!dispatched)
                return await FinishAsync(new(queue, jobId, PrintJobCancellationOutcome.Stopped,
                    "The operation stopped after durable preparation and before a cancellation request was sent.",
                    second, RecordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(new(queue, jobId, PrintJobCancellationOutcome.Stopped,
                "The operation stopped after the request may have started. Reconcile before another attempt.",
                second, command, RecordPath: pending), root, false).ConfigureAwait(false);
        }
        catch (Exception error) when (Unavailable(error))
        {
            if (!dispatched)
                return await FinishAsync(new(queue, jobId, PrintJobCancellationOutcome.Blocked,
                    "Final print-job eligibility could not be established: " + error.Message + ". No job was canceled.",
                    second, RecordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(new(queue, jobId, PrintJobCancellationOutcome.Uncertain,
                "Print-job cancellation was interrupted: " + error.Message + ". The pending record remains and no retry started.",
                second, command, RecordPath: pending), root, false).ConfigureAwait(false);
        }
    }

    public static async Task<PrintJobCancellationResult> ReconcileAsync(string directory,
        IPrintJobCancellationHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory), pending = Path.Combine(root, PendingName);
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pending)) return new("", 0, PrintJobCancellationOutcome.NoPending,
            "No interrupted print-job cancellation is present.", RecordPath: pending);
        using var operationLock = new FileStream(Path.Combine(root, "print-job-cancel.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        if (!File.Exists(pending)) return new("", 0, PrintJobCancellationOutcome.NoPending,
            "The interrupted print-job record was already reconciled. No cancellation was repeated.", RecordPath: pending);
        PrintJobCancellationPendingRecord record;
        try { record = await ReadPendingAsync(pending, token).ConfigureAwait(false); }
        catch (Exception error) when (Unavailable(error))
        {
            return new("", 0, PrintJobCancellationOutcome.Uncertain,
                "The pending print-job record is unavailable or invalid: " + error.Message +
                ". It was retained and cancellation was not repeated.", RecordPath: pending);
        }
        if (!Eligible(record.Before, record.QueueName, record.JobId, out _, out _))
            return new(record.QueueName, record.JobId, PrintJobCancellationOutcome.Uncertain,
                "The pending record lacks complete eligibility evidence. It was retained and cancellation was not repeated.",
                record.Before, RecordPath: pending);
        PrintJobInventory current;
        try
        {
            Report(progress, "Inspecting current print jobs without repeating cancellation.");
            current = await InspectAsync(host, token).ConfigureAwait(false);
        }
        catch (Exception error) when (Unavailable(error))
        {
            return new(record.QueueName, record.JobId, PrintJobCancellationOutcome.Uncertain,
                "Current print-job evidence is unavailable: " + error.Message +
                ". The pending record was retained and cancellation was not repeated.",
                record.Before, RecordPath: pending);
        }
        bool removed = Target(current, record.QueueName, record.JobId) is null;
        bool retained = OtherIdentities(record.Before, record.QueueName, record.JobId).All(identity =>
            current.Jobs.Any(job => string.Equals(job.Name, identity, StringComparison.OrdinalIgnoreCase)));
        bool verified = Complete(current) && removed && retained;
        bool notApplied = Same(record.Before, current);
        if (!verified && !notApplied) return new(record.QueueName, record.JobId, PrintJobCancellationOutcome.Uncertain,
            "Current print-job evidence cannot prove that only the selected job was removed. The pending record was retained and cancellation was not repeated.",
            record.Before, After: current, RecordPath: pending);
        return await FinishAsync(new(record.QueueName, record.JobId,
            verified ? PrintJobCancellationOutcome.Verified : PrintJobCancellationOutcome.NotApplied,
            verified
                ? "The exact selected job is absent and every other observed job remains present. Cancellation was not repeated."
                : "The complete prior inventory remains in place. No cancellation was verified and cancellation was not repeated.",
            record.Before, After: current, RecordPath: pending), root, true).ConfigureAwait(false);
    }

    public static string ValidateQueue(string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        string value = queueName.Trim();
        if (!DriverReference.ValidText(value, 512) || value != queueName)
            throw new ArgumentException("Choose one exact printer queue without leading, trailing or control characters.", nameof(queueName));
        return value;
    }

    private static bool Eligible(PrintJobInventory inventory, string queue, uint jobId,
        out PrintJobSnapshot? selected, out string blocker)
    {
        selected = Complete(inventory) ? Target(inventory, queue, jobId) : null;
        blocker = !Complete(inventory)
            ? "Complete print-job inventory is required. No job was canceled."
            : selected is null
                ? "The exact selected queue and job ID are missing or ambiguous. No job was canceled."
                : (selected.StatusMask & ActiveOrTerminalMask) != 0
                    ? "The selected job is active, completing, deleting or terminal. No job was canceled."
                    : (selected.StatusMask & EligibleMask) == 0
                        ? "Only an exact job repeatedly reported as Error or Blocked is eligible. No job was canceled."
                        : "";
        return blocker.Length == 0;
    }

    private static PrintJobSnapshot? Target(PrintJobInventory inventory, string queue, uint jobId)
    {
        var matches = inventory.Jobs.Where(job => job.JobId == jobId &&
            string.Equals(job.QueueName, queue, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static IEnumerable<string> OtherIdentities(PrintJobInventory inventory, string queue, uint jobId) =>
        inventory.Jobs.Where(job => job.JobId != jobId || !string.Equals(job.QueueName, queue, StringComparison.OrdinalIgnoreCase))
            .Select(job => job.Name);

    private static bool Complete(PrintJobInventory? inventory) => inventory is { Complete: true, Jobs.Count: <= 256 } &&
        inventory.Jobs.All(ValidJob) &&
        inventory.Jobs.Select(job => job.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == inventory.Jobs.Count;

    private static bool ValidJob(PrintJobSnapshot job) => job.JobId > 0 &&
        DriverReference.ValidText(job.QueueName, 512) && DriverReference.ValidText(job.Name, 512) &&
        string.Equals(job.Name, job.QueueName + ", " + job.JobId, StringComparison.Ordinal) &&
        (job.StatusMask & ~4095u) == 0;

    private static bool Same(PrintJobInventory left, PrintJobInventory right) => Complete(left) && Complete(right) &&
        left.Jobs.OrderBy(job => job.Name, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(right.Jobs.OrderBy(job => job.Name, StringComparer.OrdinalIgnoreCase));

    private static bool Successful(ToolResult value) => value.ExitCode == 0 && !value.TimedOut &&
        !value.Truncated && string.IsNullOrWhiteSpace(value.Error);

    private static async Task<PrintJobInventory> InspectAsync(IPrintJobCancellationHost host, CancellationToken token)
    {
        var value = await (host.InspectAsync(token) ?? throw new InvalidDataException("The print provider returned no inspection task."))
            .WaitAsync(TimeSpan.FromSeconds(40), token).ConfigureAwait(false);
        return value ?? throw new InvalidDataException("The print provider returned no inventory.");
    }

    private static async Task<PrintJobCancellationPendingRecord> ReadPendingAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 2_097_152) throw new InvalidDataException("Print-job recovery record size is invalid.");
        try
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
            if (!UniqueFields(document.RootElement))
                throw new InvalidDataException("Print-job recovery record contains conflicting fields.");
            var record = document.RootElement.Deserialize<PrintJobCancellationPendingRecord>(StrictJson);
            if (record is not { Schema: 1 } || record.StartedUtc == default || record.JobId == 0 ||
                !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Print-job recovery record is invalid or belongs to another computer.");
            return record with { QueueName = ValidateQueue(record.QueueName) };
        }
        catch (JsonException error) { throw new InvalidDataException("Print-job recovery record is not valid JSON.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Print-job recovery target is invalid.", error); }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static async Task WriteNewAsync(string path, byte[] value, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(value, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(true);
    }

    private static async Task<PrintJobCancellationResult> FinishAsync(PrintJobCancellationResult result,
        string root, bool resolve)
    {
        string journal = Path.Combine(root, "print-job-cancel-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(journal, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        string pending = Path.Combine(root, PendingName);
        if (resolve && File.Exists(pending)) File.Move(pending, journal + ".prepared.json");
        return result with { RecordPath = resolve ? journal : pending };
    }

    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or Win32Exception or System.Security.SecurityException;

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsPrintJobCancellationHost : IPrintJobCancellationHost
{
    public async Task<PrintJobInventory> InspectAsync(CancellationToken token)
    {
        ToolResult result = await PrintJobDiagnostics.CaptureAsync(token: token).ConfigureAwait(false);
        return PrintJobDiagnostics.ReadInventory(result) ?? throw new InvalidDataException("Print-job inventory was incomplete.");
    }

    public async Task<ToolResult> CancelAsync(PrintJobSnapshot expected, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(expected);
        token.ThrowIfCancellationRequested();
        string identity = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(expected.Name));
        string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$n=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('" + identity + "'));" +
            "$all=@(Get-CimInstance Win32_PrintJob -ErrorAction Stop|Select-Object -First 257);" +
            "if($all.Count-gt256){throw 'Print job inventory exceeds the safe bound.'};" +
            "$rows=@($all|Where-Object{[string]$_.Name-ceq$n});" +
            "if($rows.Count-ne1){throw 'Exact print job is missing or ambiguous.'};" +
            "$j=$rows[0];if([uint32]$j.JobId-ne" + expected.JobId + "){throw 'Print job ID changed.'};" +
            "if([uint32]$j.StatusMask-ne" + expected.StatusMask + "){throw 'Print job status changed.'};" +
            "$j|Remove-CimInstance -ErrorAction Stop;[pscustomobject]@{accepted=$true}|ConvertTo-Json -Compress";
        return await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
    }
}
