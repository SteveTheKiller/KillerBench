using System.Text.Json;
using KillerBench.Engine;

internal static class CleanupCommands
{
    internal static int Run(string[] args)
    {
        var targets = CleanupTargets.None;
        int days = 7;
        int maximumFiles = 100;
        bool json = false, execute = false;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--user-temp": targets |= CleanupTargets.UserTemp; break;
                case "--windows-temp": targets |= CleanupTargets.WindowsTemp; break;
                case "--system-temp": targets |= CleanupTargets.SystemTemp; break;
                case "--shader-caches": targets |= CleanupTargets.ShaderCaches; break;
                case "--browser-caches": targets |= CleanupTargets.BrowserCaches; break;
                case "--office-caches": targets |= CleanupTargets.OfficeCaches; break;
                case "--error-reports": targets |= CleanupTargets.ErrorReports; break;
                case "--crash-dumps": targets |= CleanupTargets.CrashDumps; break;
                case "--windows-minidumps": targets |= CleanupTargets.WindowsMinidumps; break;
                case "--delivery-optimization-cache": targets |= CleanupTargets.DeliveryOptimizationCache; break;
                case "--recycle-bin": targets |= CleanupTargets.RecycleBin; break;
                case "--thumbnail-cache": targets |= CleanupTargets.ThumbnailCache; break;
                case "--icon-cache": targets |= CleanupTargets.IconCache; break;
                case "--json": json = true; break;
                case "--execute": execute = true; break;
                case "--days" when i + 1 < args.Length && int.TryParse(args[++i], out int value): days = value; break;
                case "--max-files" when i + 1 < args.Length && int.TryParse(args[++i], out int limit): maximumFiles = limit; break;
                default: Console.Error.WriteLine("Use --user-temp, --windows-temp, --system-temp, --shader-caches, --browser-caches, --office-caches, --thumbnail-cache, --icon-cache, --error-reports, --crash-dumps, --windows-minidumps, --delivery-optimization-cache, --recycle-bin, --days 1..365, --max-files 1..10000, --json. --execute permanently removes eligible files up to the selected per-target limit (default 100); default is preview. Recycle Bin cleanup is all-or-nothing and its age is not filtered. Cache rebuilding or redownloading may cause delays, and removing error reports, dumps or Recycle Bin items discards data."); return 2;
            }
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (execute)
            {
                var removal = CleanupRemoval.RunAsync(new(targets, days, maximumFiles),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Cleanup"),
                    new Progress<string>(Console.Error.WriteLine), token: stop.Token).GetAwaiter().GetResult();
                Console.WriteLine(json ? JsonSerializer.Serialize(removal, Capture.JsonOptions) : removal.Format());
                return removal.Status == "Stopped" ? 130 : removal.Status == "Finished" ? 0 : 3;
            }
            var result = CleanupPreview.Inspect(new(targets, days, maximumFiles), new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(json ? JsonSerializer.Serialize(result, Capture.JsonOptions) : result.Format());
            return result.Partial ? 3 : 0;
        }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
        catch (OperationCanceledException) { Console.Error.WriteLine("Cleanup stopped. Review any operation journal for completed actions."); return 130; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        { Console.Error.WriteLine("Cleanup did not finish: " + e.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
