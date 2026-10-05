using System.Text.Json;

namespace KillerBench.Engine.Installation;

public enum MsiRepairOutcome { Preview, Blocked, Verified, RestartRequired, Failed, Uncertain, Stopped }

public sealed record MsiRepairResult(string ProductCode, MsiRepairOutcome Outcome, string Detail,
    string? JournalPath = null, WorkflowCheck? Before = null, ToolResult? Command = null,
    WorkflowCheck? After = null, WorkflowCheck? Activity = null, WorkflowCheck? Restart = null)
{
    public int GetExitCode() => Outcome switch
    {
        MsiRepairOutcome.Preview or MsiRepairOutcome.Verified => 0,
        MsiRepairOutcome.RestartRequired => 4,
        MsiRepairOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IMsiRepairHost
{
    bool IsAdministrator { get; }
    Task<WorkflowCheck> ReadActivityAsync(CancellationToken token);
    Task<WorkflowCheck> ReadProductAsync(string productCode, CancellationToken token);
    Task<WorkflowCheck> ReadRestartAsync(CancellationToken token);
    Task<ToolResult> RepairAsync(string productCode, string logPath, CancellationToken token);
}

public static class MsiRepair
{
    public static WorkflowCheck AssessEligibility(string productCode, WorkflowCheck check)
    {
        string target = MsiProductDiagnostics.NormalizeProductCode(productCode);
        ArgumentNullException.ThrowIfNull(check);
        if (!MsiProductDiagnostics.TryGetReportedState(target, check, out int state) || state is not (0 or -6))
            return check;
        return check with
        {
            State = CheckState.Repairable,
            Repair = RepairKind.MsiProduct,
            Detail = check.Detail + " A guarded exact-product repair is available after separate confirmation."
        };
    }

    public static RepairStep Step(string productCode, string logPath)
    {
        string normalized = MsiProductDiagnostics.NormalizeProductCode(productCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
        return new("msiexec.exe", ["/fa", normalized, "/qn", "/norestart", "/L*V", Path.GetFullPath(logPath)], false);
    }

    public static async Task<MsiRepairResult> RunAsync(string productCode, bool execute, string journalFolder,
        IMsiRepairHost host, IProgress<string>? progress = null, TimeSpan? executionTimeout = null,
        CancellationToken token = default)
    {
        string target = MsiProductDiagnostics.NormalizeProductCode(productCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan repairLimit = executionTimeout ?? TimeSpan.FromMinutes(65);
        if (repairLimit <= TimeSpan.Zero || repairLimit > TimeSpan.FromHours(2))
            throw new ArgumentOutOfRangeException(nameof(executionTimeout));
        token.ThrowIfCancellationRequested();
        if (!execute)
            return new(target, MsiRepairOutcome.Preview,
                "Execution will recheck this exact product, Installer activity, restart state and administrator access. It will request a silent full-file MSI repair without restarting Windows, retain a verbose local log, then verify the same product registration. Product settings and user data may still be changed by the vendor package.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "msi-repair.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "msi-repair.pending.json");
        if (File.Exists(pendingPath))
            return new(target, MsiRepairOutcome.Blocked,
                "A prior MSI repair needs reconciliation before another repair can start.", pendingPath);

        Report(progress, "Checking Windows Installer activity.");
        var activity = await Required(host.ReadActivityAsync, "Installer activity", token).ConfigureAwait(false);
        if (!HasIdleInstallerEvidence(activity))
            return new(target, MsiRepairOutcome.Blocked,
                "Windows Installer activity is present or unavailable. Wait for known deployment work and recheck before repair.", Activity: activity);
        Report(progress, "Checking pending restart state.");
        var restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
        if (!HasClearRestartEvidence(restart))
            return new(target, MsiRepairOutcome.Blocked,
                "Restart state is pending or unavailable. Resolve it and recheck before MSI repair.", Activity: activity);
        Report(progress, "Checking exact MSI product registration.");
        var before = await Required(ct => host.ReadProductAsync(target, ct), "MSI product", token).ConfigureAwait(false);
        if (!MsiProductDiagnostics.TryGetReportedState(target, before, out int beforeState) || beforeState is not (0 or -6))
            return new(target, MsiRepairOutcome.Blocked,
                "Only an exact product currently reported as Broken or Bad configuration is eligible. Incomplete, missing-source, advertised, absent and healthy registrations are preserved.",
                Before: before, Activity: activity);
        if (!host.IsAdministrator)
            return new(target, MsiRepairOutcome.Blocked,
                "Administrator access is required for MSI repair. Fresh evidence was retained and no package action was requested.",
                Before: before, Activity: activity);

        string operationId = Guid.NewGuid().ToString("N");
        string journalPath = Path.Combine(root, "msi-repair-" + operationId + ".jsonl");
        string logPath = Path.Combine(root, "msi-repair-" + operationId + ".log");
        await using var journal = new FileStream(journalPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        async Task RecordAsync(object value, CancellationToken writeToken)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value) + "\n");
            await journal.WriteAsync(data, writeToken).ConfigureAwait(false);
            await journal.FlushAsync(writeToken).ConfigureAwait(false);
            journal.Flush(flushToDisk: true);
        }
        await RecordAsync(new { Stage = "Started", ProductCode = target, Before = before, Activity = activity, Restart = restart }, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new
        {
            Schema = 1, Machine = Environment.MachineName, ProductCode = target, StartedUtc = DateTimeOffset.UtcNow,
            JournalPath = journalPath, LogPath = logPath, Before = before
        }, Capture.JsonOptions), token: token).ConfigureAwait(false);
        await RecordAsync(new { Stage = "Prepared", ProductCode = target, LogPath = logPath }, token).ConfigureAwait(false);

        ToolResult? command = null;
        bool repairDispatched = false;
        MsiRepairResult result;
        try
        {
            Report(progress, "Repeating exact MSI repair eligibility immediately before command dispatch.");
            var immediateActivity = await Required(host.ReadActivityAsync, "Final Installer activity", token).ConfigureAwait(false);
            var immediateRestart = await Required(host.ReadRestartAsync, "Final restart state", token).ConfigureAwait(false);
            var immediateProduct = await Required(ct => host.ReadProductAsync(target, ct), "Final MSI product", token).ConfigureAwait(false);
            await RecordAsync(new
            {
                Stage = "FinalEligibility",
                Activity = immediateActivity,
                Restart = immediateRestart,
                Product = immediateProduct
            }, CancellationToken.None).ConfigureAwait(false);
            bool finalProductEligible = MsiProductDiagnostics.TryGetReportedState(target, immediateProduct,
                out int immediateState) && immediateState is 0 or -6;
            if (!HasIdleInstallerEvidence(immediateActivity) ||
                !HasClearRestartEvidence(immediateRestart) ||
                !finalProductEligible)
            {
                bool becameInstalled = MsiProductDiagnostics.TryGetReportedState(target, immediateProduct,
                    out int currentState) && currentState is 3 or 5;
                result = new(target, MsiRepairOutcome.Blocked,
                    becameInstalled
                        ? "The exact MSI product became installed before command dispatch. No package repair was requested."
                        : "Final Installer activity, restart or exact-product evidence no longer authorizes repair. No package repair was requested.",
                    journalPath, before, After: immediateProduct, Activity: immediateActivity, Restart: immediateRestart);
                await RecordAsync(new { Stage = "Finished", Result = result }, CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Repairing exact MSI product " + target + ".");
            repairDispatched = true;
            command = await Required(ct => host.RepairAsync(target, logPath, ct), "MSI repair", token, repairLimit).ConfigureAwait(false);
            await RecordAsync(new { Stage = "Command", Result = command }, CancellationToken.None).ConfigureAwait(false);
            if (command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
                result = new(target, MsiRepairOutcome.Uncertain,
                    "MSI repair command evidence is incomplete. The package may have changed; reconcile the pending operation before retrying.",
                    journalPath, before, command, Activity: immediateActivity, Restart: immediateRestart);
            else if (command.ExitCode == 3010)
                result = new(target, MsiRepairOutcome.RestartRequired,
                    "Windows Installer reports that the repair completed and a restart is required. Registration is not treated as verified until after restart and reconciliation.",
                    journalPath, before, command, Activity: immediateActivity, Restart: immediateRestart);
            else if (command.ExitCode != 0)
                result = new(target, MsiRepairOutcome.Failed,
                    "Windows Installer did not report a successful repair. Review the retained MSI log and reconcile current product state before retrying.",
                    journalPath, before, command, Activity: immediateActivity, Restart: immediateRestart);
            else
            {
                Report(progress, "Verifying exact MSI product registration.");
                var afterActivity = await Required(host.ReadActivityAsync, "Installer verification activity", token).ConfigureAwait(false);
                var after = HasIdleInstallerEvidence(afterActivity)
                    ? await Required(ct => host.ReadProductAsync(target, ct), "MSI product verification", token).ConfigureAwait(false)
                    : null;
                bool verified = after is not null && MsiProductDiagnostics.TryGetReportedState(target, after, out int afterState) &&
                    afterState is 3 or 5;
                result = new(target, verified ? MsiRepairOutcome.Verified : MsiRepairOutcome.Uncertain,
                    verified
                        ? "Windows Installer returned success and the exact product now reports a local or default installed state. Application functionality and user settings remain untested."
                        : "The repair command returned success, but fresh idle activity and an installed state for the exact product were not both verified.",
                    journalPath, before, command, after, afterActivity, immediateRestart);
            }
        }
        catch (OperationCanceledException error)
        {
            result = new(target, MsiRepairOutcome.Stopped,
                repairDispatched
                    ? "MSI repair was stopped after command dispatch. The package may still have changed; reconcile before another repair. " + error.Message
                    : "Final MSI eligibility inspection was stopped before command dispatch. No package repair was requested. " + error.Message,
                journalPath, before, command, Activity: activity);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or InvalidOperationException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            result = new(target, repairDispatched ? MsiRepairOutcome.Uncertain : MsiRepairOutcome.Blocked,
                repairDispatched
                    ? "MSI repair was interrupted after command dispatch: " + error.Message + ". Reconcile the pending operation before another repair."
                    : "Final MSI eligibility inspection was unavailable before command dispatch: " + error.Message + ". No package repair was requested.",
                journalPath, before, command, Activity: activity);
        }
        await RecordAsync(new { Stage = "Finished", Result = result }, CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == MsiRepairOutcome.Verified || !repairDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan? timeout = null)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        var result = timeout is { } limit
            ? await pending.WaitAsync(limit, token).ConfigureAwait(false)
            : await pending.WaitAsync(token).ConfigureAwait(false);
        return result ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    internal static bool HasCompleteEvidence(WorkflowCheck check) => check.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } evidence &&
        string.IsNullOrWhiteSpace(evidence.Error) && check.EffectiveState == check.State;

    internal static bool HasIdleInstallerEvidence(WorkflowCheck check) => check.State == CheckState.Information && HasCompleteEvidence(check) &&
        check.Evidence is { } evidence && InstallerActivityDiagnostics.Interpret(evidence).State == CheckState.Information;

    internal static bool HasClearRestartEvidence(WorkflowCheck check)
    {
        if (check.State != CheckState.Clear || !HasCompleteEvidence(check) || check.Evidence is not { } evidence) return false;
        return RestartDiagnostics.Interpret(evidence).State == CheckState.Clear;
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsMsiRepairHost : IMsiRepairHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<WorkflowCheck> ReadActivityAsync(CancellationToken token) => InstallerActivityDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadProductAsync(string productCode, CancellationToken token) => MsiProductDiagnostics.CheckAsync(productCode, token);
    public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token) => Task.FromResult(RestartDiagnostics.Check());
    public Task<ToolResult> RepairAsync(string productCode, string logPath, CancellationToken token) =>
        BoundedTool.RunAsync(MsiRepair.Step(productCode, logPath), TimeSpan.FromHours(1), token: token);
}
