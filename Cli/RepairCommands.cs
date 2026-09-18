using KillerBench.Engine;

internal static class RepairCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length is < 2 or > 3 || args[1] is not ("dism" or "sfc") || args.Length == 3 && args[2] != "--execute")
        { Console.Error.WriteLine("Usage: repair <dism|sfc> [--execute]"); return 2; }
        var kind = args[1] == "dism" ? RepairKind.ComponentStore : RepairKind.SystemFiles;
        Console.WriteLine(Repairs.Preview(kind));
        if (args.Length == 2) return 0;
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; stop.Cancel(); Console.Error.WriteLine("Stop requested. Waiting for the running command to finish."); };
        Console.CancelKeyPress += handler;
        try
        {
            var result = await ServicingRepairExecution.RunAsync(kind,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Repairs"),
                new WindowsWorkflowHost(), new ProgressOutput(), stop.Token);
            Console.WriteLine(result.Eligibility.Name + ": " + result.Eligibility.Detail);
            if (result.Repair is null)
            {
                Console.Error.WriteLine("Fresh evidence did not authorize this repair. No servicing command was run.");
                return result.GetExitCode(stop.IsCancellationRequested);
            }
            Console.WriteLine(result.Repair.Status + "\nJournal: " + result.Repair.JournalPath);
            return result.GetExitCode(stop.IsCancellationRequested);
        }
        catch (OperationCanceledException) { return 130; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.Security.SecurityException)
        { Console.Error.WriteLine(ex.Message); return 1; }
        finally { Console.CancelKeyPress -= handler; }
    }
    private sealed class ProgressOutput : IProgress<string> { public void Report(string value) => Console.Error.WriteLine(value); }
}
