using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using KillerBench.Engine;
using KillerBench.Engine.BitLocker;
using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Profiles;
using KillerBench.Engine.Workflows;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

internal static class ProfileNativeAcceptanceChecks
{
    internal static async Task RunAclOnlyAsync(string scratch)
    {
        RequireGuest();
        string root = Path.Combine(scratch, "acl-native-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Console.WriteLine(await AclAsync(root));
    }
    internal static async Task RunAsync(string scratch)
    {
        RequireGuest();
        string parent = Path.GetFullPath(scratch);
        if (!Directory.Exists(parent) || (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("An existing owned non-redirected scratch directory is required.");
        string root = Path.Combine(parent, "profile-native-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var results = new List<object>();
        bool failed = false;
        foreach (var item in new (string Name, Func<string, Task<string>> Run)[]
        {
            ("KB-006 ordinary and junction", InclusionAsync),
            ("KB-006 app alias", AliasAsync),
            ("KB-006 unsupported symbolic link", UnsupportedAsync),
            ("KB-004 on-disk synthetic backup", AclAsync),
            ("INV-01 destination creation", DestinationRaceAsync),
            ("INV-01 medium-user containment", MediumContainmentAsync),
            ("Profile metadata preservation", MetadataAsync)
        })
        {
            string owned = Path.Combine(root, results.Count.ToString("D2"));
            Directory.CreateDirectory(owned);
            try
            {
                string detail = await item.Run(owned);
                string outcome = detail.StartsWith("BLOCKED:", StringComparison.Ordinal) ? "Blocked" : "Passed";
                results.Add(new { scenario = item.Name, outcome, detail });
                Console.WriteLine(outcome.ToUpperInvariant() + ": " + item.Name + ": " + detail);
            }
            catch (Exception error)
            {
                failed = true;
                string detail = error.Message + " Cause: " + error.GetBaseException().Message;
                results.Add(new { scenario = item.Name, outcome = "Failed", detail, type = error.GetType().Name });
                Console.WriteLine("FAILED: " + item.Name + ": " + detail);
            }
        }
        string resultPath = Path.Combine(root, "native-results.json");
        await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(new
        {
            machine = Environment.MachineName,
            system = WindowsIdentity.GetCurrent().User?.Value == "S-1-5-18",
            os = Environment.OSVersion.Version.ToString(),
            utc = DateTimeOffset.UtcNow,
            results,
            cleanup = "Preserved for coordinator evidence export and checkpoint restoration. No recursive cleanup attempted."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("NATIVE_RESULT: " + resultPath);
        if (failed) throw new Exception("Native acceptance has failed scenarios. Preserve evidence and restore the admitted checkpoint.");
    }

    private static void RequireGuest()
    {
        if (!OperatingSystem.IsWindows() || Environment.MachineName.Equals("SHREDDER", StringComparison.OrdinalIgnoreCase) ||
            !Environment.MachineName.Equals("KILLERLAB-W11", StringComparison.OrdinalIgnoreCase) ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable")
            throw new InvalidOperationException("Native acceptance requires the exact admitted KillerLab guest and disposable marker.");
        using var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        if (bios?.GetValue("SystemManufacturer") as string != "Microsoft Corporation" ||
            bios.GetValue("SystemProductName") as string != "Virtual Machine" ||
            !new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("Native acceptance requires authenticated Microsoft VM hardware and an elevated token.");
    }

    private static ProfileSnapshot Target(string source) => new("S-1-5-21-1-2-3-1001", source,
        false, false, DateTimeOffset.UtcNow.AddDays(-120), false);

    private static async Task<string> InclusionAsync(string root)
    {
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup"), outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(source); Directory.CreateDirectory(outside);
        string relative = @"AppData\Local\Microsoft\WindowsApps\nested\ordinary.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(source, relative))!);
        await File.WriteAllTextAsync(Path.Combine(source, relative), "owned ordinary data");
        await File.WriteAllTextAsync(Path.Combine(outside, "outside-sentinel.txt"), "outside sentinel");
        await JunctionAsync(Path.Combine(source, "omitted-junction"), outside);
        await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, null, CancellationToken.None);
        Require(await File.ReadAllTextAsync(Path.Combine(destination, relative)) == "owned ordinary data", "Ordinary WindowsApps data was not copied.");
        Require(!Directory.Exists(Path.Combine(destination, "omitted-junction")), "The junction was copied.");
        Require(await File.ReadAllTextAsync(Path.Combine(outside, "outside-sentinel.txt")) == "outside sentinel", "The outside sentinel changed.");
        var verified = await ProfileBackupArtifactVerifier.VerifyAsync(Target(source), Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName));
        Require(verified.Verified && verified.Receipt?.FileCount == 1, "The native copy did not produce a verified one-file artifact: " + verified.Detail);
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName)));
        var omissions = json.RootElement.EnumerateObject().Single(p => p.Name.Equals("OmittedLinks", StringComparison.OrdinalIgnoreCase)).Value;
        Require(omissions.GetArrayLength() == 1 && omissions[0].GetString() == "omitted-junction", "The receipt did not record the exact omitted junction.");
        return "Production Robocopy preserved ordinary nested data, omitted the junction, preserved the outside sentinel and verified the artifact and omission receipt.";
    }

    private static async Task<string> AliasAsync(string root)
    {
        string? alias = Environment.GetEnvironmentVariable("KILLERBENCH_NATIVE_APP_ALIAS");
        if (string.IsNullOrWhiteSpace(alias))
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps");
            if (Directory.Exists(folder))
                alias = Directory.EnumerateFiles(folder).Take(128).FirstOrDefault(path =>
                    (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 && ReadTag(path) == 0x8000001B);
        }
        if (string.IsNullOrWhiteSpace(alias) || !File.Exists(alias) || ReadTag(alias) != 0x8000001B)
            return "BLOCKED: no genuine app execution alias fixture was available for this token. A symbolic link is not a substitute.";
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup");
        string apps = Path.Combine(source, @"AppData\Local\Microsoft\WindowsApps");
        Directory.CreateDirectory(apps);
        string ownedAlias = Path.Combine(apps, "owned-alias.exe");
        try { CloneAlias(alias, ownedAlias); }
        catch (Win32Exception error) { return "BLOCKED: Windows refused creating the owned genuine alias fixture, error " + error.NativeErrorCode + "."; }
        await File.WriteAllTextAsync(Path.Combine(apps, "ordinary.txt"), "ordinary beside alias");
        await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, null, CancellationToken.None);
        Require(await File.ReadAllTextAsync(Path.Combine(destination, @"AppData\Local\Microsoft\WindowsApps\ordinary.txt")) == "ordinary beside alias", "Ordinary alias-adjacent data was omitted.");
        Require(!File.Exists(Path.Combine(destination, @"AppData\Local\Microsoft\WindowsApps\owned-alias.exe")), "The app execution alias was copied.");
        var verified = await ProfileBackupArtifactVerifier.VerifyAsync(Target(source), Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName));
        Require(verified.Verified && verified.Detail.Contains("Omitted 1", StringComparison.Ordinal), "The genuine alias omission was not verified and disclosed: " + verified.Detail);
        return "Production Robocopy preserved ordinary alias-adjacent data and verified one genuine app alias omission.";
    }

    private static async Task<string> UnsupportedAsync(string root)
    {
        string source = Path.Combine(root, "source"), outside = Path.Combine(root, "outside.txt"), destination = Path.Combine(root, "backup");
        Directory.CreateDirectory(source);
        await File.WriteAllTextAsync(outside, "outside sentinel");
        try { File.CreateSymbolicLink(Path.Combine(source, "unsupported-link.txt"), outside); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { return "BLOCKED: Windows refused the owned unsupported symbolic-link fixture: " + error.GetType().Name + "."; }
        try
        {
            await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, null, CancellationToken.None);
            throw new Exception("An unsupported symbolic link was accepted.");
        }
        catch (InvalidOperationException)
        {
            Require(!Directory.Exists(destination) && await File.ReadAllTextAsync(outside) == "outside sentinel", "Unsupported-link refusal changed the destination or outside data.");
            return "Unsupported symbolic link refused before native copying; destination absent and sentinel unchanged. Cloud-placeholder construction was not attempted.";
        }
    }

    private static async Task<string> DestinationRaceAsync(string root)
    {
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup"), outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(source); Directory.CreateDirectory(outside);
        string payload = "owned boundary payload " + Guid.NewGuid().ToString("N");
        await File.WriteAllTextAsync(Path.Combine(source, "race-payload.txt"), payload);
        await File.WriteAllTextAsync(Path.Combine(outside, "outside-sentinel.txt"), "outside sentinel");
        bool injected = false;
        var progress = new InlineProgress(message =>
        {
            if (injected || !message.StartsWith("Copying the selected unloaded profile", StringComparison.Ordinal)) return;
            JunctionAsync(destination, outside).GetAwaiter().GetResult();
            injected = true;
        });
        string? copyError = null;
        try { await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, progress, CancellationToken.None); }
        catch (Exception error) { copyError = error.GetType().Name + ": " + error.Message; }
        bool escaped = File.Exists(Path.Combine(outside, "race-payload.txt")) &&
            await File.ReadAllTextAsync(Path.Combine(outside, "race-payload.txt")) == payload;
        bool sentinelUnchanged = await File.ReadAllTextAsync(Path.Combine(outside, "outside-sentinel.txt")) == "outside sentinel";
        await File.WriteAllTextAsync(Path.Combine(root, "race-observation.json"), JsonSerializer.Serialize(new
        { injected, escaped, sentinelUnchanged, copyError, source, destination, outside }));
        Require(injected, "The exact post-check progress boundary was not reached; the race is untested.");
        Require(!escaped, "INV-01 CONFIRMED: production Robocopy wrote the owned payload through a newly created destination junction into the outside tree. Evidence: race-observation.json.");
        Require(sentinelUnchanged, "The outside sentinel changed.");
        return "The injected destination junction did not receive the payload. Copy result: " + (copyError ?? "completed") + ". This covers one exact boundary only.";
    }

