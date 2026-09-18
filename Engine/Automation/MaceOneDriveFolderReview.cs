using Microsoft.Win32;
using System.Runtime.Versioning;

namespace KillerBench.Engine;

public sealed record MaceKnownFolderObservation(string Folder, bool UnderRegisteredOneDriveRoot);

public sealed record MaceOneDriveFolderEvidence(int RegisteredRootCount,
    IReadOnlyList<MaceKnownFolderObservation> Folders, bool Complete, string Limitation)
{
    public int RedirectedCount => Folders.Count(item => item.UnderRegisteredOneDriveRoot);
}

public static class MaceOneDriveFolderReview
{
    private static readonly (string Name, Environment.SpecialFolder Folder)[] KnownFolders =
    [
        ("Desktop", Environment.SpecialFolder.DesktopDirectory),
        ("Documents", Environment.SpecialFolder.MyDocuments),
        ("Pictures", Environment.SpecialFolder.MyPictures)
    ];

    public static MaceOneDriveFolderEvidence Read(MaceScope scope, CancellationToken token = default)
    {
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        token.ThrowIfCancellationRequested();
        if (scope == MaceScope.AllUsers)
            return new(0, Array.Empty<MaceKnownFolderObservation>(), false,
                "All-user folder-location review does not load other users' registry hives. Current-account paths were not substituted for all-user evidence.");
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("OneDrive folder-location review requires Windows.");
        try
        {
            var roots = ReadRoots(token, out bool complete);
            var folders = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var definition in KnownFolders)
            {
                token.ThrowIfCancellationRequested();
                folders[definition.Name] = Environment.GetFolderPath(definition.Folder,
                    Environment.SpecialFolderOption.DoNotVerify);
            }
            return Analyze(scope, roots, folders, complete, token);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new(0, Array.Empty<MaceKnownFolderObservation>(), false,
                "Current-account OneDrive folder-location evidence was unavailable: " + error.GetType().Name + ".");
        }
    }

    public static MaceOneDriveFolderEvidence Analyze(MaceScope scope, IReadOnlyList<string> roots,
        IReadOnlyDictionary<string, string?> folders, bool sourceComplete = true,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentNullException.ThrowIfNull(folders);
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        token.ThrowIfCancellationRequested();
        if (scope == MaceScope.AllUsers)
            return new(0, Array.Empty<MaceKnownFolderObservation>(), false,
                "All-user folder-location review does not load other users' registry hives. Current-account paths were not substituted for all-user evidence.");

        bool complete = sourceComplete && roots.Count <= 32;
        var normalizedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots.Take(33))
        {
            token.ThrowIfCancellationRequested();
            if (!TryNormalize(root, out string? normalized)) complete = false;
            else normalizedRoots.Add(normalized);
        }

        var observations = new List<MaceKnownFolderObservation>();
        foreach (string name in KnownFolders.Select(item => item.Name))
        {
            token.ThrowIfCancellationRequested();
            if (!folders.TryGetValue(name, out string? path) || !TryNormalize(path, out string? normalized))
            {
                complete = false;
                continue;
            }
            observations.Add(new(name, normalizedRoots.Any(root => IsWithin(normalized, root))));
        }
        if (observations.Count != KnownFolders.Length) complete = false;
        string limitation = complete
            ? "Only known-folder names, registered-root count and containment results were retained. Account names, paths and file contents were excluded."
            : "Folder-location evidence was incomplete or exceeded its bound. Account names, paths and file contents were excluded.";
        return new(normalizedRoots.Count, observations.AsReadOnly(), complete, limitation);
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<string> ReadRoots(CancellationToken token, out bool complete)
    {
        complete = true;
        var roots = new List<string>();
        using var accounts = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive\Accounts", false);
        if (accounts is null) return roots;
        string[] names = accounts.GetSubKeyNames();
        if (names.Length > 32) complete = false;
        foreach (string name in names.Take(33))
        {
            token.ThrowIfCancellationRequested();
            using var account = accounts.OpenSubKey(name, false);
            if (account?.GetValue("UserFolder", null, RegistryValueOptions.DoNotExpandEnvironmentNames) is string root)
                roots.Add(Environment.ExpandEnvironmentVariables(root));
        }
        return roots;
    }

    private static bool TryNormalize(string? value, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1024 || value.IndexOfAny(['\r', '\n', '\0']) >= 0)
            return false;
        try
        {
            string full = Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Path.IsPathFullyQualified(full) || full.Length == 0) return false;
            normalized = full;
            return true;
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool IsWithin(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
