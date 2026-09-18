using System.IO;
using KillerBench.Engine;

internal static class TaskRegistrationRecoveryChecks
{
    private static readonly ToolResult Good = new(0, "fixture", "", false);

    internal static async Task RunAsync(string scratch)
    {
        DateOnly startDate = new(2026, 9, 16);
        ScheduledTaskPlan plan = Plan();

        string absentRoot = Path.Combine(scratch, "task-recovery-absent");
        var absentHost = new RecoveryHost(plan.Identity, null, Good);
        var absent = await TaskRegistrationRecovery.RecoverAsync(plan, absentRoot, absentHost);
        Require(absent.Outcome == TaskRegistrationRecoveryOutcome.NoPendingRecord && absentHost.Reads == 0,
            "Recovery inspected Scheduler without a pending exact-target record.");

        string successRoot = Path.Combine(scratch, "task-recovery-success");
        await CreatePendingAsync(successRoot, plan, startDate, schema2: true);
        string exactXml = ScheduledTaskXml.Serialize(plan, startDate);
        var progress = new ImmediateProgress();
        var successHost = new RecoveryHost(plan.Identity, exactXml, Good);
        var success = await TaskRegistrationRecovery.RecoverAsync(plan, successRoot, successHost, progress: progress);
        Require(success.Outcome == TaskRegistrationRecoveryOutcome.Verified && successHost.Reads == 1 &&
            success.PendingRecord is not null && success.RecoveryRecord is not null &&
            File.Exists(success.PendingRecord) && File.Exists(success.RecoveryRecord) &&
            Directory.GetFiles(successRoot, "*.pending.json").Length == 0 &&
            progress.Messages.SequenceEqual(new[]
            {
                "Reading the pending task registration record.",
                "Reading the exact current disabled task definition without changing it.",
                "Locking and rechecking the unchanged pending record.",
                "Exact disabled definition verified. The pending guard was closed without retrying or running the task."
            }), "Exact recovery did not durably close the no-repeat guard.");

        string pendingPath = FileTaskRegistrationJournal.GetPendingPath(successRoot, plan.Identity);
        File.Move(success.PendingRecord!, pendingPath);
        var resumed = await TaskRegistrationRecovery.RecoverAsync(plan, successRoot,
            new RecoveryHost(plan.Identity, exactXml, Good), progress: new ThrowingProgress());
        Require(resumed.Outcome == TaskRegistrationRecoveryOutcome.Verified &&
            Directory.GetFiles(successRoot, "*.pending.json").Length == 0,
            "Recovery did not finish archival after a durable result survived an interruption.");

        string mismatchRoot = Path.Combine(scratch, "task-recovery-mismatch");
        await CreatePendingAsync(mismatchRoot, plan, startDate, schema2: true);
        var changedPlan = plan with { Action = new ScheduledTaskAction(plan.Action.Executable, "--changed") };
        var mismatch = await TaskRegistrationRecovery.RecoverAsync(plan, mismatchRoot,
            new RecoveryHost(plan.Identity, ScheduledTaskXml.Serialize(changedPlan, startDate), Good));
        Require(mismatch.Outcome == TaskRegistrationRecoveryOutcome.Conflict &&
            Directory.GetFiles(mismatchRoot, "*.pending.json").Length == 1,
            "A changed current definition closed the pending guard.");

        string unavailableRoot = Path.Combine(scratch, "task-recovery-unavailable");
        await CreatePendingAsync(unavailableRoot, plan, startDate, schema2: true);
        var unavailable = await TaskRegistrationRecovery.RecoverAsync(plan, unavailableRoot,
            new RecoveryHost(plan.Identity, null, new(-1, "", "unavailable", false)));
        Require(unavailable.Outcome == TaskRegistrationRecoveryOutcome.Inconclusive &&
            Directory.GetFiles(unavailableRoot, "*.pending.json").Length == 1,
            "Unavailable current evidence did not retain the pending guard.");

        string oldRoot = Path.Combine(scratch, "task-recovery-schema1");
        await CreatePendingAsync(oldRoot, plan, startDate, schema2: false);
        var oldHost = new RecoveryHost(plan.Identity, exactXml, Good);
        var old = await TaskRegistrationRecovery.RecoverAsync(plan, oldRoot, oldHost);
        Require(old.Outcome == TaskRegistrationRecoveryOutcome.Conflict && oldHost.Reads == 0 &&
            Directory.GetFiles(oldRoot, "*.pending.json").Length == 1,
            "A journal without an exact calendar boundary was treated as recoverable.");

        string planConflictRoot = Path.Combine(scratch, "task-recovery-plan-conflict");
        await CreatePendingAsync(planConflictRoot, plan, startDate, schema2: true);
        var planConflictHost = new RecoveryHost(plan.Identity, exactXml, Good);
        var planConflict = await TaskRegistrationRecovery.RecoverAsync(changedPlan, planConflictRoot, planConflictHost);
        Require(planConflict.Outcome == TaskRegistrationRecoveryOutcome.Conflict && planConflictHost.Reads == 0 &&
            Directory.GetFiles(planConflictRoot, "*.pending.json").Length == 1,
            "A different supplied plan advanced recovery.");

        string canceledRoot = Path.Combine(scratch, "task-recovery-canceled");
        await CreatePendingAsync(canceledRoot, plan, startDate, schema2: true);
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            try
            {
                await TaskRegistrationRecovery.RecoverAsync(plan, canceledRoot,
                    new RecoveryHost(plan.Identity, exactXml, Good), canceled.Token);
                throw new Exception("Canceled recovery proceeded.");
            }
            catch (OperationCanceledException) { }
        }
        Require(Directory.GetFiles(canceledRoot, "*.pending.json").Length == 1,
            "Cancellation removed the task registration guard.");

