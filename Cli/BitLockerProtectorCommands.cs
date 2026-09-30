using System.Text.Json;
using KillerBench.Engine;

internal static class BitLockerProtectorCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recovery = args[0] == "bitlocker-protector-recover";
        bool execute = !recovery && args.Length == 4 && args[3] == "--execute";
        if (recovery ? args.Length != 1 : args.Length is < 3 or > 4 || (args.Length == 4 && !execute)) return Usage();
        string volume = "C:";
        BitLockerNewProtector kind = BitLockerNewProtector.RecoveryPassword;
        if (!recovery)
        {
            try { volume = BitLockerProtectorCreation.NormalizeVolume(args[1]); }
            catch (ArgumentException) { return Usage(); }
            switch (args[2].ToLowerInvariant())
            {
                case "tpm": kind = BitLockerNewProtector.Tpm; break;
                case "recovery": kind = BitLockerNewProtector.RecoveryPassword; break;
                default: return Usage();
            }
        }
        if ((execute || recovery) && !OperatingSystem.IsWindows())
        { Console.Error.WriteLine("BitLocker protectors require Windows. No change performed."); return 3; }
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitLockerProtector");

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var host = new WindowsBitLockerProtectorCreationHost();
            BitLockerProtectorCreationResult result;
            if (recovery)
            {
                Console.Error.WriteLine("Checking the pending protector request without repeating it.");
                result = await BitLockerProtectorCreation.ReconcileAsync(folder, host, new ConsoleProgress(), stop.Token);
            }
            else
            {
                Console.Error.WriteLine(execute
                    ? "Checking the selected volume before one protector request."
                    : "Previewing protector creation. No machine inspection or change is performed.");
                result = await BitLockerProtectorCreation.RunAsync(volume, kind, execute, folder, host, new ConsoleProgress(), stop.Token);
            }
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Operation stopped. Use bitlocker-protector-recover before adding another protector."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BitLocker protector creation is unavailable. No verified result was established: " + error.Message); return 3; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: bitlocker-protector-add DRIVE: tpm|recovery [--execute] | bitlocker-protector-recover. Previews by default; recovery passwords are never printed.");
        return 2;
    }
}
