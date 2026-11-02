using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using KillerBench.Engine.Profiles;

internal static class NativeAcceptancePackage
{
    internal static async Task RunAsync(Func<Task> action)
    {
        using var bios = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        string directory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        string runtime = Environment.ProcessPath ?? "";
        if (Environment.MachineName != "KILLERLAB-W11" ||
            !Equals(bios?.GetValue("SystemManufacturer"), "Microsoft Corporation") ||
            !Equals(bios?.GetValue("SystemProductName"), "Virtual Machine") ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable" ||
            !string.Equals(Path.GetDirectoryName(directory), @"C:\KillerLab\bench-resume-20261101", StringComparison.OrdinalIgnoreCase) ||
            !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(directory), @"\Agreen[0-9]+\z") ||
            runtime != @"C:\Program Files\dotnet\dotnet.exe")
            throw new InvalidOperationException("Protected acceptance package admission failed before writes.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("Protected package requires an elevated administrator.");
        if (!new DriveInfo(Path.GetPathRoot(directory)!).DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Protected package requires the admitted NTFS volume.");
        using var package = new PackageLease();
        var ancestors = new Stack<string>();
        for (string? path = Path.GetDirectoryName(directory); path is not null; path = Path.GetDirectoryName(path))
            ancestors.Push(path);
        foreach (string path in ancestors) package.Pin(path);
        package.Pin(directory);
        var directories = new List<string> { directory };
        var files = new List<string>();
        for (int i = 0; i < directories.Count; i++)
            foreach (string path in Directory.EnumerateFileSystemEntries(directories[i]))
            {
                if (directories.Count + files.Count >= 10_000)
                    throw new InvalidOperationException("Protected package exceeds its entry bound.");
                bool isDirectory = (File.GetAttributes(path) & FileAttributes.Directory) != 0;
                package.Pin(path, isDirectory);
                if (isDirectory) directories.Add(path); else files.Add(path);
            }

        var administrators = new SecurityIdentifier("S-1-5-32-544");
        var system = new SecurityIdentifier("S-1-5-18");
        foreach (string path in directories)
        {
            var security = new DirectorySecurity();
            security.SetOwner(administrators);
            security.SetAccessRuleProtection(true, false);
            foreach (var sid in new[] { administrators, system })
                security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(path).SetAccessControl(security);
        }
        foreach (string path in files)
        {
            var security = new FileSecurity();
            security.SetOwner(administrators);
            security.SetAccessRuleProtection(true, false);
            foreach (var sid in new[] { administrators, system })
                security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
            new FileInfo(path).SetAccessControl(security);
        }
        using var runtimeFolders = ProfileBackupPathGuard.Open(Path.GetDirectoryName(runtime)!, Path.Combine(Path.GetDirectoryName(runtime)!, "absent-package-hold"));
        var holds = new List<FileStream>();
        try
        {
            foreach (string file in files) holds.Add(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read));
            holds.Add(File.Open(runtime, FileMode.Open, FileAccess.Read, FileShare.Read));
            await action();
        }
        finally { foreach (var hold in holds) hold.Dispose(); }
    }

    private sealed class PackageLease : IDisposable
    {
        private readonly List<SafeFileHandle> handles = [];
        internal void Pin(string path, bool directory = true)
        {
            var handle = CreateFile(path, 0x80000000, 1, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
            if (handle.IsInvalid) { handle.Dispose(); throw new Win32Exception(Marshal.GetLastWin32Error()); }
            handles.Add(handle);
            if (!GetFileInformationByHandle(handle, out var information)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if ((information.Attributes & 0x400) != 0 || ((information.Attributes & 0x10) != 0) != directory)
                throw new IOException("The exact package object is redirected or has changed type.");
            if (!directory && information.Links != 1)
                throw new IOException("Package files must have exactly one hard link before any security changes.");
        }
        public void Dispose()
        {
            for (int i = handles.Count - 1; i >= 0; i--) handles[i].Dispose();
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInformation information);
}