        string cliRoot = Path.Combine(scratch, "task-recovery-cli");
        await CreatePendingAsync(cliRoot, plan, startDate, schema2: true);
        string planPath = Path.Combine(scratch, "task-recovery-plan.json");
        await File.WriteAllBytesAsync(planPath, ScheduledTaskPlanJson.Serialize(plan));
        var cliHost = new RecoveryHost(plan.Identity, exactXml, Good);
        var cli = await InvokeCliAsync(["task-register-recover", planPath], cliHost, cliRoot);
        Require(cli.Exit == 0 && cliHost.Reads == 1 &&
            !cli.Json.RootElement.GetProperty("retryAttempted").GetBoolean() &&
            !cli.Json.RootElement.GetProperty("taskChanged").GetBoolean() &&
            !cli.Json.RootElement.GetProperty("taskExecuted").GetBoolean() &&
            cli.Json.RootElement.GetProperty("result").GetProperty("outcome").GetString() == "Verified",
            "CLI recovery did not preserve the read-only exact-definition boundary.");
        var rejectedCli = await InvokeCliAsync(["task-register-recover"],
            new RecoveryHost(plan.Identity, exactXml, Good), cliRoot);
        Require(rejectedCli.Exit == 2, "CLI recovery accepted a missing plan path.");

        Console.WriteLine("PASS: exact task registration recovery, durable resume, app-independent CLI, mismatch, old-schema, unavailable and cancellation boundaries; substituted providers only.");
    }

    private static ScheduledTaskPlan Plan() => new(
        new ScheduledTaskIdentity("\\Fixture\\", "Recovered"),
        new ScheduledTaskAction("C:\\Fixture\\signed-tool.exe", "--fixture"),
        new TaskScheduleOptions(TaskScheduleKind.Daily, new TimeOnly(9, 0)),
        new ScheduledTaskPrincipal(ScheduledTaskAccountKind.LocalSystem, "S-1-5-18", true),
        TimeSpan.FromMinutes(30))
    { TimeZoneId = TimeZoneInfo.Local.Id };

    private static async Task CreatePendingAsync(string root, ScheduledTaskPlan plan, DateOnly startDate, bool schema2)
    {
        var result = await TaskRegistration.RunAsync(plan, new RegistrationHost(plan), true,
            journal: new FileTaskRegistrationJournal(root, schema2 ? startDate : null));
        Require(result.Status == "Unverified" && result.CreateAttempted &&
            Directory.GetFiles(root, "*.pending.json").Length == 1,
            "Fixture did not create one interrupted registration guard.");
    }

    private static async Task<(int Exit, System.Text.Json.JsonDocument Json)> InvokeCliAsync(string[] args,
        RecoveryHost host, string journalDirectory)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        TextWriter originalOutput = Console.Out;
        TextWriter originalError = Console.Error;
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            int exit = await TaskPlanCommands.RecoverRegistrationAsync(args, () => host, journalDirectory);
            string json = output.ToString();
            return (exit, System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json));
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    private sealed class RegistrationHost(ScheduledTaskPlan plan) : ITaskRegistrationHost
    {
        public Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan requested, CancellationToken token) =>
            Task.FromResult(new TaskRegistrationPreflight(plan.Identity, true, "fixture", Good));
        public Task<ToolResult> CreateNewAsync(ScheduledTaskPlan requested, CancellationToken token) =>
            Task.FromResult(new ToolResult(-1, "", "synthetic interrupted creation", false));
        public Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token) =>
            throw new Exception("Read-back should not run after incomplete creation evidence.");
    }

    private sealed class RecoveryHost(ScheduledTaskIdentity target, string? xml, ToolResult evidence) : ITaskRegistrationRecoveryHost
    {
        public int Reads { get; private set; }
        public Task<ScheduledTaskDefinitionResult> ReadAsync(ScheduledTaskIdentity requested, CancellationToken token)
        {
            Reads++;
            return Task.FromResult(new ScheduledTaskDefinitionResult(target, xml, "fixture", evidence));
        }
    }

    private sealed class ImmediateProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];
        public void Report(string value) => Messages.Add(value);
    }

    private sealed class ThrowingProgress : IProgress<string>
    {
        public void Report(string value) => throw new InvalidOperationException("fixture observer failure");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
