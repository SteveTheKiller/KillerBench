using System.Text.Json;
using KillerBench.Engine;

internal static class BitLockerEncryptionCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recovery = args[0] == "bitlocker-encrypt-recover";
        if (recovery && args.Length != 1) return Usage();
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KillerBench", "BitLockerEncryption");
        BitLockerOptions? options = null;
        Guid protector = Guid.Empty;
        bool execute = false;
        if (!recovery)
        {
            if (args.Length < 4 || !Guid.TryParse(args[2], out protector) || protector == Guid.Empty) return Usage();
            string volume = args[1];
            string backupFile = args[3];
            var method = BitLockerEncryption.XtsAes128;
            bool usedSpaceOnly = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 4; index < args.Length; index++)
            {
                string flag = args[index];
                if (!seen.Add(flag)) return Usage();
                if (flag == "--execute") { execute = true; continue; }
                if (flag == "--used-space-only") { usedSpaceOnly = true; continue; }
                if (flag != "--encryption" || ++index >= args.Length ||
                    !Enum.TryParse(args[index], true, out method) || !Enum.IsDefined(method)) return Usage();
            }
            try
            {
                options = new BitLockerOptions(volume, BitLockerOperation.Encrypt, method, usedSpaceOnly,
                    RecoveryDestination.File, backupFile);
                options.Validate();
            }
            catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        }
        if ((execute || recovery) && !OperatingSystem.IsWindows())
        { Console.Error.WriteLine("BitLocker encryption requires Windows. No change performed."); return 3; }

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var host = new WindowsBitLockerEncryptionHost();
            BitLockerEncryptionResult result;
            if (recovery)
            {
                Console.Error.WriteLine("Checking the pending BitLocker encryption request without repeating it.");
                result = await BitLockerEncryptionExecution.ReconcileAsync(folder, host,
                    new ConsoleProgress(), stop.Token);
            }
            else
            {
                Console.Error.WriteLine(execute
                    ? "Checking the selected volume, protectors and recovery backup before one encryption request."
                    : "Previewing BitLocker encryption. No machine inspection, backup or encryption is performed.");
                result = await BitLockerEncryptionExecution.RunAsync(options!, protector, execute, folder, host,
                    new ConsoleProgress(), stop.Token);
            }
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Operation stopped. Encryption state may be unverified; use bitlocker-encrypt-recover before retrying."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BitLocker encryption is unavailable. No verified result was established: " + error.Message); return 3; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption Aes128|Aes256|XtsAes128|XtsAes256] [--used-space-only] [--execute] | bitlocker-encrypt-recover. Encryption previews by default; recovery never repeats the request.");
        return 2;
    }
}
