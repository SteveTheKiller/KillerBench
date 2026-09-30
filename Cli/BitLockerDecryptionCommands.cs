using System.Text.Json;
using KillerBench.Engine;

internal static class BitLockerDecryptionCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recovery = args[0] == "bitlocker-decrypt-recover";
        bool execute = !recovery && args.Length == 4 && args[2] == "--execute" && args[3] == "--confirm-unprotected";
        if (recovery ? args.Length != 1 : !(args.Length == 2 || execute)) return Usage();
        string volume = "C:";
        if (!recovery)
        {
            try { volume = BitLockerDecryption.NormalizeVolume(args[1]); }
            catch (ArgumentException) { return Usage(); }
        }
        if ((execute || recovery) && !OperatingSystem.IsWindows())
        { Console.Error.WriteLine("BitLocker decryption requires Windows. No change performed."); return 3; }
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitLockerDecrypt");

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var host = new WindowsBitLockerDecryptionHost();
            BitLockerDecryptionResult result;
            if (recovery)
            {
                Console.Error.WriteLine("Checking the pending decryption request without repeating it.");
                result = await BitLockerDecryption.ReconcileAsync(folder, host, new ConsoleProgress(), stop.Token);
            }
            else
            {
                Console.Error.WriteLine(execute
                    ? "Checking the selected volume before one decryption request. The volume will be unprotected afterwards."
                    : "Previewing decryption. No machine inspection or change is performed.");
                result = await BitLockerDecryption.RunAsync(volume, execute, execute, folder, host, new ConsoleProgress(), stop.Token);
            }
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Operation stopped. Use bitlocker-decrypt-recover before trying again."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BitLocker decryption is unavailable. No verified result was established: " + error.Message); return 3; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: bitlocker-decrypt DRIVE: [--execute --confirm-unprotected] | bitlocker-decrypt-recover. Previews by default; decryption leaves the volume unprotected.");
        return 2;
    }
}
