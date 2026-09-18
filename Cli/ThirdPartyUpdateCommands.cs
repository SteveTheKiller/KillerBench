using System.Text.Json;
using KillerBench.Engine;

internal static class ThirdPartyUpdateCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 1 && args[0] == "third-party-updates")
            return await DiscoverAsync().ConfigureAwait(false);
        if (args.Length == 1 && args[0] == "third-party-update-recover")
            return await RecoverAsync().ConfigureAwait(false);
        if (args.Length is < 4 or > 6 || args[0] != "third-party-update")
        {
            Usage();
            return 2;
        }
        var flags = args.Skip(4).ToArray();
        if (flags.Distinct(StringComparer.Ordinal).Count() != flags.Length || flags.Any(flag => flag is not ("--accept-agreements" or "--execute")))
        { Usage(); return 2; }
        bool accept = flags.Contains("--accept-agreements", StringComparer.Ordinal);
        bool execute = flags.Contains("--execute", StringComparer.Ordinal);
        try
        {
            var selection = new ThirdPartyUpdateSelection(args[1], args[2], args[3]);
            if (!execute)
            {
                var discovery = await ThirdPartyUpdateDiscovery.ReadAsync(new Progress<string>(Console.Error.WriteLine)).ConfigureAwait(false);
                var plan = ThirdPartyUpdatePlanning.Create(discovery, selection);
                Console.WriteLine(JsonSerializer.Serialize(plan, Capture.JsonOptions));
                return plan.Eligible ? 0 : 3;
            }
            if (!accept) { Console.Error.WriteLine("Execution requires --accept-agreements after reviewing the exact package, source and version."); return 2; }
            using var stop = new ConsoleStop();
            var result = await ThirdPartyUpdateExecution.RunAsync(selection, true, JournalFolder(), new WindowsThirdPartyUpdateExecutionHost(),
                new Progress<string>(Console.Error.WriteLine), stop.Token).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Third-party update stopped. Reconcile pending evidence before retrying."); return 130; }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Third-party update unavailable: " + error.Message); return 1; }
    }

    private static async Task<int> DiscoverAsync()
    {
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var result = await ThirdPartyUpdateDiscovery.ReadAsync(new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.ProviderAvailable && result.Complete ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Third-party update discovery stopped. No application changed."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Third-party update discovery unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> RecoverAsync()
    {
        using var stop = new ConsoleStop();
        try
        {
            var result = await ThirdPartyUpdateExecution.ReconcileAsync(JournalFolder(), new WindowsThirdPartyUpdateExecutionHost(), stop.Token).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Outcome is ThirdPartyUpdateRecoveryOutcome.NoPendingRecord or ThirdPartyUpdateRecoveryOutcome.Verified ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Third-party update recovery stopped. No update was retried."); return 130; }
    }

    private sealed class ConsoleStop : IDisposable
    {
        private readonly CancellationTokenSource source = new();
        private readonly ConsoleCancelEventHandler cancel;
        internal ConsoleStop()
        {
            cancel = (_, e) => { e.Cancel = true; source.Cancel(); };
            Console.CancelKeyPress += cancel;
        }
        internal CancellationToken Token => source.Token;
        public void Dispose() { Console.CancelKeyPress -= cancel; source.Dispose(); }
    }

    private static string JournalFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "ThirdPartyUpdates");

    private static void Usage() => Console.Error.WriteLine(
        "Usage: third-party-updates | third-party-update ID SOURCE VERSION [--accept-agreements] [--execute] | third-party-update-recover");
}
