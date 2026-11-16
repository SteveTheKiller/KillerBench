using System.Diagnostics;
using System.IO;
using KillerBench.Engine.Storage;

internal static class StorageRedirectedRootChecks
{
    internal static async Task RunAsync()
    {
        const string task = @"C:\Users\agent\AgentLab\killerbench-repair-20261115";
        if (!Environment.MachineName.Equals("SN01", StringComparison.OrdinalIgnoreCase) ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable" ||
            !File.Exists(Path.Combine(task, "owned-test-marker.txt")))
            throw new InvalidOperationException("Redirected-root acceptance requires the owned SN01 fixture.");
        string folder = Path.Combine(task, "redirected-root-" + Guid.NewGuid().ToString("N"));
        string target = Path.Combine(folder, "target");
        Directory.CreateDirectory(Path.Combine(target, "below"));
        string sentinel = Path.Combine(target, "below", "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unsupported target sentinel");
        byte[] before = await File.ReadAllBytesAsync(sentinel);
        string junction = Path.Combine(folder, "junction");
        var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("mklink");
        start.ArgumentList.Add("/J");
        start.ArgumentList.Add(junction);
        start.ArgumentList.Add(target);
        using (var process = Process.Start(start)!)
        {
            Task<string> output = process.StandardOutput.ReadToEndAsync(), error = process.StandardError.ReadToEndAsync();
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(stop.Token);
            if (process.ExitCode != 0) throw new IOException("Owned junction fixture failed: " + await output + await error);
        }
        int count = 0;
        foreach (string root in new[] { junction, Path.Combine(junction, "below") })
        {
            var reader = new ProbeReader();
            var large = await LargeFileReview.ScanAsync(root, minimumBytes: 1, directoryReader: reader);
            var duplicate = await DuplicateFileReview.ScanAsync(root, directoryReader: reader);
            if (large.Complete || duplicate.Complete || large.Files.Count != 0 || reader.Reads != 0)
                throw new Exception("Redirected root or ancestor was enumerated or presented as complete.");
            count += 2;
        }
        var ordinary = await LargeFileReview.ScanAsync(target, minimumBytes: 1);
        if (!ordinary.Complete || ordinary.Files.Count != 1) throw new Exception("Ordinary local positive control failed.");
        byte[] after = await File.ReadAllBytesAsync(sentinel);
        if (!before.SequenceEqual(after)) throw new Exception("Unsupported target sentinel changed.");
        Console.WriteLine($"PASS: {count} native redirected-root/ancestor refusals before enumeration, ordinary local control and unchanged sentinel. Fixture: {folder}");
    }

    private sealed class ProbeReader : IFileReviewDirectoryReader
    {
        internal int Reads;
        public IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options) { Reads++; return Directory.EnumerateFiles(directory, "*", options); }
        public IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options) { Reads++; return Directory.EnumerateDirectories(directory, "*", options); }
    }
}
