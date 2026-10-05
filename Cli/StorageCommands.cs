using System.Text.Json;
using KillerBench.Engine;

internal static class StorageCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length >= 1 && args[0] == "volume-optimize") return await OptimizeVolumeAsync(args);
        if (args.Length >= 1 && args[0] == "volume-optimize-recover") return await RecoverOptimizationAsync(args);
        if (args.Length >= 1 && args[0] == "storage-repair-plan") return PreviewRepair(args);
        if (args.Length >= 1 && args[0] == "storage-repair-review") return await ReviewRepairAsync(args);
        if (args.Length >= 1 && args[0] == "storage-repair") return await ExecuteRepairAsync(args);
        if (args.Length >= 1 && args[0] == "storage-repair-recover") return await RecoverRepairAsync(args);
        if (args.Length != 2)
        { Console.Error.WriteLine("Usage: storage-check DRIVE_LETTER (read-only fixed NTFS volume check)"); return 2; }
        string volume;
        try { volume = StorageDiagnostics.NormalizeVolume(args[1]); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var check = await StorageDiagnostics.CheckVolumeAsync(volume, new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.State == CheckState.Clear ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Storage check stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Storage check unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> OptimizeVolumeAsync(string[] args)
    {
        bool execute = args.Contains("--execute", StringComparer.Ordinal);
        if (args.Length is < 2 or > 4 || args.Skip(3).Any(value => value != "--execute") ||
            args.Skip(3).Distinct(StringComparer.Ordinal).Count() != args.Length - 3 ||
            args.Length >= 3 && args[2] != "--execute" &&
            (!Enum.TryParse<VolumeOptimizationMode>(args[2], true, out var parsed) || !Enum.IsDefined(parsed) ||
             !parsed.ToString().Equals(args[2], StringComparison.OrdinalIgnoreCase)))
        {
            Console.Error.WriteLine("Usage: volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]");
            return 2;
        }
        VolumeOptimizationMode mode = args.Length >= 3 && args[2] != "--execute"
            ? Enum.Parse<VolumeOptimizationMode>(args[2], true) : VolumeOptimizationMode.Recommended;
        VolumeOptimizationRequest request;
        try { request = VolumeOptimization.CreateRequest(args[1], mode); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "VolumeOptimization");
            var host = new WindowsVolumeOptimizationHost();
            var progress = new Progress<string>(Console.Error.WriteLine);
            var result = execute
                ? await VolumeOptimization.RunAsync(request, true, folder, host, progress, token: stop.Token)
                : await VolumeOptimization.ReviewAsync(request, host, progress, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Volume optimization stopped."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        { Console.Error.WriteLine("Volume optimization unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> RecoverOptimizationAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: volume-optimize-recover (verify a pending operation without repeating it)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "VolumeOptimization");
            var result = await VolumeOptimization.ReconcileAsync(folder, new WindowsVolumeOptimizationHost(),
                new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Volume optimization recovery stopped."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        { Console.Error.WriteLine("Volume optimization recovery unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> RecoverRepairAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: storage-repair-recover (recheck an interrupted filesystem repair without repeating it)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "FileSystemRepair");
            var result = await FileSystemRepairReconciliation.RunAsync(folder, new WindowsFileSystemRepairHost(),
                new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Filesystem repair recovery stopped."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        { Console.Error.WriteLine("Filesystem repair recovery unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> ExecuteRepairAsync(string[] args)
    {
        bool execute = args.Contains("--execute", StringComparer.Ordinal);
        bool allowDismount = args.Contains("--allow-dismount", StringComparer.Ordinal);
        bool backupConfirmed = args.Contains("--backup-confirmed", StringComparer.Ordinal);
        if (args.Length is < 3 or > 6 || args.Skip(3).Distinct(StringComparer.Ordinal).Count() != args.Length - 3 ||
            args.Skip(3).Any(value => value is not ("--allow-dismount" or "--backup-confirmed" or "--execute")) ||
            !Enum.TryParse<FileSystemRepairMode>(args[2], true, out var mode) || !Enum.IsDefined(mode) ||
            !mode.ToString().Equals(args[2], StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Usage: storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]");
            return 2;
        }
        string volume;
        try { volume = StorageDiagnostics.NormalizeVolume(args[1]); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var draft = CreateDraft(volume, mode, allowDismount, backupConfirmed);
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "FileSystemRepair");
            var result = await FileSystemRepairExecution.RunAsync(draft, execute, folder,
                new WindowsFileSystemRepairHost(), new Progress<string>(Console.Error.WriteLine), token: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Filesystem repair stopped."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        { Console.Error.WriteLine("Filesystem repair unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> ReviewRepairAsync(string[] args)
    {
        if (!TryRepairArguments(args, "storage-repair-review", out var volume, out var mode, out var allowDismount)) return 2;
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var draft = CreateDraft(volume, mode, allowDismount);
            var diagnosis = await StorageDiagnostics.CheckVolumeAsync(volume,
                new Progress<string>(Console.Error.WriteLine), stop.Token);
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "StorageReviews");
            var record = await FileSystemRepairPlanning.SaveReviewAsync(draft, diagnosis, folder, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(record, Capture.JsonOptions));
            return 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Storage repair review stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Storage repair review unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int PreviewRepair(string[] args)
    {
        if (!TryRepairArguments(args, "storage-repair-plan", out var volume, out var mode, out var allowDismount)) return 2;
        try
        {
            var plan = CreateDraft(volume, mode, allowDismount);
            Console.WriteLine(JsonSerializer.Serialize(plan, Capture.JsonOptions));
            return plan.VolumeEligible ? 0 : 3;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Storage repair planning unavailable: " + error.Message); return 1; }
    }

    private static FileSystemRepairDraft CreateDraft(string volume, FileSystemRepairMode mode, bool allowDismount,
        bool backupConfirmed = false)
    {
        var drive = new DriveInfo(volume + "\\");
        bool ready = drive.IsReady;
        return FileSystemRepairPlanning.Create(volume, mode,
            new(ready, drive.DriveType == DriveType.Fixed, ready ? drive.DriveFormat : null),
            string.Equals(volume, StorageDiagnostics.SystemVolume(), StringComparison.OrdinalIgnoreCase), allowDismount,
            backupConfirmed);
    }

    private static bool TryRepairArguments(string[] args, string command, out string volume,
        out FileSystemRepairMode mode, out bool allowDismount)
    {
        volume = "";
        mode = default;
        allowDismount = args is [_, _, _, "--allow-dismount"];
        if (args.Length is not (3 or 4) || args.Length == 4 && !allowDismount ||
            !Enum.TryParse(args[2], true, out mode) || !Enum.IsDefined(mode) ||
            !mode.ToString().Equals(args[2], StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Usage: {command} DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] (read-only)");
            return false;
        }
        try { volume = StorageDiagnostics.NormalizeVolume(args[1]); return true; }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return false; }
    }
}
