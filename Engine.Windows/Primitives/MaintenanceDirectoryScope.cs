using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Pins a configured known-folder anchor and refuses redirection beneath it.</summary>
internal sealed class MaintenanceDirectoryScope : IDisposable
{
    private readonly List<SafeFileHandle> handles = [];
    private Action<string, Action>? createDirectory;
    public string Path { get; private set; } = "";
    public bool Exists { get; private set; } = true;

    public static MaintenanceDirectoryScope Open(string anchor, string target, bool create = false, Action<string, Action>? createDirectory = null)
    {
        var scope = new MaintenanceDirectoryScope { createDirectory = createDirectory };
        try
        {
            string fullAnchor = System.IO.Path.GetFullPath(anchor);
            string fullTarget = System.IO.Path.GetFullPath(target);
            string relative = System.IO.Path.GetRelativePath(fullAnchor, fullTarget);
            if (System.IO.Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new IOException("The maintenance directory is outside its selected anchor.");

            // The OS-selected anchor may be redirected. All later access uses its verified physical path.
            using var selectedAnchor = OpenDirectory(fullAnchor, false);
            var selectedIdentity = Information(selectedAnchor);
            string canonicalAnchor = FinalPath(selectedAnchor);
            string root = System.IO.Path.GetPathRoot(canonicalAnchor) ?? throw new IOException("The anchor has no supported root.");
            scope.Path = root;
            scope.Pin(root);
            foreach (string component in Components(System.IO.Path.GetRelativePath(root, canonicalAnchor)))
            {
                scope.Path = System.IO.Path.Combine(scope.Path, component);
                scope.Pin(scope.Path);
            }
            var pinnedIdentity = Information(scope.handles[^1]);
            if (selectedIdentity.Volume != pinnedIdentity.Volume || selectedIdentity.IndexHigh != pinnedIdentity.IndexHigh || selectedIdentity.IndexLow != pinnedIdentity.IndexLow)
                throw new IOException("The selected anchor changed while its identity was being pinned.");
            scope.Descend(relative, create);
            return scope;
        }
        catch { scope.Dispose(); throw; }
    }

    public void Descend(string relative, bool create)
    {
        foreach (string component in Components(relative))
        {
            if (component == "..") throw new IOException("A maintenance directory component escaped its selected scope.");
            Path = System.IO.Path.Combine(Path, component);
            try { Keep(OpenChild(component, false), Path); }
            catch (DirectoryNotFoundException) when (create)
            {
                SafeFileHandle? created = null;
                void Create() { Validate(); created = OpenChild(component, true); }
                try
                {
                    if (createDirectory is null) Create(); else createDirectory(Path, Create);
                    if (created is null) throw new IOException("The maintenance directory creation did not return a pinned handle.");
                    Keep(created, Path);
                }
                catch { created?.Dispose(); throw; }
            }
            catch (DirectoryNotFoundException)
            {
                Exists = false;
                return;
            }
        }
    }

    private void Pin(string path)
    {
        Keep(OpenDirectory(path, true), path);
    }

    private void Keep(SafeFileHandle handle, string path)
    {
        try
        {
            var info = Information(handle);
            if ((info.Attributes & 0x10) == 0 || (info.Attributes & 0x400) != 0)
                throw new IOException("A maintenance directory is redirected or is not a directory.");
            if (!System.IO.Path.TrimEndingDirectorySeparator(FinalPath(handle)).Equals(System.IO.Path.TrimEndingDirectorySeparator(path), StringComparison.OrdinalIgnoreCase))
                throw new IOException("A maintenance directory resolved outside its pinned scope.");
            handles.Add(handle);
        }
        catch { handle.Dispose(); throw; }
    }

    private SafeFileHandle OpenChild(string name, bool create)
    {
        int bytes = Encoding.Unicode.GetByteCount(name);
        if (bytes > ushort.MaxValue - 2) throw new IOException("The maintenance directory component is too long.");
        IntPtr text = Marshal.StringToHGlobalUni(name);
        IntPtr unicode = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeString>());
        try
        {
            var value = new UnicodeString { Length = (ushort)bytes, MaximumLength = (ushort)(bytes + 2), Buffer = text };
            Marshal.StructureToPtr(value, unicode, false);
            var attributes = new ObjectAttributes
            {
                Length = Marshal.SizeOf<ObjectAttributes>(), RootDirectory = handles[^1].DangerousGetHandle(),
                ObjectName = unicode, Attributes = 0x40
            };
            uint status = NtCreateFile(out var handle, 0x00100081, ref attributes, out _, IntPtr.Zero, 0x10, 3,
                create ? 3u : 1u, 0x00200021, IntPtr.Zero, 0);
            if (status == 0) return handle;
            handle?.Dispose();
            if (status is 0xC0000034 or 0xC000003A) throw new DirectoryNotFoundException("The selected maintenance directory does not exist.");
            throw new IOException("The maintenance directory child could not be pinned. NT status " + status.ToString("X8"));
        }
        finally { Marshal.FreeHGlobal(unicode); Marshal.FreeHGlobal(text); }
    }

    private static SafeFileHandle OpenDirectory(string path, bool openLink)
    {
        // GENERIC_READ participates in sharing checks; permit child operations but prevent directory rename and deletion.
        var handle = CreateFileW(path, 0x80000000, 3, IntPtr.Zero, 3, 0x02000000u | (openLink ? 0x00200000u : 0), IntPtr.Zero);
        if (!handle.IsInvalid) return handle;
        int error = Marshal.GetLastWin32Error();
        handle.Dispose();
        if (error is 2 or 3) throw new DirectoryNotFoundException("The selected maintenance directory does not exist.");
        throw new IOException("The maintenance directory could not be pinned. Windows error " + error, new Win32Exception(error));
    }

