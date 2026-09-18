using System.Security.Cryptography;

namespace KillerBench.Engine;

public sealed record DuplicateFile(string Path, long Length, string Sha256);
public sealed record DuplicateFileGroup(long Length, string Sha256, IReadOnlyList<DuplicateFile> Files);
public sealed record DuplicateFileReviewResult(string Root, int FilesExamined, bool Complete,
    IReadOnlyList<DuplicateFileGroup> Groups, string Detail);

public static class DuplicateFileReview
{
    public static async Task<DuplicateFileReviewResult> ScanAsync(string root, long minimumBytes = 1,
        int maximumFiles = 10_000, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Choose a folder to review.", nameof(root));
        if (minimumBytes < 0) throw new ArgumentOutOfRangeException(nameof(minimumBytes));
        if (maximumFiles is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(maximumFiles));
        string fullRoot = Path.GetFullPath(root);
        if (!Path.IsPathFullyQualified(fullRoot) || fullRoot.StartsWith("\\\\", StringComparison.Ordinal))
            throw new ArgumentException("Duplicate review accepts one local folder only.", nameof(root));
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException("The selected folder does not exist.");

        var candidates = new List<FileInfo>();
        bool complete = true;
        try
        {
            foreach (string path in Directory.EnumerateFiles(fullRoot, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                token.ThrowIfCancellationRequested();
                if (candidates.Count >= maximumFiles) { complete = false; break; }
                try
                {
                    var file = new FileInfo(path);
                    if ((file.Attributes & FileAttributes.ReparsePoint) == 0 && file.Length >= minimumBytes) candidates.Add(file);
                }
                catch (IOException) { complete = false; }
                catch (UnauthorizedAccessException) { complete = false; }
            }
        }
        catch (UnauthorizedAccessException) { complete = false; }
        catch (IOException) { complete = false; }

        var groups = new List<DuplicateFileGroup>();
        foreach (var lengthGroup in candidates.GroupBy(file => file.Length).Where(group => group.Count() > 1).OrderBy(group => group.Key))
        {
            var hashes = new Dictionary<string, List<DuplicateFile>>(StringComparer.Ordinal);
            foreach (var file in lengthGroup.OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read,
                        131_072, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    string hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, token).ConfigureAwait(false));
                    if (!hashes.TryGetValue(hash, out var matches)) hashes.Add(hash, matches = []);
                    matches.Add(new DuplicateFile(file.FullName, file.Length, hash));
                }
                catch (IOException) { complete = false; }
                catch (UnauthorizedAccessException) { complete = false; }
            }
            groups.AddRange(hashes.Where(pair => pair.Value.Count > 1).OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new DuplicateFileGroup(lengthGroup.Key, pair.Key, pair.Value.AsReadOnly())));
        }
        string detail = complete
            ? "Exact SHA-256 duplicate review completed. Nothing was changed."
            : "Duplicate review is incomplete because a file limit or inaccessible content was encountered. Nothing was changed.";
        return new(fullRoot, candidates.Count, complete, groups.AsReadOnly(), detail);
    }
}
