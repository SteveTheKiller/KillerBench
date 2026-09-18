using System.IO;
using KillerBench.Engine;

internal static class BrowserExtensionReviewChecks
{
    internal static void Run(string scratch)
    {
        string root = Path.Combine(scratch, "browser-extension-review");
        Directory.CreateDirectory(root);
        try
        {
            string chrome = Path.Combine(root, "Chrome");
            string valid = new('a', 32);
            Directory.CreateDirectory(Path.Combine(chrome, "Default", "Extensions", valid));
            Directory.CreateDirectory(Path.Combine(chrome, "Default", "Extensions", "not-an-extension"));
            BrowserExtensionReviewResult result = BrowserExtensionReview.Read([new("Chrome", chrome)]);
            Require(result.Complete && result.Extensions.SequenceEqual([new("Chrome", "Default", valid)]) &&
                result.Detail.Contains("No browser or policy changed", StringComparison.Ordinal),
                "Browser extension review retains only valid identities and states its read-only boundary");

            for (int index = 1; index <= 16; index++)
                Directory.CreateDirectory(Path.Combine(chrome, "Profile " + index, "Extensions"));
            result = BrowserExtensionReview.Read([new("Chrome", chrome)]);
            Require(!result.Complete, "Browser extension profile limits retain incomplete coverage");

            string edge = Path.Combine(root, "Edge");
            string extensions = Path.Combine(edge, "Default", "Extensions");
            for (int index = 0; index < 257; index++)
                Directory.CreateDirectory(Path.Combine(extensions, ExtensionId(index)));
            result = BrowserExtensionReview.Read([new("Edge", edge)]);
            Require(!result.Complete && result.Extensions.Count == 256, "Browser extension limits retain incomplete coverage");

            string firefox = Path.Combine(root, "Firefox");
            Directory.CreateDirectory(Path.Combine(firefox, "fixture.default-release"));
            File.WriteAllText(Path.Combine(firefox, "fixture.default-release", "extensions.json"),
                "{\"addons\":[{\"id\":\"fixture@example.invalid\"}]}" );
            result = BrowserExtensionReview.Read([new("Firefox", firefox, BrowserExtensionLayout.Firefox)]);
            Require(result.Complete && result.Extensions.SequenceEqual([new("Firefox", "fixture.default-release", "fixture@example.invalid")]),
                "Firefox review retains extension identities without browser content or settings");

            try { _ = BrowserExtensionReview.Read([]); throw new Exception("An empty browser root list was accepted."); }
            catch (ArgumentException) { }
            Console.WriteLine("PASS: browser extension review retains Chromium and Firefox identities, bounded coverage and no browser changes.");
        }
        finally { Directory.Delete(root, true); }
    }

    private static string ExtensionId(int value)
    {
        const string alphabet = "abcdefghijklmnop";
        var characters = Enumerable.Repeat('a', 32).ToArray();
        for (int offset = 31; value > 0; offset--, value >>= 4) characters[offset] = alphabet[value & 15];
        return new(characters);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
