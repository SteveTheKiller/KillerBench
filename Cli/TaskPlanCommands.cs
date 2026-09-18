using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class TaskPlanCommands
{
    internal sealed class Readers
    {
        internal Func<ScheduledTaskPrincipal, CancellationToken, Task<ScheduledTaskAccountResult>> Account { get; init; } = ScheduledTaskAccount.ResolveAsync;
        internal Func<ScheduledTaskAction, CancellationToken, Task<ScheduledTaskExecutableResult>> Executable { get; init; } = ScheduledTaskExecutable.InspectAsync;
    }
    internal static async Task<int> ExportXmlAsync(string[] args)
    {
        if (args.Length != 3 || !DateOnly.TryParseExact(args[2], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var start))
        { Console.Error.WriteLine("Usage: killerbench-cli task-xml PLAN.json YYYY-MM-DD (disabled review definition only)"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            var plan = await ScheduledTaskPlanJson.ReadFileAsync(args[1], stop.Token);
            Console.WriteLine(ScheduledTaskXml.Serialize(plan, start));
            return 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Task XML export timed out."); return 130; }
        catch (Exception error) when (error is ArgumentException or JsonException or NotSupportedException)
        { Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { Console.Error.WriteLine(error.Message); return 1; }
    }

    internal static async Task<int> RegisterAsync(string[] args,
        Func<DateOnly, ITaskRegistrationHost>? hostFactory = null,
        Func<string, ITaskRegistrationJournal>? journalFactory = null)
    {
        bool execute = false;
        string? reportPath = null;
        var positional = new List<string>();
        for (int index = 1; index < args.Length; index++)
        {
            if (args[index] == "--execute" && !execute) { execute = true; continue; }
            if (args[index] == "--report" && reportPath is null && index + 1 < args.Length)
            { reportPath = args[++index]; continue; }
            positional.Add(args[index]);
        }
        if (args.Length < 3 || args[0] != "task-register" || positional.Count != 2 ||
            !DateOnly.TryParseExact(positional[1], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var start))
        {
            Console.Error.WriteLine("Usage: killerbench-cli task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt] (preview by default; disabled create-only Local System definition)");
            return 2;
        }
        if (!TryValidateReportPath(reportPath, out string? reportError)) { Console.Error.WriteLine(reportError); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var plan = await ScheduledTaskPlanJson.ReadFileAsync(positional[0], stop.Token);
            var host = (hostFactory ?? (date => new WindowsTaskRegistrationHost(date)))(start)
                ?? throw new InvalidOperationException("Task registration host is unavailable.");
            string journalDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "TaskRegistration");
            var journal = (journalFactory ?? (directory => new FileTaskRegistrationJournal(directory, start)))(journalDirectory)
                ?? throw new InvalidOperationException("Task registration journal is unavailable.");
            var result = await TaskRegistration.RunAsync(plan, host, execute, stop.Token, journal,
                progress: new Progress<string>(Console.Error.WriteLine));
            if (reportPath is not null)
            {
                await ExportTaskRegistrationReportAsync(plan, result, execute, reportPath).ConfigureAwait(false);
                Console.Error.WriteLine("Readable report saved: " + Path.GetFullPath(reportPath));
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                previewOnly = !execute,
                definitionEnabled = false,
                taskExecuted = false,
                target = plan.Identity.FullName,
                reportPath,
                result
            }, Capture.JsonOptions));
            return result.Status switch
            {
                "Preview" or "Registered" => 0,
                "Canceled" or "Timed out" => 130,
                "Blocked" or "Unverified" => 3,
                _ => 3
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Task registration canceled or timed out.");
            return 130;
        }
        catch (Exception error) when (error is ArgumentException or JsonException)
        {
            Console.Error.WriteLine(error.Message);
            return 2;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static bool TryValidateReportPath(string? reportPath, out string? error)
    {
        error = null;
        if (reportPath is null) return true;
        string extension = Path.GetExtension(reportPath);
        if (extension is not (".html" or ".txt")) { error = "--report output must use .html or .txt."; return false; }
        if (File.Exists(reportPath)) { error = "--report will not overwrite an existing file."; return false; }
        return true;
    }

    private static async Task ExportTaskRegistrationReportAsync(ScheduledTaskPlan plan, TaskRegistrationResult result,
        bool execute, string reportPath)
    {
        bool registered = result.Status == "Registered";
        bool preview = result.Status == "Preview";
        var report = new WorkflowResult(WorkflowKind.ScheduledTasks, Environment.MachineName, DateTimeOffset.UtcNow,
            registered ? "Task registration verified" : preview ? "Task registration preview" : "Task registration requires review",
            [new WorkflowCheck("Disabled task registration", registered || preview ? CheckState.Information : CheckState.Unknown,
                plan.Identity.FullName + ": " + result.Detail),
             new WorkflowCheck("Change boundary", CheckState.Information,
                "The task definition remains disabled and was not run.")], [],
            "Task registration command. Review the JSON output and local recovery records for exact evidence.");
        string content = Path.GetExtension(reportPath) == ".html"
            ? WorkflowReportExport.BuildHtml(report, execute ? WorkflowReportScope.DiagnosticsAndRepairs : WorkflowReportScope.Diagnostics)
            : WorkflowReportExport.BuildText(report, execute ? WorkflowReportScope.DiagnosticsAndRepairs : WorkflowReportScope.Diagnostics);
        await File.WriteAllTextAsync(reportPath, content).ConfigureAwait(false);
    }

    internal static async Task<int> RecoverRegistrationAsync(string[] args,
        Func<ITaskRegistrationRecoveryHost>? hostFactory = null,
        string? journalDirectory = null)
    {
        if (args.Length != 2 || args[0] != "task-register-recover")
        {
            Console.Error.WriteLine("Usage: killerbench-cli task-register-recover PLAN.json (read-only exact-definition reconciliation; never retries or runs a task)");
            return 2;
        }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var plan = await ScheduledTaskPlanJson.ReadFileAsync(args[1], stop.Token);
            journalDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "TaskRegistration");
            var host = (hostFactory ?? (() => new WindowsTaskRegistrationRecoveryHost()))()
                ?? throw new InvalidOperationException("Task registration recovery host is unavailable.");
            var result = await TaskRegistrationRecovery.RecoverAsync(plan, journalDirectory, host, stop.Token,
                progress: new Progress<string>(Console.Error.WriteLine));
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                retryAttempted = false,
                taskChanged = false,
                taskExecuted = false,
                target = plan.Identity.FullName,
                result
            }, Capture.JsonOptions));
            return result.Outcome switch
            {
                TaskRegistrationRecoveryOutcome.Verified or TaskRegistrationRecoveryOutcome.NoPendingRecord => 0,
                TaskRegistrationRecoveryOutcome.Inconclusive or TaskRegistrationRecoveryOutcome.Conflict => 3,
                _ => 3
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Task registration recovery canceled or timed out. The pending guard remains in place.");
            return 130;
        }
        catch (Exception error) when (error is ArgumentException or JsonException)
        {
            Console.Error.WriteLine(error.Message);
            return 2;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    internal static async Task<int> RunAsync(string[] args, Readers? readers = null)
    {
        var options = args.Skip(2).ToArray();
        if (args.Length is < 2 or > 6 || args[0] != "task-plan" || options.Distinct().Count() != options.Length ||
            options.Any(option => option is not ("--check-conflicts" or "--inspect-executable" or "--resolve-account" or "--inspect-signature")))
        { Console.Error.WriteLine("Usage: killerbench-cli task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature] (review only)"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        readers ??= new();
        ScheduledTaskPlan? plan = null;
        ScheduledTaskConflictResult? conflict = null;
        ToolResult? evidence = null;
        ScheduledTaskExecutableResult? executable = null;
        ScheduledTaskAccountResult? account = null;
        FileSignatureResult? signature = null;
        ScheduledTaskDefinitionResult? existingDefinition = null;
        void WriteReview(bool incomplete, string? failure = null)
        {
            if (plan is null) return;
            Console.WriteLine(JsonSerializer.Serialize(new { previewOnly = true, taskRegistered = false, incomplete, failure, plan,
                review = ScheduledTaskReview.Format(plan, DateTimeOffset.UtcNow),
                conflict, evidence, executable, account, signature, existingDefinition,
                limitation = "Review draft only. Missing observations are not successful checks. Account, executable trust, time-zone binding and registration eligibility are not verified." }, Capture.JsonOptions));
        }
        try
        {
            plan = await ScheduledTaskPlanJson.ReadFileAsync(args[1], stop.Token);
            if (options.Contains("--inspect-signature")) signature = await FileSignature.InspectAsync(plan.Action, stop.Token);
            if (options.Contains("--resolve-account")) account = await readers.Account(plan.Principal, stop.Token);
            if (options.Contains("--inspect-executable"))
                executable = await readers.Executable(plan.Action, stop.Token);
            if (options.Contains("--check-conflicts"))
            {
                var inventory = await ScheduledTaskInventory.ReadAsync(stop.Token);
                conflict = ScheduledTaskConflict.Review(plan.Identity, inventory);
                evidence = inventory.Evidence;
                if (conflict.State == ScheduledTaskConflictState.ExistingTask)
                    existingDefinition = await ScheduledTaskDefinition.ReadAsync(plan.Identity, stop.Token);
            }
            WriteReview(false);
            return signature is not null && signature.Status != "Valid" || account is { Sid: null } || executable is { RequiresReview: true } || conflict?.State is ScheduledTaskConflictState.ExistingTask or ScheduledTaskConflictState.Unknown ? 3 : 0;
        }
        catch (OperationCanceledException) { WriteReview(true, "Task-plan review canceled or timed out."); Console.Error.WriteLine("Task-plan review canceled or timed out."); return 130; }
        catch (Exception error) when (error is ArgumentException or JsonException)
        { WriteReview(true, error.Message); Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { WriteReview(true, error.Message); Console.Error.WriteLine(error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
