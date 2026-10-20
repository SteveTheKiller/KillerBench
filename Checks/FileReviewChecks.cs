using System.IO;
using KillerBench.Engine;

internal static class FileReviewChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "file-review");
        Directory.CreateDirectory(root);
        try
        {
            string first = Path.Combine(root, "first.bin");
            string second = Path.Combine(root, "second.bin");
            string unique = Path.Combine(root, "unique.bin");
            await File.WriteAllBytesAsync(first, [1, 2, 3, 4]);
            await File.WriteAllBytesAsync(second, [1, 2, 3, 4]);
            await File.WriteAllBytesAsync(unique, [1, 2, 3, 5, 6, 7]);
            DuplicateFileReviewResult duplicates = await DuplicateFileReview.ScanAsync(root);
            Require(duplicates.Complete && duplicates.Groups.Count == 1 && duplicates.Groups[0].Files.Count == 2 &&
                File.Exists(first) && File.Exists(second) && File.Exists(unique),
                "Duplicate review hashes matching files without deleting any file");
            duplicates = await DuplicateFileReview.ScanAsync(root, maximumFiles: 2);
            Require(!duplicates.Complete, "Duplicate review reports its file cap as incomplete coverage");

            LargeFileReviewResult large = await LargeFileReview.ScanAsync(root, minimumBytes: 4, resultLimit: 1);
            Require(!large.Complete && large.Files.Count == 1 && File.Exists(first) && File.Exists(second) && File.Exists(unique),
                "Large-file review retains its result cap and changes no file");

            string denied = Path.Combine(root, "denied");
            string sibling = Path.Combine(root, "accessible-sibling");
            Directory.CreateDirectory(denied);
            Directory.CreateDirectory(sibling);
            await File.WriteAllBytesAsync(Path.Combine(denied, "hidden.bin"), [9, 9, 9, 9]);
            string accessible = Path.Combine(sibling, "accessible.bin");
            await File.WriteAllBytesAsync(accessible, [1, 2, 3, 4]);
            var reader = new DenyingDirectoryReader(denied);
            string link = Path.Combine(root, "controlled-reparse-directory");
            Directory.CreateDirectory(link);
            string excluded = Path.Combine(link, "excluded.bin");
            await File.WriteAllBytesAsync(excluded, [9, 9, 9, 9]);
            reader.ReparseDirectories.Add(link);
            large = await LargeFileReview.ScanAsync(root, minimumBytes: 4, directoryReader: reader);
            Require(!large.Complete && large.Files.Any(file => file.Path == first) && large.Files.Any(file => file.Path == accessible),
                "Large-file review must report inaccessible descendants and preserve accessible siblings");
            duplicates = await DuplicateFileReview.ScanAsync(root, directoryReader: reader);
            Require(!duplicates.Complete && duplicates.Groups.Any(group => group.Files.Any(file => file.Path == first)
                && group.Files.Any(file => file.Path == second) && group.Files.Any(file => file.Path == accessible)),
                "Duplicate review must report inaccessible descendants and preserve accessible duplicate siblings");
            reader.DenyEnabled = false;
            large = await LargeFileReview.ScanAsync(root, minimumBytes: 4, directoryReader: reader);
            duplicates = await DuplicateFileReview.ScanAsync(root, directoryReader: reader);
            Require(large.Complete && large.Files.Count == 5 && large.Files.All(file => file.Path != excluded)
                && duplicates.Complete && duplicates.FilesExamined == 5 && duplicates.Groups.Count == 1,
                "File reviews recover complete coverage and keep controlled reparse directories excluded");
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            try { await LargeFileReview.ScanAsync(root, token: canceled.Token, directoryReader: reader); throw new Exception("Large-file cancellation was ignored."); }
            catch (OperationCanceledException) { }
            try { await DuplicateFileReview.ScanAsync(root, token: canceled.Token, directoryReader: reader); throw new Exception("Duplicate-file cancellation was ignored."); }
            catch (OperationCanceledException) { }
            Console.WriteLine("PASS: public file-review scans report denied descendants, preserve accessible siblings, recover on retry, retain limits/cancellation/reparse exclusions, and change no reviewed file.");
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class DenyingDirectoryReader(string denied) : IFileReviewDirectoryReader
    {
        internal bool DenyEnabled = true;
        internal readonly HashSet<string> ReparseDirectories = new(StringComparer.OrdinalIgnoreCase);
        public IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options)
        {
            if (DenyEnabled && string.Equals(directory, denied, StringComparison.OrdinalIgnoreCase))
            {
                if (options.IgnoreInaccessible) yield break;
                throw new UnauthorizedAccessException("Controlled inaccessible child directory.");
            }
            foreach (string path in Directory.EnumerateFiles(directory, "*", TopOnly(options))) yield return path;
            if (options.RecurseSubdirectories)
                foreach (string child in EnumerateDirectories(directory, options))
                    foreach (string path in EnumerateFiles(child, options)) yield return path;
        }

        public IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options)
        {
            if (DenyEnabled && string.Equals(directory, denied, StringComparison.OrdinalIgnoreCase))
            {
                if (options.IgnoreInaccessible) return [];
                throw new UnauthorizedAccessException("Controlled inaccessible child directory.");
            }
            return Directory.EnumerateDirectories(directory, "*", TopOnly(options)).Where(path =>
                (options.AttributesToSkip & FileAttributes.ReparsePoint) == 0 || !ReparseDirectories.Contains(path));
        }

        private static EnumerationOptions TopOnly(EnumerationOptions options) => new()
        {
            RecurseSubdirectories = false, IgnoreInaccessible = options.IgnoreInaccessible,
            AttributesToSkip = options.AttributesToSkip, ReturnSpecialDirectories = false
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
