using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace KillerBench.Engine.Infrastructure;

/// <summary>The account signed in to this desktop session, which owns the per-user caches the cleanups
/// target. Elevating over the shoulder runs KillerBench as another administrator; without this, the
/// current user's Temp, browser and Office caches would resolve to that administrator's profile and the
/// cleanup would report success for the wrong person.</summary>
public static partial class SignedInUser
{
    public sealed record Identity(string LocalApplicationData, bool DifferentAccount);

    // PublicationOnly: a failed lookup is retried on the next call instead of being cached for the process.
    private static readonly Lazy<Identity> current = new(ResolveCurrent, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>The signed-in user's local application-data folder and whether KillerBench runs as
    /// another account. Throws <see cref="InvalidOperationException"/> when that user cannot be established.</summary>
    public static Identity Current => current.Value;

    /// <summary>The signed-in user's local application-data folder, or null with the reason it is unavailable.</summary>
    public static string? TryLocalApplicationData(out string reason)
    {
        try { reason = ""; return Current.LocalApplicationData; }
        catch (InvalidOperationException error) { reason = error.Message; return null; }
    }

    /// <summary>Picks the signed-in user's local application-data folder from the accounts that own a
    /// desktop shell in this session. No shell (a service or scheduled task) means the running account is
    /// the only one; more than one account is ambiguous and refused. Another account's folder comes from
    /// its own User Shell Folders value, expanded against its own profile path, never this process's environment.</summary>
    public static Identity Resolve(string processSid, IReadOnlyCollection<string> sessionShellSids, string ownLocalApplicationData,
        Func<string, string?> readLocalShellFolder, Func<string, string?> readProfilePath)
    {
        string[] accounts = [.. sessionShellSids.Distinct(StringComparer.OrdinalIgnoreCase)];
        if (accounts.Length == 0 || (accounts.Length == 1 && accounts[0].Equals(processSid, StringComparison.OrdinalIgnoreCase)))
            return new(ownLocalApplicationData, false);
        if (accounts.Length > 1)
            throw new InvalidOperationException("More than one account has a desktop shell in this session, so the signed-in user is ambiguous.");
        string sid = accounts[0];
        string profile = readProfilePath(sid) is { Length: > 0 } registered && Path.IsPathFullyQualified(registered)
            ? registered : throw new InvalidOperationException("The signed-in user's profile folder is not registered.");
        string? configured = readLocalShellFolder(sid);
        string path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(profile, "AppData", "Local")
            : configured.Replace("%USERPROFILE%", profile, StringComparison.OrdinalIgnoreCase);
        if (path.Contains('%') || !Path.IsPathFullyQualified(path))
            throw new InvalidOperationException("The signed-in user's local application-data folder uses a location this account cannot resolve.");
        return new(Path.GetFullPath(path), true);
    }

    private static Identity ResolveCurrent()
    {
        string own = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!OperatingSystem.IsWindows()) return new(own, false);
        using var self = WindowsIdentity.GetCurrent();
        string processSid = self.User?.Value ?? throw new InvalidOperationException("The running account has no security identifier.");
        return Resolve(processSid, SessionShellAccounts(), own, ReadLocalShellFolder, ReadProfilePath);
    }

    private static List<string> SessionShellAccounts()
    {
        int session = Process.GetCurrentProcess().SessionId;
        var accounts = new List<string>();
        foreach (var shell in Process.GetProcessesByName("explorer"))
            using (shell)
            {
                if (shell.SessionId != session) continue;
                string? sid = TryReadOwner(shell.Id);
                if (sid is not null) accounts.Add(sid);
                else if (!HasExited(shell))
                    throw new InvalidOperationException("The owner of this session's desktop shell could not be read.");
            }
        return accounts;
    }

    private static bool HasExited(Process process)
    {
        try { return process.HasExited; }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception) { return false; }
    }

    private static string? TryReadOwner(int processId)
    {
        if (!OperatingSystem.IsWindows()) return null;
        const uint QueryLimitedInformation = 0x1000, TokenQuery = 0x0008;
        nint process = OpenProcess(QueryLimitedInformation, false, (uint)processId);
        if (process == 0) return null;
        try
        {
            if (!OpenProcessToken(process, TokenQuery, out nint token)) return null;
            try { using var identity = new WindowsIdentity(token); return identity.User?.Value; }
            finally { CloseHandle(token); }
        }
        finally { CloseHandle(process); }
    }

    private static string? ReadLocalShellFolder(string sid)
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var key = Registry.Users.OpenSubKey(sid + @"\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
        return key?.GetValue("Local AppData", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
    }

    private static string? ReadProfilePath(string sid)
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\" + sid);
        return key?.GetValue("ProfileImagePath") is string path ? Environment.ExpandEnvironmentVariables(path) : null;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint processId);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(nint process, uint access, out nint token);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint handle);
}
