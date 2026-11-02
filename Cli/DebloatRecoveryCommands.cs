using System.Text.Json;
using KillerBench.Engine;

internal static class DebloatRecoveryCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2 || args[1] is not ("power-plan" or "power-timers" or "hibernation")) return Usage();
        string? folder = null;
        bool json = false;
        for (int index = 2; index < args.Length; index++)
        {
            if (args[index] == "--json" && !json) { json = true; continue; }
            if (args[index] == "--folder" && folder is null && index + 1 < args.Length)
            { folder = args[++index]; continue; }
            return Usage();
        }
        if (string.IsNullOrWhiteSpace(folder) || !Path.IsPathFullyQualified(folder)) return Usage();
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string root = Path.GetFullPath(folder);
            var progress = new ConsoleProgress();
            var result = args[1] switch
            {
                "power-plan" => await DebloatPowerReconciliation.RunPlanAsync(root, new WindowsDebloatPowerPlanChangeHost(), progress, stop.Token),
                "power-timers" => await DebloatPowerReconciliation.RunTimersAsync(root, new WindowsDebloatPowerTimerHost(), progress, stop.Token),
                _ => await DebloatPowerReconciliation.RunHibernationAsync(root, new WindowsDebloatHibernationHost(), progress, stop.Token)
            };
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { return 130; }
        catch (ArgumentException) { return Usage(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.Security.SecurityException)
        { Console.Error.WriteLine("Power recovery could not complete: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: debloat-recover power-plan|power-timers|hibernation --folder PATH [--json]");
        Console.Error.WriteLine("Rechecks a pending operation without repeating a power change. Resolved local records may be archived. Output is JSON.");
        return 2;
    }
}
