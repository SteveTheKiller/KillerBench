using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using KillerBench.Engine.Profiles;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class ProfileMetadataNativeAcceptanceChecks
{
    private const string GuestParent = @"C:\KillerLab\bench-resume-20261101";
    private const string ProtectedSddl = "O:BAG:BAD:P(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)";
    private static readonly string[] PrivilegeNames = ["SeBackupPrivilege", "SeRestorePrivilege", "SeSecurityPrivilege"];
    private sealed class BlockedException(string message) : Exception(message);
    private sealed record Result(string Scenario, string Outcome, string Detail, object? Evidence, bool OriginalTokenPreserved);

    internal static async Task RunAsync()
    {
        // This guard must finish before creating anything or enabling a duplicate-token privilege.
        string baseDirectory = RequireGuest();
        string root = Path.Combine(baseDirectory, "extended-profile-" + Guid.NewGuid().ToString("N"));
        CreateProtectedDirectory(root);
        var results = new List<Result>();
        foreach (var item in new (string Name, Func<string, Task<object>> Run)[]
        {
            ("Explicit root, nested-directory and file SACL preservation", SaclAsync),
            ("EFS refusal before backup creation with unchanged encrypted source", EfsAsync)
        })
        {
            TokenState? before = null;
            string scenarioRoot = Path.Combine(root, results.Count == 0 ? "sacl" : "efs");
            Result result;
            try
            {
                before = OriginalTokenState();
                CreateProtectedDirectory(scenarioRoot);
                object evidence = await item.Run(scenarioRoot).ConfigureAwait(false);
                result = new(item.Name, "Passed", "Independent native observations matched the synthetic source.", evidence, true);
            }
            catch (BlockedException error)
            {
                result = new(item.Name, "Blocked", error.Message, null, true);
            }
            catch (Exception error)
            {
                result = new(item.Name, "Failed", error.Message, new { type = error.GetType().Name, cause = error.GetBaseException().Message }, true);
            }
            bool preserved;
            try { preserved = before is not null && SameTokenState(before, OriginalTokenState()); }
            catch { preserved = false; }
            if (!preserved) result = result with { Outcome = "Failed", Detail = result.Detail + " Original token privileges or identity changed.", OriginalTokenPreserved = false };
            results.Add(result);
            await WriteNewJsonAsync(Path.Combine(root, results.Count == 1 ? "sacl-result.json" : "efs-result.json"), result).ConfigureAwait(false);
            Console.WriteLine(result.Outcome.ToUpperInvariant() + ": " + result.Scenario + ": " + result.Detail);
        }
        string resultPath = Path.Combine(root, "extended-profile-results.json");
        await WriteNewJsonAsync(resultPath, new
        {
            schemaVersion = 1,
            utc = DateTimeOffset.UtcNow,
            syntheticDataOnly = true,
            nativeCasesSelected = 2,
            passed = results.Count(result => result.Outcome == "Passed"),
            blocked = results.Count(result => result.Outcome == "Blocked"),
            failed = results.Count(result => result.Outcome == "Failed"),
            results,
            cleanup = "All owned evidence and fixtures retained. Coordinator exports evidence and restores the admitted snapshot. No recursive cleanup."
        }).ConfigureAwait(false);
        Console.WriteLine("EXTENDED_PROFILE_RESULT: " + resultPath);
        if (results.Any(result => result.Outcome != "Passed"))
            throw new InvalidOperationException("Extended profile acceptance has failed or blocked cases. Preserve evidence and restore the admitted snapshot.");
    }

    private static string RequireGuest()
    {
        if (!OperatingSystem.IsWindows() || Environment.MachineName != "KILLERLAB-W11" ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable")
            throw new InvalidOperationException("Extended native acceptance requires the exact admitted guest and explicit disposable marker.");
        using var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        if (!Equals(bios?.GetValue("SystemManufacturer"), "Microsoft Corporation") ||
            !Equals(bios?.GetValue("SystemProductName"), "Virtual Machine"))
            throw new InvalidOperationException("Extended native acceptance requires Microsoft Virtual Machine BIOS identity.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("Extended native acceptance requires an elevated administrator token.");
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        string name = Path.GetFileName(full);
        if (!string.Equals(Path.GetDirectoryName(full), GuestParent, StringComparison.OrdinalIgnoreCase) ||
            !name.StartsWith("green", StringComparison.Ordinal) || name.Length <= 5 || !name[5..].All(char.IsAsciiDigit))
            throw new InvalidOperationException("Extended native acceptance must run from the exact admitted green-number fixture directory.");
        for (string? ancestor = full; ancestor is not null; ancestor = Path.GetDirectoryName(ancestor))
            if (!Directory.Exists(ancestor) || (File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("The admitted fixture directory and every ancestor must be existing nonredirected directories.");
        if (!new DriveInfo(Path.GetPathRoot(full)!).DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Extended native acceptance requires the admitted NTFS volume.");
        return full;
    }

    private static async Task<object> SaclAsync(string root)
    {
        using var privileged = DuplicateWithSecurityPrivilege();
        return await WindowsIdentity.RunImpersonatedAsync(privileged, async () =>
        {
            string source = Path.Combine(root, "source"), nested = Path.Combine(source, "nested");
            string destination = Path.Combine(root, "backup");
            CreateProtectedDirectory(source);
            CreateProtectedDirectory(nested);
            string file = Path.Combine(nested, "synthetic.txt");
            const string content = "Owned synthetic SACL preservation content.";
            await WriteNewTextAsync(file, content).ConfigureAwait(false);
            string rootFile = Path.Combine(source, "root.txt");
            await WriteNewTextAsync(rootFile, "Owned root-level profile content.").ConfigureAwait(false);
            SetExplicitAudit(source, directory: true);
            SetExplicitAudit(nested, directory: true);
            SetExplicitAudit(file, directory: false);
            SetExplicitAudit(rootFile, directory: false);
            string[] relative = ["", "root.txt", "nested", @"nested\synthetic.txt"];
            var before = relative.ToDictionary(path => path, path => ReadAuditSddl(Path.Combine(source, path)));
            foreach (string sddl in before.Values)
            {
                var descriptor = new RawSecurityDescriptor(sddl);
                if ((descriptor.ControlFlags & ControlFlags.SystemAclProtected) == 0 || descriptor.SystemAcl is not { Count: 1 } acl ||
                    acl[0] is not CommonAce ace || ace.AceQualifier != AceQualifier.SystemAudit ||
                    ace.AceFlags != (AceFlags.SuccessfulAccess | AceFlags.FailedAccess) || ace.AccessMask != 3 ||
                    ace.SecurityIdentifier.Value != "S-1-5-32-544")
                    throw new InvalidOperationException("Independent source SDDL did not contain the explicit administrator audit ACE.");
            }
            RequireAbsent(destination);
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, null, deadline.Token).ConfigureAwait(false);
            var observations = new List<object>();
            foreach (string path in relative)
            {
                string sourceAfter = ReadAuditSddl(Path.Combine(source, path));
                string copied = ReadAuditSddl(Path.Combine(destination, path));
                if (before[path] != sourceAfter || before[path] != copied)
                    throw new InvalidOperationException("SACL SDDL mismatch for owned fixture object: " + (path.Length == 0 ? "source root" : path));
                observations.Add(new { relative = path, sourceBefore = before[path], sourceAfter, destination = copied });
            }
            if (await ReadSyntheticTextAsync(Path.Combine(destination, @"nested\synthetic.txt")).ConfigureAwait(false) != content)
                throw new InvalidOperationException("SACL copy changed the synthetic file content.");
            if (await ReadSyntheticTextAsync(Path.Combine(destination, "root.txt")).ConfigureAwait(false) != "Owned root-level profile content.")
                throw new InvalidOperationException("The root-level profile file was not preserved.");
            return (object)new { independentlyReadWith = "GetNamedSecurityInfoW and ConvertSecurityDescriptorToStringSecurityDescriptorW, SACL only", objects = observations };
        }).ConfigureAwait(false);
    }

    private static async Task<object> EfsAsync(string root)
    {
        using (var identity = WindowsIdentity.GetCurrent())
            if (identity.User?.Value is null or "S-1-5-18")
                throw new BlockedException("EFS case requires the admitted ordinary guest user, not SYSTEM or an unavailable user identity.");
        if (!GetVolumeInformation(@"C:\", null, 0, out _, out _, out uint flags, null, 0))
            throw new BlockedException("EFS volume capabilities could not be inspected, Windows error " + Marshal.GetLastWin32Error() + ".");
        if ((flags & 0x00020000) == 0) throw new BlockedException("The admitted volume does not advertise file encryption support.");
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup");
        CreateProtectedDirectory(source);
        string file = Path.Combine(source, "synthetic-encrypted.txt");
        string content = "Owned synthetic EFS fixture " + Guid.NewGuid().ToString("N");
        await WriteNewTextAsync(file, content).ConfigureAwait(false);
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var security = new FileInfo(file).GetAccessControl(AccessControlSections.Owner);
            security.SetOwner(identity.User!);
            new FileInfo(file).SetAccessControl(security);
            if (!Equals(new FileInfo(file).GetAccessControl(AccessControlSections.Owner).GetOwner(typeof(SecurityIdentifier)), identity.User))
                throw new InvalidOperationException("The synthetic EFS source was not owned by the admitted guest user.");
        }
        if (!EncryptFile(file))
            throw new BlockedException("Windows could not establish the owned EFS fixture, error " + Marshal.GetLastWin32Error() + ". EFS policy, edition or key availability remains unverified.");
        bool sourceEncrypted = Encrypted(file);
        if (!sourceEncrypted) throw new InvalidOperationException("The owned EFS source was not independently observed encrypted.");
        string sourceBefore = await ReadSyntheticTextAsync(file).ConfigureAwait(false);
        if (sourceBefore != content) throw new InvalidOperationException("The ordinary user's decrypted source did not match the synthetic fixture.");
        RequireAbsent(destination);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        bool refused = false;
        try { await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, null, deadline.Token).ConfigureAwait(false); }
        catch (Exception error) when (error is IOException or InvalidOperationException && error.Message.Contains("EFS", StringComparison.Ordinal))
        { refused = true; }
        bool sourceAfterEncrypted = Encrypted(file);
        string sourceAfter = await ReadSyntheticTextAsync(file).ConfigureAwait(false);
        if (!refused || Directory.Exists(destination) || File.Exists(destination) || !sourceAfterEncrypted || sourceBefore != sourceAfter)
            throw new InvalidOperationException("EFS refusal did not preserve the encrypted source and absent backup destination.");
        return new
        {
            sourceBeforeEncrypted = sourceEncrypted,
            sourceAfterEncrypted,
            refusedBeforeDestinationCreation = true,
            encryptedSourceUnchanged = true,
            sourceOwnedByAdmittedUser = true,
            syntheticPlaintextSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
            keyHandling = "No key or certificate was exported or recorded. Windows may have created the disposable guest user's EFS key; snapshot restoration is required."
        };
    }

    private static bool Encrypted(string path)
    {
        if (!FileEncryptionStatus(path, out uint status)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Owned EFS status could not be read.");
        return status == 1 && (File.GetAttributes(path) & FileAttributes.Encrypted) != 0;
    }

    private static void SetExplicitAudit(string path, bool directory)
    {
        FileSystemSecurity security = directory ? new DirectoryInfo(path).GetAccessControl(AccessControlSections.Audit) : new FileInfo(path).GetAccessControl(AccessControlSections.Audit);
        security.SetAuditRuleProtection(isProtected: true, preserveInheritance: false);
        foreach (FileSystemAuditRule existing in security.GetAuditRules(true, true, typeof(SecurityIdentifier)))
            security.RemoveAuditRuleSpecific(existing);
        security.AddAuditRule(new FileSystemAuditRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            FileSystemRights.ReadData | FileSystemRights.WriteData, InheritanceFlags.None, PropagationFlags.None, AuditFlags.Success | AuditFlags.Failure));
        if (directory) new DirectoryInfo(path).SetAccessControl((DirectorySecurity)security);
        else new FileInfo(path).SetAccessControl((FileSecurity)security);
    }

    private static string ReadAuditSddl(string path)
    {
        using var privileged = DuplicateWithSecurityPrivilege();
        return WindowsIdentity.RunImpersonated(privileged, () => ReadAuditSddlCore(path));
    }

    private static string ReadAuditSddlCore(string path)
    {
        uint error = GetNamedSecurityInfo(path, 1, 8, out _, out _, out _, out _, out var descriptor);
        if (error != 0) throw new Win32Exception((int)error, "Independent owned-object SACL readback failed: " + error + "; object=" + Path.GetFileName(Path.TrimEndingDirectorySeparator(path)));
        try
        {
            if (!ConvertSecurityDescriptorToStringSecurityDescriptor(descriptor, 1, 8, out var text, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Owned SACL SDDL conversion failed.");
            try { return Marshal.PtrToStringUni(text) ?? throw new InvalidOperationException("Owned SACL SDDL was empty."); }
            finally { LocalFree(text); }
        }
        finally { LocalFree(descriptor); }
    }

    private static SafeAccessTokenHandle DuplicateWithSecurityPrivilege()
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (!DuplicateTokenEx(identity.AccessToken, 0x2E, IntPtr.Zero, 2, 2, out var duplicate))
            throw new BlockedException("A scoped impersonation token could not be duplicated, error " + Marshal.GetLastWin32Error() + ".");
        try
        {
            if (!LookupPrivilegeValue(null, "SeSecurityPrivilege", out var luid)) throw new BlockedException("SeSecurityPrivilege is unavailable.");
            var adjustment = new TokenPrivilege { Count = 1, Luid = luid, Attributes = 2 };
            if (!AdjustTokenPrivileges(duplicate, false, ref adjustment, 0, IntPtr.Zero, IntPtr.Zero) || Marshal.GetLastWin32Error() != 0)
                throw new BlockedException("The account cannot enable SeSecurityPrivilege in the scoped duplicate token.");
            return duplicate;
        }
        catch { duplicate.Dispose(); throw; }
    }

    private sealed record TokenState(string? Sid, uint?[] Privileges);
    private static TokenState OriginalTokenState()
    {
        using var identity = WindowsIdentity.GetCurrent();
        GetTokenInformation(identity.AccessToken, 3, IntPtr.Zero, 0, out int length);
        if (length is <= 0 or > 65536) throw new InvalidOperationException("Original token privilege data could not be bounded.");
        IntPtr data = Marshal.AllocHGlobal(length);
        try
        {
            if (!GetTokenInformation(identity.AccessToken, 3, data, length, out _)) throw new Win32Exception(Marshal.GetLastWin32Error());
            int count = Marshal.ReadInt32(data);
            if (count < 0 || count > (length - 4) / 12) throw new InvalidOperationException("Original token privilege data was invalid.");
            var attributes = new uint?[PrivilegeNames.Length];
            for (int wanted = 0; wanted < PrivilegeNames.Length; wanted++)
            {
                if (!LookupPrivilegeValue(null, PrivilegeNames[wanted], out var id)) throw new Win32Exception(Marshal.GetLastWin32Error());
                for (int i = 0; i < count; i++)
                {
                    var item = Marshal.PtrToStructure<LuidAndAttributes>(data + 4 + i * 12);
                    if (item.Luid.Low == id.Low && item.Luid.High == id.High) attributes[wanted] = item.Attributes;
                }
            }
            return new(identity.User?.Value, attributes);
        }
        finally { Marshal.FreeHGlobal(data); }
    }

    private static bool SameTokenState(TokenState before, TokenState after) => before.Sid == after.Sid && before.Privileges.SequenceEqual(after.Privileges);
    private static ProfileSnapshot Target(string source) => new("S-1-5-21-1-2-3-1001", source, false, false, DateTimeOffset.UtcNow.AddDays(-120), false);
    private static void RequireAbsent(string path)
    {
        if (File.Exists(path) || Directory.Exists(path)) throw new InvalidOperationException("Fresh owned backup destination already exists.");
    }

    private static void CreateProtectedDirectory(string path)
    {
        RequireAbsent(path);
        if (!ConvertStringSecurityDescriptorToSecurityDescriptor(ProtectedSddl, 1, out var descriptor, out _)) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var attributes = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor };
            if (!CreateDirectory(path, ref attributes)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Fresh protected fixture directory could not be created.");
            var security = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access);
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
            if (!security.AreAccessRulesProtected || rules.Length != 2 || rules.Any(rule => rule.IsInherited || rule.AccessControlType != AccessControlType.Allow ||
                rule.IdentityReference.Value is not ("S-1-5-18" or "S-1-5-32-544") || (rule.FileSystemRights & FileSystemRights.FullControl) != FileSystemRights.FullControl))
                throw new InvalidOperationException("Protected fixture root DACL did not independently verify.");
        }
        finally { LocalFree(descriptor); }
    }

    private static async Task WriteNewTextAsync(string path, string text)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(text)).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }
    private static Task WriteNewJsonAsync(string path, object value) => WriteNewTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    private static async Task<string> ReadSyntheticTextAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 4096) throw new InvalidOperationException("Synthetic content exceeded its fixed readback bound.");
        byte[] bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes).ConfigureAwait(false);
        if (stream.ReadByte() != -1) throw new InvalidOperationException("Synthetic content changed while reading.");
        return Encoding.UTF8.GetString(bytes);
    }

    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool Inherit; }
    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct LuidAndAttributes { public Luid Luid; public uint Attributes; }
    [StructLayout(LayoutKind.Sequential)] private struct TokenPrivilege { public uint Count; public Luid Luid; public uint Attributes; }
    [DllImport("kernel32.dll", EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateDirectory(string path, ref SecurityAttributes attributes);
    [DllImport("advapi32.dll", EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string text, uint revision, out IntPtr descriptor, out uint length);
    [DllImport("advapi32.dll", EntryPoint = "GetNamedSecurityInfoW", CharSet = CharSet.Unicode)] private static extern uint GetNamedSecurityInfo(string path, uint kind, uint info, out IntPtr owner, out IntPtr group, out IntPtr dacl, out IntPtr sacl, out IntPtr descriptor);
    [DllImport("advapi32.dll", EntryPoint = "ConvertSecurityDescriptorToStringSecurityDescriptorW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(IntPtr descriptor, uint revision, uint info, out IntPtr text, out uint length);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool DuplicateTokenEx(SafeAccessTokenHandle source, uint access, IntPtr attributes, int level, int kind, out SafeAccessTokenHandle duplicate);
    [DllImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool LookupPrivilegeValue(string? system, string name, out Luid luid);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool AdjustTokenPrivileges(SafeAccessTokenHandle token, bool disableAll, ref TokenPrivilege privileges, uint length, IntPtr previous, IntPtr returned);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int kind, IntPtr information, int length, out int returned);
    [DllImport("advapi32.dll", EntryPoint = "EncryptFileW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool EncryptFile(string path);
    [DllImport("advapi32.dll", EntryPoint = "FileEncryptionStatusW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool FileEncryptionStatus(string path, out uint status);
    [DllImport("kernel32.dll", EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool GetVolumeInformation(string root, StringBuilder? volume, uint volumeLength, out uint serial, out uint maximumName, out uint flags, StringBuilder? format, uint formatLength);
}
