using System.IO;
using KillerBench.Engine.Storage;

internal static class StorageReviewScopeChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "scope-swap");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "original.bin"), "original");
        try
        {
            var reader = new SwappingReader(root);
            var result = await LargeFileReview.ScanAsync(root, minimumBytes: 1, directoryReader: reader);
            Require(reader.Blocked && result.Complete && result.Files.Single().Path.EndsWith("original.bin", StringComparison.Ordinal),
                $"Large-file review must hold the selected directory against replacement before enumeration. Blocked={reader.Blocked}, Complete={result.Complete}, Files={result.Files.Count}.");
            reader = new SwappingReader(root);
            var duplicates = await DuplicateFileReview.ScanAsync(root, directoryReader: reader);
            Require(reader.Blocked && duplicates.Complete && duplicates.FilesExamined == 1,
                "Duplicate review must hold the selected directory against replacement before enumeration.");
            var missing = new MissingDirectoryReader(root);
            result = await LargeFileReview.ScanAsync(root, minimumBytes: 1, directoryReader: missing);
            Require(!result.Complete && missing.TargetReads == 0,
                "Large-file review must report an unholdable directory as incomplete before enumerating it.");
            missing = new MissingDirectoryReader(root);
            duplicates = await DuplicateFileReview.ScanAsync(root, directoryReader: missing);
            Require(!duplicates.Complete && missing.TargetReads == 0,
                "Duplicate review must report an unholdable directory as incomplete before enumerating it.");
            var outside = new OutsideReader(Path.Combine(scratch, "outside-sentinel.bin"));
            result = await LargeFileReview.ScanAsync(root, minimumBytes: 1, directoryReader: outside);
            Require(!result.Complete && result.FilesExamined == 0 && result.Files.Count == 0,
                "Large-file review refuses an enumerator path outside the held directory before metadata reads.");
            duplicates = await DuplicateFileReview.ScanAsync(root, directoryReader: outside);
            Require(!duplicates.Complete && duplicates.FilesExamined == 0 && duplicates.Groups.Count == 0,
                "Duplicate review refuses an enumerator path outside the held directory before opening it.");
            Console.WriteLine("PASS: 6 file-review scope checks (root replacement, failed boundary and out-of-scope paths).");
        }
        finally
        {
            Directory.Delete(root, true);
            if (Directory.Exists(root + "-moved")) Directory.Delete(root + "-moved", true);
        }
    }

    private sealed class MissingDirectoryReader(string root) : IFileReviewDirectoryReader
    {
        internal int TargetReads;
        public IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options)
        {
            if (directory == root) return [Path.Combine(root, "unavailable")];
            TargetReads++;
            return [];
        }
        public IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options)
        {
            if (directory != root) TargetReads++;
            return [];
        }
    }

    private sealed class OutsideReader(string outside) : IFileReviewDirectoryReader
    {
        public IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options) => [];
        public IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options) => [outside];
    }

    private sealed class SwappingReader(string root) : IFileReviewDirectoryReader
    {
        internal bool Blocked;
        public IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options)
        {
            try
            {
                Directory.Move(root, root + "-moved");
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "unsupported-target.bin"), "sentinel");
            }
            catch (IOException) { Blocked = true; }
            return [];
        }
        public IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options) => Directory.EnumerateFiles(directory, "*", options);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
