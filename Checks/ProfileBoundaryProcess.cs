using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

internal static class ProfileBoundaryProcess
{
    internal static bool TryRun(string[] args)
    {
        if (args is not ["--profile-boundary-attacker", var root]) return false;
        if (!OperatingSystem.IsWindows() || Environment.MachineName != "KILLERLAB-W11" ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable" ||
            !Path.GetFullPath(root).StartsWith(@"C:\KillerLab\bench-readiness-20261101\evidence\profile-native-", StringComparison.OrdinalIgnoreCase))
            throw new Exception("The medium-token fixture requires its exact owned guest root.");
        try { return RunAttacker(root); }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(root, "medium-error.txt"), error.ToString());
            throw;
        }
    }

    private static bool RunAttacker(string root)
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator) || Integrity(identity.AccessToken) != 8192)
            throw new Exception("The attacker did not receive a genuine medium token without administrator membership.");
        File.WriteAllText(Path.Combine(root, "medium-control.txt"), "medium control write");
        string destination = Path.Combine(root, "backup"), nested = Path.Combine(destination, "nested"), data = Path.Combine(nested, "data.txt");
        var allowed = new List<string>();
        void Refuse(string operation, Action action)
        {
            try { action(); allowed.Add(operation); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException) { }
        }
        Refuse("root creation", () => File.WriteAllText(Path.Combine(destination, "injected.txt"), "unexpected"));
        Refuse("nested data write", () => File.WriteAllText(data, "unexpected"));
        Refuse("nested stream write", () => File.WriteAllText(data + ":unexpected", "unexpected"));
        Refuse("file deletion", () => File.Delete(data));
        Refuse("root rename", () => Directory.Move(destination, destination + ".moved"));
        Refuse("source write", () => File.WriteAllText(Path.Combine(root, "source", "nested", "data.txt"), "unexpected"));
        Refuse("source rename", () => Directory.Move(Path.Combine(root, "source", "nested"), Path.Combine(root, "source", "moved")));
        Refuse("owner DACL rewrite", () =>
        {
            var directory = new DirectoryInfo(nested);
            var security = directory.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl, AccessControlType.Allow));
            directory.SetAccessControl(security);
        });
        File.WriteAllText(Path.Combine(root, "medium-result.json"), JsonSerializer.Serialize(new
        { medium = true, administrator = false, user = identity.User!.Value, allowed }));
        if (allowed.Count != 0) throw new Exception("The medium attacker changed protected backup state: " + string.Join(", ", allowed));
        return true;
    }

    internal static void Run(string root)
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (!DuplicateTokenEx(identity.AccessToken, 0xF01FF, IntPtr.Zero, 2, 1, out var primary)) throw Error("Primary token creation failed.");
        using (primary)
        {
            var administrator = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            byte[] sid = new byte[administrator.BinaryLength]; administrator.GetBinaryForm(sid, 0);
            var pinned = GCHandle.Alloc(sid, GCHandleType.Pinned);
            try
            {
                var disabled = new SidAndAttributes { Sid = pinned.AddrOfPinnedObject() };
                if (!CreateRestrictedToken(primary, 1, 1, ref disabled, 0, IntPtr.Zero, 0, IntPtr.Zero, out var token))
                    throw Error("Restricted token creation failed.");
                using (token)
                {
                    if (!ConvertStringSidToSid("S-1-16-8192", out var integrity)) throw Error("Medium integrity SID unavailable.");
                    try
                    {
                        var label = new SidAndAttributes { Sid = integrity, Attributes = 0x20 };
                        if (!SetTokenInformation(token, 25, ref label, Marshal.SizeOf<SidAndAttributes>() + 12))
                            throw Error("Medium integrity could not be assigned.");
                    }
                    finally { LocalFree(integrity); }
                    PermitChildObjects(token, identity.User!);
                    string executable = Environment.ProcessPath!;
                    string command = "\"" + executable + "\"";
                    if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                        command += " \"" + typeof(ProfileBoundaryProcess).Assembly.Location + "\"";
                    command += " --profile-boundary-attacker \"" + root + "\"";
                    using var desktop = new PrivateDesktop(identity.User!.Value);
                    var startup = new Startup { Size = Marshal.SizeOf<Startup>(), Desktop = desktop.Name };
                    string environmentText = "COMPUTERNAME=" + Environment.MachineName + "\0" +
                        "KILLERBENCH_DISPOSABLE=yes-this-machine-is-disposable\0OS=Windows_NT\0" +
                        "PATH=" + Environment.SystemDirectory + "\0" +
                        "SystemRoot=" + Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\0" +
                        "TEMP=" + root + "\0TMP=" + root + "\0" +
                        "WINDIR=" + Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\0\0";
                    IntPtr environment = Marshal.StringToHGlobalUni(environmentText);
                    ProcessInfo process;
                    try
                    {
                        if (!CreateProcessWithTokenW(token, 0, executable, new System.Text.StringBuilder(command), 0x08000400, environment,
                            Path.GetDirectoryName(typeof(ProfileBoundaryProcess).Assembly.Location)!, ref startup, out process))
                            throw Error("The medium-token fixture did not start.");
                    }
                    finally { Marshal.FreeHGlobal(environment); }
                    try
                    {
                        if (WaitForSingleObject(process.Process, 20_000) != 0)
                        {
                            TerminateProcess(process.Process, 1);
                            if (WaitForSingleObject(process.Process, 10_000) != 0) throw new Exception("Medium fixture termination unverified.");
                            throw new Exception("Medium fixture exceeded its deadline.");
                        }
                        if (!GetExitCodeProcess(process.Process, out uint exit) || exit != 0)
                            throw new Exception("Medium fixture failed with exit " + exit + ". " +
                                (File.Exists(Path.Combine(root, "medium-error.txt")) ? File.ReadAllText(Path.Combine(root, "medium-error.txt")) : "No managed error record was created."));
                        var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "medium-result.json")));
                        using (json)
                            if (!json.RootElement.GetProperty("medium").GetBoolean() ||
                                json.RootElement.GetProperty("user").GetString() != identity.User!.Value ||
                                json.RootElement.GetProperty("allowed").GetArrayLength() != 0)
                                throw new Exception("The same-user medium attacker was not proven contained.");
                    }
                    finally { CloseHandle(process.Thread); CloseHandle(process.Process); }
                }
            }
            finally { pinned.Free(); }
        }
    }

    private static void PermitChildObjects(SafeAccessTokenHandle token, SecurityIdentifier user)
    {
        GetTokenInformation(token, 6, IntPtr.Zero, 0, out int needed);
        if (needed < IntPtr.Size || needed > 65_536) throw new Exception("Restricted token default DACL size is invalid.");
        IntPtr information = Marshal.AllocHGlobal(needed);
        IntPtr replacement = IntPtr.Zero;
        try
        {
            if (!GetTokenInformation(token, 6, information, needed, out _)) throw Error("Restricted token default DACL unavailable.");
            IntPtr original = Marshal.ReadIntPtr(information);
            if (original == IntPtr.Zero) return;
            int length = (ushort)Marshal.ReadInt16(original, 2);
            byte[] bytes = new byte[length];
            Marshal.Copy(original, bytes, 0, length);
            var acl = new RawAcl(bytes, 0);
            Console.WriteLine("Medium fixture original token default DACL: " +
                new RawSecurityDescriptor(ControlFlags.DiscretionaryAclPresent, user, null, null, acl).GetSddlForm(AccessControlSections.Access));
            if (acl.Cast<GenericAce>().OfType<CommonAce>().Any(ace => ace.SecurityIdentifier == user &&
                ace.AceQualifier == AceQualifier.AccessAllowed && (ace.AccessMask & 0x10000000) != 0)) return;
            acl.InsertAce(acl.Count, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0x10000000, user, false, null));
            bytes = new byte[acl.BinaryLength]; acl.GetBinaryForm(bytes, 0);
            replacement = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, replacement, bytes.Length);
            Marshal.WriteIntPtr(information, replacement);
            if (!SetTokenDefaultDacl(token, 6, information, IntPtr.Size)) throw Error("Restricted token child-object DACL could not be assigned.");
        }
        finally
        {
            if (replacement != IntPtr.Zero) Marshal.FreeHGlobal(replacement);
            Marshal.FreeHGlobal(information);
        }
    }

    private static int Integrity(SafeAccessTokenHandle token)
    {
        GetTokenInformation(token, 25, IntPtr.Zero, 0, out int needed);
        IntPtr buffer = Marshal.AllocHGlobal(needed);
        try
        {
            if (!GetTokenInformation(token, 25, buffer, needed, out _)) throw Error("Token integrity unavailable.");
            var sid = new SecurityIdentifier(Marshal.ReadIntPtr(buffer));
            return int.Parse(sid.Value.Split('-')[^1]);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    private static Exception Error(string message) => new IOException(message, new Win32Exception(Marshal.GetLastWin32Error()));
    private sealed class PrivateDesktop : IDisposable
    {
        internal string Name { get; }
        private IntPtr station, desktop;
        internal PrivateDesktop(string user)
        {
            string stationName = "KillerBenchBoundary_" + Guid.NewGuid().ToString("N");
            Name = stationName + @"\Default";
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
                "D:(A;;GA;;;SY)(A;;GA;;;BA)(A;;GA;;;" + user + ")S:(ML;;NW;;;ME)", 1, out var descriptor, out _))
                throw Error("Private desktop security unavailable.");
            try
            {
                var attributes = new SecurityAttributes { Size = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor };
                station = CreateWindowStation(stationName, 0, 0xF037F, ref attributes);
                if (station == IntPtr.Zero) throw Error("Private window station unavailable.");
                IntPtr previous = GetProcessWindowStation();
                if (previous == IntPtr.Zero) throw Error("Original window station unavailable.");
                try
                {
                    if (!SetProcessWindowStation(station)) throw Error("Private window station selection failed.");
                    desktop = CreateDesktop("Default", null, IntPtr.Zero, 0, 0xF01FF, ref attributes);
                    if (desktop == IntPtr.Zero) throw Error("Private medium desktop unavailable.");
                }
                finally
                {
                    if (!SetProcessWindowStation(previous)) throw Error("Original process window station restoration failed.");
                }
            }
            catch { Dispose(); throw; }
            finally { LocalFree(descriptor); }
        }
        public void Dispose()
        {
            if (desktop != IntPtr.Zero) { CloseDesktop(desktop); desktop = IntPtr.Zero; }
            if (station != IntPtr.Zero) { CloseWindowStation(station); station = IntPtr.Zero; }
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Size; public IntPtr Descriptor; public int Inherit; }
    [StructLayout(LayoutKind.Sequential)] private struct SidAndAttributes { public IntPtr Sid; public uint Attributes; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct Startup
    { public int Size; public string? Reserved, Desktop, Title; public uint X, Y, XSize, YSize, XCount, YCount, Fill, Flags; public ushort Show, Reserved2; public IntPtr ReservedBytes, Input, Output, Error; }
    [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { public IntPtr Process, Thread; public uint Id, ThreadId; }
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool DuplicateTokenEx(SafeAccessTokenHandle token, uint access, IntPtr attributes, int level, int kind, out SafeAccessTokenHandle duplicate);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool CreateRestrictedToken(SafeAccessTokenHandle token, uint flags, uint disableCount, ref SidAndAttributes disabled, uint deleteCount, IntPtr deleted, uint restrictCount, IntPtr restricted, out SafeAccessTokenHandle result);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool SetTokenInformation(SafeAccessTokenHandle token, int kind, ref SidAndAttributes information, int size);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int kind, IntPtr information, int size, out int needed);
    [DllImport("advapi32.dll", EntryPoint = "SetTokenInformation", SetLastError = true)] private static extern bool SetTokenDefaultDacl(SafeAccessTokenHandle token, int kind, IntPtr information, int size);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ConvertStringSidToSid(string text, out IntPtr sid);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcessWithTokenW(SafeAccessTokenHandle token, uint logonFlags, string application, [In, Out] System.Text.StringBuilder command, uint flags, IntPtr environment, string directory, ref Startup startup, out ProcessInfo process);
    [DllImport("advapi32.dll", EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string text, uint revision, out IntPtr descriptor, out uint size);
    [DllImport("user32.dll", EntryPoint = "CreateWindowStationW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowStation(string name, uint flags, uint access, ref SecurityAttributes security);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetProcessWindowStation(IntPtr station);
    [DllImport("user32.dll", EntryPoint = "CreateDesktopW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateDesktop(string name, string? device, IntPtr mode, uint flags, uint access, ref SecurityAttributes security);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseWindowStation(IntPtr station);
    [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(IntPtr process, out uint code);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(IntPtr process, uint exit);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
}
