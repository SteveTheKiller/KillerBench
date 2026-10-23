using System.Diagnostics;

namespace KillerBench.Engine.Storage;

/// <summary>Bounds traversal independently of how many files match a review.</summary>
public sealed record FileReviewTraversalLimits
{
    public int MaximumDirectories { get; init; } = 10_000;
    public int MaximumPendingDirectories { get; init; } = 1_024;
    public int MaximumDepth { get; init; } = 128;
    public TimeSpan MaximumDuration { get; init; } = TimeSpan.FromSeconds(30);

    internal void Validate()
    {
        if (MaximumDirectories is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(MaximumDirectories));
        if (MaximumPendingDirectories is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(MaximumPendingDirectories));
        if (MaximumDepth is < 0 or > 1_024) throw new ArgumentOutOfRangeException(nameof(MaximumDepth));
        if (MaximumDuration <= TimeSpan.Zero || MaximumDuration > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(MaximumDuration));
    }
}

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
    internal static IEnumerable<string> Files(string root, IFileReviewDirectoryReader reader, CancellationToken token,
        Action incomplete, FileReviewTraversalLimits limits)
    {
        limits.Validate();
        var elapsed = Stopwatch.StartNew();
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((root, 0));
        int visited = 0;
        while (pending.TryPop(out var current))
        {
            token.ThrowIfCancellationRequested();
            if (!WithinTime() || visited >= limits.MaximumDirectories) { incomplete(); yield break; }
            visited++;
            var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = false,
                AttributesToSkip = FileAttributes.ReparsePoint, ReturnSpecialDirectories = false };
            foreach (string child in Read(() => reader.EnumerateDirectories(current.Path, options), token, incomplete, WithinTime))
            {
                if (current.Depth >= limits.MaximumDepth || pending.Count >= limits.MaximumPendingDirectories)
                { incomplete(); break; }
                pending.Push((child, current.Depth + 1));
            }
            foreach (string file in Read(() => reader.EnumerateFiles(current.Path, options), token, incomplete, WithinTime)) yield return file;
        }

        bool WithinTime() => elapsed.Elapsed < limits.MaximumDuration;
    }

    private static IEnumerable<string> Read(Func<IEnumerable<string>> enumerate, CancellationToken token, Action incomplete, Func<bool> withinTime)
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
                if (!withinTime()) { incomplete(); yield break; }
                bool moved = false;
                string? path = null;
                try { moved = iterator.MoveNext(); if (moved) path = iterator.Current; }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException) { moved = false; incomplete(); }
                if (!withinTime()) { incomplete(); yield break; }
                if (!moved) break;
                yield return path!;
            }
        }
    }
}
