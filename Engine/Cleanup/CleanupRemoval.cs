using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Cleanup;

public sealed record CleanupFileOutcome(string Path, string Status, string Detail);
public sealed record CleanupRemovalResult(CleanupOptions Options, string Status, string JournalPath,
    IReadOnlyList<CleanupFileOutcome> Files, bool Partial)
{
    public string Format() => $"CLEANUP / {Status}\nDeleted: {Files.Count(f => f.Status == "Deleted")}\n" +
        $"Skipped: {Files.Count(f => f.Status == "Skipped")}\nUnverified or failed: {Files.Count(f => f.Status == "Unverified")}\n" +
        $"Partial coverage: {Partial}\nJournal: {JournalPath}\n\n" +
        "Only selected temporary-file, cache, error-report, crash-dump and Recycle Bin targets were considered. Deletions are permanent and cannot be undone. Recycle Bin cleanup is all-or-nothing and guarded by the selected item limit. Thumbnail, icon, shader, Office and Delivery Optimization caches may need rebuilding or redownloading, causing initial delays. Removing error reports, crash dumps or Recycle Bin items discards data. Logical file sizes do not prove reclaimed disk space.";
}

public interface ICleanupRemovalHost
{
    CleanupPreviewResult Inspect(CleanupOptions options, IProgress<string>? progress, CancellationToken token);
    CleanupFileOutcome Remove(string root, CleanupCandidate candidate, DateTime cutoffUtc);
    CleanupFileOutcome RemoveAggregate(CleanupEstimate target, int maximumItems) =>
        new(target.Root, "Skipped", "This provider does not support the selected aggregate cleanup target.");
}

public static class CleanupRemoval
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<CleanupRemovalResult> RunAsync(CleanupOptions options, string journalFolder,
        IProgress<string>? progress = null, ICleanupRemovalHost? host = null, CancellationToken token = default)
    {
        options.Validate();
        await Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            host ??= new WindowsCleanupRemovalHost();
            var preview = host.Inspect(options, progress, token);
            Directory.CreateDirectory(journalFolder);
            string journal = Path.Combine(journalFolder, "cleanup-" + Guid.NewGuid().ToString("N") + ".jsonl");
            using var stream = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            void Record(object value) { writer.WriteLine(JsonSerializer.Serialize(value)); writer.Flush(); stream.Flush(true); }
            var outcomes = new List<CleanupFileOutcome>();
            bool partial = preview.Partial;
            string status = "Finished";
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            Record(new { Stage = "Started", Options = options, preview.CapturedUtc, preview.Partial });
            try
            {
                foreach (var target in preview.Targets)
                {
                    if (target.AggregateAction)
                    {
                        Record(new { Stage = "Prepared", target.Root, target.CandidateFiles, target.CandidateBytes });
                        CleanupFileOutcome aggregate = target.Partial || target.CandidateFiles > options.MaximumFilesPerTarget
                            ? new(target.Root, "Skipped", "Aggregate target is incomplete or above the selected item limit.")
                            : target.CandidateFiles == 0
                                ? new(target.Root, "Skipped", "Aggregate target is already empty.")
                                : host.RemoveAggregate(target, options.MaximumFilesPerTarget);
                        outcomes.Add(aggregate);
                        Record(new { Stage = "Result", Outcome = aggregate });
                        progress?.Report(aggregate.Status + ": " + target.Target);
                        continue;
                    }
                    // Only retained, individually identified candidates can be acted on.
                    partial |= target.CandidateFiles > Math.Min(options.MaximumFilesPerTarget, target.Examples.Count);
                    foreach (var candidate in target.Examples.Take(options.MaximumFilesPerTarget))
                    {
                        token.ThrowIfCancellationRequested();
                        if (elapsed.Elapsed > TimeSpan.FromMinutes(2)) { partial = true; status = "Time limit reached"; break; }
                        Record(new { Stage = "Prepared", target.Root, Candidate = candidate });
                        CleanupFileOutcome outcome;
                        try { outcome = host.Remove(target.Root, candidate, preview.CapturedUtc.UtcDateTime.AddDays(-options.MinimumAgeDays)); }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
                        { outcome = new(candidate.Path, "Unverified", e.Message); }
                        outcomes.Add(outcome);
                        Record(new { Stage = "Result", Outcome = outcome });
                        progress?.Report($"{outcome.Status}: {candidate.Path}");
                    }
                    if (status == "Time limit reached") break;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { status = "Stopped"; partial = true; }
            if (status == "Finished" && (partial || outcomes.Any(o => o.Status == "Unverified"))) status = "Needs review";
            var result = new CleanupRemovalResult(options, status, journal, outcomes, partial);
            Record(new { Stage = "Finished", Result = result });
            return result;
        }
        finally { Gate.Release(); }
    }
}

public sealed partial class WindowsCleanupRemovalHost : ICleanupRemovalHost
{
    public CleanupPreviewResult Inspect(CleanupOptions options, IProgress<string>? progress, CancellationToken token) =>
        CleanupPreview.Inspect(options, progress, token);

