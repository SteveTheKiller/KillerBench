namespace KillerBench.Engine;

public sealed record BrowserExtensionIdentity(string Browser, string Profile, string ExtensionId);
public sealed record BrowserExtensionReviewResult(IReadOnlyList<BrowserExtensionIdentity> Extensions, bool Complete, string Detail);

public static class BrowserExtensionReview
{
    public static BrowserExtensionReviewResult Read(CancellationToken token = default)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        var roots = new[] { ("Chrome", Path.Combine(local, "Google", "Chrome", "User Data")), ("Edge", Path.Combine(local, "Microsoft", "Edge", "User Data")), ("Brave", Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data")) };
        var found = new List<BrowserExtensionIdentity>(); bool complete = true;
        foreach (var root in roots)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (!Directory.Exists(root.Item2) || (File.GetAttributes(root.Item2) & FileAttributes.ReparsePoint) != 0) { if (Directory.Exists(root.Item2)) complete = false; continue; }
                foreach (string profile in Directory.EnumerateDirectories(root.Item2).Where(path => Path.GetFileName(path) == "Default" || Path.GetFileName(path).StartsWith("Profile ", StringComparison.Ordinal)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Take(16))
                {
                    string extensions = Path.Combine(profile, "Extensions");
                    if (!Directory.Exists(extensions) || (File.GetAttributes(extensions) & FileAttributes.ReparsePoint) != 0) { if (Directory.Exists(extensions)) complete = false; continue; }
                    foreach (string id in Directory.EnumerateDirectories(extensions).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Take(256))
                    {
                        token.ThrowIfCancellationRequested();
                        if ((File.GetAttributes(id) & FileAttributes.ReparsePoint) != 0) { complete = false; continue; }
                        string value = Path.GetFileName(id);
                        if (value.Length != 32 || value.Any(character => character is < 'a' or > 'p')) continue;
                        if (found.Count >= 1024) { complete = false; break; }
                        found.Add(new(root.Item1, Path.GetFileName(profile), value));
                    }
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { complete = false; }
        }
        return new(found.OrderBy(item => item.Browser).ThenBy(item => item.Profile).ThenBy(item => item.ExtensionId).ToArray(), complete,
            "Read-only Chromium extension directory identity review. Browser history, credentials, extension settings, manifest content, commands and policy values were excluded. Presence does not prove enablement, trust, ownership or safe removal. No browser or policy changed.");
    }
}
