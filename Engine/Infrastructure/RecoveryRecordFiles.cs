using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Infrastructure;

internal static partial class RecoveryRecordFiles
{
    internal static FileStream CreateNew(string path)
    {
        if (!OperatingSystem.IsWindows())
            return new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.Asynchronous);
        var handle = CreateFileW(path, 0xC0010000, 1, IntPtr.Zero, 1, 0x40000080, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            int code = Marshal.GetLastWin32Error();
            handle.Dispose();
            if (code == 3) throw new DirectoryNotFoundException("The publication directory does not exist.");
            throw new IOException("The new publication file could not be created.", new System.ComponentModel.Win32Exception(code));
        }
        try { return new FileStream(handle, FileAccess.ReadWrite, 4096, isAsync: true); }
        catch { handle.Dispose(); throw; }
    }

    internal static FileStream OpenLocked(string path)
    {
        var handle = CreateFileW(path, 0x80010000, 1, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw new IOException("Recovery evidence could not be locked."); }
        try { return new FileStream(handle, FileAccess.Read); }
        catch { handle.Dispose(); throw; }
    }

    internal static void Archive(FileStream stream, string destination, bool overwrite = false, SafeFileHandle? parent = null)
    {
        if (!OperatingSystem.IsWindows()) { File.Move(stream.Name, destination, overwrite); return; }
        byte[] name = System.Text.Encoding.Unicode.GetBytes(parent is null ? Path.GetFullPath(destination) : Path.GetFileName(destination));
        int lengthOffset = IntPtr.Size == 8 ? 16 : 8;
        int nameOffset = lengthOffset + 4;
        byte[] info = new byte[nameOffset + name.Length + 2];
        info[0] = overwrite ? (byte)1 : (byte)0;
        if (parent is not null)
        {
            if (IntPtr.Size == 8) BitConverter.GetBytes(parent.DangerousGetHandle().ToInt64()).CopyTo(info, 8);
            else BitConverter.GetBytes(parent.DangerousGetHandle().ToInt32()).CopyTo(info, 4);
        }
        BitConverter.GetBytes(name.Length).CopyTo(info, lengthOffset);
        name.CopyTo(info, nameOffset);
        if (parent is not null)
        {
            int status = NtSetInformationFile(stream.SafeFileHandle, out _, info, (uint)info.Length, 10);
            if (status < 0) throw new IOException("The owned file could not be published in its held directory.", new System.ComponentModel.Win32Exception((int)RtlNtStatusToDosError(status)));
        }
        else if (!SetFileInformationByHandle(stream.SafeFileHandle, 3, info, (uint)info.Length))
            throw new IOException("Verified recovery evidence could not be archived.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
    }

    internal static void DeleteOwned(FileStream stream)
    {
        if (!OperatingSystem.IsWindows()) { File.Delete(stream.Name); return; }
        if (!SetFileInformationByHandle(stream.SafeFileHandle, 4, [1], 1))
            throw new IOException("The owned publication file could not be removed.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
    }

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetFileInformationByHandle(SafeFileHandle handle, int kind, [In] byte[] info, uint size);
    [StructLayout(LayoutKind.Sequential)] private struct IoStatus { public IntPtr Status, Information; }
    [DllImport("ntdll.dll")] private static extern int NtSetInformationFile(SafeFileHandle handle, out IoStatus status, byte[] information, uint size, int kind);
    [DllImport("ntdll.dll")] private static extern uint RtlNtStatusToDosError(int status);
}
