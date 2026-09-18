namespace KillerBench.Engine;

public sealed record BrowserExtensionIdentity(string Browser, string Profile, string ExtensionId);
public sealed record BrowserExtensionReviewResult(IReadOnlyList<BrowserExtensionIdentity> Extensions, bool Complete, string Detail);
public sealed record BrowserExtensionRoot(string Browser, string Path);

public static class BrowserExtensionReview
{
    public static BrowserExtensionReviewResult Read(CancellationToken token = default)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        return Read([
            new("Chrome", Path.Combine(local, "Google", "Chrome", "User Data")),
            new("Edge", Path.Combine(local, "Microsoft", "Edge", "User Data")),
            new("Brave", Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data"))
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
                int profiles = 0;
                foreach (string profile in Directory.EnumerateDirectories(root.Path).Where(path => Path.GetFileName(path) == "Default" || Path.GetFileName(path).StartsWith("Profile ", StringComparison.Ordinal)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    if (profiles++ >= 16) { complete = false; break; }
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
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { complete = false; }
        }
        return new(found.OrderBy(item => item.Browser).ThenBy(item => item.Profile).ThenBy(item => item.ExtensionId).ToArray(), complete,
            "Read-only Chromium extension directory identity review. Browser history, credentials, extension settings, manifest content, commands and policy values were excluded. Presence does not prove enablement, trust, ownership or safe removal. No browser or policy changed.");
    }
}
