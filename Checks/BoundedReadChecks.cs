using System.IO;
using KillerBench.Engine.Storage;
using KillerBench.Engine.Applications;

internal static class BoundedReadChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "bounded-reads-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string first = Path.Combine(root, "a.bin"), second = Path.Combine(root, "b.bin");
            File.WriteAllBytes(first, [1]); File.WriteAllBytes(second, [1]);
            var result = await DuplicateFileReview.ScanAsync(root, maximumHashBytes: 2,
                directoryReader: new MutatingReader([first, second], () =>
                {
                    File.WriteAllBytes(first, new byte[4096]); File.WriteAllBytes(second, new byte[4096]);
                }));
            Require(!result.Complete && result.Groups.Count == 0, "Grown files must not produce stale duplicate certainty beyond the hash budget.");
            Console.WriteLine("PASS: bounded duplicate reads reject enumeration-to-hash growth.");
            File.WriteAllBytes(first, [1]); File.WriteAllBytes(second, [1]);
            result = await DuplicateFileReview.ScanAsync(root, maximumHashBytes: 2);
            Require(result.Complete && result.Groups.Count == 1 && result.Groups[0].Length == 1 &&
                result.Groups[0].Sha256 == "4BF5122F344554C53BDE2EBB8CD2B7E3D1600AD631C385A5D7CCE23C7785459A",
                "Stable files must retain exact duplicate lengths and known SHA-256.");
            foreach (string change in new[] { "shrink", "replace", "rewrite" })
            {
                File.WriteAllBytes(first, [1, 2]); File.WriteAllBytes(second, [1, 2]);
                result = await DuplicateFileReview.ScanAsync(root, directoryReader: new MutatingReader([first, second], () =>
                {
                    DateTime created = File.GetCreationTimeUtc(first), modified = File.GetLastWriteTimeUtc(first);
                    if (change == "replace")
                    {
                        string replacement = Path.Combine(root, "replacement");
                        File.WriteAllBytes(replacement, [1, 2]);
                        File.Move(replacement, first, true);
                        File.SetCreationTimeUtc(first, created); File.SetLastWriteTimeUtc(first, modified);
                    }
                    else if (change == "shrink") File.WriteAllBytes(first, [1]);
                    else { File.WriteAllBytes(first, [3, 4]); File.SetLastWriteTimeUtc(first, modified.AddSeconds(5)); }
                }));
                Require(!result.Complete && result.Groups.Count == 0, "Changed candidate must remain incomplete: " + change);
            }
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                try { await DuplicateFileReview.ScanAsync(root, token: canceled.Token); throw new Exception("Duplicate cancellation ignored."); }
                catch (OperationCanceledException) { }
            }
            Console.WriteLine("PASS: duplicate stable hashes, shrink, same-metadata replacement, rewrite and cancellation.");
            string firefox = Path.Combine(root, "Firefox");
            string bad = Path.Combine(firefox, "a-bad"), good = Path.Combine(firefox, "z-good");
            Directory.CreateDirectory(bad); Directory.CreateDirectory(good);
            File.WriteAllText(Path.Combine(good, "extensions.json"), "{\"addons\":[{\"id\":\"good@example.invalid\"}]}");
            foreach (string json in new[] { "[]", "null", "true", "{\"addons\":[null]}", "{\"addons\":[17]}", "{\"addons\":{}}", "{\"addons\":[{\"id\":7}]}", "{" })
            {
                File.WriteAllText(Path.Combine(bad, "extensions.json"), json);
                var browsers = BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)]);
                Require(!browsers.Complete && browsers.Extensions.Any(item => item.ExtensionId == "good@example.invalid"),
                    "Malformed Firefox profile must preserve another good profile: " + json);
            }
            Console.WriteLine("PASS: malformed browser profile shapes preserve good profiles.");
            int enumerated = 0;
            var bounded = BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)],
                enumerateDirectories: _ => ManyProfiles());
            Require(!bounded.Complete && enumerated <= 17, "Profile cap must bound directory enumeration before sorting.");
            IEnumerable<string> ManyProfiles()
            {
                for (int index = 0; index < 10_000; index++) { enumerated++; yield return good; }
            }
            Console.WriteLine("PASS: browser directory enumeration stops at the profile bound before sorting.");
            string chrome = Path.Combine(root, "Chrome");
            string extensions = Path.Combine(chrome, "Default", "Extensions");
            string identity = Path.Combine(extensions, new string('a', 32));
            Directory.CreateDirectory(identity);
            enumerated = 0;
            bounded = BrowserExtensionReview.Read([new("Chrome", chrome)], enumerateDirectories: path =>
                path == chrome ? [Path.Combine(chrome, "Default")] : ManyExtensions());
            Require(!bounded.Complete && enumerated <= 257, "Extension cap must bound enumeration before sorting.");
            IEnumerable<string> ManyExtensions()
            {
                for (int index = 0; index < 10_000; index++) { enumerated++; yield return identity; }
            }
            using (var canceled = new CancellationTokenSource())
            {
                enumerated = 0;
                try
                {
                    BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)], canceled.Token,
                        _ => CancelingProfiles());
                    throw new Exception("Browser enumeration cancellation ignored.");
                }
                catch (OperationCanceledException) { Require(enumerated == 1, "Cancellation must interrupt enumeration before sorting."); }
                IEnumerable<string> CancelingProfiles() { enumerated++; canceled.Cancel(); yield return good; }
            }
            File.WriteAllText(Path.Combine(bad, "extensions.json"), new string(' ', 4 * 1024 * 1024 + 1));
            bounded = BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)]);
            Require(!bounded.Complete && bounded.Extensions.Any(item => item.ExtensionId == "good@example.invalid"), "Oversized profile must preserve good results.");
            File.WriteAllText(Path.Combine(bad, "extensions.json"), "{\"addons\":[]}");
            using (var locked = new FileStream(Path.Combine(bad, "extensions.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                bounded = BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)]);
                Require(!bounded.Complete && bounded.Extensions.Any(item => item.ExtensionId == "good@example.invalid"), "Inaccessible profile must preserve good results.");
            }
            Console.WriteLine("PASS: extension enumeration, interruption, oversized and inaccessible profiles remain bounded and partial.");
            bounded = BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)],
                enumerateDirectories: _ => FailingProfiles());
            Require(!bounded.Complete && bounded.Extensions.Any(item => item.ExtensionId == "good@example.invalid"),
                "Interrupted profile enumeration must preserve profiles already found.");
            IEnumerable<string> FailingProfiles() { yield return good; throw new IOException("Fixture enumeration failure."); }
            bounded = BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)],
                enumerateDirectories: _ => [Path.Combine(firefox, "0-disappeared"), good]);
            Require(!bounded.Complete && bounded.Extensions.Any(item => item.ExtensionId == "good@example.invalid"),
                "A disappeared profile must not abort remaining good profiles.");
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class MutatingReader(string[] files, Action mutate) : IFileReviewDirectoryReader
    {
        public IEnumerable<string> EnumerateDirectories(string directory, EnumerationOptions options) => [];
        public IEnumerable<string> EnumerateFiles(string directory, EnumerationOptions options)
        {
            foreach (string file in files) yield return file;
            mutate();
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
