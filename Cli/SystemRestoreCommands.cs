using System.Text.Json;
using KillerBench.Engine;

internal static class SystemRestoreCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recover = args[0] == "restore-point-recover";
        if (recover && args.Length != 1)
        { Console.Error.WriteLine("Usage: restore-point-recover (verify a pending request without creating another point)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "SystemRestore");
            var host = new WindowsSystemRestoreCreationHost();
            SystemRestoreCreationResult result;
            if (recover)
                result = await SystemRestoreCreation.ReconcileAsync(folder, host, new Progress(), stop.Token);
            else
            {
                if (args.Length is < 2 or > 4)
                    throw new ArgumentException("Use restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute].");
                bool execute = args.Contains("--execute", StringComparer.Ordinal);
                var values = args.Skip(2).Where(value => value != "--execute").ToArray();
                if (values.Length > 1 || args.Skip(1).Count(value => value == "--execute") > 1)
                    throw new ArgumentException("Use one optional reason and one optional --execute switch.");
                var reason = values.Length == 0 ? SystemRestorePointReason.ModifySettings :
                    Enum.Parse<SystemRestorePointReason>(values[0], ignoreCase: true);
                var request = SystemRestoreCreation.CreateRequest(args[1], reason);
                result = await SystemRestoreCreation.RunAsync(request, execute, folder, host, new Progress(), stop.Token);
            }
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Restore-point operation stopped. Reconcile before trying again."); return 130; }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Restore-point operation unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class Progress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
