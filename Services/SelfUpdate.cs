using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KillerBench.Services;

/// <summary>
/// KillerBench's release names and the parsing rules its checks cover: the latest stable GitHub
/// release, the installer and portable package names, and a package's SHA-256 from the release's
/// SHA256SUMS.txt. The download and install run on the shared update stack
/// (Services\UpdateService.cs, with KillerBench's modes in Services\UpdateService.Extras.cs).
/// </summary>
public static partial class SelfUpdate
{
    /// <summary>Setting that turns the startup check off. Anything but "0" leaves it on.</summary>
    public const string StartupCheckSetting = "CheckForUpdatesOnStartup";

    public const string Repo = "https://github.com/SteveTheKiller/KillerBench";
    public const string InstallerAsset = "KillerBench.exe";
    public const string PortableAsset = "KillerBench-Portable.exe";

    public static bool StartupCheckEnabled(string? setting) => setting != "0";

    /// <summary>The newer stable release's tag, or null when this copy is current, offline, or
    /// there is no release yet.</summary>
    public static async Task<string?> FindNewerReleaseAsync(Version current)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            using var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/repos/SteveTheKiller/KillerBench/releases/latest");
            request.Headers.UserAgent.ParseAdd("KillerBench-UpdateCheck");
            using var response = await http.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return ParseNewerRelease(await response.Content.ReadAsStringAsync().ConfigureAwait(false), current);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    /// <summary>Only a published, non-prerelease vX.Y.Z tag newer than the running version is
    /// offered. The tag later goes into the update script, so anything else is refused here.</summary>
    public static string? ParseNewerRelease(string json, Version current)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || root.TryGetProperty("draft", out var draft) && draft.ValueKind != JsonValueKind.False
            || root.TryGetProperty("prerelease", out var prerelease) && prerelease.ValueKind != JsonValueKind.False
            || !root.TryGetProperty("tag_name", out var tagElement)
            || tagElement.ValueKind != JsonValueKind.String)
            return null;
        string? tag = tagElement.GetString();
        if (tag is null || !StableTag().IsMatch(tag) || !Version.TryParse(tag[1..], out var latest))
            return null;
        var installed = new Version(current.Major, current.Minor, Math.Max(0, current.Build));
        return latest > installed ? tag : null;
    }

    /// <summary>The checksum listed for exactly <paramref name="assetName"/>, or null.</summary>
    public static string? ExpectedHash(string sums, string assetName)
    {
        foreach (string line in sums.Replace("\r", "", StringComparison.Ordinal).Split('\n'))
        {
            string[] parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && string.Equals(parts[0], assetName, StringComparison.OrdinalIgnoreCase))
                return parts[1];
        }
        return null;
    }

    [GeneratedRegex(@"\Av[0-9]+\.[0-9]+\.[0-9]+\z", RegexOptions.CultureInvariant)]
    private static partial Regex StableTag();
}
