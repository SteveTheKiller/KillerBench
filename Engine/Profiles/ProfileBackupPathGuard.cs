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

    private static uint Tag(string path)
    {
        using var find = FindFirstFile(path, out FindData data);
        if (find.IsInvalid) throw new IOException("A profile entry could not be inspected: error " + Marshal.GetLastWin32Error() + ".");
        return (data.Attributes & (uint)FileAttributes.ReparsePoint) != 0 ? data.Tag : 0;
    }

    /// <summary>Junctions and app execution aliases are links with no data of their own. The copy
    /// omits them and verification accounts for them.</summary>
    public static bool IsOmittedLink(string path) => Tag(path) is MountPointTag or AppAliasTag;

    /// <summary>Why this profile cannot be backed up, or null when every reparse entry under it is
    /// one the copy omits. Anything else stops the backup before it starts.</summary>
    public static string? UnsupportedEntry(string source, CancellationToken token)
    {
        var folders = new Stack<string>();
        folders.Push(source);
        while (folders.Count > 0)
        {
            string folder = folders.Pop();
            foreach (string entry in Directory.EnumerateFileSystemEntries(folder))
            {
                token.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    if (!IsOmittedLink(entry))
                        return "The profile contains a link or cloud placeholder this backup cannot preserve: " + Path.GetRelativePath(source, entry);
                }
                else if ((attributes & FileAttributes.Directory) != 0) folders.Push(entry);
            }
        }
        return null;
    }

    private static IEnumerable<string> Ancestors(string path)
    {
        var parents = new Stack<string>();
        for (var folder = Directory.GetParent(Path.GetFullPath(path).TrimEnd('\\')); folder is not null; folder = folder.Parent)
            parents.Push(folder.FullName);
        return parents;
    }

    private sealed class Lease : IDisposable
    {
        private readonly List<SafeFileHandle> handles = [];

        internal void Pin(string path)
        {
            // Read access without delete sharing: a rename or swap of a held folder then fails.
            var handle = CreateFile(path, 0x80000000, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
            if (handle.IsInvalid) { int code = Marshal.GetLastWin32Error(); handle.Dispose(); throw new Win32Exception(code); }
            handles.Add(handle);
            if (!GetFileInformationByHandle(handle, out FileInformation information)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if ((information.Attributes & (uint)FileAttributes.ReparsePoint) != 0 || (information.Attributes & (uint)FileAttributes.Directory) == 0)
                throw new IOException("A backup path folder is redirected.");
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
