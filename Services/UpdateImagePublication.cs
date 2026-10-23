using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace KillerBench.Services
{
    internal static class UpdateImagePublication
    {
        private static readonly ConcurrentDictionary<string, string> VerifiedHashes =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal static void RememberVerified(string path, string sha256)
        {
            if (sha256.Length != 64 || Array.Exists(sha256.ToCharArray(), c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException("The verified update hash is invalid.");
            VerifiedHashes[Path.GetFullPath(path)] = sha256;
        }

        /// <summary>The SHA-256 the download was verified against, or an exception when it was never verified.</summary>
        internal static string VerifiedHash(string download)
        {
            download = Path.GetFullPath(download);
            return VerifiedHashes.TryGetValue(download, out string? expected)
                ? expected
                : throw new InvalidDataException("This update has no retained verification hash. Download retained at: " + download);
        }

        /// <summary>The portable-copy publication script, run by <see cref="UpdateHelperLaunch"/>. It opens the releases page when publication fails.</summary>
        internal static string Script(string download, string target, bool elevated,
            string releasesUrl, string displayName, string? version = null, string? hive = null,
            string? reopen = null)
        {
            download = Path.GetFullPath(download);
            target = Path.GetFullPath(target);
            string expected = VerifiedHash(download);
            string staged = target + ".new-" + Guid.NewGuid().ToString("N");
            string backup = target + ".old-" + Guid.NewGuid().ToString("N");
            string restore = target + ".restore-" + Guid.NewGuid().ToString("N");
            string report = download + ".recovery.txt";
            File.WriteAllText(report, "Update publication has not been confirmed. If the helper cannot run, retain these files."
                + Environment.NewLine + "Target: " + target + Environment.NewLine + "Download: " + download
                + Environment.NewLine + "Staging path (not yet verified): " + staged
                + Environment.NewLine + "Backup path: " + backup + Environment.NewLine + "Restoration path: " + restore
                + Environment.NewLine + "Expected SHA-256: " + expected);
            var script = new StringBuilder(Function);
            script.AppendLine("$plan = [pscustomobject]@{");
            Value(script, "Download", download); Value(script, "Target", target);
            Value(script, "Staged", staged); Value(script, "Backup", backup);
            Value(script, "Restore", restore);
            Value(script, "Report", report); Value(script, "Hash", expected);
            Value(script, "Releases", releasesUrl); Value(script, "DisplayName", displayName);
            Value(script, "Version", version ?? ""); Value(script, "Hive", hive ?? "");
            Value(script, "Reopen", reopen ?? "");
            Value(script, "Signer", "Stephen Riley");
            Value(script, "Explorer", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"));
            script.AppendLine("Elevated = " + (elevated ? "$true" : "$false")).AppendLine("}");
            script.AppendLine(ExecutionGuard);
            script.AppendLine(Operations);
            script.AppendLine("if (Invoke-UpdateImagePublication $plan $ops) { exit 0 } else { try { Start-Process $plan.Releases } catch { }; exit 1 }");
            return script.ToString();
        }

        private static void Value(StringBuilder script, string name, string value) =>
            script.AppendLine(name + " = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('"
                + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "'))");

        private const string Function = """
$ErrorActionPreference = 'Stop'
function Invoke-UpdateImagePublication {
    param($plan, $ops)
    $stageVerified = $false
    $parents = $null
    $image = $null
    try {
        $bytes = & $ops.Read $plan.Download
        if ((& $ops.Hash $bytes) -ne $plan.Hash) { throw 'The download no longer matches its verified SHA-256.' }
        $parents = [PortablePublicationGuard]::PinParents($plan.Target)
        & $ops.Stage $plan.Staged $bytes
        if ((& $ops.Hash (& $ops.Read $plan.Staged)) -ne $plan.Hash) { throw 'The staged image failed SHA-256 verification.' }
        $stageVerified = $true
        if (& $ops.Exists $plan.Target) {
            & $ops.PrepareTarget $plan.Target
            & $ops.Replace $plan.Staged $plan.Target $plan.Backup
        }
        else { & $ops.Move $plan.Staged $plan.Target }
        $image = New-Object IO.FileStream($plan.Target, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if (([IO.File]::GetAttributes($plan.Target) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'The published image is redirected.' }
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $finalHash = [BitConverter]::ToString($sha.ComputeHash($image)).Replace('-', '') } finally { $sha.Dispose() }
        if ($finalHash -ne $plan.Hash) { throw 'The published image failed SHA-256 verification.' }
        $signature = Get-AuthenticodeSignature -LiteralPath $plan.Target
        if ($signature.Status -ne [Management.Automation.SignatureStatus]::Valid -or $null -eq $signature.SignerCertificate -or
            $signature.SignerCertificate.Subject.IndexOf($plan.Signer, [StringComparison]::OrdinalIgnoreCase) -lt 0) { throw 'The published image is not signed by the publisher.' }
        & $ops.Metadata $plan
        & $ops.Relaunch $plan
    }
    catch {
        $failure = $_.Exception.ToString()
        $restoration = ''
        try {
            if (!(& $ops.Exists $plan.Target) -and (& $ops.Exists $plan.Backup)) {
                & $ops.Restore $plan.Backup $plan.Target $plan.Restore
            }
        }
        catch { $restoration = [Environment]::NewLine + 'Restoration failed: ' + $_.Exception.ToString() }
        $message = 'Update publication failed: ' + $failure + $restoration + [Environment]::NewLine +
            'Target: ' + $plan.Target + [Environment]::NewLine + 'Download: ' + $plan.Download +
            [Environment]::NewLine + 'Staging path (verified: ' + $stageVerified + '): ' + $plan.Staged +
            [Environment]::NewLine + 'Backup: ' + $plan.Backup + [Environment]::NewLine + 'Restoration path: ' + $plan.Restore +
            [Environment]::NewLine + 'Report: ' + $plan.Report
        try { & $ops.Report $plan.Report $message } catch { [Console]::Error.WriteLine($message) }
        return $false
    }
    finally {
        if ($null -ne $image) { $image.Dispose() }
        if ($null -ne $parents) { $parents.Dispose() }
    }
    foreach ($path in @($plan.Download, $plan.Staged, $plan.Backup, $plan.Restore, $plan.Report)) {
        try { & $ops.Delete $path } catch { }
    }
    return $true
}

""";
        private const string Operations = """
$ops = @{
    Read = { param($path) ,([IO.File]::ReadAllBytes($path)) }
    Hash = { param($bytes)
        $hash = [Security.Cryptography.SHA256]::Create()
        try { [BitConverter]::ToString($hash.ComputeHash([byte[]]$bytes)).Replace('-', '') }
        finally { $hash.Dispose() }
    }
    Stage = { param($path, $bytes)
        $stream = New-Object IO.FileStream($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write,
            [IO.FileShare]::None, 65536, [IO.FileOptions]::WriteThrough)
        try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) }
        finally { $stream.Dispose() }
    }
    Exists = { param($path) [IO.File]::Exists($path) }
    PrepareTarget = { param($path)
        $attributes = [IO.File]::GetAttributes($path)
        if (($attributes -band [IO.FileAttributes]::ReadOnly) -ne 0) {
            [IO.File]::SetAttributes($path, ($attributes -band (-bnot [IO.FileAttributes]::ReadOnly)))
        }
    }
    Replace = { param($source, $target, $backup) [IO.File]::Replace($source, $target, $backup) }
    Move = { param($source, $target) [IO.File]::Move($source, $target) }
    Restore = { param($backup, $target, $restore)
        $bytes = & $ops.Read $backup
        $expected = & $ops.Hash $bytes
        & $ops.Stage $restore $bytes
        if ((& $ops.Hash (& $ops.Read $restore)) -ne $expected) { throw 'The restoration image failed SHA-256 verification.' }
        & $ops.Move $restore $target
    }
    Report = { param($path, $message) [IO.File]::WriteAllText($path, $message) }
    Delete = { param($path) [IO.File]::Delete($path) }
    Metadata = { param($plan)
        if ($plan.Version.Length -eq 0 -or $plan.Hive.Length -eq 0) { return }
        if ($plan.Hive -eq 'HKLM') { $root = [Microsoft.Win32.Registry]::LocalMachine }
        elseif ($plan.Hive -eq 'HKCU') { $root = [Microsoft.Win32.Registry]::CurrentUser }
        else { throw 'The update registry scope is invalid.' }
        foreach ($keyPath in @(('Software\Microsoft\Windows\CurrentVersion\Uninstall\' + $plan.DisplayName),
            ('Software\' + $plan.DisplayName))) {
            $key = $root.CreateSubKey($keyPath)
            try {
                if ($keyPath.Contains('Uninstall')) {
                    $key.SetValue('DisplayVersion', $plan.Version)
                    $key.SetValue('Publisher', 'Steve the Killer')
                }
                else { $key.SetValue('Version', $plan.Version) }
            }
            finally { if ($null -ne $key) { $key.Dispose() } }
        }
    }
    Relaunch = { param($plan)
        [PortablePublicationGuard]::Launch($plan.Target, $plan.Reopen, $plan.Elevated)
    }
}
""";

        private const string ExecutionGuard = """
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;
public static class PortablePublicationGuard {
    private sealed class Parents : IDisposable {
        internal readonly List<SafeFileHandle> Handles = new List<SafeFileHandle>();
        public void Dispose() { for (int i = Handles.Count - 1; i >= 0; i--) Handles[i].Dispose(); }
    }
    public static IDisposable PinParents(string target) {
        target = Path.GetFullPath(target);
        if (target.StartsWith(@"\\", StringComparison.Ordinal)) throw new IOException("Portable publication requires a local, unredirected path.");
        var paths = new Stack<string>();
        for (var dir = Directory.GetParent(target); dir != null; dir = dir.Parent) paths.Push(dir.FullName);
        var lease = new Parents();
        try {
            while (paths.Count > 0) {
                string path = paths.Pop();
                var handle = CreateFile(path, 0, 3, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
                if (handle.IsInvalid) { handle.Dispose(); throw new Win32Exception(Marshal.GetLastWin32Error()); }
                lease.Handles.Add(handle);
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("A publication directory is redirected.");
            }
            if (File.Exists(target) && (File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0) throw new IOException("The publication target is redirected.");
            return lease;
        } catch { lease.Dispose(); throw; }
    }
    public static void Launch(string target, string reopen, bool elevated) {
        string arguments = string.IsNullOrEmpty(reopen) ? "" : Quote(reopen);
        if (!elevated) {
            using (var process = Process.Start(new ProcessStartInfo(target, arguments) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(target) }))
                if (process == null) throw new IOException("The updated application did not start.");
            return;
        }
        // Obtain an actual created-process result at the shell user's normal integrity level.
        // An asynchronous Explorer handoff cannot establish when the verified image was mapped.
        uint shellPid;
        IntPtr shell = GetShellWindow();
        if (shell == IntPtr.Zero || GetWindowThreadProcessId(shell, out shellPid) == 0) throw new IOException("The normal desktop shell is unavailable.");
        using (var shellProcess = Process.GetProcessById((int)shellPid))
            if (shellProcess.SessionId != Process.GetCurrentProcess().SessionId) throw new IOException("The shell is in another session.");
        IntPtr processHandle = OpenProcess(0x1000, false, shellPid), token = IntPtr.Zero, environment = IntPtr.Zero;
        if (processHandle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        try {
            if (!OpenProcessToken(processHandle, 0xB, out token)) throw new Win32Exception(Marshal.GetLastWin32Error());
            using (var shellIdentity = new WindowsIdentity(token))
            using (var current = WindowsIdentity.GetCurrent())
                if (shellIdentity.User == null || !shellIdentity.User.Equals(current.User)) throw new IOException("The shell belongs to another account.");
            int elevation, returned;
            if (!GetTokenInformation(token, 20, out elevation, sizeof(int), out returned) || returned != sizeof(int) || elevation != 0)
                throw new IOException("A normal-integrity shell token could not be established.");
            if (!CreateEnvironmentBlock(out environment, token, false)) throw new Win32Exception(Marshal.GetLastWin32Error());
            var startup = new StartupInfo(); startup.Size = Marshal.SizeOf(typeof(StartupInfo)); startup.Desktop = @"winsta0\default";
            ProcessInfo info;
            if (!CreateProcessWithToken(token, 0, target, new StringBuilder(Quote(target) + " " + arguments), 0x400, environment, Path.GetDirectoryName(target), ref startup, out info))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            CloseHandle(info.Thread); CloseHandle(info.Process);
        } finally {
            if (environment != IntPtr.Zero) DestroyEnvironmentBlock(environment);
            if (token != IntPtr.Zero) CloseHandle(token);
            CloseHandle(processHandle);
        }
    }
    private static string Quote(string value) {
        if (value.IndexOf('"') >= 0 || value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0) throw new IOException("The launch path is invalid.");
        int trailingSlashes = 0;
        for (int i = value.Length - 1; i >= 0 && value[i] == '\\'; i--) trailingSlashes++;
        return "\"" + value + new string('\\', trailingSlashes) + "\"";
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct StartupInfo {
        public int Size; public string Reserved; public string Desktop; public string Title;
        public int X, Y, Width, Height, XChars, YChars, Fill, Flags; public short Show, ReservedSize;
        public IntPtr ReservedBytes, Input, Output, Error;
    }
    [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { public IntPtr Process, Thread; public uint ProcessId, ThreadId; }
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetTokenInformation(IntPtr token, int kind, out int value, int length, out int returned);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("userenv.dll", SetLastError = true)] private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inherit);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("userenv.dll")] private static extern bool DestroyEnvironmentBlock(IntPtr environment);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] [DllImport("advapi32.dll", EntryPoint = "CreateProcessWithTokenW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcessWithToken(IntPtr token, uint flags, string application, StringBuilder command, uint creation, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInfo process);
}
'@
""";
    }
}
