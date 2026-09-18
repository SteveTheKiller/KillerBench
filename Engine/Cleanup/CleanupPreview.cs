namespace KillerBench.Engine;

[Flags]
public enum CleanupTargets { None = 0, UserTemp = 1, WindowsTemp = 2, ShaderCaches = 4, BrowserCaches = 8, ErrorReports = 16, CrashDumps = 32, WindowsMinidumps = 64, OfficeCaches = 128, DeliveryOptimizationCache = 256, RecycleBin = 512, ThumbnailCache = 1024, IconCache = 2048, SystemTemp = 4096 }
public sealed record CleanupOptions(CleanupTargets Targets, int MinimumAgeDays = 7, int MaximumFilesPerTarget = 100)
{
    public void Validate()
    {
        if (Targets == CleanupTargets.None || (Targets & ~(CleanupTargets.UserTemp | CleanupTargets.WindowsTemp | CleanupTargets.ShaderCaches | CleanupTargets.BrowserCaches | CleanupTargets.ErrorReports | CleanupTargets.CrashDumps | CleanupTargets.WindowsMinidumps | CleanupTargets.OfficeCaches | CleanupTargets.DeliveryOptimizationCache | CleanupTargets.RecycleBin | CleanupTargets.ThumbnailCache | CleanupTargets.IconCache | CleanupTargets.SystemTemp)) != 0)
            throw new ArgumentException("Select at least one supported cleanup target.");
        if (MinimumAgeDays is < 1 or > 365) throw new ArgumentException("Minimum age must be between 1 and 365 days.");
        if (MaximumFilesPerTarget is < 1 or > 10_000) throw new ArgumentException("File limit must be between 1 and 10,000 per target.");
    }
}
public sealed record CleanupCandidate(string Path, long Bytes, DateTime LastWriteUtc);
public sealed record CleanupEstimate(string Target, string Root, long CandidateFiles, long CandidateBytes,
    long SkippedEntries, bool Partial, IReadOnlyList<CleanupCandidate> Examples, string Detail)
{
    public bool AggregateAction { get; init; }
}
public sealed record CleanupPreviewResult(CleanupOptions Options, DateTimeOffset CapturedUtc, IReadOnlyList<CleanupEstimate> Targets)
{
    public bool Partial => Targets.Any(t => t.Partial);
    public string Format() => "CLEANUP / PREVIEW ONLY\nNo files removed. Estimates are logical file sizes, not guaranteed disk space.\n" +
        $"Minimum age: {Options.MinimumAgeDays} days since creation, modification and access.\n\n" +
        string.Join("\n\n", Targets.Select(t => $"{t.Target}: {t.CandidateFiles:N0} candidates, {t.CandidateBytes:N0} bytes" +
            $"\n{t.Root}\nSkipped entries: {t.SkippedEntries:N0}. {(t.Partial ? "PARTIAL. " : "")}{t.Detail}")) +
        $"\n\nPreview does not prove files are unused. Optional removal rechecks up to {Options.MaximumFilesPerTarget:N0} retained candidates per ordinary target before permanent deletion. Recycle Bin cleanup is all-or-nothing, ignores the age setting and is blocked above that item limit. Shader, Office and Delivery Optimization caches may need rebuilding or redownloading, causing initial delays. Removing error reports, crash dumps or Recycle Bin items discards data.";
}

