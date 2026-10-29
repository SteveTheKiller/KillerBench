using System.ComponentModel;
using System.Runtime.InteropServices;

internal static partial class IsolatedDesktop
{
    public static int Run(string childArguments = "--isolated-ui")
    {
        RequireAppClosed();
        const uint readObjects = 0x0001, createWindow = 0x0002, createMenu = 0x0004, writeObjects = 0x0080;
        string name = "KillerBenchChecks_" + Guid.NewGuid().ToString("N");
        nint desktop = CreateDesktopW(name, 0, 0, 0,
            readObjects | createWindow | createMenu | writeObjects, 0);
        if (desktop == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot create isolated UI test desktop.");
        try
        {
            string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Missing test executable.");
            string command = "\"" + executable + "\" ";
            if (System.IO.Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                command += "\"" + typeof(IsolatedDesktop).Assembly.Location + "\" ";
            var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>(), Desktop = name,
                Flags = 0x100, Input = GetStdHandle(-10), Output = GetStdHandle(-11), Error = GetStdHandle(-12) };
            if (!CreateProcessW(executable, new System.Text.StringBuilder(command + childArguments), 0, 0, true,
                0x08000000, 0, null, ref startup, out var process))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot launch isolated UI tests.");
            try
            {
                // A child stuck behind a dialog nobody can see on the hidden desktop is ended, not waited on forever.
                uint waited = WaitForSingleObject(process.Process, 15 * 60 * 1000);
                if (waited == 0x102) { TerminateProcess(process.Process, 1); throw new TimeoutException("Isolated UI tests did not finish within 15 minutes."); }
                if (waited != 0 || !GetExitCodeProcess(process.Process, out uint exit))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return unchecked((int)exit);
            }
            finally { CloseHandle(process.Thread); CloseHandle(process.Process); }
        }
        finally { CloseDesktop(desktop); }
    }

    public static void Verify()
    {
        RequireAppClosed();
        var name = new System.Text.StringBuilder(256);
        if (!GetUserObjectInformationW(GetThreadDesktop(GetCurrentThreadId()), 2, name, 512, out _) ||
            !name.ToString().StartsWith("KillerBenchChecks_", StringComparison.Ordinal))
            throw new InvalidOperationException("UI thread is not isolated. Visible desktop testing is disabled.");
        Console.WriteLine("UI tests verified on separate desktop: " + name);
        nint input = OpenInputDesktop(0, false, 0x0001);
        if (input == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot verify that UI tests are hidden.");
        try
        {
            var visibleName = new System.Text.StringBuilder(256);
            if (!GetUserObjectInformationW(input, 2, visibleName, 512, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            if (string.Equals(name.ToString(), visibleName.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("UI test desktop is visible. Window tests are disabled.");
        }
        finally { CloseDesktop(input); }
    }

    private static void RequireAppClosed()
    {
        var applications = System.Diagnostics.Process.GetProcessesByName("KillerBench");
        try
        {
            if (applications.Length != 0)
                throw new InvalidOperationException("Window checks are disabled while KillerBench is running. Use non-UI checks.");
        }
        finally
        {
            foreach (var application in applications) application.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string? Reserved, Desktop, Title;
        public uint X, Y, Width, Height, XChars, YChars, Fill, Flags;
        public ushort Show, ReservedSize;
        public nint ReservedPointer, Input, Output, Error;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInfo { public nint Process, Thread; public uint ProcessId, ThreadId; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(string application, System.Text.StringBuilder command, nint processAttributes,
        nint threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags, nint environment,
        string? directory, ref StartupInfo startup, out ProcessInfo process);
    [LibraryImport("kernel32.dll")] private static partial nint GetStdHandle(int id);
    [LibraryImport("kernel32.dll", SetLastError = true)] private static partial uint WaitForSingleObject(nint handle, uint milliseconds);
    [LibraryImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static partial bool TerminateProcess(nint handle, uint exitCode);
    [LibraryImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetExitCodeProcess(nint process, out uint exit);
    [LibraryImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static partial bool CloseHandle(nint handle);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint CreateDesktopW(string name, nint device, nint deviceMode, uint flags, uint access, nint attributes);
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseDesktop(nint desktop);
    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();
    [LibraryImport("user32.dll")]
    private static partial nint GetThreadDesktop(uint threadId);
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUserObjectInformationW(nint handle, int index, System.Text.StringBuilder information, uint length, out uint needed);
}
