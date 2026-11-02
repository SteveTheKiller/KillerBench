using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Profiles;

/// <summary>Holds every existing folder above a backup's source and destination without following
/// links, so a string comparison of the two paths describes where the copy really goes.</summary>
public static class ProfileBackupPathGuard
{
    private const uint MountPointTag = 0xA0000003, AppAliasTag = 0x8000001B;

    public static IDisposable Open(string source, string destination)
    {
        var lease = new Lease();
        try
        {
            foreach (string path in Ancestors(source).Append(Path.GetFullPath(source).TrimEnd('\\')))
                lease.Pin(path);
            foreach (string path in Ancestors(destination).Where(Directory.Exists))
                lease.Pin(path);
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }

    internal static Lease OpenTree(string source, string destination, CancellationToken token)
    {
        var lease = new Lease();
        try
        {
            foreach (string path in Ancestors(source).Concat(Ancestors(destination)).Distinct(StringComparer.OrdinalIgnoreCase)) lease.Pin(path);
            void Capture(string root, bool allowOmissions)
            {
                var folders = new Stack<string>();
                var rootHandle = lease.Pin(root);
                if (!allowOmissions) lease.DestinationRoot = rootHandle;
                folders.Push(root);
                int entries = 0;
                while (folders.Count != 0)
                {
                    token.ThrowIfCancellationRequested();
                    foreach (string entry in Directory.EnumerateFileSystemEntries(folders.Pop()))
                    {
                        token.ThrowIfCancellationRequested();
                        if (++entries > 100_000) throw new IOException("The protected profile comparison exceeds its entry limit.");
                        var attributes = File.GetAttributes(entry);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            if (allowOmissions && IsOmittedLink(entry)) continue;
                            throw new IOException("A profile comparison entry is redirected.");
                        }
                        bool directory = (attributes & FileAttributes.Directory) != 0;
                        lease.Pin(entry, directory);
                        if (directory) folders.Push(entry);
                    }
                }
            }
            Capture(source, true);
            Capture(destination, false);
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }

    internal static FileStream OpenFile(string path, long maximumBytes)
    {
        var handle = CreateFile(path, 0x80000000, 1, IntPtr.Zero, 3, 0x42200000, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw new IOException("The exact profile file could not be opened."); }
        try
        {
            if (!GetFileInformationByHandle(handle, out var info) || (info.Attributes & 0x410) != 0)
                throw new IOException("The exact profile file is unavailable or redirected.");
            var stream = new FileStream(handle, FileAccess.Read, 65_536, isAsync: true);
            if (stream.Length > maximumBytes) { stream.Dispose(); throw new InvalidDataException("The profile file exceeds its size bound."); }
            return stream;
        }
        catch { handle.Dispose(); throw; }
    }

    private static uint Tag(string path)
    {
        using var find = FindFirstFile(path, out FindData data);
        if (find.IsInvalid) throw new IOException("A profile entry could not be inspected: error " + Marshal.GetLastWin32Error() + ".");
        return (data.Attributes & (uint)FileAttributes.ReparsePoint) != 0 ? data.Tag : 0;
    }

    /// <summary>Junctions and app execution aliases are links with no data of their own. The copy
    /// omits them and verification accounts for them.</summary>
    public static bool IsOmittedLink(string path) => Tag(path) is MountPointTag or AppAliasTag;

    /// <summary>Returns the exact supported links excluded from a copy, without traversing them.</summary>
    public static IReadOnlyList<string> OmittedLinks(string source, CancellationToken token)
        => OmittedLinks(source, token, rejectEncrypted: false);

    internal static IReadOnlyList<string> OmittedLinks(string source, CancellationToken token, bool rejectEncrypted)
    {
        var omitted = new List<string>();
        var folders = new Stack<string>();
        folders.Push(source);
        while (folders.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            string folder = folders.Pop();
            if (rejectEncrypted && (File.GetAttributes(folder) & FileAttributes.Encrypted) != 0)
                throw new IOException("Protected profile backup does not support EFS-encrypted entries. No files were copied.");
            if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("A redirected profile directory was found while planning the backup.");
            foreach (string entry in Directory.EnumerateFileSystemEntries(folder))
            {
                token.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    if (!IsOmittedLink(entry))
                        throw new IOException("The profile contains a link or cloud placeholder this backup cannot preserve: " + Path.GetRelativePath(source, entry));
                    omitted.Add(entry);
                }
                else
                {
                    if (rejectEncrypted && (attributes & FileAttributes.Encrypted) != 0)
                        throw new IOException("Protected profile backup does not support EFS-encrypted entries. No files were copied.");
                    if ((attributes & FileAttributes.Directory) != 0) folders.Push(entry);
                }
            }
        }
        return omitted;
    }

    /// <summary>Why this profile cannot be backed up, or null when links are supported omissions
    /// and no EFS-encrypted entries are present. Unsupported entries stop the copy before it starts.</summary>
    public static string? UnsupportedEntry(string source, CancellationToken token)
    {
        try
        {
            _ = OmittedLinks(source, token, rejectEncrypted: true);
            return null;
        }
        catch (IOException error) { return error.Message; }
    }

    private static IEnumerable<string> Ancestors(string path)
    {
        var parents = new Stack<string>();
        for (var folder = Directory.GetParent(Path.GetFullPath(path).TrimEnd('\\')); folder is not null; folder = folder.Parent)
            parents.Push(folder.FullName);
        return parents;
    }

    internal sealed class Lease : IDisposable
    {
        private readonly List<SafeFileHandle> handles = [];
        internal SafeFileHandle DestinationRoot = null!;

        internal void PermitPublication(string destination)
        {
            // The caller already owns a nondeletable artifact in this root. It cannot become empty
            // and acquire a reparse point while its directory sharing permits the final rename.
            var previous = DestinationRoot;
            DestinationRoot = Pin(destination, share: 3);
            previous.Dispose();
        }

        internal SafeFileHandle Pin(string path, bool directory = true, uint share = 1)
        {
            // Read access without delete sharing: a rename or swap of a held folder then fails.
            var handle = CreateFile(path, 0x80000000, share, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
            if (handle.IsInvalid) { int code = Marshal.GetLastWin32Error(); handle.Dispose(); throw new Win32Exception(code); }
            handles.Add(handle);
            if (!GetFileInformationByHandle(handle, out FileInformation information)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if ((information.Attributes & (uint)FileAttributes.ReparsePoint) != 0 ||
                ((information.Attributes & (uint)FileAttributes.Directory) != 0) != directory)
                throw new IOException("A backup path folder is redirected.");
            return handle;
        }

        public void Dispose()
        {
            for (int i = handles.Count - 1; i >= 0; i--) handles[i].Dispose();
            handles.Clear();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FindData
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        public uint SizeHigh, SizeLow, Tag, Reserved;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string Alternate;
    }

    private sealed class FindHandle() : SafeHandleZeroOrMinusOneIsInvalid(true)
    {
        protected override bool ReleaseHandle() => FindClose(handle);
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInformation information);
    [DllImport("kernel32.dll", EntryPoint = "FindFirstFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern FindHandle FindFirstFile(string path, out FindData data);
    [DllImport("kernel32.dll")]
    private static extern bool FindClose(IntPtr handle);
}