public static class CleanupPreview
{
    public static string SystemTempRoot() => SystemTempRoot(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows));

    public static string SystemTempRoot(string windowsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowsDirectory);
        return Path.Combine(Path.GetFullPath(windowsDirectory), "SystemTemp");
    }

    public static CleanupEstimate InspectSystemTemp(string windowsDirectory, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.SystemTemp))
            throw new ArgumentException("Select the Windows SystemTemp cleanup target.");
        token.ThrowIfCancellationRequested();
        string root = SystemTempRoot(windowsDirectory);
        return Directory.Exists(root)
            ? InspectDirectory("Windows SystemTemp files", root, options.MinimumAgeDays, now, progress, token,
                maximumCandidates: options.MaximumFilesPerTarget)
            : new CleanupEstimate("Windows SystemTemp files", root, 0, 0, 0, false, [],
                "Target is absent; no temporary files are retained at the standard system location.");
    }

    public static string ThumbnailCacheRoot() => ThumbnailCacheRoot(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static string ThumbnailCacheRoot(string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);
        return Path.Combine(Path.GetFullPath(localApplicationData), "Microsoft", "Windows", "Explorer");
    }

    public static bool IsThumbnailCacheFile(string path)
    {
        string name = Path.GetFileName(path);
        return name.StartsWith("thumbcache_", StringComparison.OrdinalIgnoreCase) &&
            name.EndsWith(".db", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIconCacheFile(string path)
    {
        string name = Path.GetFileName(path);
        return name.StartsWith("iconcache_", StringComparison.OrdinalIgnoreCase) &&
            name.EndsWith(".db", StringComparison.OrdinalIgnoreCase);
    }

    public static CleanupEstimate InspectThumbnailCache(string localApplicationData, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.ThumbnailCache))
            throw new ArgumentException("Select the thumbnail cache cleanup target.");
        token.ThrowIfCancellationRequested();
        string root = ThumbnailCacheRoot(localApplicationData);
        return Directory.Exists(root)
            ? InspectDirectory("Current user thumbnail cache", root, options.MinimumAgeDays, now, progress, token,
                maximumCandidates: options.MaximumFilesPerTarget, includeFile: IsThumbnailCacheFile)
            : new CleanupEstimate("Current user thumbnail cache", root, 0, 0, 0, false, [],
                "Target is absent; no thumbnail database files are retained at the standard location.");
    }

    public static CleanupEstimate InspectIconCache(string localApplicationData, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.IconCache))
            throw new ArgumentException("Select the icon cache cleanup target.");
        token.ThrowIfCancellationRequested();
        string root = ThumbnailCacheRoot(localApplicationData);
        return Directory.Exists(root)
            ? InspectDirectory("Current user icon cache", root, options.MinimumAgeDays, now, progress, token,
                maximumCandidates: options.MaximumFilesPerTarget, includeFile: IsIconCacheFile)
            : new CleanupEstimate("Current user icon cache", root, 0, 0, 0, false, [],
                "Target is absent; no icon database files are retained at the standard location.");
    }

    public static string DeliveryOptimizationCacheRoot() => DeliveryOptimizationCacheRoot(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows));

    public static string DeliveryOptimizationCacheRoot(string windowsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowsDirectory);
        return Path.Combine(Path.GetFullPath(windowsDirectory), "ServiceProfiles", "NetworkService", "AppData", "Local", "Microsoft", "Windows", "DeliveryOptimization", "Cache");
    }

    public static CleanupEstimate InspectDeliveryOptimizationCache(string windowsDirectory, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.DeliveryOptimizationCache))
            throw new ArgumentException("Select the Delivery Optimization cache cleanup target.");
        token.ThrowIfCancellationRequested();
        string root = DeliveryOptimizationCacheRoot(windowsDirectory);
        return Directory.Exists(root)
            ? InspectDirectory("Delivery Optimization cache", root, options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget)
            : new CleanupEstimate("Delivery Optimization cache", root, 0, 0, 0, false, [], "Target is absent; no cached delivery files are retained at the standard service location.");
    }

    public static IReadOnlyList<string> ShaderRoots() => new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMD", "DxCache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "GLCache")
    };

    public static IReadOnlyList<string> OfficeCacheRoots() => OfficeCacheRoots(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static IReadOnlyList<string> OfficeCacheRoots(string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);
        string root = Path.GetFullPath(localApplicationData);
        return new[]
        {
            Path.Combine(root, "Microsoft", "Office", "16.0", "OfficeFileCache"),
            Path.Combine(root, "Microsoft", "Office", "OTele")
        };
    }

    public static IReadOnlyList<CleanupEstimate> InspectOfficeCaches(string localApplicationData, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.OfficeCaches))
            throw new ArgumentException("Select the Office cache cleanup target.");
        token.ThrowIfCancellationRequested();
        return OfficeCacheRoots(localApplicationData).Select(root => Directory.Exists(root)
            ? InspectDirectory("Current user Office cache: " + Path.GetFileName(root), root, options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget)
            : new CleanupEstimate("Current user Office cache: " + Path.GetFileName(root), root, 0, 0, 0, false, [], "Target is absent; no cache files are retained at this standard location.")).ToArray();
    }

    public static IReadOnlyList<string> ErrorReportRoots() => ErrorReportRoots(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static IReadOnlyList<string> ErrorReportRoots(string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);
        string root = Path.GetFullPath(localApplicationData);
        return new[]
        {
            Path.Combine(root, "Microsoft", "Windows", "WER", "ReportArchive"),
            Path.Combine(root, "Microsoft", "Windows", "WER", "ReportQueue")
        };
    }

    public static string CrashDumpRoot() => CrashDumpRoot(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static string CrashDumpRoot(string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);
        return Path.Combine(Path.GetFullPath(localApplicationData), "CrashDumps");
    }

    public static CleanupEstimate InspectCrashDumps(string localApplicationData, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.CrashDumps))
            throw new ArgumentException("Select the crash-dump cleanup target.");
        token.ThrowIfCancellationRequested();
        string root = CrashDumpRoot(localApplicationData);
        return Directory.Exists(root)
            ? InspectDirectory("Current user application crash dumps", root, options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget)
            : new CleanupEstimate("Current user application crash dumps", root, 0, 0, 0, false, [], "Target is absent; no dumps are retained at this standard location.");
    }

    public static string WindowsMinidumpRoot() => WindowsMinidumpRoot(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows));

    public static string WindowsMinidumpRoot(string windowsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowsDirectory);
        return Path.Combine(Path.GetFullPath(windowsDirectory), "Minidump");
    }

    public static CleanupEstimate InspectWindowsMinidumps(string windowsDirectory, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.WindowsMinidumps))
            throw new ArgumentException("Select the Windows minidump cleanup target.");
        token.ThrowIfCancellationRequested();
        string root = WindowsMinidumpRoot(windowsDirectory);
        return Directory.Exists(root)
            ? InspectDirectory("Windows crash minidumps", root, options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget)
            : new CleanupEstimate("Windows crash minidumps", root, 0, 0, 0, false, [], "Target is absent; no minidumps are retained at this standard location.");
    }

    public static IReadOnlyList<CleanupEstimate> InspectErrorReports(string localApplicationData, CleanupOptions options,
        DateTimeOffset now, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (!options.Targets.HasFlag(CleanupTargets.ErrorReports))
            throw new ArgumentException("Select the error-report cleanup target.");
        token.ThrowIfCancellationRequested();
        return ErrorReportRoots(localApplicationData).Select(root => Directory.Exists(root)
            ? InspectDirectory("Current user Windows error reports", root, options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget)
            : new CleanupEstimate("Current user Windows error reports", root, 0, 0, 0, false, [], "Target is absent; no reports are retained at this standard location.")).ToArray();
    }

    public static CleanupPreviewResult Inspect(CleanupOptions options, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        var now = DateTimeOffset.UtcNow;
        var targets = new List<CleanupEstimate>();
        if (options.Targets.HasFlag(CleanupTargets.UserTemp))
            targets.Add(InspectDirectory("Current user temporary files", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"), options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget));
        if (options.Targets.HasFlag(CleanupTargets.WindowsTemp))
            targets.Add(InspectDirectory("Windows temporary files", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget));
        if (options.Targets.HasFlag(CleanupTargets.SystemTemp))
            targets.Add(InspectSystemTemp(Environment.GetFolderPath(Environment.SpecialFolder.Windows), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.ShaderCaches))
            foreach (string root in ShaderRoots())
                targets.Add(InspectDirectory("Current user shader cache: " + Path.GetFileName(root), root, options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget));
        if (options.Targets.HasFlag(CleanupTargets.BrowserCaches))
        {
            var discovery = BrowserCacheRoots.Discover(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), token);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            bool partial = discovery.Partial;
            foreach (string root in discovery.Roots)
            {
                if (timer.Elapsed > TimeSpan.FromSeconds(60)) { partial = true; break; }
                targets.Add(InspectDirectory("Current user browser cache", root, options.MinimumAgeDays, now, progress, token, maximumCandidates: options.MaximumFilesPerTarget));
            }
            targets.Add(new("Browser profile discovery", "", 0, 0, 0, partial, [],
                "Default Chrome, Edge, Brave, Firefox and Opera cache locations only; custom locations are not searched. Cookies, saved logins and profile settings are outside these targets."));
        }
        if (options.Targets.HasFlag(CleanupTargets.OfficeCaches))
            targets.AddRange(InspectOfficeCaches(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.ErrorReports))
            targets.AddRange(InspectErrorReports(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.CrashDumps))
            targets.Add(InspectCrashDumps(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.WindowsMinidumps))
            targets.Add(InspectWindowsMinidumps(Environment.GetFolderPath(Environment.SpecialFolder.Windows), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.DeliveryOptimizationCache))
            targets.Add(InspectDeliveryOptimizationCache(Environment.GetFolderPath(Environment.SpecialFolder.Windows), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.ThumbnailCache))
            targets.Add(InspectThumbnailCache(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.IconCache))
            targets.Add(InspectIconCache(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), options, now, progress, token));
        if (options.Targets.HasFlag(CleanupTargets.RecycleBin))
        {
            progress?.Report("Inspecting the current shell Recycle Bin without opening item contents...");
            targets.Add(RecycleBinCleanup.Inspect(options.MaximumFilesPerTarget));
        }
        return new(options, now, targets);
    }

    public static CleanupEstimate InspectDirectory(string target, string root, int minimumAgeDays, DateTimeOffset now,
        IProgress<string>? progress = null, CancellationToken token = default, int maximumEntries = 100_000, int maximumCandidates = 100,
        Func<string, bool>? includeFile = null)
    {
        new CleanupOptions(CleanupTargets.UserTemp, minimumAgeDays).Validate();
        token.ThrowIfCancellationRequested();
        if (maximumEntries is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        if (maximumCandidates is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(maximumCandidates));
        root = Path.GetFullPath(root);
        var cutoff = now.UtcDateTime.AddDays(-minimumAgeDays);
        long files = 0, bytes = 0, skipped = 0;
        int visited = 0;
        bool partial = false;
        var examples = new List<CleanupCandidate>();
        var pending = new Stack<string>();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            for (var parent = new DirectoryInfo(root); parent is not null; parent = parent.Parent)
            {
                if (!parent.Exists) return new(target, root, 0, 0, 1, true, [], "Target or parent is absent or unavailable.");
                if ((parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    return new(target, root, 0, 0, 1, true, [], "Linked target or parent excluded.");
            }
            pending.Push(root);
            progress?.Report("Estimating " + target + "...");
            while (pending.TryPop(out var directory))
            {
                token.ThrowIfCancellationRequested();
                if (timer.Elapsed > TimeSpan.FromSeconds(15) || visited >= maximumEntries) { partial = true; break; }
                try
                {
                    if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) { skipped++; continue; }
                    foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
                    {
                        token.ThrowIfCancellationRequested();
                        if (timer.Elapsed > TimeSpan.FromSeconds(15) || visited++ >= maximumEntries) { partial = true; break; }
                        try
                        {
                            var attributes = File.GetAttributes(entry);
                            if ((attributes & FileAttributes.ReparsePoint) != 0) { skipped++; continue; }
                            if ((attributes & FileAttributes.Directory) != 0) { pending.Push(entry); continue; }
                            if (includeFile is not null && !includeFile(entry)) { skipped++; continue; }
                            var info = new FileInfo(entry);
                            if ((attributes & (FileAttributes.System | FileAttributes.ReadOnly | FileAttributes.Offline)) != 0 ||
                                info.CreationTimeUtc > cutoff || info.LastWriteTimeUtc > cutoff || info.LastAccessTimeUtc > cutoff)
                            { skipped++; continue; }
                            long length = info.Length;
                            bytes = checked(bytes + length);
                            files++;
                            if (examples.Count < maximumCandidates) examples.Add(new(info.FullName, length, info.LastWriteTimeUtc));
                        }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { skipped++; partial = true; }
                    }
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { skipped++; partial = true; }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { return new(target, root, files, bytes, skipped, true, examples, "Target unavailable: " + e.Message); }
        progress?.Report($"{target}: {files:N0} candidate files, {bytes:N0} bytes.");
        return new(target, root, files, bytes, skipped, partial, examples,
            $"Links and protected or recent files excluded. Up to {maximumCandidates:N0} candidates retained. Each target is bounded to 15 seconds and 100,000 entries.");
    }
}