    private static async Task<string> MediumContainmentAsync(string root)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var parent = new DirectoryInfo(root);
        var acl = parent.GetAccessControl();
        acl.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(identity.User!,
            System.Security.AccessControl.FileSystemRights.FullControl,
            System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
            System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
        parent.SetAccessControl(acl);
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup");
        Directory.CreateDirectory(Path.Combine(source, "nested"));
        await File.WriteAllTextAsync(Path.Combine(source, "nested", "data.txt"), "original profile data");
        bool tested = false;
        var progress = new InlineProgress(message =>
        {
            if (tested || !message.StartsWith("The copy provider completed", StringComparison.Ordinal)) return;
            var directory = new DirectoryInfo(Path.Combine(destination, "nested"));
            var security = directory.GetAccessControl();
            string originalDacl = security.GetSecurityDescriptorSddlForm(System.Security.AccessControl.AccessControlSections.Access);
            security.SetOwner(identity.User!);
            using (var ownerHandle = CreateFile(directory.FullName, 0x000E0000, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero))
            {
                if (ownerHandle.IsInvalid || !SetKernelObjectSecurity(ownerHandle, 5, security.GetSecurityDescriptorBinaryForm()))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            Require(directory.GetAccessControl().GetSecurityDescriptorSddlForm(System.Security.AccessControl.AccessControlSections.Access) == originalDacl,
                "The ownership fixture setup did not retain the original protected DACL.");
            Require(Equals(directory.GetAccessControl().GetOwner(typeof(SecurityIdentifier)), identity.User),
                "The nested staging directory was not owned by the same user for the OWNER RIGHTS test.");
            ProfileBoundaryProcess.Run(root);
            tested = true;
        });
        await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, progress, CancellationToken.None);
        Require(tested && await File.ReadAllTextAsync(Path.Combine(destination, "nested", "data.txt")) == "original profile data",
            "The real medium-user attempt was not contained during protected copying.");
        return "Same-user medium token proved by token integrity and role checks; root/nested content, ADS, deletion, rename, owner DACL and held source mutations were denied.";
    }