    private static FileInformation Information(SafeFileHandle handle) => GetFileInformationByHandle(handle, out var info)
        ? info : throw new IOException("The maintenance directory identity could not be read. Windows error " + Marshal.GetLastWin32Error());

    private static string FinalPath(SafeFileHandle handle)
    {
        var text = new StringBuilder(32768);
        uint length = GetFinalPathNameByHandleW(handle, text, (uint)text.Capacity, 0);
        if (length == 0 || length >= text.Capacity) throw new IOException("The maintenance directory canonical path could not be read.");
        string path = text.ToString();
        return path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase) ? @"\\" + path[8..]
            : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..] : path;
    }

    private static IEnumerable<string> Components(string relative) => relative == "." ? []
        : relative.Split([System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

    public void Dispose()
    {
        for (int i = handles.Count - 1; i >= 0; i--) handles[i].Dispose();
        handles.Clear();
    }

    public void MoveFileTo(string source, MaintenanceDirectoryScope destination)
    {
        Validate();
        destination.Validate();
        if (!System.IO.Path.GetDirectoryName(source)!.Equals(System.IO.Path.TrimEndingDirectorySeparator(Path), StringComparison.OrdinalIgnoreCase))
            throw new IOException("The retained file is outside its pinned source directory.");
        using var file = CreateFileW(source, 0x10081, 0, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (file.IsInvalid) throw new IOException("The retained file could not be opened safely. Windows error " + Marshal.GetLastWin32Error());
        var info = Information(file);
        if ((info.Attributes & (0x10 | 0x400)) != 0 || !FinalPath(file).Equals(source, StringComparison.OrdinalIgnoreCase))
            throw new IOException("The retained file identity changed or is redirected.");
        if (info.Volume != Information(destination.handles[^1]).Volume)
            throw new IOException("The source and retained folders are on different volumes; the source file stayed in place.");

        byte[] name = Encoding.Unicode.GetBytes(System.IO.Path.GetFileName(source));
        int rootOffset = IntPtr.Size == 8 ? 8 : 4;
        int lengthOffset = rootOffset + IntPtr.Size;
        int nameOffset = lengthOffset + 4;
        byte[] rename = new byte[nameOffset + name.Length];
        if (IntPtr.Size == 8) BitConverter.GetBytes(destination.handles[^1].DangerousGetHandle().ToInt64()).CopyTo(rename, rootOffset);
        else BitConverter.GetBytes(destination.handles[^1].DangerousGetHandle().ToInt32()).CopyTo(rename, rootOffset);
        BitConverter.GetBytes(name.Length).CopyTo(rename, lengthOffset);
        name.CopyTo(rename, nameOffset);
        uint status = NtSetInformationFile(file, out _, rename, (uint)rename.Length, 10);
        if (status != 0) throw new IOException("The retained file move was rejected. NT status " + status.ToString("X8"));
    }

    public IEnumerable<string> EnumerateFiles(string filter)
    {
        byte[] buffer = new byte[65536];
        bool restart = true;
        while (true)
        {
            Validate();
            bool read = GetFileInformationByHandleEx(handles[^1], restart ? 11 : 10, buffer, (uint)buffer.Length);
            restart = false;
            if (!read)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 18) yield break;
                throw new IOException("The pinned maintenance directory could not be enumerated. Windows error " + error);
            }
            int offset = 0;
            while (true)
            {
                const int nameOffset = 104;
                if (offset > buffer.Length - nameOffset) throw new IOException("Directory enumeration metadata was incomplete.");
                int nameLength = BitConverter.ToInt32(buffer, offset + 60);
                if (nameLength < 0 || nameLength % 2 != 0 || nameLength > buffer.Length - offset - nameOffset)
                    throw new IOException("Directory enumeration name was incomplete.");
                uint attributes = BitConverter.ToUInt32(buffer, offset + 56);
                string name = Encoding.Unicode.GetString(buffer, offset + nameOffset, nameLength);
                if ((attributes & 0x10) == 0 && System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(filter, name, ignoreCase: true))
                    yield return System.IO.Path.Combine(Path, name);
                uint next = BitConverter.ToUInt32(buffer, offset);
                if (next == 0) break;
                if (next < nameOffset || next > buffer.Length - offset) throw new IOException("Directory enumeration entry was invalid.");
                offset += (int)next;
            }
        }
    }

    private void Validate()
    {
        foreach (var handle in handles)
            if ((Information(handle).Attributes & 0x400) != 0) throw new IOException("A pinned maintenance directory became redirected.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Created, Accessed, Written;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoStatusBlock { public IntPtr Status, Information; }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString { public ushort Length, MaximumLength; public IntPtr Buffer; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ObjectAttributes
    {
        public int Length;
        public IntPtr RootDirectory, ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor, SecurityQualityOfService;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation info);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, uint size, uint flags);
    [DllImport("ntdll.dll")]
    private static extern uint NtSetInformationFile(SafeFileHandle handle, out IoStatusBlock status, byte[] info, uint size, int kind);
    [DllImport("ntdll.dll")]
    private static extern uint NtCreateFile(out SafeFileHandle handle, uint access, ref ObjectAttributes attributes,
        out IoStatusBlock status, IntPtr allocation, uint fileAttributes, uint share, uint disposition, uint options, IntPtr ea, uint eaLength);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int kind, [Out] byte[] info, uint size);
}
