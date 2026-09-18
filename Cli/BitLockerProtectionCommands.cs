using System.Text.Json;
using KillerBench.Engine;

internal static class BitLockerProtectionCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recovery = args[0] == "bitlocker-resume-recover";
        bool execute = !recovery && args.Length == 3 && args[2] == "--execute";
        if (recovery ? args.Length != 1 : args.Length is < 2 or > 3 || (args.Length == 3 && !execute)) return Usage();
        string volume;
        try { volume = recovery ? "C:" : BitLockerProtectionResume.NormalizeVolume(args[1]); }
        catch (ArgumentException) { return Usage(); }
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KillerBench", "BitLockerResume");
        if ((execute || recovery) && !OperatingSystem.IsWindows())
        { Console.Error.WriteLine("BitLocker protection requires Windows. No change performed."); return 3; }

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var host = new WindowsBitLockerProtectionResumeHost();
            BitLockerProtectionResumeResult result;
            if (recovery)
            {
                Console.Error.WriteLine("Checking the pending BitLocker request without repeating it.");
                result = await BitLockerProtectionResume.ReconcileAsync(folder, host,
                    new ConsoleProgress(), stop.Token);
            }
            else
            {
                Console.Error.WriteLine(execute
                    ? "Checking the selected volume before one BitLocker resume request."
                    : "Previewing BitLocker resume. No machine inspection or change is performed.");
                result = await BitLockerProtectionResume.RunAsync(volume, execute, folder, host,
                    new ConsoleProgress(), stop.Token);
            }
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Operation stopped. Protection state may be unverified; use bitlocker-resume-recover before retrying."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BitLocker resume is unavailable. No verified result was established: " + error.Message); return 3; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: bitlocker-resume DRIVE: [--execute] | bitlocker-resume-recover. Resume previews by default; recovery never repeats the request.");
        return 2;
    }
}
