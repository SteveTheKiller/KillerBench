using System.Text.Json;
using KillerBench.Engine;

internal static class PreviousWindowsCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recover = args.Length > 0 && args[0] == "previous-windows-recover";
        bool execute = false;
        bool backupConfirmed = false;
        if (recover)
        {
            if (args.Length != 1) return Usage();
        }
        else
        {
            foreach (string arg in args.Skip(1))
            {
                if (arg == "--execute") execute = true;
                else if (arg == "--backup-confirmed") backupConfirmed = true;
                else return Usage();
            }
            if (backupConfirmed && !execute) return Usage();
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "PreviousWindowsInstallation");
            if (recover)
            {
                var reconciliation = await PreviousWindowsInstallationRemoval.ReconcileAsync(folder,
                    new WindowsPreviousWindowsInstallationHost(), stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(reconciliation, Capture.JsonOptions));
                return reconciliation.GetExitCode();
            }
            var result = await PreviousWindowsInstallationRemoval.RunAsync(execute, backupConfirmed, folder,
                new WindowsPreviousWindowsInstallationHost(), new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Previous-Windows operation stopped. Reconcile before retrying."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or NotSupportedException or TimeoutException or
            System.ComponentModel.Win32Exception or System.Security.SecurityException)
        { Console.Error.WriteLine("Previous-Windows operation unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: previous-windows [--execute --backup-confirmed] | previous-windows-recover. Preview is the default; recovery never repeats removal.");
        return 2;
    }
}
