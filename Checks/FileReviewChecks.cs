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
            Console.WriteLine("PASS: duplicate and large-file reviews retain limits and never delete reviewed files.");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
