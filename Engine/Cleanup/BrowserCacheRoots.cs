namespace KillerBench.Engine;

public sealed record BrowserCacheDiscovery(IReadOnlyList<string> Roots, bool Partial);

public static class BrowserCacheRoots
{
    private static readonly string[] ProfileBases =
    [@"Google\Chrome\User Data", @"Microsoft\Edge\User Data", @"BraveSoftware\Brave-Browser\User Data", @"Mozilla\Firefox\Profiles"];

    public static BrowserCacheDiscovery Discover(string localData, CancellationToken token = default)
    {
        localData = Path.GetFullPath(localData);
        var roots = new List<string>();
        int visited = 0;
        bool partial = false;
        foreach (string relative in ProfileBases)
        {
            token.ThrowIfCancellationRequested();
            string profileBase = Path.Combine(localData, relative);
            try
            {
                if (!Directory.Exists(profileBase)) continue;
                if (Linked(profileBase)) { partial = true; continue; }
                foreach (string profile in Directory.EnumerateDirectories(profileBase))
                {
                    token.ThrowIfCancellationRequested();
                    if (++visited > 128 || roots.Count >= 32) return new(roots, true);
                    if ((File.GetAttributes(profile) & FileAttributes.ReparsePoint) != 0) { partial = true; continue; }
                    string cache = Path.Combine(profile, relative.StartsWith("Mozilla", StringComparison.Ordinal) ? "cache2" : "Cache");
                    if (Directory.Exists(cache))
                    {
                        if (Linked(cache)) partial = true;
                        else roots.Add(cache);
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { partial = true; }
        }
        string opera = Path.Combine(localData, "Opera Software", "Opera Stable", "Cache");
        try
        {
            if (Directory.Exists(opera))
            {
                if (roots.Count >= 32 || Linked(opera)) partial = true;
                else roots.Add(opera);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { partial = true; }
        token.ThrowIfCancellationRequested();
        return new(roots, partial);
    }

    public static bool IsKnownRoot(string root, string localData)
    {
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        localData = Path.TrimEndingDirectorySeparator(Path.GetFullPath(localData));
        if (root.Equals(Path.Combine(localData, "Opera Software", "Opera Stable", "Cache"), StringComparison.OrdinalIgnoreCase)) return true;
        foreach (string relative in ProfileBases)
        {
            string prefix = Path.Combine(localData, relative) + Path.DirectorySeparatorChar;
            if (!root.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var parts = root[prefix.Length..].Split(Path.DirectorySeparatorChar);
            string leaf = relative.StartsWith("Mozilla", StringComparison.Ordinal) ? "cache2" : "Cache";
            if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Equals(leaf, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool Linked(string path)
    {
        for (var directory = new DirectoryInfo(path); directory is not null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) return true;
        return false;
    }
}
