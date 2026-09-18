using System.Text.Json;
using KillerBench.Engine;

internal static class PrinterDriverCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        { Console.Error.WriteLine("Usage: printer-drivers (read-only queue/driver inventory; JSON output)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Reading printer queue and driver registrations...");
            var checks = await PrinterDriverDiagnostics.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
            return checks.Count > 0 && checks.All(check => check.State == CheckState.Information) ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Printer driver inventory stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Printer driver inventory unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
