using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class TaskPlanCommandChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string path = Path.Combine(scratch, "partial-task-plan.json");
        await File.WriteAllTextAsync(path, """
            {"identity":{"path":"\\Fixture\\","name":"Review"},"action":{"executable":"C:\\Fixture\\tool.exe","arguments":""},"schedule":{"kind":"Startup"},"principal":{"kind":"LocalSystem","userId":"S-1-5-18","highestPrivileges":true},"executionLimit":"00:01:00"}
            """);
        foreach (Exception? failure in new Exception?[] { new OperationCanceledException(), new IOException("Fixture read failure"), null })
        {
            var output = new StringWriter();
            var error = new StringWriter();
            var originalOutput = Console.Out;
            var originalError = Console.Error;
            int exit;
            try
            {
                Console.SetOut(output);
                Console.SetError(error);
                exit = await TaskPlanCommands.RunAsync(["task-plan", path, "--resolve-account", "--inspect-executable"], new()
                {
                    Account = (principal, _) => Task.FromResult(new ScheduledTaskAccountResult(principal.UserId, "S-1-5-18", "Fixture identity", new(0, "fixture evidence", "", false))),
                    Executable = (action, _) => failure is not null ? Task.FromException<ScheduledTaskExecutableResult>(failure) :
                        Task.FromResult(new ScheduledTaskExecutableResult(action.Executable, 3, "fixture", "Fixture image") { HasPeHeader = true, IsDll = false })
                });
            }
            finally { Console.SetOut(originalOutput); Console.SetError(originalError); }
            using var report = JsonDocument.Parse(output.ToString());
            var root = report.RootElement;
            if (exit != (failure is OperationCanceledException ? 130 : failure is null ? 0 : 1) ||
                root.GetProperty("incomplete").GetBoolean() != (failure is not null) || root.GetProperty("taskRegistered").GetBoolean() ||
                root.GetProperty("account").GetProperty("sid").GetString() != "S-1-5-18" ||
                root.GetProperty("account").GetProperty("evidence").GetProperty("output").GetString() != "fixture evidence")
                throw new Exception("Task-plan command lost completed evidence or misreported partial outcome");
            if (failure is not null && (!root.TryGetProperty("failure", out var message) || string.IsNullOrWhiteSpace(message.GetString())))
                throw new Exception("Incomplete task-plan report omitted failure detail");
        }
        var plan = await ScheduledTaskPlanJson.ReadFileAsync(path);
        var previewHost = new RegistrationHost(plan, true);
        var previewJournal = new RegistrationJournal();
        var preview = await InvokeRegistrationAsync(["task-register", path, "2026-09-14"], previewHost, previewJournal);
        if (preview.Exit != 0 || previewHost.Checks != 0 || previewHost.Creates != 0 || previewHost.Reads != 0 ||
            previewJournal.Prepares != 0 || previewJournal.Completes != 0 ||
            !preview.Json.RootElement.GetProperty("previewOnly").GetBoolean() ||
            preview.Json.RootElement.GetProperty("result").GetProperty("status").GetString() != "Preview")
            throw new Exception("Task registration preview performed work or misreported its result.");

        string previewReportPath = Path.Combine(scratch, "task-registration-preview.txt");
        var previewReportHost = new RegistrationHost(plan, true);
        var previewReportJournal = new RegistrationJournal();
        var previewWithReport = await InvokeRegistrationAsync(
            ["task-register", path, "2026-09-14", "--report", previewReportPath], previewReportHost, previewReportJournal);
        if (previewWithReport.Exit != 0 || previewReportHost.Checks != 0 || previewReportHost.Creates != 0 ||
            previewReportJournal.Prepares != 0 || !File.Exists(previewReportPath) ||
            !File.ReadAllText(previewReportPath).Contains("Task registration preview", StringComparison.Ordinal) ||
            previewWithReport.Json.RootElement.GetProperty("reportPath").GetString() != previewReportPath)
            throw new Exception("Task registration preview report changed state or omitted its readable outcome.");

        var executeHost = new RegistrationHost(plan, true);
        var executeJournal = new RegistrationJournal();
        var executed = await InvokeRegistrationAsync(["task-register", path, "2026-09-14", "--execute"], executeHost, executeJournal);
        if (executed.Exit != 0 || executeHost.Checks != 2 || executeHost.Creates != 1 || executeHost.Reads != 1 ||
            executeJournal.Prepares != 1 || executeJournal.Completes != 1 || executeJournal.Abandons != 0 ||
            executed.Json.RootElement.GetProperty("previewOnly").GetBoolean() ||
            executed.Json.RootElement.GetProperty("definitionEnabled").GetBoolean() ||
            executed.Json.RootElement.GetProperty("taskExecuted").GetBoolean() ||
            executed.Json.RootElement.GetProperty("result").GetProperty("status").GetString() != "Registered")
            throw new Exception("Task registration CLI did not preserve guarded coordinator sequencing.");

        var blockedHost = new RegistrationHost(plan, false);
        var blockedJournal = new RegistrationJournal();
        var blocked = await InvokeRegistrationAsync(["task-register", path, "2026-09-14", "--execute"], blockedHost, blockedJournal);
        if (blocked.Exit != 3 || blockedHost.Checks != 1 || blockedHost.Creates != 0 || blockedHost.Reads != 0 ||
            blockedJournal.Prepares != 0 || blocked.Json.RootElement.GetProperty("result").GetProperty("status").GetString() != "Blocked")
            throw new Exception("Blocked task registration reached mutation or returned the wrong exit code.");

        foreach (string[] invalid in new[]
        {
            new[] { "task-register", path },
            new[] { "task-register", path, "09/14/2026" },
            new[] { "task-register", path, "2026-09-14", "--force" },
            new[] { "task-register", path, "2026-09-14", "--report", Path.Combine(scratch, "invalid-report.json") }
        })
        {
            var rejected = await InvokeRegistrationAsync(invalid, new RegistrationHost(plan, true), new RegistrationJournal());
            if (rejected.Exit != 2) throw new Exception("Invalid task registration arguments were accepted.");
        }
        Console.WriteLine("PASS: task-plan review and guarded task-register CLI sequencing use substituted providers only.");
    }

    private static async Task<(int Exit, JsonDocument Json)> InvokeRegistrationAsync(string[] args,
        RegistrationHost host, RegistrationJournal journal)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            int exit = await TaskPlanCommands.RegisterAsync(args, _ => host, _ => journal);
            return (exit, string.IsNullOrWhiteSpace(output.ToString()) ? JsonDocument.Parse("{}") : JsonDocument.Parse(output.ToString()));
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    private sealed class RegistrationHost(ScheduledTaskPlan plan, bool eligible) : ITaskRegistrationHost
    {
        private static readonly ToolResult Good = new(0, "fixture", "", false);
        internal int Checks { get; private set; }
        internal int Creates { get; private set; }
        internal int Reads { get; private set; }

        public Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan requested, CancellationToken token)
        {
            Checks++;
            return Task.FromResult(new TaskRegistrationPreflight(plan.Identity, eligible, "fixture", Good));
        }

        public Task<ToolResult> CreateNewAsync(ScheduledTaskPlan requested, CancellationToken token)
        {
            Creates++;
            return Task.FromResult(Good);
        }

        public Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token)
        {
            Reads++;
            return Task.FromResult(new TaskRegistrationVerification(plan, Good));
        }
    }

    private sealed class RegistrationJournal : ITaskRegistrationJournal
    {
        internal int Prepares { get; private set; }
        internal int Completes { get; private set; }
        internal int Abandons { get; private set; }
        public Task PrepareAsync(ScheduledTaskPlan plan, TaskRegistrationPreflight preflight, CancellationToken token)
        { Prepares++; return Task.CompletedTask; }
        public Task CompleteAsync(TaskRegistrationVerification verification, CancellationToken token)
        { Completes++; return Task.CompletedTask; }
        public Task AbandonAsync(string detail, CancellationToken token)
        { Abandons++; return Task.CompletedTask; }
    }
}
