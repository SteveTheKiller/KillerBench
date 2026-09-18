using System.Text.Json;
using KillerBench.Engine;

internal static class TcpCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (!NetworkCheckOptions.TryTimeout(args, 3, out var timeout) || !int.TryParse(args[2], out int port))
        { Console.Error.WriteLine("Usage: tcp-check HOST PORT [--timeout-seconds 1..60] (connect only; no application data sent)"); return 2; }
        try { TcpDiagnostics.Validate(args[1], port); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Checking the selected TCP endpoint...");
            var result = await TcpDiagnostics.CheckAsync(args[1], port, timeout, stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.State == TcpCheckState.Connected ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("TCP check stopped."); return 130; }
        catch (Exception error) when (error is InvalidOperationException or NotSupportedException or System.Security.SecurityException)
        { Console.Error.WriteLine("TCP check unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
