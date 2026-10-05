using System.Text.Json;
using KillerBench.Engine;

internal static class DnsCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (!NetworkCheckOptions.TryTimeout(args, 2, out var timeout)) { Console.Error.WriteLine("Usage: dns-check HOST [--timeout-seconds 1..60] (configured system resolver; no settings changed)"); return 2; }
        try { DnsDiagnostics.ValidateHost(args[1]); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Resolving " + args[1] + "...");
            var result = await DnsDiagnostics.CheckAsync(args[1], timeout, token: stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.State == DnsLookupState.Resolved ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("DNS lookup stopped."); return 130; }
        catch (Exception error) when (error is InvalidOperationException or NotSupportedException or System.Security.SecurityException)
        { Console.Error.WriteLine("DNS lookup unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
