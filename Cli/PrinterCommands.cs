using System.Text.Json;
using KillerBench.Engine;

internal static class PrinterCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        using var stop = new CancellationTokenSource();
        bool executionStarted = false;
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (args[0] is "print-job-list" or "print-job-cancel" or "print-job-cancel-recover")
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "PrintJobs");
                bool executeCancellation = false;
                bool customFolder = false;
                var values = new List<string>();
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--execute" && args[0] == "print-job-cancel" && !executeCancellation)
                    { executeCancellation = true; continue; }
                    if (args[i] == "--folder" && i + 1 < args.Length)
                    {
                        if (customFolder) throw new ArgumentException("Specify --folder only once.");
                        customFolder = true;
                        folder = args[++i];
                        continue;
                    }
                    if (!args[i].StartsWith("--", StringComparison.Ordinal))
                    { values.Add(args[i]); continue; }
                    throw new ArgumentException("Use print-job-list, print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH], or print-job-cancel-recover [--folder PATH].");
                }
                if (args[0] == "print-job-list")
                {
                    if (values.Count != 0 || executeCancellation || customFolder)
                        throw new ArgumentException("print-job-list takes no options.");
                    var captured = await PrintJobDiagnostics.CaptureAsync(stop.Token);
                    var inventory = PrintJobDiagnostics.ReadInventory(captured);
                    Console.WriteLine(JsonSerializer.Serialize(new { Available = inventory is not null,
                        Complete = inventory?.Complete, Jobs = inventory?.Jobs,
                        Note = "Current visible print jobs only. No job or queue changed." }, Capture.JsonOptions));
                    return inventory is null ? 3 : 0;
                }
                var host = new WindowsPrintJobCancellationHost();
                if (args[0] == "print-job-cancel-recover")
                {
                    if (values.Count != 0 || executeCancellation)
                        throw new ArgumentException("Use print-job-cancel-recover [--folder PATH].");
                    executionStarted = true;
                    var recovery = await PrintJobCancellation.ReconcileAsync(folder, host, new PrinterProgress(), stop.Token);
                    Console.WriteLine(JsonSerializer.Serialize(recovery, Capture.JsonOptions));
                    return recovery.GetExitCode();
                }
                if (values.Count != 2 || !uint.TryParse(values[1], out uint jobId) || jobId == 0)
                    throw new ArgumentException("Choose one exact target: print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH].");
                executionStarted = executeCancellation;
                var result = await PrintJobCancellation.RunAsync(values[0], jobId, executeCancellation,
                    folder, host, new PrinterProgress(), stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            if (args[0] is "default-printer-set" or "default-printer-set-recover")
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "DefaultPrinter");
                string? queue = null;
                bool executeSelection = false;
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--execute" && args[0] == "default-printer-set" && !executeSelection)
                    { executeSelection = true; continue; }
                    if (args[i] == "--folder" && i + 1 < args.Length)
                    { folder = args[++i]; continue; }
                    if (args[0] == "default-printer-set" && queue is null && !args[i].StartsWith("--", StringComparison.Ordinal))
                    { queue = args[i]; continue; }
                    throw new ArgumentException("Use default-printer-set QUEUE [--execute] [--folder PATH] or default-printer-set-recover [--folder PATH].");
                }
                var host = new WindowsDefaultPrinterSelectionHost();
                if (args[0] == "default-printer-set-recover")
                {
                    executionStarted = true;
                    var recovery = await DefaultPrinterSelection.ReconcileAsync(folder, host, new PrinterProgress(), stop.Token);
                    Console.WriteLine(JsonSerializer.Serialize(recovery, Capture.JsonOptions));
                    return recovery.GetExitCode();
                }
                if (queue is null) throw new ArgumentException("Choose one exact queue: default-printer-set QUEUE [--execute] [--folder PATH].");
                executionStarted = executeSelection;
                var result = await DefaultPrinterSelection.RunAsync(queue, executeSelection, folder, host, new PrinterProgress(), stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            if (args[0] == "printer-list")
            {
                if (args.Length != 1) throw new ArgumentException("printer-list takes no options.");
                var snapshot = await PrinterDiagnostics.DiscoverAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { Available = snapshot is not null, Queues = snapshot?.Queues,
                    Note = "Local queue discovery only. No settings changed." }, Capture.JsonOptions));
                return snapshot is null ? 3 : 0;
            }
            var queues = new List<string>();
            bool execute = false;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--execute" && args[0] == "printer-repair" && !execute)
                { execute = true; continue; }
                if (args[i] != "--queue" || i + 1 >= args.Length)
                    throw new ArgumentException("Use printer-check or printer-repair --queue NAME [--queue NAME ...]. Only printer-repair accepts --execute.");
                queues.Add(args[++i]);
            }
            if (args[0] == "printer-repair")
            {
                var plan = PrinterOfflineRecovery.Plan(queues);
                if (!execute)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { plan.Title, plan.Effects, Queues = queues,
                        Note = "Preview only. Use --execute to authorize these changes after reviewing their effects. Fresh diagnosis and administrator prerequisites are required." }, Capture.JsonOptions));
                    return 0;
                }
                Console.Error.WriteLine(plan.Effects);
                executionStarted = true;
                var result = await Repairs.RunAsync(RepairKind.PrinterOffline,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Repairs"),
                    new PrinterProgress(), stop.Token, selectedPrinterQueues: queues);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return stop.IsCancellationRequested ? 130 : Repairs.IsVerified(result) ? 0 : 3;
            }
            var check = await PrinterDiagnostics.CheckOfflineAsync(queues, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new { Check = check, Note = "Selected local queue offline settings only. Physical printer health is not tested. No settings changed." }, Capture.JsonOptions));
            return check.State == CheckState.Clear ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine(executionStarted ? "Printer operation stopped. Reconcile any pending record before retrying." : "Printer check stopped or timed out. No settings changed."); return 130; }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Console.Error.WriteLine("Printer operation unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
    private sealed class PrinterProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
