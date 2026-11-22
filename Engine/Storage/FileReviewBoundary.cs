using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Storage;

/// <summary>Holds each path component against replacement and reparse changes until review ends.</summary>
internal sealed class FileReviewBoundary : IDisposable
{
    private readonly Dictionary<string, SafeFileHandle> directories = new(StringComparer.OrdinalIgnoreCase);

    internal void Hold(string path)
    {
        if (!OperatingSystem.IsWindows()) throw new IOException("Local directory identity cannot be held on this platform.");
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        // A local long path (\\?\C:\...) is still local; \\?\UNC\ keeps its prefix and is refused as network below.
        if (full.StartsWith(@"\\?\", StringComparison.Ordinal) && !full.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            full = full[4..];
        string volume = Path.GetPathRoot(full)!;
        if (volume.StartsWith("\\\\", StringComparison.Ordinal) || new DriveInfo(volume).DriveType == DriveType.Network)
            throw new IOException("Network or redirected review roots are unsupported.");
        string current = volume;
        Open(current);
        foreach (string component in full[volume.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);
            Open(current);
        }
    }

    private void Open(string path)
    {
        if (directories.ContainsKey(path)) return;
        // OPEN_REPARSE_POINT inspects the entry itself. Previously held parents prevent a path swap.
        var handle = CreateFile(path, 0x80000000, FileShare.Read, IntPtr.Zero, FileMode.Open, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            throw new IOException("The local review boundary could not be held.");
        }
        try
        {
            FileAttributes attributes = File.GetAttributes(handle);
            if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("A redirected or unsupported directory was excluded from review.");
            directories.Add(path, handle);
        }
        catch { handle.Dispose(); throw; }
    }

    internal static bool IsDirectChild(string parent, string child) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Path.GetFullPath(child))!),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)), StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        foreach (var handle in directories.Values.Reverse()) handle.Dispose();
        directories.Clear();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFile(string path, uint access, FileShare share, IntPtr security,
        FileMode disposition, uint flags, IntPtr template);
}
