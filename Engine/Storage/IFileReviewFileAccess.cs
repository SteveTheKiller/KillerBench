using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace KillerBench.Engine.Storage;

/// <summary>The actual file-open boundary for read-only duplicate review.</summary>
public interface IFileReviewFileAccess
{
    SafeFileHandle OpenRead(string path, bool asynchronous);
}

public sealed class SystemFileReviewFileAccess : IFileReviewFileAccess
{
    public static SystemFileReviewFileAccess Instance { get; } = new();

    public SafeFileHandle OpenRead(string path, bool asynchronous)
    {
        if (!OperatingSystem.IsWindows())
            return File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                asynchronous ? FileOptions.Asynchronous | FileOptions.SequentialScan : FileOptions.None);
        uint flags = 0x00200080u | (asynchronous ? 0x48000000u : 0u);
        var handle = CreateFile(path, 0x80000000, FileShare.Read, IntPtr.Zero, FileMode.Open, flags, IntPtr.Zero);
        if (!handle.IsInvalid) return handle;
        int code = Marshal.GetLastWin32Error();
        handle.Dispose();
        throw new IOException("The file entry could not be opened without following links.", new System.ComponentModel.Win32Exception(code));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFile(string path, uint access, FileShare share, IntPtr security,
        FileMode disposition, uint flags, IntPtr template);
}
