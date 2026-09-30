using System.Text.Json;
using KillerBench.Engine;

internal static class BitLockerAutoUnlockCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recovery = args[0] == "bitlocker-auto-unlock-recover";
        bool execute = !recovery && args.Length == 3 && args[2] == "--execute";
        if (recovery ? args.Length != 1 : !(args.Length == 2 || execute)) return Usage();
        string volume = "C:";
        if (!recovery)
        {
            try { volume = BitLockerAutoUnlock.NormalizeVolume(args[1]); }
            catch (ArgumentException) { return Usage(); }
        }
        if ((execute || recovery) && !OperatingSystem.IsWindows())
        { Console.Error.WriteLine("BitLocker auto-unlock requires Windows. No change performed."); return 3; }
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitLockerAutoUnlock");

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var host = new WindowsBitLockerAutoUnlockHost();
            BitLockerAutoUnlockResult result;
            if (recovery)
            {
                Console.Error.WriteLine("Checking the pending auto-unlock request without repeating it.");
                result = await BitLockerAutoUnlock.ReconcileAsync(folder, host, new ConsoleProgress(), stop.Token);
            }
            else
            {
                Console.Error.WriteLine(execute
                    ? "Checking the selected data volume and the operating-system volume before one auto-unlock request."
                    : "Previewing auto-unlock. No machine inspection or change is performed.");
                result = await BitLockerAutoUnlock.RunAsync(volume, execute, folder, host, new ConsoleProgress(), stop.Token);
            }
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Operation stopped. Use bitlocker-auto-unlock-recover before trying again."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BitLocker auto-unlock is unavailable. No verified result was established: " + error.Message); return 3; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: bitlocker-auto-unlock DRIVE: [--execute] | bitlocker-auto-unlock-recover. Previews by default; applies to an encrypted data volume.");
        return 2;
    }
}
