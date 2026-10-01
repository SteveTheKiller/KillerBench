using System.Text.Json;

namespace KillerBench.Engine.Installation;

public enum InstallerRegistrationRepairOutcome { Preview, Blocked, Verified, RestartRequired, Failed, Uncertain, Stopped }
public enum InstallerRegistrationRepairAction { None, Reconcile, Repair }

public sealed record InstallerRegistrationRepairResult(InstallerRegistrationRepairOutcome Outcome, string Detail,
    string? JournalPath = null, WorkflowCheck? Before = null, WorkflowCheck? EngineFiles = null,
    WorkflowCheck? Activity = null, WorkflowCheck? Restart = null, IReadOnlyList<ToolResult>? Commands = null,
    WorkflowCheck? After = null)
{
    public int GetExitCode() => Outcome switch
    {
        InstallerRegistrationRepairOutcome.Preview or InstallerRegistrationRepairOutcome.Verified => 0,
        InstallerRegistrationRepairOutcome.RestartRequired => 4,
        InstallerRegistrationRepairOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IInstallerRegistrationRepairHost
{
    bool IsAdministrator { get; }
    Task<WorkflowCheck> ReadActivityAsync(CancellationToken token);
    Task<WorkflowCheck> ReadRestartAsync(CancellationToken token);
    Task<WorkflowCheck> ReadEngineFilesAsync(CancellationToken token);
    Task<WorkflowCheck> ReadRegistrationAsync(CancellationToken token);
    Task<IReadOnlyList<ToolResult>> RegisterAsync(IReadOnlyList<RepairStep> steps, CancellationToken token);
}

public static class InstallerRegistrationRepair
{
    public static IReadOnlyList<RepairStep> Steps()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Installer registration requires Windows.");
        var steps = new List<RepairStep>
        {
            new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msiexec.exe"), ["/regserver"], false)
        };
        if (Environment.Is64BitOperatingSystem)
        {
            string wow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "msiexec.exe");
            if (File.Exists(wow)) steps.Add(new(wow, ["/regserver"], false));
        }
        return steps.AsReadOnly();
    }

    public static async Task<InstallerRegistrationRepairResult> RunAsync(bool execute, string journalFolder,
        IInstallerRegistrationRepairHost host, IProgress<string>? progress = null, CancellationToken token = default,
        TimeSpan? executionTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = executionTimeout ?? TimeSpan.FromMinutes(5);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(15)) throw new ArgumentOutOfRangeException(nameof(executionTimeout));
        token.ThrowIfCancellationRequested();
        if (!execute)
            return new(InstallerRegistrationRepairOutcome.Preview,
                "Execution will require trusted Windows Installer engine files, idle Installer activity, clear restart evidence, administrator access and a fresh exact service-registration defect. It will run /regserver for the available native Installer architectures without unregistering either engine, then verify the exact service registration. Packages and user data are not selected.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "installer-registration-repair.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "installer-registration-repair.pending.json");
        if (File.Exists(pendingPath))
            return new(InstallerRegistrationRepairOutcome.Blocked,
                "A prior Installer registration repair needs reconciliation before another repair can start.", pendingPath);

        Report(progress, "Checking Windows Installer activity.");
        var activity = await Required(host.ReadActivityAsync, "Installer activity", token).ConfigureAwait(false);
        if (!MsiRepair.HasIdleInstallerEvidence(activity))
            return Blocked("Windows Installer activity is present or unavailable. Wait for known deployment work and recheck before registration repair.", activity: activity);
        Report(progress, "Checking pending restart state.");
        var restart = await Required(host.ReadRestartAsync, "Restart state", token).ConfigureAwait(false);
        if (!MsiRepair.HasClearRestartEvidence(restart))
            return Blocked("Restart state is pending or unavailable. Resolve it and recheck before registration repair.", activity: activity, restart: restart);
        Report(progress, "Checking Windows Installer engine files.");
        var files = await Required(host.ReadEngineFilesAsync, "Installer engine files", token).ConfigureAwait(false);
        if (files.State != CheckState.Information || !TrustedEngineFiles(files))
            return Blocked("Every available Windows Installer engine file must be present with Valid Authenticode status before registration repair.", files, activity, restart);
        Report(progress, "Checking exact Windows Installer service registration.");
        var before = await Required(host.ReadRegistrationAsync, "Installer service registration", token).ConfigureAwait(false);
        if (!HasExactRegistrationEvidence(before, CheckState.Attention))
            return Blocked(before.State == CheckState.Information
                ? "Windows Installer service registration already matches the expected configuration. No repair is needed."
                : "Only a complete exact service-registration defect is eligible for this repair.", files, activity, restart, before);
        if (!host.IsAdministrator)
            return Blocked("Administrator access is required for Installer registration repair. Fresh evidence was retained and no command was requested.", files, activity, restart, before);

        IReadOnlyList<RepairStep> steps = Steps();
        string operationId = Guid.NewGuid().ToString("N");
        string journalPath = Path.Combine(root, "installer-registration-repair-" + operationId + ".jsonl");
        await using var journal = new FileStream(journalPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        async Task RecordAsync(object value, CancellationToken writeToken)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value) + "\n");
            await journal.WriteAsync(data, writeToken).ConfigureAwait(false);
            await journal.FlushAsync(writeToken).ConfigureAwait(false);
            journal.Flush(flushToDisk: true);
        }
        await RecordAsync(new { Stage = "Started", Before = before, EngineFiles = files, Activity = activity, Restart = restart }, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new InstallerRegistrationRepairPendingRecord(1,
            Environment.MachineName, DateTimeOffset.UtcNow, journalPath, before, files), Capture.JsonOptions), token).ConfigureAwait(false);
        await RecordAsync(new { Stage = "Prepared", Architectures = steps.Count }, token).ConfigureAwait(false);

        IReadOnlyList<ToolResult>? commands = null;
        bool registrationDispatched = false;
        InstallerRegistrationRepairResult result;
        try
        {
            Report(progress, "Repeating Installer eligibility immediately before registration.");
            var immediateActivity = await Required(host.ReadActivityAsync, "Final Installer activity", token).ConfigureAwait(false);
            var immediateRestart = await Required(host.ReadRestartAsync, "Final restart state", token).ConfigureAwait(false);
            var immediateFiles = await Required(host.ReadEngineFilesAsync, "Final Installer engine files", token).ConfigureAwait(false);
            var immediateRegistration = await Required(host.ReadRegistrationAsync, "Final Installer service registration", token).ConfigureAwait(false);
            await RecordAsync(new
            {
                Stage = "FinalEligibility",
                Activity = immediateActivity,
                Restart = immediateRestart,
                EngineFiles = immediateFiles,
                Registration = immediateRegistration
            }, CancellationToken.None).ConfigureAwait(false);
            if (!MsiRepair.HasIdleInstallerEvidence(immediateActivity) ||
                !MsiRepair.HasClearRestartEvidence(immediateRestart) ||
                immediateFiles.State != CheckState.Information || !TrustedEngineFiles(immediateFiles) ||
                !HasExactRegistrationEvidence(immediateRegistration, CheckState.Attention))
            {
                result = new(InstallerRegistrationRepairOutcome.Blocked,
                    HasExactRegistrationEvidence(immediateRegistration, CheckState.Information)
                        ? "Windows Installer registration became healthy before command dispatch. No registration command was requested."
                        : "Final Installer activity, restart, engine-file or registration evidence no longer authorizes repair. No registration command was requested.",
                    journalPath, before, immediateFiles, immediateActivity, immediateRestart, After: immediateRegistration);
                await RecordAsync(new { Stage = "Finished", Result = result }, CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Registering available Windows Installer engine architectures.");
            registrationDispatched = true;
            commands = await Required(ct => host.RegisterAsync(steps, ct), "Installer registration", token, limit).ConfigureAwait(false);
            await RecordAsync(new { Stage = "Commands", Results = commands }, CancellationToken.None).ConfigureAwait(false);
            if (commands.Count != steps.Count || commands.Any(command => command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error)))
                result = new(InstallerRegistrationRepairOutcome.Uncertain,
                    "Installer registration command evidence is incomplete. Registration may have changed; reconcile before retrying.",
                    journalPath, before, files, activity, restart, commands);
            else if (commands.Any(command => command.ExitCode == 3010))
                result = new(InstallerRegistrationRepairOutcome.RestartRequired,
                    "Windows Installer registration reported that a restart is required. Registration remains unverified until later reconciliation.",
                    journalPath, before, files, activity, restart, commands);
            else if (commands.Any(command => command.ExitCode != 0))
                result = new(InstallerRegistrationRepairOutcome.Failed,
                    "One or more Windows Installer registration commands failed. Current registration must be reconciled before another attempt.",
                    journalPath, before, files, activity, restart, commands);
            else
            {
                Report(progress, "Verifying exact Windows Installer service registration.");
                var afterActivity = await Required(host.ReadActivityAsync, "Installer verification activity", token).ConfigureAwait(false);
                var after = MsiRepair.HasIdleInstallerEvidence(afterActivity)
                    ? await Required(host.ReadRegistrationAsync, "Installer registration verification", token).ConfigureAwait(false)
                    : null;
                bool verified = after is not null && HasExactRegistrationEvidence(after, CheckState.Information);
                result = new(verified ? InstallerRegistrationRepairOutcome.Verified : InstallerRegistrationRepairOutcome.Uncertain,
                    verified
                        ? "The registration commands returned success and the exact Windows Installer service registration now matches the expected configuration. Package installation and application behavior remain untested."
                        : "The registration commands returned success, but fresh idle activity and the exact expected service registration were not both verified.",
                    journalPath, before, files, afterActivity, restart, commands, after);
            }
        }
        catch (OperationCanceledException error)
        {
            result = new(InstallerRegistrationRepairOutcome.Stopped,
                registrationDispatched
                    ? "Installer registration repair stopped after command dispatch. Registration may have changed; reconcile before another attempt. " + error.Message
                    : "Final Installer eligibility inspection stopped before command dispatch. No registration command was requested. " + error.Message,
                journalPath, before, files, activity, restart, commands);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or InvalidOperationException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            result = new(registrationDispatched ? InstallerRegistrationRepairOutcome.Uncertain : InstallerRegistrationRepairOutcome.Blocked,
                registrationDispatched
                    ? "Installer registration repair was interrupted after command dispatch: " + error.Message + ". Reconcile before another attempt."
                    : "Final Installer eligibility inspection was unavailable before command dispatch: " + error.Message + ". No registration command was requested.",
                journalPath, before, files, activity, restart, commands);
        }
        await RecordAsync(new { Stage = "Finished", Result = result }, CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == InstallerRegistrationRepairOutcome.Verified || !registrationDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    private static InstallerRegistrationRepairResult Blocked(string detail, WorkflowCheck? files = null,
        WorkflowCheck? activity = null, WorkflowCheck? restart = null, WorkflowCheck? before = null) =>
        new(InstallerRegistrationRepairOutcome.Blocked, detail, Before: before, EngineFiles: files, Activity: activity, Restart: restart);

    public static bool IsEligible(WorkflowCheck check) =>
        HasExactRegistrationEvidence(check, CheckState.Attention);

    public static InstallerRegistrationRepairAction SelectAction(bool automaticRepair,
        bool pendingRecordPresent, IEnumerable<WorkflowCheck> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);
        if (!automaticRepair) return InstallerRegistrationRepairAction.None;
        if (pendingRecordPresent) return InstallerRegistrationRepairAction.Reconcile;
        return checks.Any(IsEligible)
            ? InstallerRegistrationRepairAction.Repair
            : InstallerRegistrationRepairAction.None;
    }

    internal static bool TrustedEngineFiles(WorkflowCheck check)
    {
        if (!HasCompleteEvidence(check) || check.State != CheckState.Information || check.Evidence is null) return false;
        try
        {
            var snapshot = InstallerEngineFileDiagnostics.Parse(check.Evidence.Output);
            return snapshot.Complete && snapshot.Files.All(file => file.Exists && file.Signature == "Valid");
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException) { return false; }
    }

    internal static bool HasExactRegistrationEvidence(WorkflowCheck check, CheckState expected)
    {
        if (!HasCompleteEvidence(check) || check.State != expected ||
            check.Evidence is null || check.Name != "Windows Installer service registration") return false;
        try
        {
            var snapshot = JsonSerializer.Deserialize<InstallerServiceRegistrationSnapshot>(check.Evidence.Output,
                Capture.JsonOptions);
            if (snapshot is null) return false;
            var assessed = InstallerServiceRegistrationDiagnostics.Assess(snapshot);
            return assessed.State == expected && assessed.EffectiveState == expected;
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return false;
        }
    }

    internal static bool HasCompleteEvidence(WorkflowCheck check) => check.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } evidence &&
        string.IsNullOrWhiteSpace(evidence.Error) && check.EffectiveState == check.State;

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan? timeout = null)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        var result = timeout is { } limit
            ? await pending.WaitAsync(limit, token).ConfigureAwait(false)
            : await pending.WaitAsync(token).ConfigureAwait(false);
        return result ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsInstallerRegistrationRepairHost : IInstallerRegistrationRepairHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<WorkflowCheck> ReadActivityAsync(CancellationToken token) => InstallerActivityDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token) => Task.FromResult(RestartDiagnostics.Check());
    public Task<WorkflowCheck> ReadEngineFilesAsync(CancellationToken token) => InstallerEngineFileDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadRegistrationAsync(CancellationToken token) => Task.FromResult(InstallerServiceRegistrationDiagnostics.Check());
    public async Task<IReadOnlyList<ToolResult>> RegisterAsync(IReadOnlyList<RepairStep> steps, CancellationToken token)
    {
        var results = new List<ToolResult>(steps.Count);
        foreach (var step in steps)
        {
            token.ThrowIfCancellationRequested();
            results.Add(await BoundedTool.RunAsync(step, TimeSpan.FromMinutes(2), token: token).ConfigureAwait(false));
        }
        return results.AsReadOnly();
    }
}
