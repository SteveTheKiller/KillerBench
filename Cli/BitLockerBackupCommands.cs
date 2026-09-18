using System.Text.Json;
using KillerBench.Engine;

internal static class BitLockerBackupCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool inventory = args[0] == "bitlocker-protectors";
        bool recovery = args[0] == "bitlocker-backup-recover";
        bool execute = args.Length == 5 && args[4] == "--execute";
        if (inventory ? args.Length != 2 : args.Length != 4 && !execute) return Usage();
        Guid protector = Guid.Empty;
        BitLockerOptions options;
        try
        {
            if (!inventory && (!Guid.TryParse(args[2], out protector) || protector == Guid.Empty)) return Usage();
            options = inventory ? new(args[1]) : new(args[1], BitLockerOperation.BackupRecovery,
                Backup: RecoveryDestination.File, BackupFile: args[3]);
            options.Validate();
        }
        catch (ArgumentException) { return Usage(); }
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitLockerBackup");
        if (!inventory && !execute && !recovery)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Mode = "Preview", Volume = options.Volume.ToUpperInvariant(), Protector = protector,
                Destination = "File", JournalDirectory = folder,
                Effects = "Write an existing recovery password to a new file at the chosen destination. Existing files are never overwritten. No encryption or protector changes.",
                Verification = "No machine inspection performed. Execution rechecks volume identity and protector presence before and after writing.",
                Execution = "Use --execute after reviewing the destination. The parent directory must already exist. Backup path and recovery password are omitted from ordinary output and journals."
            }, Capture.JsonOptions));
            return 0;
        }
        if (!OperatingSystem.IsWindows() || !WindowsState.IsAdministrator())
        { Console.Error.WriteLine("Windows administrator access is required. No backup performed."); return 3; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (inventory)
            {
                var result = await BitLockerProtectors.InspectAsync(options.Volume, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.Complete ? 0 : 3;
            }
            if (recovery)
            {
                Console.Error.WriteLine("Checking the selected existing recovery backup without writing it again.");
                var result = await BitLockerBackupReconciliation.RunAsync(options, protector, folder,
                    new WindowsBitLockerBackupHost(), stop.Token, new ConsoleProgress());
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            Console.Error.WriteLine("Backing up the selected recovery protector. Journal directory: " + folder);
            var backup = await BitLockerBackupExecution.RunAsync(options, protector, folder, new WindowsBitLockerBackupHost(), stop.Token, new ConsoleProgress());
            Console.WriteLine(JsonSerializer.Serialize(backup, Capture.JsonOptions));
            return backup.Status.StartsWith("Stopped", StringComparison.Ordinal) ? 130 : backup.Receipt is { Verified: true } ? 0 : 3;
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Operation stopped. A backup may exist but is not verified. Review the destination and journal directory: " + folder); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Operation unavailable. No verified backup established. Review the destination and journal directory: " + folder); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: bitlocker-protectors DRIVE: | bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute] | bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH. Backup previews by default; recovery never writes the file.");
        return 2;
    }
}
