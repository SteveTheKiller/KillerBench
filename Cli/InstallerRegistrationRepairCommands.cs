using System.Text.Json;
using KillerBench.Engine;

internal static class InstallerRegistrationRepairCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool repair = args.Length > 0 && args[0] == "installer-registration-repair";
        bool recover = args.Length > 0 && args[0] == "installer-registration-repair-recover";
        bool execute = repair && args.Length == 2 && args[1] == "--execute";
        if ((!repair && !recover) || recover && args.Length != 1 ||
            repair && args.Length is not (1 or 2) || repair && args.Length == 2 && !execute)
        {
            Console.Error.WriteLine(recover
                ? "Usage: installer-registration-repair-recover (recheck interrupted registration without repeating commands)"
                : "Usage: installer-registration-repair [--execute] (preview by default; exact eligible service-registration defect only)");
            return 2;
        }

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "InstallerRegistrationRepair");
            var progress = new Progress<string>(message => Console.Error.WriteLine(message));
            var host = new WindowsInstallerRegistrationRepairHost();
            if (recover)
            {
                var result = await InstallerRegistrationRepairReconciliation.RunAsync(
                    folder, host, progress, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }

            var repairResult = await InstallerRegistrationRepair.RunAsync(
                execute, folder, host, progress, token: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(repairResult, Capture.JsonOptions));
            return repairResult.GetExitCode();
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Installer registration repair stopped.");
            return 130;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or
            UnauthorizedAccessException or InvalidOperationException or NotSupportedException or
            TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        {
            Console.Error.WriteLine("Installer registration repair unavailable: " + error.Message);
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }
}
