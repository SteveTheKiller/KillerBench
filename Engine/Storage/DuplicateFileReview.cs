using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Storage;

public sealed record DuplicateFile(string Path, long Length, string Sha256);
public sealed record DuplicateFileGroup(long Length, string Sha256, IReadOnlyList<DuplicateFile> Files);
/// <summary>FilesExamined counts every file visited and is what the file cap bounds; CandidateFiles counts those large enough to compare.</summary>
public sealed record DuplicateFileReviewResult(string Root, int FilesExamined, bool Complete,
    IReadOnlyList<DuplicateFileGroup> Groups, string Detail)
{
    public int CandidateFiles { get; init; }
}

public static class DuplicateFileReview
{
    public static Task<DuplicateFileReviewResult> ScanAsync(string root, long minimumBytes = 1,
        int maximumFiles = 10_000, CancellationToken token = default, IFileReviewDirectoryReader? directoryReader = null,
        FileReviewTraversalLimits? traversalLimits = null, long maximumHashBytes = 100L * 1024 * 1024 * 1024) => Task.Run(async () =>
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Choose a folder to review.", nameof(root));
        ArgumentOutOfRangeException.ThrowIfNegative(minimumBytes);
        if (maximumFiles is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(maximumFiles));
        string fullRoot = Path.GetFullPath(root);
        if (!Path.IsPathFullyQualified(fullRoot) || fullRoot.StartsWith("\\\\", StringComparison.Ordinal))
            throw new ArgumentException("Duplicate review accepts one local folder only.", nameof(root));
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException("The selected folder does not exist.");

        ArgumentOutOfRangeException.ThrowIfNegative(maximumHashBytes);
        var candidates = new List<(FileInfo File, FileSnapshot Snapshot)>();
        int examined = 0;
        bool complete = true;
        try
        {
            foreach (string path in FileReviewDirectoryWalker.Files(fullRoot,
                directoryReader ?? SystemFileReviewDirectoryReader.Instance, token, () => complete = false, traversalLimits ?? new FileReviewTraversalLimits()))
            {
                token.ThrowIfCancellationRequested();
                if (examined >= maximumFiles) { complete = false; break; }
                examined++;
                try
                {
                    var file = new FileInfo(path);
                    if ((file.Attributes & FileAttributes.ReparsePoint) == 0 && file.Length >= minimumBytes)
                    {
                        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var snapshot = Snapshot(handle);
                        if ((snapshot.Attributes & FileAttributes.ReparsePoint) == 0 && snapshot.Length >= minimumBytes)
                            candidates.Add((file, snapshot));
                    }
                }
                catch (IOException) { complete = false; }
                catch (UnauthorizedAccessException) { complete = false; }
            }
        }
        catch (UnauthorizedAccessException) { complete = false; }
        catch (IOException) { complete = false; }

        var groups = new List<DuplicateFileGroup>();
        long hashedBytes = 0;
        foreach (var lengthGroup in candidates.GroupBy(file => file.Snapshot.Length).Where(group => group.Count() > 1).OrderBy(group => group.Key))
        {
            var hashes = new Dictionary<string, List<DuplicateFile>>(StringComparer.Ordinal);
            foreach (var candidate in lengthGroup.OrderBy(file => file.File.FullName, StringComparer.OrdinalIgnoreCase))
            {
                var file = candidate.File;
                token.ThrowIfCancellationRequested();
                try
                {
                    await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read,
                        1, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    var before = Snapshot(stream.SafeFileHandle);
                    if (before != candidate.Snapshot || before.Length > maximumHashBytes - hashedBytes)
                    { complete = false; continue; }
                    using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    byte[] buffer = new byte[131_072];
                    long consumed = 0;
                    while (consumed < before.Length)
                    {
                        token.ThrowIfCancellationRequested();
                        int allowed = (int)Math.Min(buffer.Length, Math.Min(before.Length - consumed, maximumHashBytes - hashedBytes));
                        if (allowed == 0) break;
                        int read = await stream.ReadAsync(buffer.AsMemory(0, allowed), token).ConfigureAwait(false);
                        if (read == 0) break;
                        hashedBytes += read;
                        consumed += read;
                        digest.AppendData(buffer, 0, read);
                    }
                    if (consumed != before.Length || Snapshot(stream.SafeFileHandle) != before)
                    { complete = false; continue; }
                    string hash = Convert.ToHexString(digest.GetHashAndReset());
                    if (!hashes.TryGetValue(hash, out var matches)) hashes.Add(hash, matches = []);
                    matches.Add(new DuplicateFile(file.FullName, consumed, hash));
                }
                catch (IOException) { complete = false; }
                catch (UnauthorizedAccessException) { complete = false; }
            }
            groups.AddRange(hashes.Where(pair => pair.Value.Count > 1).OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new DuplicateFileGroup(lengthGroup.Key, pair.Key, pair.Value.AsReadOnly())));
        }
        string detail = complete
            ? "Exact SHA-256 duplicate review completed. Nothing was changed."
            : "Duplicate review is incomplete because a file, directory, depth, pending-directory, elapsed-time or hashing limit, or inaccessible content, was encountered. Some content was omitted. Nothing was changed.";
        return new DuplicateFileReviewResult(fullRoot, examined, complete, groups.AsReadOnly(), detail) { CandidateFiles = candidates.Count };
    }, token);

    private sealed record FileSnapshot(long Length, long Creation, long Modified, FileAttributes Attributes,
        uint Volume = 0, uint IndexHigh = 0, uint IndexLow = 0);

    private static FileSnapshot Snapshot(SafeFileHandle handle)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!GetFileInformationByHandle(handle, out var info))
                throw new IOException("File identity could not be verified.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            return new(((long)info.SizeHigh << 32) | info.SizeLow, info.Creation, info.Modified,
                (FileAttributes)info.Attributes, info.Volume, info.IndexHigh, info.IndexLow);
        }
        return new(RandomAccess.GetLength(handle), File.GetCreationTimeUtc(handle).Ticks,
            File.GetLastWriteTimeUtc(handle).Ticks, File.GetAttributes(handle));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct HandleInformation
    {
        public uint Attributes;
        public long Creation;
        public long Accessed;
        public long Modified;
        public uint Volume;
        public uint SizeHigh;
        public uint SizeLow;
        public uint Links;
        public uint IndexHigh;
        public uint IndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out HandleInformation information);
}
