using Microsoft.Win32;

namespace KillerBench.Engine;

public static class ProfileAbsence
{
    public static bool? FolderPresence(string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) return null;
        try
        {
            path = Path.GetFullPath(path);
            if (path.Length < 4 || !char.IsAsciiLetter(path[0]) || path[1] != ':' || path[2] != '\\' || path[3..].Contains(':')) return null;
            for (var parent = Directory.GetParent(path); parent is not null; parent = parent.Parent)
            {
                token.ThrowIfCancellationRequested();
                if (!parent.Exists || (parent.Attributes & FileAttributes.ReparsePoint) != 0) return null;
            }
            try { _ = File.GetAttributes(path); return true; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException)
        { return null; }
    }

    public static bool Evaluate(ProfileSnapshot target, ProfileInventoryResult inventory, bool? registryPresent, bool? folderPresent) =>
        inventory.Complete && registryPresent == false && folderPresent == false &&
        !inventory.Profiles.Any(p => p.Sid.Equals(target.Sid, StringComparison.OrdinalIgnoreCase) || p.Path.Equals(target.Path, StringComparison.OrdinalIgnoreCase));

    public static async Task<bool> CheckAsync(ProfileSnapshot target, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ProfilePlanning.ValidateSelection([target.Sid]);
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(target.Path) || !Path.IsPathFullyQualified(target.Path)) return false;
        string path = Path.GetFullPath(target.Path);
        if (path.Length < 4 || !char.IsAsciiLetter(path[0]) || path[1] != ':' || path[2] != '\\' || path[3..].Contains(':')) return false;
        string? root = Path.GetPathRoot(path);
        if (root is null || path.TrimEnd('\\').Equals(root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) || !Directory.Exists(root)) return false;
        var inventory = await ProfileInventory.ReadAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        try
        {
            using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var profiles = machine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList", false);
            if (profiles is null) return false;
            using var entry = profiles.OpenSubKey(target.Sid, false);
            using var backupEntry = profiles.OpenSubKey(target.Sid + ".bak", false);
            bool? folderPresent = FolderPresence(path, token);
            token.ThrowIfCancellationRequested();
            return Evaluate(target with { Path = path }, inventory, entry is not null || backupEntry is not null, folderPresent);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return false; }
    }
}
