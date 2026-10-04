using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Infrastructure;

internal static partial class RecoveryRecordFiles
{
    internal static FileStream OpenLocked(string path)
    {
        var handle = CreateFileW(path, 0x80010000, 1, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw new IOException("Recovery evidence could not be locked."); }
        try { return new FileStream(handle, FileAccess.Read); }
        catch { handle.Dispose(); throw; }
    }

    internal static void Archive(FileStream stream, string destination)
    {
        byte[] name = System.Text.Encoding.Unicode.GetBytes(Path.GetFullPath(destination));
        int lengthOffset = IntPtr.Size == 8 ? 16 : 8;
        int nameOffset = lengthOffset + 4;
        byte[] info = new byte[nameOffset + name.Length + 2];
        BitConverter.GetBytes(name.Length).CopyTo(info, lengthOffset);
        name.CopyTo(info, nameOffset);
        if (!SetFileInformationByHandle(stream.SafeFileHandle, 3, info, (uint)info.Length))
            throw new IOException("Verified recovery evidence could not be archived.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
    }

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetFileInformationByHandle(SafeFileHandle handle, int kind, [In] byte[] info, uint size);
}
