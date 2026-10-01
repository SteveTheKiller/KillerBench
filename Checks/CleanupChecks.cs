using System.IO;
using KillerBench.Engine;

internal static class CleanupChecks
{
    internal static void Run(string scratch)
    {
        string root = Path.Combine(scratch, "cleanup");
        Directory.CreateDirectory(root);
        string old = Path.Combine(root, "old.tmp"), recent = Path.Combine(root, "recent.tmp");
        File.WriteAllText(old, "fixture");
        File.WriteAllText(recent, "recent");
        var now = DateTimeOffset.UtcNow;
        File.SetCreationTimeUtc(old, now.UtcDateTime.AddDays(-30));
        File.SetLastWriteTimeUtc(old, now.UtcDateTime.AddDays(-30));
        File.SetLastAccessTimeUtc(old, now.UtcDateTime.AddDays(-30));
        var result = CleanupPreview.InspectDirectory("fixture", root, 7, now);
        Require(result.CandidateFiles == 1 && result.CandidateBytes == 7 && !result.Partial, "Only old files qualify");
        Require(File.Exists(old) && File.Exists(recent), "Preview preserves all files");
        string largerRoot = Path.Combine(scratch, "cleanup-larger");
        Directory.CreateDirectory(largerRoot);
        for (int i = 0; i < 105; i++)
        {
            string path = Path.Combine(largerRoot, i + ".tmp");
            File.WriteAllText(path, "fixture");
            File.SetCreationTimeUtc(path, now.UtcDateTime.AddDays(-30));
            File.SetLastWriteTimeUtc(path, now.UtcDateTime.AddDays(-30));
            File.SetLastAccessTimeUtc(path, now.UtcDateTime.AddDays(-30));
        }
        Require(CleanupPreview.InspectDirectory("fixture", largerRoot, 7, now).Examples.Count == 100 &&
            CleanupPreview.InspectDirectory("fixture", largerRoot, 7, now, maximumCandidates: 250).Examples.Count == 105 &&
            Directory.GetFiles(largerRoot).Length == 105,
            "Larger preview retains additional candidates without removing fixture files");
        Require(CleanupPreview.InspectDirectory("fixture", root, 60, now).CandidateFiles == 0, "Age option affects selection");
        Require(CleanupPreview.InspectDirectory("fixture", root, 7, now, maximumEntries: 1).Partial, "Entry limit reports partial coverage");
        Require(CleanupPreview.InspectDirectory("missing", Path.Combine(root, "missing"), 7, now).Partial, "Missing target is not an empty success");
        Require(CleanupPreview.InspectDirectory("missing", Path.Combine(root, "missing"), 7, now).Detail.Contains("absent or unavailable"), "Missing target must not be reported as a link");
        foreach (var options in new[] { new CleanupOptions(CleanupTargets.None), new CleanupOptions((CleanupTargets)8192), new CleanupOptions(CleanupTargets.UserTemp, 0), new CleanupOptions(CleanupTargets.UserTemp, 366) })
        {
            try { options.Validate(); throw new Exception("Invalid options accepted"); }
            catch (ArgumentException) { }
        }
        try { CleanupPreview.InspectDirectory("fixture", root, 7, now, token: new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        CheckRemoval(scratch).GetAwaiter().GetResult();
        string local = Path.Combine(scratch, "browser-fixture");
        string profile = Path.Combine(local, "Google", "Chrome", "User Data", "Default");
        string cache = Path.Combine(profile, "Cache");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(profile, "Cookies"), "fixture");
        var browser = BrowserCacheRoots.Discover(local);
        string[] expectedBrowserRoots = [cache];
        Require(browser.Roots.SequenceEqual(expectedBrowserRoots) && !browser.Partial, "Browser discovery selects cache folders only");
        Require(BrowserCacheRoots.IsKnownRoot(cache, local) && !BrowserCacheRoots.IsKnownRoot(profile, local) && !BrowserCacheRoots.IsKnownRoot(Path.Combine(profile, "Cookies"), local), "Browser root gate rejects profile and cookie targets");
        for (int i = 0; i < 33; i++) Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(profile)!, "Profile " + i, "Cache"));
        var cappedBrowsers = BrowserCacheRoots.Discover(local);
        Require(cappedBrowsers.Partial && cappedBrowsers.Roots.Count == 32, "Browser profile discovery is bounded");
        string officeCache = Path.Combine(local, "Microsoft", "Office", "16.0", "OfficeFileCache");
        Directory.CreateDirectory(officeCache);
        string oldOfficeCache = Path.Combine(officeCache, "old.dat");
        File.WriteAllText(oldOfficeCache, "office fixture");
        File.SetCreationTimeUtc(oldOfficeCache, now.UtcDateTime.AddDays(-30));
        File.SetLastWriteTimeUtc(oldOfficeCache, now.UtcDateTime.AddDays(-30));
        File.SetLastAccessTimeUtc(oldOfficeCache, now.UtcDateTime.AddDays(-30));
        var office = CleanupPreview.InspectOfficeCaches(local, new(CleanupTargets.OfficeCaches, 7), now);
        Require(office.Count == 2 && office[0].CandidateFiles == 1 && office[0].CandidateBytes == 14 && !office[0].Partial &&
            office[1].CandidateFiles == 0 && !office[1].Partial && File.Exists(oldOfficeCache),
            "Office cache preview retains eligible files and treats an absent standard cache as empty");
        try { CleanupPreview.InspectOfficeCaches(local, new(CleanupTargets.UserTemp), now); throw new Exception("Office cache inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectOfficeCaches(local, new(CleanupTargets.OfficeCaches), now, token: new(true)); throw new Exception("Office cache inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        string reportArchive = Path.Combine(local, "Microsoft", "Windows", "WER", "ReportArchive", "AppCrash_fixture");
        Directory.CreateDirectory(reportArchive);
        string oldReport = Path.Combine(reportArchive, "Report.wer");
        File.WriteAllText(oldReport, "report fixture");
        File.SetCreationTimeUtc(oldReport, now.UtcDateTime.AddDays(-30));
        File.SetLastWriteTimeUtc(oldReport, now.UtcDateTime.AddDays(-30));
        File.SetLastAccessTimeUtc(oldReport, now.UtcDateTime.AddDays(-30));
        var reports = CleanupPreview.InspectErrorReports(local, new(CleanupTargets.ErrorReports, 7), now);
        Require(reports.Count == 2 && reports[0].CandidateFiles == 1 && reports[0].CandidateBytes == 14 && !reports[0].Partial &&
            reports[1].CandidateFiles == 0 && !reports[1].Partial && File.Exists(oldReport),
            "Error-report preview retains eligible files and treats an absent standard queue as empty");
        try { CleanupPreview.InspectErrorReports(local, new(CleanupTargets.UserTemp), now); throw new Exception("Error-report inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectErrorReports(local, new(CleanupTargets.ErrorReports), now, token: new(true)); throw new Exception("Error-report inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        string dumpRoot = CleanupPreview.CrashDumpRoot(local);
        Directory.CreateDirectory(dumpRoot);
        string oldDump = Path.Combine(dumpRoot, "fixture.exe.1234.dmp");
        File.WriteAllText(oldDump, "dump fixture");
        File.SetCreationTimeUtc(oldDump, now.UtcDateTime.AddDays(-30));
        File.SetLastWriteTimeUtc(oldDump, now.UtcDateTime.AddDays(-30));
        File.SetLastAccessTimeUtc(oldDump, now.UtcDateTime.AddDays(-30));
        var dumps = CleanupPreview.InspectCrashDumps(local, new(CleanupTargets.CrashDumps, 7), now);
        Require(dumps.CandidateFiles == 1 && dumps.CandidateBytes == 12 && !dumps.Partial && File.Exists(oldDump) &&
            dumps.Root == dumpRoot, "Crash-dump preview retains eligible evidence under the exact current-user root");
        try { CleanupPreview.InspectCrashDumps(local, new(CleanupTargets.ErrorReports), now); throw new Exception("Crash-dump inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectCrashDumps(local, new(CleanupTargets.CrashDumps), now, token: new(true)); throw new Exception("Crash-dump inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        string windows = Path.Combine(scratch, "windows-fixture");
        string systemTempRoot = CleanupPreview.SystemTempRoot(windows);
        Directory.CreateDirectory(systemTempRoot);
        string oldSystemTemp = Path.Combine(systemTempRoot, "old-system.tmp");
        File.WriteAllText(oldSystemTemp, "system temp fixture");
        File.SetCreationTimeUtc(oldSystemTemp, now.UtcDateTime.AddDays(-30));
        File.SetLastWriteTimeUtc(oldSystemTemp, now.UtcDateTime.AddDays(-30));
        File.SetLastAccessTimeUtc(oldSystemTemp, now.UtcDateTime.AddDays(-30));
        var systemTemp = CleanupPreview.InspectSystemTemp(windows, new(CleanupTargets.SystemTemp, 7), now);
        Require(systemTemp.CandidateFiles == 1 && systemTemp.CandidateBytes == 19 && !systemTemp.Partial &&
            systemTemp.Root == systemTempRoot && File.Exists(oldSystemTemp),
            "Windows SystemTemp preview retains eligible files under the exact independent system root");
        try { CleanupPreview.InspectSystemTemp(windows, new(CleanupTargets.WindowsTemp), now); throw new Exception("SystemTemp inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectSystemTemp(windows, new(CleanupTargets.SystemTemp), now, token: new(true)); throw new Exception("SystemTemp inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        string minidumpRoot = CleanupPreview.WindowsMinidumpRoot(windows);
        Directory.CreateDirectory(minidumpRoot);
        string oldMinidump = Path.Combine(minidumpRoot, "091426-1234-01.dmp");
        File.WriteAllText(oldMinidump, "minidump fixture");
        File.SetCreationTimeUtc(oldMinidump, now.UtcDateTime.AddDays(-30));
        File.SetLastWriteTimeUtc(oldMinidump, now.UtcDateTime.AddDays(-30));
        File.SetLastAccessTimeUtc(oldMinidump, now.UtcDateTime.AddDays(-30));
        var minidumps = CleanupPreview.InspectWindowsMinidumps(windows, new(CleanupTargets.WindowsMinidumps, 7), now);
        Require(minidumps.CandidateFiles == 1 && minidumps.CandidateBytes == 16 && !minidumps.Partial && File.Exists(oldMinidump) &&
            minidumps.Root == minidumpRoot, "Windows minidump preview retains eligible evidence under the exact system root");
        try { CleanupPreview.InspectWindowsMinidumps(windows, new(CleanupTargets.CrashDumps), now); throw new Exception("Windows minidump inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectWindowsMinidumps(windows, new(CleanupTargets.WindowsMinidumps), now, token: new(true)); throw new Exception("Windows minidump inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        string deliveryRoot = CleanupPreview.DeliveryOptimizationCacheRoot(windows);
        Directory.CreateDirectory(deliveryRoot);
        string oldDeliveryFile = Path.Combine(deliveryRoot, "old.cache");
        File.WriteAllText(oldDeliveryFile, "delivery fixture");
        File.SetCreationTimeUtc(oldDeliveryFile, now.UtcDateTime.AddDays(-30));
        File.SetLastWriteTimeUtc(oldDeliveryFile, now.UtcDateTime.AddDays(-30));
        File.SetLastAccessTimeUtc(oldDeliveryFile, now.UtcDateTime.AddDays(-30));
        var delivery = CleanupPreview.InspectDeliveryOptimizationCache(windows, new(CleanupTargets.DeliveryOptimizationCache, 7), now);
        Require(delivery.CandidateFiles == 1 && delivery.CandidateBytes == 16 && !delivery.Partial &&
            delivery.Root == deliveryRoot && File.Exists(oldDeliveryFile),
            "Delivery Optimization preview retains only eligible files under the exact service cache root");
        try { CleanupPreview.InspectDeliveryOptimizationCache(windows, new(CleanupTargets.WindowsTemp), now); throw new Exception("Delivery Optimization inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectDeliveryOptimizationCache(windows, new(CleanupTargets.DeliveryOptimizationCache), now, token: new(true)); throw new Exception("Delivery Optimization inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        string thumbnailRoot = CleanupPreview.ThumbnailCacheRoot(local);
        Directory.CreateDirectory(thumbnailRoot);
        string oldThumbnail = Path.Combine(thumbnailRoot, "thumbcache_256.db");
        string iconCache = Path.Combine(thumbnailRoot, "iconcache_256.db");
        string explorerHistory = Path.Combine(thumbnailRoot, "unrelated.dat");
        foreach (string path in new[] { oldThumbnail, iconCache, explorerHistory })
        {
            File.WriteAllText(path, "thumbnail fixture");
            File.SetCreationTimeUtc(path, now.UtcDateTime.AddDays(-30));
            File.SetLastWriteTimeUtc(path, now.UtcDateTime.AddDays(-30));
            File.SetLastAccessTimeUtc(path, now.UtcDateTime.AddDays(-30));
        }
        var thumbnails = CleanupPreview.InspectThumbnailCache(local, new(CleanupTargets.ThumbnailCache, 7), now);
        Require(thumbnails.CandidateFiles == 1 && thumbnails.CandidateBytes == 17 && !thumbnails.Partial &&
            thumbnails.Examples is [{ Path: var thumbnailPath }] && thumbnailPath == oldThumbnail &&
            File.Exists(iconCache) && File.Exists(explorerHistory),
            "Thumbnail cleanup retains only exact thumbcache database files and excludes other Explorer data");
        Require(CleanupPreview.IsThumbnailCacheFile("thumbcache_32.db") &&
            CleanupPreview.IsThumbnailCacheFile("THUMBCACHE_CUSTOM_STREAM.DB") &&
            !CleanupPreview.IsThumbnailCacheFile("iconcache_32.db") &&
            !CleanupPreview.IsThumbnailCacheFile("thumbcache_32.db.backup"),
            "Thumbnail cache filename allowlist is exact and case-insensitive");
        try { CleanupPreview.InspectThumbnailCache(local, new(CleanupTargets.UserTemp), now); throw new Exception("Thumbnail cache inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectThumbnailCache(local, new(CleanupTargets.ThumbnailCache), now, token: new(true)); throw new Exception("Thumbnail cache inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        var icons = CleanupPreview.InspectIconCache(local, new(CleanupTargets.IconCache, 7), now);
        Require(icons.CandidateFiles == 1 && icons.CandidateBytes == 17 && !icons.Partial &&
            icons.Examples is [{ Path: var iconPath }] && iconPath == iconCache && File.Exists(oldThumbnail) &&
            File.Exists(explorerHistory),
            "Icon cleanup retains only exact iconcache database files and excludes thumbnail and other Explorer data");
        Require(CleanupPreview.IsIconCacheFile("iconcache_32.db") &&
            CleanupPreview.IsIconCacheFile("ICONCACHE_CUSTOM_STREAM.DB") &&
            !CleanupPreview.IsIconCacheFile("thumbcache_32.db") &&
            !CleanupPreview.IsIconCacheFile("iconcache_32.db.backup"),
            "Icon cache filename allowlist is exact and case-insensitive");
        try { CleanupPreview.InspectIconCache(local, new(CleanupTargets.UserTemp), now); throw new Exception("Icon cache inspection accepted another target"); }
        catch (ArgumentException) { }
        try { CleanupPreview.InspectIconCache(local, new(CleanupTargets.IconCache), now, token: new(true)); throw new Exception("Icon cache inspection ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: cleanup age selection, bounded preview, unavailable roots, cancellation and input validation.");
    }
    private static async Task CheckRemoval(string scratch)
    {
        var host = new FakeRemoval();
        var options = new CleanupOptions(CleanupTargets.UserTemp, 14);
        var result = await CleanupRemoval.RunAsync(options, scratch, host: host);
        Require(host.Options == options && result.Files.Count == 3 && result.Status == "Needs review", "Selected options reach collection and mixed outcomes remain visible");
        var shaderHost = new FakeRemoval();
        var shaders = new CleanupOptions(CleanupTargets.ShaderCaches, 30);
        await CleanupRemoval.RunAsync(shaders, scratch, host: shaderHost);
        Require(shaderHost.Options == shaders, "Shader selection and age reach the removal provider without enabling temporary files");
        var officeHost = new FakeRemoval();
        var office = new CleanupOptions(CleanupTargets.OfficeCaches, 30);
        await CleanupRemoval.RunAsync(office, scratch, host: officeHost);
        Require(officeHost.Options == office && CleanupPreview.OfficeCacheRoots().Count == 2 &&
            CleanupPreview.OfficeCacheRoots().Distinct(StringComparer.OrdinalIgnoreCase).Count() == 2 &&
            CleanupPreview.OfficeCacheRoots().All(Path.IsPathFullyQualified),
            "Office cache selection and age reach the removal provider with two exact current-user roots");
        var reportHost = new FakeRemoval();
        var reports = new CleanupOptions(CleanupTargets.ErrorReports, 45);
        await CleanupRemoval.RunAsync(reports, scratch, host: reportHost);
        Require(reportHost.Options == reports, "Error-report selection and age reach the removal provider without enabling other cleanup targets");
        Require(CleanupPreview.ErrorReportRoots().Count == 2 && CleanupPreview.ErrorReportRoots().Distinct(StringComparer.OrdinalIgnoreCase).Count() == 2 &&
            CleanupPreview.ErrorReportRoots().All(path => Path.IsPathFullyQualified(path)), "Error-report cleanup uses two exact current-user roots");
        var dumpHost = new FakeRemoval();
        var dumps = new CleanupOptions(CleanupTargets.CrashDumps, 45);
        await CleanupRemoval.RunAsync(dumps, scratch, host: dumpHost);
        Require(dumpHost.Options == dumps && Path.IsPathFullyQualified(CleanupPreview.CrashDumpRoot()),
            "Crash-dump selection and age reach the removal provider with one exact current-user root");
        var minidumpHost = new FakeRemoval();
        var minidumps = new CleanupOptions(CleanupTargets.WindowsMinidumps, 45);
        await CleanupRemoval.RunAsync(minidumps, scratch, host: minidumpHost);
        Require(minidumpHost.Options == minidumps && Path.IsPathFullyQualified(CleanupPreview.WindowsMinidumpRoot()),
            "Windows minidump selection and age reach the removal provider with one exact system root");
        var systemTempHost = new FakeRemoval();
        var systemTemp = new CleanupOptions(CleanupTargets.SystemTemp, 45);
        await CleanupRemoval.RunAsync(systemTemp, scratch, host: systemTempHost);
        Require(systemTempHost.Options == systemTemp && Path.IsPathFullyQualified(CleanupPreview.SystemTempRoot()),
            "Windows SystemTemp selection and age reach the bounded removal provider with one exact system root");
        var deliveryHost = new FakeRemoval();
        var delivery = new CleanupOptions(CleanupTargets.DeliveryOptimizationCache, 30);
        await CleanupRemoval.RunAsync(delivery, scratch, host: deliveryHost);
        Require(deliveryHost.Options == delivery && Path.IsPathFullyQualified(CleanupPreview.DeliveryOptimizationCacheRoot()),
            "Delivery Optimization cache selection and age reach the bounded removal provider without enabling other cleanup targets");
        var thumbnailHost = new FakeRemoval();
        var thumbnail = new CleanupOptions(CleanupTargets.ThumbnailCache, 30);
        await CleanupRemoval.RunAsync(thumbnail, scratch, host: thumbnailHost);
        Require(thumbnailHost.Options == thumbnail && Path.IsPathFullyQualified(CleanupPreview.ThumbnailCacheRoot()),
            "Thumbnail cache selection and age reach the bounded removal provider with one exact current-user root");
        var iconHost = new FakeRemoval();
        var icon = new CleanupOptions(CleanupTargets.IconCache, 30);
        await CleanupRemoval.RunAsync(icon, scratch, host: iconHost);
        Require(iconHost.Options == icon && Path.IsPathFullyQualified(CleanupPreview.ThumbnailCacheRoot()),
            "Icon cache selection and age reach the bounded removal provider with one exact current-user root");
        var aggregateHost = new AggregateRemoval(12, 4096);
        var recycleBin = new CleanupOptions(CleanupTargets.RecycleBin, 30, 20);
        var aggregateResult = await CleanupRemoval.RunAsync(recycleBin, scratch, host: aggregateHost);
        Require(aggregateHost.EmptyCalls == 1 && aggregateResult.Files is [{ Status: "Deleted" }] &&
            File.ReadAllText(aggregateResult.JournalPath).IndexOf("Prepared", StringComparison.Ordinal) < File.ReadAllText(aggregateResult.JournalPath).IndexOf("Deleted", StringComparison.Ordinal),
            "Recycle Bin aggregate cleanup records intent, respects the selected count, and retains verification");
        aggregateHost = new AggregateRemoval(21, 4096);
        aggregateResult = await CleanupRemoval.RunAsync(recycleBin, scratch, host: aggregateHost);
        Require(aggregateHost.EmptyCalls == 0 && aggregateResult.Files is [{ Status: "Skipped" }] && aggregateResult.Partial,
            "Recycle Bin cleanup remains preview-only above the selected item limit");
        string journal = File.ReadAllText(result.JournalPath);
        Require(journal.IndexOf("Prepared", StringComparison.Ordinal) < journal.IndexOf("Deleted", StringComparison.Ordinal) && journal.Contains("Finished"), "Journal records intent before results and final state");
        using var stop = new CancellationTokenSource();
        var stopped = await CleanupRemoval.RunAsync(options, scratch, token: stop.Token, host: new FakeRemoval { AfterFirst = stop.Cancel });
        Require(stopped.Status == "Stopped" && stopped.Files.Count == 1 && stopped.Partial, "Cancellation preserves completed actions and skips later files");
        var capped = await CleanupRemoval.RunAsync(options, scratch, host: new FakeRemoval { Count = 101 });
        Require(capped.Files.Count == 100 && capped.Partial, "Removal bound cannot be bypassed by a provider");
        var expanded = await CleanupRemoval.RunAsync(options with { MaximumFilesPerTarget = 250 }, scratch, host: new FakeRemoval { Count = 251 });
        Require(expanded.Files.Count == 250 && expanded.Partial && expanded.Options.MaximumFilesPerTarget == 250,
            "Selected larger limit reaches execution while excess candidates stay untouched");
        foreach (int limit in new[] { 0, 10001 })
        {
            try { (options with { MaximumFilesPerTarget = limit }).Validate(); throw new Exception("Invalid file limit accepted"); }
            catch (ArgumentException) { }
        }
        var native = new WindowsCleanupRemovalHost();
        var rejected = native.Remove(Path.GetPathRoot(scratch)!, new(Path.Combine(scratch, "unsupported.tmp"), 0, DateTime.UtcNow.AddDays(-30)), DateTime.UtcNow.AddDays(-7));
        Require(rejected.Status == "Skipped", "Native provider rejects unsupported roots before opening files");
        Console.WriteLine("PASS: cleanup removal option flow, mixed outcomes, journaling, cancellation and limits (simulated removals only).");
    }
    private sealed class FakeRemoval : ICleanupRemovalHost
    {
        public CleanupOptions? Options;
        public Action? AfterFirst;
        public int Count = 3;
        private int calls;
        public CleanupPreviewResult Inspect(CleanupOptions options, IProgress<string>? progress, CancellationToken token)
        {
            Options = options;
            return new(options, DateTimeOffset.UtcNow, [new("fixture", "fixture", Count, Count, 0, false,
                [.. Enumerable.Range(0, Count).Select(i => new CleanupCandidate("fake" + i, 1, DateTime.UtcNow.AddDays(-30)))], "fixture")]);
        }
        public CleanupFileOutcome Remove(string root, CleanupCandidate candidate, DateTime cutoffUtc)
        {
            calls++;
            if (calls == 1) AfterFirst?.Invoke();
            return new(candidate.Path, calls == 1 ? "Deleted" : calls == 2 ? "Skipped" : "Unverified", "simulated");
        }
    }
    private sealed class AggregateRemoval(long items, long bytes) : ICleanupRemovalHost
    {
        public int EmptyCalls;
        public CleanupPreviewResult Inspect(CleanupOptions options, IProgress<string>? progress, CancellationToken token) =>
            new(options, DateTimeOffset.UtcNow, [new(RecycleBinCleanup.TargetName, RecycleBinCleanup.ShellRoot, items, bytes, 0,
                items > options.MaximumFilesPerTarget, [], "fixture") { AggregateAction = true }]);
        public CleanupFileOutcome Remove(string root, CleanupCandidate candidate, DateTime cutoffUtc) => throw new InvalidOperationException();
        public CleanupFileOutcome RemoveAggregate(CleanupEstimate target, int maximumItems)
        {
            EmptyCalls++;
            return new(target.Root, "Deleted", "simulated verified empty state");
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
