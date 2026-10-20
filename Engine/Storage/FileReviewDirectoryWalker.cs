namespace KillerBench.Engine.Storage;

/// <summary>The filesystem enumeration boundary for read-only local file reviews.</summary>
public interface IFileReviewDirectoryReader
{
    IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options);
    IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options);
}

internal sealed class SystemFileReviewDirectoryReader : IFileReviewDirectoryReader
{
    internal static readonly SystemFileReviewDirectoryReader Instance = new();
    public IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options) => Directory.EnumerateFiles(directory, "*", options);
    public IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options) => Directory.EnumerateDirectories(directory, "*", options);
}

internal static class FileReviewDirectoryWalker
{
    internal static IEnumerable<string> Files(string root, IFileReviewDirectoryReader reader, CancellationToken token, Action incomplete)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out string? directory))
        {
            token.ThrowIfCancellationRequested();
            var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = false,
                AttributesToSkip = FileAttributes.ReparsePoint, ReturnSpecialDirectories = false };
            foreach (string child in Read(() => reader.EnumerateDirectories(directory, options), token, incomplete)) pending.Push(child);
            foreach (string file in Read(() => reader.EnumerateFiles(directory, options), token, incomplete)) yield return file;
        }
    }

    private static IEnumerable<string> Read(Func<IEnumerable<string>> enumerate, CancellationToken token, Action incomplete)
    {
        IEnumerator<string>? iterator = null;
        try { iterator = enumerate().GetEnumerator(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException) { incomplete(); }
        if (iterator is null) yield break;
        using (iterator)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                bool moved = false;
                string? path = null;
                try { moved = iterator.MoveNext(); if (moved) path = iterator.Current; }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException) { moved = false; incomplete(); }
                if (!moved) break;
                yield return path!;
            }
        }
    }
}
