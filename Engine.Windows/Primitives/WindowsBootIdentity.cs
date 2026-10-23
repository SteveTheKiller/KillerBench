using System.Runtime.InteropServices;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Reads the kernel boot identifier independently of wall-clock corrections.</summary>
public static class WindowsBootIdentity
{
    public static string? Read()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            int status = NtQuerySystemInformation(90, out var info, Marshal.SizeOf<BootEnvironment>(), out int returned);
            return status == 0 && returned >= 16 && info.Identifier != Guid.Empty ? info.Identifier.ToString("D") : null;
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        { return null; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BootEnvironment
    {
        public Guid Identifier;
        public int FirmwareType;
        public ulong BootFlags;
    }

    [DllImport("ntdll.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int NtQuerySystemInformation(int informationClass, out BootEnvironment information, int length, out int returnedLength);
}
