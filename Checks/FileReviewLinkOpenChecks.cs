using System.IO;
using KillerBench.Engine.Storage;

internal static class FileReviewLinkOpenChecks
{
    internal static void Run()
    {
        const string root = @"C:\Users\agent\AgentLab\killerbench-repair-20261115";
        if (Environment.MachineName != "SN01" ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable" ||
            !File.Exists(Path.Combine(root, "current-task.json")))
            throw new InvalidOperationException("Link-open acceptance requires the owned SN01 fixture.");
        using var checkpoint = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "current-task.json")));
        if (checkpoint.RootElement.GetProperty("leaseToken").GetString() != "kb-repair-20261115-7b506f55")
            throw new InvalidOperationException("Link-open acceptance requires the current task lease.");
        string link = Path.Combine(root, "child-link-template.bin"), target = Path.Combine(root, "child-link-target.bin");
        var entry = new FileInfo(link);
        if ((entry.Attributes & FileAttributes.ReparsePoint) == 0 ||
            !string.Equals(entry.LinkTarget, target, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The manually prepared link does not match this fixture.");
        bool created = !File.Exists(target);
        try
        {
            if (created)
            {
                using var file = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                file.Write("Owned file-open fixture"u8);
            }
            foreach (bool asynchronous in new[] { false, true })
            {
                using var handle = SystemFileReviewFileAccess.Instance.OpenRead(link, asynchronous);
                if ((File.GetAttributes(handle) & FileAttributes.ReparsePoint) == 0)
                    throw new Exception("The public file-open boundary followed a link instead of retaining the link entry.");
            }
            Console.WriteLine("PASS: 2 real file-open modes retain the manually prepared link entry without reading its target.");
        }
        finally { if (created) File.Delete(target); }
    }
}
