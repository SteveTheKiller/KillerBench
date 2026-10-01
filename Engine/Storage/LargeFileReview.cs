namespace KillerBench.Engine.Storage;

public sealed record LargeFile(string Path, long Length, DateTimeOffset LastWriteTimeUtc);
public sealed record LargeFileReviewResult(string Root, int FilesExamined, bool Complete,
    IReadOnlyList<LargeFile> Files, string Detail);

public static class LargeFileReview
{
    public static Task<LargeFileReviewResult> ScanAsync(string root, long minimumBytes = 100 * 1024 * 1024,
        int maximumFiles = 10_000, int resultLimit = 500, CancellationToken token = default) => Task.Run(() =>
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Choose a folder to review.", nameof(root));
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumBytes, 1);
        if (maximumFiles is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(maximumFiles));
        if (resultLimit is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(resultLimit));
        string fullRoot = Path.GetFullPath(root);
        if (!Path.IsPathFullyQualified(fullRoot) || fullRoot.StartsWith("\\\\", StringComparison.Ordinal))
            throw new ArgumentException("Large-file review accepts one local folder only.", nameof(root));
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException("The selected folder does not exist.");
        var results = new List<LargeFile>();
        int examined = 0;
        bool complete = true;
        try
        {
            foreach (string path in Directory.EnumerateFiles(fullRoot, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                token.ThrowIfCancellationRequested();
                if (examined >= maximumFiles) { complete = false; break; }
                examined++;
                try
                {
                    var file = new FileInfo(path);
                    if ((file.Attributes & FileAttributes.ReparsePoint) == 0 && file.Length >= minimumBytes)
                        results.Add(new(file.FullName, file.Length, file.LastWriteTimeUtc));
                }
                catch (IOException) { complete = false; }
                catch (UnauthorizedAccessException) { complete = false; }
            }
        }
        catch (IOException) { complete = false; }
        catch (UnauthorizedAccessException) { complete = false; }
        var retained = results.OrderByDescending(file => file.Length).ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase).Take(resultLimit).ToArray();
        if (results.Count > resultLimit) complete = false;
        string detail = complete ? "Large-file review completed. Nothing was changed." : "Large-file review is incomplete because a limit or inaccessible content was encountered. Nothing was changed.";
        return new LargeFileReviewResult(fullRoot, examined, complete, Array.AsReadOnly(retained), detail);
    }, token);
}
