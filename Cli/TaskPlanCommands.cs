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
        bool execute = args.Length == 4 && args[3] == "--execute";
        if (args.Length is < 3 or > 4 || args[0] != "task-register" || args.Length == 4 && !execute ||
            !DateOnly.TryParseExact(args[2], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var start))
        {
            Console.Error.WriteLine("Usage: killerbench-cli task-register PLAN.json YYYY-MM-DD [--execute] (preview by default; disabled create-only Local System definition)");
            return 2;
        }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var plan = await ScheduledTaskPlanJson.ReadFileAsync(args[1], stop.Token);
            var host = (hostFactory ?? (date => new WindowsTaskRegistrationHost(date)))(start)
                ?? throw new InvalidOperationException("Task registration host is unavailable.");
            string journalDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "TaskRegistration");
            var journal = (journalFactory ?? (directory => new FileTaskRegistrationJournal(directory, start)))(journalDirectory)
                ?? throw new InvalidOperationException("Task registration journal is unavailable.");
            var result = await TaskRegistration.RunAsync(plan, host, execute, stop.Token, journal,
                progress: new Progress<string>(Console.Error.WriteLine));
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                previewOnly = !execute,
                definitionEnabled = false,
                taskExecuted = false,
                target = plan.Identity.FullName,
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
