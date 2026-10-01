using System.Text.Json;

namespace KillerBench.Engine.Applications;

public sealed record BrowserExtensionIdentity(string Browser, string Profile, string ExtensionId);
public sealed record BrowserExtensionReviewResult(IReadOnlyList<BrowserExtensionIdentity> Extensions, bool Complete, string Detail);
public enum BrowserExtensionLayout { Chromium, Firefox }
public sealed record BrowserExtensionRoot(string Browser, string Path, BrowserExtensionLayout Layout = BrowserExtensionLayout.Chromium);

public static class BrowserExtensionReview
{
    public static BrowserExtensionReviewResult Read(CancellationToken token = default)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        return Read([
            new("Chrome", Path.Combine(local, "Google", "Chrome", "User Data")),
            new("Edge", Path.Combine(local, "Microsoft", "Edge", "User Data")),
            new("Brave", Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data")),
            new("Firefox", Path.Combine(roaming, "Mozilla", "Firefox", "Profiles"), BrowserExtensionLayout.Firefox)
        ], token);
    }

    public static BrowserExtensionReviewResult Read(IReadOnlyList<BrowserExtensionRoot> roots, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(roots);
        if (roots.Count == 0 || roots.Count > 16 || roots.Any(root => string.IsNullOrWhiteSpace(root.Browser) || string.IsNullOrWhiteSpace(root.Path) || !Path.IsPathFullyQualified(root.Path)))
            throw new ArgumentException("Browser roots must be a bounded list with names and absolute paths.", nameof(roots));
        var found = new List<BrowserExtensionIdentity>(); bool complete = true;
        foreach (var root in roots)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (!Directory.Exists(root.Path) || (File.GetAttributes(root.Path) & FileAttributes.ReparsePoint) != 0) { if (Directory.Exists(root.Path)) complete = false; continue; }
                IEnumerable<string> profiles = root.Layout == BrowserExtensionLayout.Chromium
                    ? Directory.EnumerateDirectories(root.Path).Where(path => Path.GetFileName(path) == "Default" || Path.GetFileName(path).StartsWith("Profile ", StringComparison.Ordinal))
                    : Directory.EnumerateDirectories(root.Path);
                int profileCount = 0;
                foreach (string profile in profiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    if (profileCount++ >= 16) { complete = false; break; }
                    if ((File.GetAttributes(profile) & FileAttributes.ReparsePoint) != 0) { complete = false; continue; }
                    if (root.Layout == BrowserExtensionLayout.Firefox)
                    {
                        ReadFirefoxProfile(root.Browser, profile, found, ref complete, token);
                        if (found.Count >= 1024) break;
                        continue;
                    }
                    string extensions = Path.Combine(profile, "Extensions");
                    if (!Directory.Exists(extensions) || (File.GetAttributes(extensions) & FileAttributes.ReparsePoint) != 0) { if (Directory.Exists(extensions)) complete = false; continue; }
                    int extensionsFound = 0;
                    foreach (string id in Directory.EnumerateDirectories(extensions).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                    {
                        token.ThrowIfCancellationRequested();
                        if (extensionsFound++ >= 256) { complete = false; break; }
                        if ((File.GetAttributes(id) & FileAttributes.ReparsePoint) != 0) { complete = false; continue; }
                        string value = Path.GetFileName(id);
                        if (value.Length != 32 || value.Any(character => character is < 'a' or > 'p')) continue;
                        if (found.Count >= 1024) { complete = false; break; }
                        found.Add(new(root.Browser, Path.GetFileName(profile), value));
                    }
                    if (found.Count >= 1024) break;
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException) { complete = false; }
        }
        return new([.. found.OrderBy(item => item.Browser).ThenBy(item => item.Profile).ThenBy(item => item.ExtensionId)], complete,
            "Read-only browser extension identity review. Browser history, credentials, extension settings, manifest content, commands and policy values were excluded. Presence does not prove enablement, trust, ownership or safe removal. No browser or policy changed.");
    }

    private static void ReadFirefoxProfile(string browser, string profile, List<BrowserExtensionIdentity> found,
        ref bool complete, CancellationToken token)
    {
        string file = Path.Combine(profile, "extensions.json");
        if (!File.Exists(file)) return;
        if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) { complete = false; return; }
        var info = new FileInfo(file);
        if (info.Length > 4 * 1024 * 1024) { complete = false; return; }
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(file));
        if (!document.RootElement.TryGetProperty("addons", out JsonElement addons) || addons.ValueKind != JsonValueKind.Array)
        { complete = false; return; }
        int count = 0;
        foreach (JsonElement addon in addons.EnumerateArray())
        {
            token.ThrowIfCancellationRequested();
            if (count++ >= 256) { complete = false; break; }
            if (!addon.TryGetProperty("id", out JsonElement id) || id.ValueKind != JsonValueKind.String) { complete = false; continue; }
            string? value = id.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 512 || found.Any(item => item.Browser == browser && item.Profile == Path.GetFileName(profile) && item.ExtensionId == value))
            { complete = false; continue; }
            if (found.Count >= 1024) { complete = false; break; }
            found.Add(new(browser, Path.GetFileName(profile), value));
        }
    }
}