    private static async Task<string> MetadataAsync(string root)
    {
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup");
        string nested = Path.Combine(source, "nested"), data = Path.Combine(nested, "data.txt");
        Directory.CreateDirectory(source);
        var sourceDirectory = new DirectoryInfo(source);
        var sourceSecurity = sourceDirectory.GetAccessControl();
        sourceSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.WorldSid, null), System.Security.AccessControl.FileSystemRights.Read,
            System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
            System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
        sourceDirectory.SetAccessControl(sourceSecurity);
        Directory.CreateDirectory(Path.Combine(nested, "empty"));
        await File.WriteAllTextAsync(data, "ordinary content");
        await File.WriteAllTextAsync(data + ":named", "file stream content");
        await File.WriteAllTextAsync(nested + ":named", "directory stream content");
        await File.WriteAllTextAsync(source + ":named", "root stream content");
        string readOnly = Path.Combine(nested, "readonly.txt");
        await File.WriteAllTextAsync(readOnly, "read-only source content");
        File.SetAttributes(readOnly, FileAttributes.ReadOnly);
        var sections = System.Security.AccessControl.AccessControlSections.Access |
            System.Security.AccessControl.AccessControlSections.Owner | System.Security.AccessControl.AccessControlSections.Group;
        string[] entries = ["", "nested", @"nested\empty", @"nested\data.txt", @"nested\readonly.txt"];
        var security = entries.ToDictionary(relative => relative, relative =>
            relative.EndsWith(".txt", StringComparison.Ordinal)
                ? new FileInfo(Path.Combine(source, relative)).GetAccessControl(sections).GetSecurityDescriptorSddlForm(sections)
                : new DirectoryInfo(Path.Combine(source, relative)).GetAccessControl(sections).GetSecurityDescriptorSddlForm(sections));
        DateTime written = new(2025, 4, 5, 6, 7, 8, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(data, written);
        await new WindowsProfileBackupHost().CopyAsync(Target(source), destination, null, CancellationToken.None);
        var problems = new List<string>();
        foreach (string relative in entries)
        {
            string copied = Path.Combine(destination, relative);
            string actual = relative.EndsWith(".txt", StringComparison.Ordinal)
                ? new FileInfo(copied).GetAccessControl(sections).GetSecurityDescriptorSddlForm(sections)
                : new DirectoryInfo(copied).GetAccessControl(sections).GetSecurityDescriptorSddlForm(sections);
            if (actual != security[relative]) problems.Add("Owner, group or DACL changed: " + relative);
        }
        Require(Directory.Exists(Path.Combine(destination, @"nested\empty")), "An empty source directory was lost.");
        Require(await File.ReadAllTextAsync(Path.Combine(destination, @"nested\readonly.txt")) == "read-only source content" &&
            (File.GetAttributes(Path.Combine(destination, @"nested\readonly.txt")) & FileAttributes.ReadOnly) != 0,
            "A read-only file's data or attributes were lost.");
        foreach (var item in new[]
        {
            (Path.Combine(destination, @"nested\data.txt") + ":named", "file stream content"),
            (Path.Combine(destination, "nested") + ":named", "directory stream content"),
            (destination + ":named", "root stream content")
        })
        {
            try { if (await File.ReadAllTextAsync(item.Item1) != item.Item2) problems.Add("Incorrect alternate stream: " + item.Item2); }
            catch (IOException) { problems.Add("Missing alternate stream: " + item.Item2); }
        }
        var artifactRules = new FileInfo(Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName))
            .GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));
        Require(!artifactRules.Cast<System.Security.AccessControl.FileSystemAccessRule>().Any(rule => rule.IdentityReference.Value == "S-1-1-0"),
            "Root security restoration propagated into the already-created artifact.");
        Require(problems.Count == 0, string.Join("; ", problems));
        Require(File.GetLastWriteTimeUtc(Path.Combine(destination, @"nested\data.txt")) == written, "The regular file timestamp was lost.");
        return "Regular and directory streams, empty directories, file timestamp, owner, group and DACL matched the selected source objects. SACL and EFS remain separate acceptance cases.";
    }

    private static async Task<string> AclAsync(string root)
    {
        Guid protector = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var options = new BitLockerOptions("Z:", BitLockerOperation.BackupRecovery,
            Backup: RecoveryDestination.File, BackupFile: Path.Combine(root, "synthetic-backup.txt"));
        string prefix = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "function Get-BitLockerVolume { param([string]$MountPoint) [pscustomobject]@{MountPoint='Z:';KeyProtector=@([pscustomobject]@{KeyProtectorId='{11111111-1111-1111-1111-111111111111}';KeyProtectorType='RecoveryPassword';RecoveryPassword=('111111-'*7)+'111111'})} };";
        var creation = await ProductionPowerShellAsync(BitLockerFileBackup.Step(options, protector), prefix);
        Require(BitLockerFileBackup.IsVerified(creation, protector), "Production backup creation did not verify with synthetic volume-provider material.");
        var inspection = await ProductionPowerShellAsync(BitLockerFileBackup.InspectionStep(options, protector), prefix);
        Require(inspection.ExitCode == 0 && inspection.Error.Length == 0 && inspection.Output.Trim() == "BACKUP_VERIFIED", "Production inspection rejected the on-disk backup.");
        string independent = "$acl=[Security.AccessControl.FileSecurity]::new($path,[Security.AccessControl.AccessControlSections]::Access);" +
            "$rules=@($acl.GetAccessRules($true,$true,[Security.Principal.SecurityIdentifier]));" +
            "$expected=@(@([Security.Principal.WindowsIdentity]::GetCurrent().User.Value,'S-1-5-18','S-1-5-32-544')|Sort-Object -Unique);" +
            "$seen=@($rules|ForEach-Object{$_.IdentityReference.Value}|Sort-Object -Unique);" +
            "if(!$acl.AreAccessRulesProtected -or $seen.Count -ne $expected.Count -or @($expected|Where-Object{$_ -notin $seen}).Count -ne 0 -or @($rules|Where-Object{$_.IsInherited -or $_.AccessControlType -ne 'Allow' -or ($_.FileSystemRights -band 2032127) -ne 2032127}).Count -ne 0){throw 'Independent ACL mismatch'};'ACL_VERIFIED'";
        string encodedPath = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(options.BackupFile!));
        var independentStep = new RepairStep(inspectionTool(), ["-NoProfile", "-NonInteractive", "-Command",
            prefix + "$path=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + encodedPath + "'));" + independent], true);
        var readback = await RunToolAsync(independentStep, TimeSpan.FromSeconds(30));
        Require(readback.ExitCode == 0 && readback.Error.Length == 0 && readback.Output.Trim() == "ACL_VERIFIED", "Independent on-disk ACL read-back did not verify.");
        return "Production file creation and inspection plus independent on-disk ACL read-back verified using synthetic material and a substituted Get-BitLockerVolume only. Current token SYSTEM=" + (WindowsIdentity.GetCurrent().User?.Value == "S-1-5-18") + ". Native hardware and full reconciliation remain separate.";
    }

    private static string inspectionTool() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");

    private static Task<ToolResult> ProductionPowerShellAsync(RepairStep step, string prefix) =>
        RunToolAsync(new(inspectionTool(), [.. step.Arguments[..^1], prefix + step.Arguments[^1]], step.Verification), TimeSpan.FromSeconds(45));

    private static async Task<ToolResult> RunToolAsync(RepairStep step, TimeSpan timeout)
    {
        var start = new System.Diagnostics.ProcessStartInfo(step.Tool)
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in step.Arguments) start.ArgumentList.Add(argument);
        start.Environment["PSModulePath"] = Environment.GetEnvironmentVariable("PSModulePath", EnvironmentVariableTarget.Machine);
        using var process = System.Diagnostics.Process.Start(start) ?? throw new Exception("Native acceptance child did not start.");
        Task<string> output = process.StandardOutput.ReadToEndAsync(), error = process.StandardError.ReadToEndAsync();
        using var stop = new CancellationTokenSource(timeout);
        try { await process.WaitForExitAsync(stop.Token); }
        catch
        {
            if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); }
            throw;
        }
        return new(process.ExitCode, await output, await error, false);
    }

    private static async Task JunctionAsync(string path, string target)
    {
        string command = "New-Item -ItemType Junction -Path '" + path.Replace("'", "''") + "' -Target '" + target.Replace("'", "''") + "' -ErrorAction Stop | Out-Null";
        var result = await RunToolAsync(new(inspectionTool(), ["-NoProfile", "-NonInteractive", "-Command", "$ProgressPreference='SilentlyContinue';" + command], false), TimeSpan.FromSeconds(20));
        Require(result.ExitCode == 0 && !result.TimedOut && !result.Truncated && result.Error.Length == 0 && ReadTag(path) == 0xA0000003, "The owned junction fixture was not created and verified.");
    }

    private sealed class InlineProgress(Action<string> action) : IProgress<string>
    { public void Report(string value) => action(value); }

    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool SetKernelObjectSecurity(SafeFileHandle handle, uint information, byte[] descriptor);

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static uint ReadTag(string path) => BitConverter.ToUInt32(ReadReparse(path), 0);

    private static byte[] ReadReparse(string path)
    {
        using var handle = CreateFile(path, 0, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
        byte[] buffer = new byte[16_384];
        if (!DeviceIoControl(handle, 0x900A8, null, 0, buffer, buffer.Length, out int bytes, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return buffer[..bytes];
    }

    private static void CloneAlias(string existing, string owned)
    {
        byte[] data = ReadReparse(existing);
        Require(BitConverter.ToUInt32(data, 0) == 0x8000001B, "The source fixture is not a genuine app execution alias.");
        using (File.Create(owned)) { }
        using var handle = CreateFile(owned, 0x40000000, 0, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (handle.IsInvalid || !DeviceIoControl(handle, 0x900A4, data, data.Length, null, 0, out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        Require(ReadTag(owned) == 0x8000001B, "The owned alias fixture did not retain its actual reparse tag.");
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, byte[]? input, int inputBytes,
        byte[]? output, int outputBytes, out int returned, IntPtr overlapped);
}
