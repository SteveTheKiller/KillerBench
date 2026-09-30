using System.Text.Json;
using KillerBench.Engine;

internal static class BitLockerEscrowCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool execute = args.Length == 5 && args[4] == "--execute";
        if (args.Length is < 4 or > 5 || (args.Length == 5 && !execute)) return Usage();
        string volume;
        try { volume = BitLockerEscrow.NormalizeVolume(args[1]); }
        catch (ArgumentException) { return Usage(); }
        if (!Guid.TryParse(args[2], out Guid protector) || protector == Guid.Empty) return Usage();
        BitLockerEscrowTarget target;
        switch (args[3].ToLowerInvariant())
        {
            case "ad": target = BitLockerEscrowTarget.ActiveDirectory; break;
            case "entra": target = BitLockerEscrowTarget.EntraId; break;
            default: return Usage();
        }
        if (execute && !OperatingSystem.IsWindows())
        { Console.Error.WriteLine("BitLocker escrow requires Windows. No request was sent."); return 3; }
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitLockerEscrow");

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine(execute
                ? "Checking the selected protector before one backup request to " + BitLockerEscrow.TargetName(target) + "."
                : "Previewing BitLocker escrow. No machine inspection or request is performed.");
            var result = await BitLockerEscrow.RunAsync(volume, target, protector, execute, folder,
                new WindowsBitLockerEscrowHost(), new ConsoleProgress(), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Operation stopped. Repeating a backup only stores the same recovery information again."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BitLocker escrow is unavailable. No verified result was established: " + error.Message); return 3; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]. Previews by default; only an existing recovery-password protector is sent.");
        return 2;
    }
}
