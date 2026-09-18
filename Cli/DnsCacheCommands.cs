using System.Text.Json;
using KillerBench.Engine;

internal static class DnsCacheCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recover = args[0] == "dns-cache-recover";
        if (recover && args.Length != 1)
        {
            Console.Error.WriteLine("Usage: dns-cache-recover (recheck an interrupted operation; never repeats the flush)");
            return 2;
        }
        bool execute = args.Length == 3 && args[2] == "--execute";
        if (!recover && args.Length != 2 && !execute)
        {
            Console.Error.WriteLine("Usage: dns-cache HOST [--execute] (preview by default; exact hostname only)");
            return 2;
        }
        string? host = null;
        if (!recover)
        {
            try { host = DnsDiagnostics.ValidateHost(args[1]); }
            catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(75));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KillerBench", "DnsCacheRecovery");
        try
        {
            var recoveryHost = new WindowsDnsCacheRecoveryHost();
            if (recover)
            {
                Console.Error.WriteLine("Rechecking an interrupted DNS cache operation without repeating the flush.");
                var reconciliation = await DnsCacheReconciliation.RunAsync(folder, recoveryHost.ResolveAsync,
                    stop.Token, new ConsoleProgress());
                Console.WriteLine(JsonSerializer.Serialize(reconciliation, Capture.JsonOptions));
                return reconciliation.GetExitCode();
            }
            Console.Error.WriteLine("Resolving " + host + " before reviewing DNS cache recovery.");
            var selected = await recoveryHost.ResolveAsync(host!, stop.Token);
            var result = await DnsCacheRecovery.RunAsync(selected, execute, recoveryHost,
                new DnsCacheRecoveryJournal(folder), stop.Token, new ConsoleProgress());
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("DNS cache recovery stopped or timed out. If execution began, recheck the hostname before another repair. Journal directory: " + folder);
            return 130;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or InvalidOperationException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine("DNS cache recovery unavailable: " + error.Message + ". No verified recovery established. Journal directory: " + folder);
            return 1;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
