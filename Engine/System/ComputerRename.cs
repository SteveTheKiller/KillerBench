using System.Text.Json;

namespace KillerBench.Engine;

public enum ComputerRenameOutcome { Preview, Blocked, RestartRequired, Verified, NotChanged, Uncertain, Stopped, NoPendingRecord }

public sealed record ComputerRenamePendingRecord(int Schema, string OriginalName, string TargetName,
    DateTimeOffset StartedUtc, string JournalPath, ComputerNameContext Before)
{
    public string Machine { get; init; } = "";
}

public sealed record ComputerRenameResult(string TargetName, ComputerRenameOutcome Outcome, string Detail,
    string? RecordPath = null, ComputerNameContext? Before = null, ToolResult? Command = null,
    ComputerNameContext? After = null, WorkflowCheck? Restart = null)
{
    public int GetExitCode() => Outcome switch
    {
        ComputerRenameOutcome.Preview or ComputerRenameOutcome.Verified or ComputerRenameOutcome.NoPendingRecord => 0,
        ComputerRenameOutcome.RestartRequired => 4,
        ComputerRenameOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IComputerRenameHost
{
    bool IsAdministrator { get; }
    Task<ComputerNameContext> ReadContextAsync(CancellationToken token);
    Task<WorkflowCheck> ReadRestartAsync(CancellationToken token);
    Task<ToolResult> RenameAsync(string targetName, CancellationToken token);
}

public static class ComputerRename
{
    private const string PendingFile = "computer-rename.pending.json";

    public static RepairStep Step(string targetName)
    {
        string target = ComputerNameReview.Review(targetName, new(null, null, null)).ProposedName;
        string script = "$ErrorActionPreference='Stop'; $name='" + target + "'; " +
            "Rename-Computer -NewName $name -Force -ErrorAction Stop; [pscustomobject]@{requested=$name}|ConvertTo-Json -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], false);
    }

    public static WorkflowCheck ToWorkflowCheck(string targetName, ComputerNameContext context, bool elevated, WorkflowCheck restart)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(restart);
        var review = ComputerNameReview.Review(targetName, context);
        var evidence = new ToolResult(0, JsonSerializer.Serialize(new { Context = context, Restart = restart }, Capture.JsonOptions), "", false);
        string names = "Current name: " + (context.CurrentName ?? "Unknown") + "; configured name: " +
            (context.ConfiguredName ?? "Unknown") + "; proposed name: " + review.ProposedName + ". ";
        if (context.CurrentName is null || context.ConfiguredName is null || context.RestartPending is null || context.DomainRole is null)
            return new("Computer name change", CheckState.Unknown,
                names + "Current identity, configured identity, role or rename-pending evidence is incomplete. No rename is eligible.", Evidence: evidence);
        if (context.RestartPending == false && !string.Equals(context.CurrentName, context.ConfiguredName, StringComparison.OrdinalIgnoreCase))
            return new("Computer name change", CheckState.Unknown,
                names + "Active and configured identities disagree without a reported pending rename. Recollect before planning a change.", Evidence: evidence);
        if (string.Equals(context.CurrentName, review.ProposedName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(context.ConfiguredName, review.ProposedName, StringComparison.OrdinalIgnoreCase))
            return new("Computer name change", CheckState.Clear, names + "The exact target is already active. No rename is needed.", Evidence: evidence);
        if (context.DomainRole is not (0 or 2))
            return new("Computer name change", CheckState.Attention,
                names + "Automatic rename is limited to standalone workgroup computers. Domain members and domain controllers require their dedicated administrative procedure.", Evidence: evidence);
        if (context.RestartPending == true)
            return new("Computer name change", CheckState.Attention,
                names + "A computer-name change is already pending. Restart and reconcile it before another rename.", Evidence: evidence);
        if (restart.EffectiveState != CheckState.Clear)
            return new("Computer name change", restart.EffectiveState == CheckState.Unknown ? CheckState.Unknown : CheckState.Attention,
                names + "Restart state is pending or unavailable. Resolve it and collect fresh identity before renaming.", Evidence: evidence);
        if (!elevated)
            return new("Computer name change", CheckState.Attention,
                names + "Administrator access is required before the exact standalone rename can be requested.", Evidence: evidence);
        return new("Computer name change", CheckState.Repairable,
            names + "The exact standalone target passed current identity, role, restart and administrator gates. Separate confirmation and a fresh repeated prerequisite check are required before rename.",
            RepairKind.ComputerRename, evidence);
    }

    public static WorkflowCheck ReviewSelection(string targetName, ComputerNameContext context, bool elevated,
        WorkflowCheck restart)
    {
        var check = ToWorkflowCheck(targetName, context, elevated, restart);
        return check.State == CheckState.Repairable
            ? check with
            {
                State = CheckState.Attention,
                Repair = null,
                Detail = check.Detail + " This read-only workflow review does not authorize or request the rename."
            }
            : check with { Repair = null };
    }

    public static async Task<ComputerRenameResult> RunAsync(string targetName, bool execute, string directory,
        IComputerRenameHost host, IProgress<string>? progress = null, CancellationToken token = default,
        TimeSpan? executionTimeout = null)
    {
        string target = ComputerNameReview.Review(targetName, new(null, null, null)).ProposedName;
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = executionTimeout ?? TimeSpan.FromMinutes(2);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(executionTimeout));
        token.ThrowIfCancellationRequested();
        if (!execute) return new(target, ComputerRenameOutcome.Preview,
            "Execution will recheck the current name, workgroup role, restart state and administrator access. It will request the exact new name without joining a domain or restarting Windows. A successful request requires restart and later reconciliation.");

        string root = Path.GetFullPath(directory);
        Directory.CreateDirectory(root);
        using var operationLock = new FileStream(Path.Combine(root, "computer-rename.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, PendingFile);
        if (File.Exists(pendingPath)) return new(target, ComputerRenameOutcome.Blocked,
            "A prior computer rename needs reconciliation before another rename can start.", pendingPath);

        Report(progress, "Checking current computer identity.");
        var before = await Required(host.ReadContextAsync, "Computer identity", token).ConfigureAwait(false);
        if (!Eligible(before, target, out string blocker))
            return new(target, ComputerRenameOutcome.Blocked, blocker, Before: before);
        Report(progress, "Checking pending restart state.");
        var restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
        if (!HasClearRestartEvidence(restart))
            return new(target, ComputerRenameOutcome.Blocked, "Restart state is pending or unavailable. Resolve it and collect fresh identity before renaming.", Before: before, Restart: restart);
        if (!host.IsAdministrator)
            return new(target, ComputerRenameOutcome.Blocked, "Administrator access is required. No rename was requested.", Before: before, Restart: restart);

        string operationId = Guid.NewGuid().ToString("N");
        string journalPath = Path.Combine(root, "computer-rename-" + operationId + ".jsonl");
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new ComputerRenamePendingRecord(1,
            before.CurrentName!, target, DateTimeOffset.UtcNow, journalPath, before)
        { Machine = Environment.MachineName }, Capture.JsonOptions), token).ConfigureAwait(false);
        await AppendRecordAsync(journalPath, new { Stage = "Prepared", Target = target, Before = before, Restart = restart }, token).ConfigureAwait(false);

        ToolResult? command = null;
        bool renameDispatched = false;
        try
        {
            Report(progress, "Repeating computer rename eligibility immediately before the request.");
            var immediate = await Required(host.ReadContextAsync, "Final computer identity", token).ConfigureAwait(false);
            var immediateRestart = await Required(host.ReadRestartAsync, "Final restart state", token).ConfigureAwait(false);
            if (!Eligible(immediate, target, out string finalBlocker) || !HasClearRestartEvidence(immediateRestart) || !host.IsAdministrator)
            {
                string detail = finalBlocker.Length > 0
                    ? finalBlocker + " Final evidence changed after preparation. No rename was requested."
                    : !HasClearRestartEvidence(immediateRestart)
                        ? "Final restart evidence no longer authorizes a rename. No rename was requested."
                        : "Administrator access is no longer available. No rename was requested.";
                var blocked = new ComputerRenameResult(target, ComputerRenameOutcome.Blocked, detail,
                    journalPath, before, After: immediate, Restart: immediateRestart);
                await AppendRecordAsync(journalPath, new
                {
                    Stage = "FinalEligibility",
                    Initial = new { Context = before, Restart = restart },
                    Current = new { Context = immediate, Restart = immediateRestart, Administrator = host.IsAdministrator },
                    Result = blocked
                }, CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                return blocked;
            }

            Report(progress, "Requesting computer name " + target + ".");
            renameDispatched = true;
            command = await Required(ct => host.RenameAsync(target, ct), "Computer rename", token, limit).ConfigureAwait(false);
            if (command.ExitCode != 0 || command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
            {
                var incomplete = new ComputerRenameResult(target, ComputerRenameOutcome.Uncertain,
                    "The rename request did not return complete success evidence. Reconcile the pending record before another attempt.", journalPath, before, command, Restart: restart);
                await AppendRecordAsync(journalPath, new { Stage = "Finished", Result = incomplete }, CancellationToken.None).ConfigureAwait(false);
                return incomplete;
            }
            Report(progress, "Verifying the configured computer name.");
            var after = await Required(host.ReadContextAsync, "Configured computer identity", token).ConfigureAwait(false);
            bool configured = string.Equals(after.ConfiguredName, target, StringComparison.OrdinalIgnoreCase);
            bool active = string.Equals(after.CurrentName, target, StringComparison.OrdinalIgnoreCase);
            var result = active && configured
                ? new ComputerRenameResult(target, ComputerRenameOutcome.Verified, "The active and configured computer names match the exact target. Application dependencies and network name propagation remain unverified.", journalPath, before, command, after, restart)
                : configured && after.RestartPending == true
                    ? new ComputerRenameResult(target, ComputerRenameOutcome.RestartRequired, "Windows accepted the exact configured name. Restart is required. KillerBench did not restart Windows; reconcile after restart.", journalPath, before, command, after, restart)
                    : new ComputerRenameResult(target, ComputerRenameOutcome.Uncertain, "The rename command returned success, but the exact configured target was not verified. Reconcile before another attempt.", journalPath, before, command, after, restart);
            await AppendRecordAsync(journalPath, new { Stage = "Finished", Result = result }, CancellationToken.None).ConfigureAwait(false);
            if (result.Outcome == ComputerRenameOutcome.Verified) File.Move(pendingPath, journalPath + ".prepared.json");
            return result;
        }
        catch (OperationCanceledException error)
        {
            var stopped = new ComputerRenameResult(target, ComputerRenameOutcome.Stopped,
                renameDispatched
                    ? "The rename was stopped after request dispatch. Current state is unknown; reconcile before another attempt. " + error.Message
                    : "Final computer-name eligibility inspection was stopped before request dispatch. No rename was requested. " + error.Message,
                journalPath, before, command, Restart: restart);
            await AppendRecordAsync(journalPath, new { Stage = "Finished", Result = stopped }, CancellationToken.None).ConfigureAwait(false);
            if (!renameDispatched) File.Move(pendingPath, journalPath + ".prepared.json");
            return stopped;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            var uncertain = new ComputerRenameResult(target,
                renameDispatched ? ComputerRenameOutcome.Uncertain : ComputerRenameOutcome.Blocked,
                renameDispatched
                    ? "The rename was interrupted after request dispatch: " + error.Message + ". Reconcile before another attempt."
                    : "Final computer-name eligibility inspection was unavailable before request dispatch: " + error.Message + ". No rename was requested.",
                journalPath, before, command, Restart: restart);
            await AppendRecordAsync(journalPath, new { Stage = "Finished", Result = uncertain }, CancellationToken.None).ConfigureAwait(false);
            if (!renameDispatched) File.Move(pendingPath, journalPath + ".prepared.json");
            return uncertain;
        }
    }

    public static async Task<ComputerRenameResult> ReconcileAsync(string directory, IComputerRenameHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory), pendingPath = Path.Combine(root, PendingFile);
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath)) return new("", ComputerRenameOutcome.NoPendingRecord, "No interrupted computer rename record is present.", pendingPath);
        await using var operationLock = new FileStream(Path.Combine(root, "computer-rename.reconcile.lock"), FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        ComputerRenamePendingRecord pending;
        try
        {
            pending = await ReadPendingAsync(pendingPath, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error))
        {
            return new("", ComputerRenameOutcome.Uncertain,
                "The interrupted rename record could not be verified for this computer. It was retained and the rename was not repeated.",
                pendingPath);
        }
        WorkflowCheck restart;
        ComputerNameContext context;
        try
        {
            Report(progress, "Checking restart state without repeating the rename.");
            restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
            Report(progress, "Checking current and configured computer names.");
            context = await Required(host.ReadContextAsync, "Computer identity", token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error))
        {
            return new(pending.TargetName, ComputerRenameOutcome.Uncertain,
                "Current identity or restart evidence could not be established. The record was retained and the rename was not repeated.",
                pendingPath, pending.Before);
        }
        bool activeTarget = string.Equals(context.CurrentName, pending.TargetName, StringComparison.OrdinalIgnoreCase);
        bool configuredTarget = string.Equals(context.ConfiguredName, pending.TargetName, StringComparison.OrdinalIgnoreCase);
        bool original = string.Equals(context.CurrentName, pending.OriginalName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(context.ConfiguredName, pending.OriginalName, StringComparison.OrdinalIgnoreCase);
        ComputerRenameOutcome outcome;
        string detail;
        if (restart.State == CheckState.Clear && activeTarget && configuredTarget && context.RestartPending == false)
        { outcome = ComputerRenameOutcome.Verified; detail = "The active and configured names match the exact target after restart. The rename was not repeated."; }
        else if (configuredTarget && (restart.State == CheckState.Attention || context.RestartPending == true))
        { outcome = ComputerRenameOutcome.RestartRequired; detail = "The exact target remains configured and a restart is still required. The rename was not repeated."; }
        else if (restart.State == CheckState.Clear && original && context.RestartPending == false)
        { outcome = ComputerRenameOutcome.NotChanged; detail = "The original active and configured names remain in place. Evidence was archived without repeating the rename."; }
        else
        { outcome = ComputerRenameOutcome.Uncertain; detail = "Current identity or restart evidence is incomplete or conflicts with the pending rename. The record was retained and the rename was not repeated."; }
        string record = pendingPath;
        if (outcome is ComputerRenameOutcome.Verified or ComputerRenameOutcome.NotChanged)
        {
            record = Path.Combine(root, "computer-rename-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".json");
            await JournalPersistence.WriteAsync(record, JsonSerializer.Serialize(new { Outcome = outcome, Pending = pending, Current = context, Restart = restart }, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Move(pendingPath, record + ".prepared.json");
        }
        return new(pending.TargetName, outcome, detail, record, pending.Before, After: context, Restart: restart);
    }

    public static async Task<ComputerRenamePendingRecord> ReadPendingAsync(string path, CancellationToken token = default)
    {
        string fullPath = Path.GetFullPath(path);
        long length = new FileInfo(fullPath).Length;
        if (length is <= 0 or > 262_144) throw new InvalidDataException("Computer rename record size is invalid.");
        byte[] bytes = await File.ReadAllBytesAsync(fullPath, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                document.RootElement.EnumerateObject().GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() != 1))
                throw new InvalidDataException("Computer rename record contains conflicting fields.");
            var record = JsonSerializer.Deserialize<ComputerRenamePendingRecord>(bytes, new JsonSerializerOptions(Capture.JsonOptions) { RespectRequiredConstructorParameters = true });
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !record.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(record.JournalPath) ||
                !DriverReference.ValidText(record.OriginalName, 63) || record.OriginalName != record.OriginalName.Trim() ||
                !string.Equals(record.Before.CurrentName, record.OriginalName, StringComparison.OrdinalIgnoreCase) || record.Before.DomainRole is not (0 or 2))
                throw new InvalidDataException("Computer rename record is invalid.");
            string target = ComputerNameReview.Review(record.TargetName, new(null, null, null)).ProposedName;
            return record with { TargetName = target };
        }
        catch (JsonException error) { throw new InvalidDataException("Computer rename record is not valid JSON.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Computer rename target is invalid.", error); }
    }

    private static bool Eligible(ComputerNameContext context, string target, out string blocker)
    {
        blocker = context.CurrentName is null || context.ConfiguredName is null || context.RestartPending is null
            ? "Current or configured computer identity is incomplete. No rename was requested."
            : context.DomainRole is not (0 or 2)
                ? "Automatic rename is limited to standalone workgroup computers. Domain members and domain controllers require their dedicated administrative procedure."
                : context.RestartPending == true
                    ? "A computer-name change is already pending. Restart and reconcile it before another rename."
                    : string.Equals(context.CurrentName, target, StringComparison.OrdinalIgnoreCase)
                        ? "The exact target is already active. No rename is needed."
                        : "";
        return blocker.Length == 0;
    }

    private static bool HasClearRestartEvidence(WorkflowCheck restart) =>
        restart.State == CheckState.Clear && restart.EffectiveState == CheckState.Clear &&
        restart.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } evidence &&
        string.IsNullOrWhiteSpace(evidence.Error);

    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
        System.Security.SecurityException;

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan? timeout = null)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        var result = timeout is { } limit ? await pending.WaitAsync(limit, token).ConfigureAwait(false) : await pending.WaitAsync(token).ConfigureAwait(false);
        return result ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static async Task AppendRecordAsync(string path, object value, CancellationToken token)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Capture.JsonOptions) + "\n");
        await using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsComputerRenameHost : IComputerRenameHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<ComputerNameContext> ReadContextAsync(CancellationToken token) => ComputerNameCollector.ReadAsync(token);
    public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token) => Task.FromResult(RestartDiagnostics.Check());
    public Task<ToolResult> RenameAsync(string targetName, CancellationToken token) =>
        BoundedTool.RunAsync(ComputerRename.Step(targetName), TimeSpan.FromMinutes(2), token: token);
}