    public CleanupFileOutcome Remove(string root, CleanupCandidate candidate, DateTime cutoffUtc)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        string path = Path.GetFullPath(candidate.Path);
        if (cutoffUtc > DateTime.UtcNow.AddDays(-1) || candidate.Bytes < 0 || path.AsSpan(2).Contains(':'))
            return new(path, "Skipped", "Invalid age, size or alternate-stream target.");
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string userRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
        string windowsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        string systemTempRoot = CleanupPreview.SystemTempRoot();
        bool thumbnailCandidate = CleanupPreview.ThumbnailCacheRoot().Equals(root, StringComparison.OrdinalIgnoreCase) &&
            CleanupPreview.IsThumbnailCacheFile(path);
        bool iconCandidate = CleanupPreview.ThumbnailCacheRoot().Equals(root, StringComparison.OrdinalIgnoreCase) &&
            CleanupPreview.IsIconCacheFile(path);
        if ((!root.Equals(userRoot, StringComparison.OrdinalIgnoreCase) && !root.Equals(windowsRoot, StringComparison.OrdinalIgnoreCase) &&
            !root.Equals(systemTempRoot, StringComparison.OrdinalIgnoreCase) &&
            !CleanupPreview.ShaderRoots().Contains(root, StringComparer.OrdinalIgnoreCase) &&
            !CleanupPreview.OfficeCacheRoots().Contains(root, StringComparer.OrdinalIgnoreCase) &&
            !CleanupPreview.ErrorReportRoots().Contains(root, StringComparer.OrdinalIgnoreCase) &&
            !CleanupPreview.CrashDumpRoot().Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !CleanupPreview.WindowsMinidumpRoot().Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !CleanupPreview.DeliveryOptimizationCacheRoot().Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !thumbnailCandidate && !iconCandidate &&
            !BrowserCacheRoots.IsKnownRoot(root, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))) ||
            !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return new(path, "Skipped", "Path is outside supported temporary-file, cache, error-report and crash-dump targets.");

        // DELETE and FILE_READ_ATTRIBUTES, exclusive sharing, existing file, open the link itself.
        using (var handle = CreateFileW(path, 0x10080, 0, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero))
        {
            if (handle.IsInvalid) return new(path, "Skipped", "File is unavailable, in use or not permitted. Windows error " + Marshal.GetLastWin32Error());
            var finalPath = new StringBuilder(32768);
            uint length = GetFinalPathNameByHandleW(handle, finalPath, (uint)finalPath.Capacity, 0);
            string resolved = finalPath.ToString();
            if (resolved.StartsWith(@"\\?\", StringComparison.Ordinal)) resolved = resolved[4..];
            if (length == 0 || length >= finalPath.Capacity || !resolved.Equals(path, StringComparison.OrdinalIgnoreCase))
                return new(path, "Skipped", "Resolved file path changed or could not be verified.");
            if (!TryGetFileInformation(handle, out var info)) return new(path, "Skipped", "File metadata could not be verified.");
            const uint excluded = 0x1 | 0x4 | 0x10 | 0x400 | 0x1000;
            long bytes = ((long)info.SizeHigh << 32) | info.SizeLow;
            if ((info.Attributes & excluded) != 0 || info.Links != 1 || bytes != candidate.Bytes ||
                ToUtc(info.Written) != candidate.LastWriteUtc || ToUtc(info.Created) > cutoffUtc ||
                ToUtc(info.Accessed) > cutoffUtc || ToUtc(info.Written) > cutoffUtc)
                return new(path, "Skipped", "File changed, is recent, protected, linked or no longer eligible.");
            byte delete = 1;
            if (!SetFileInformationByHandle(handle, 4, ref delete, 1))
                return new(path, "Unverified", "Windows rejected removal. Error " + Marshal.GetLastWin32Error());
        }
        uint attributes = GetFileAttributesW(path);
        int error = Marshal.GetLastWin32Error();
        return attributes == uint.MaxValue && error is 2 or 3
            ? new(path, "Deleted", "Handle deletion completed and path absence verified.")
            : new(path, "Unverified", "Removal requested, but path absence was not verified. No retry was made.");
    }

    public CleanupFileOutcome RemoveAggregate(CleanupEstimate target, int maximumItems) =>
        target.AggregateAction && target.Target == RecycleBinCleanup.TargetName && target.Root == RecycleBinCleanup.ShellRoot
            ? RecycleBinCleanup.EmptyAndVerify(target.CandidateFiles, target.CandidateBytes, maximumItems)
            : new(target.Root, "Skipped", "Unsupported aggregate cleanup target.");

    private static DateTime ToUtc(System.Runtime.InteropServices.ComTypes.FILETIME time) => DateTime.FromFileTimeUtc(((long)time.dwHighDateTime << 32) | (uint)time.dwLowDateTime);
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Created, Accessed, Written;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, uint size, uint flags);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetFileInformationByHandle(SafeFileHandle handle, FileInformation* info);
    private static unsafe bool TryGetFileInformation(SafeFileHandle handle, out FileInformation info)
    {
        FileInformation value;
        bool ok = GetFileInformationByHandle(handle, &value);
        info = value;
        return ok;
    }
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetFileInformationByHandle(SafeFileHandle handle, int kind, ref byte info, uint size);
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial uint GetFileAttributesW(string path);
}
