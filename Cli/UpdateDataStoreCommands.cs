using System.Text.Json;
using KillerBench.Engine;

internal static class UpdateDataStoreCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recover = args.Length > 0 && args[0] == "update-store-reset-recover";
        if (recover && args.Length != 1) return Usage();
        bool software = true, drivers = false, execute = false;
        UpdateSearchSource source = UpdateSearchSource.Configured;
        var switches = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 1; i < args.Length; i++)
        {
            if (!switches.Add(args[i])) return Usage();
            switch (args[i])
            {
                case "--execute": execute = true; break;
                case "--drivers": drivers = true; break;
                case "--no-software": software = false; break;
                case "--source" when i + 1 < args.Length:
                    if (!Enum.TryParse(args[++i], true, out source) || !Enum.IsDefined(source)) return Usage();
                    break;
                default: return Usage();
            }
        }
        if (!software && !drivers) return Usage();
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var progress = new Progress<string>(Console.Error.WriteLine);
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "UpdateDataStoreReset");
            if (recover)
            {
                var reconciliation = await UpdateDataStoreResetReconciliation.RunAsync(folder,
                    new WindowsUpdateDataStoreResetHost(), progress, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(reconciliation, Capture.JsonOptions));
                return reconciliation.GetExitCode();
            }
            var options = new UpdateSearchOptions(software, drivers, source);
            Console.Error.WriteLine("Running a fresh Windows Update search to establish exact reset eligibility. No update will be installed.");
            var search = await UpdateDiscovery.SearchAsync(options, progress, stop.Token);
            var result = await UpdateDataStoreReset.RunAsync(search, execute, folder,
                new WindowsUpdateDataStoreResetHost(), progress, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Windows Update data-store operation stopped. Reconcile a prepared operation before retrying.");
            return 130;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        {
            Console.Error.WriteLine("Windows Update data-store operation unavailable: " + error.Message);
            return 1;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute] | update-store-reset-recover. Fresh-search gated; preview by default; recovery never repeats a reset.");
        return 2;
    }
}
