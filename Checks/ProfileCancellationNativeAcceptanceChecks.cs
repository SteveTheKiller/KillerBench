using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KillerBench.Engine.Profiles;
using Microsoft.Win32;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class ProfileCancellationNativeAcceptanceChecks
{
    private const string GuestParent = @"C:\KillerLab\bench-resume-20261101";
    private const string FileName = "synthetic-512MiB.bin";
    private sealed class BlockedException(string message) : Exception(message);
    private sealed record NativeProcess(int Pid, int Parent, string Executable, string CommandLine, string Created);

    // The caller must use NativeAcceptancePackage.RunAsync to pin the package and runtime first.
    internal static async Task RunAsync()
    {
        string directory = RequireGuest();
        NativeProcess[] baseline = await Task.Run(QueryRobocopy).WaitAsync(TimeSpan.FromSeconds(10));
        if (baseline.Length != 0)
            throw new BlockedException("Baseline contains Robocopy. No fixture writes or cancellation were performed.");
        string root = Path.Combine(directory, "profile-cancellation-" + Guid.NewGuid().ToString("N"));
        CreateProtectedDirectory(root);
        string outcome = "Failed", detail = "The cancellation case did not finish.";
        object? evidence = null;
        try
        {
            evidence = await ObserveCancellationAsync(root).ConfigureAwait(false);
            outcome = "Passed";
            detail = "Cancellation was requested while the exact owned Robocopy was live; it had exited when CopyAsync returned.";
        }
        catch (BlockedException error) { outcome = "Blocked"; detail = error.Message; }
        catch (Exception error) { detail = error.GetType().Name + ": " + error.Message; }
        string report = Path.Combine(root, "cancellation-result.json");
        await using (var stream = new FileStream(report, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await JsonSerializer.SerializeAsync(stream, new
            {
                schemaVersion = 1, selected = 1, outcome, detail, evidence,
                baselineRobocopyCount = baseline.Length, syntheticDataOnly = true,
                cleanup = "Preserve all fixtures and evidence for coordinator snapshot restoration. No recursive cleanup or ACL relaxation."
            }, new JsonSerializerOptions { WriteIndented = true }).ConfigureAwait(false);
        Console.WriteLine(outcome.ToUpperInvariant() + ": profile cancellation: " + detail);
        Console.WriteLine("PROFILE_CANCELLATION_RESULT: " + report);
        if (outcome != "Passed") throw new InvalidOperationException("Native cancellation acceptance did not pass: " + outcome + ": " + detail);
    }

    private static async Task<object> ObserveCancellationAsync(string root)
    {
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup");
        CreateProtectedDirectory(source);
        string file = Path.Combine(source, FileName);
        byte[] block = new byte[1024 * 1024];
        for (int i = 0; i < block.Length; i++) block[i] = (byte)((i * 31 + (i >> 8) + 17) & 255);
        string writtenHash;
        using (var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            await using var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                block.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
            for (int i = 0; i < 512; i++)
            {
                await stream.WriteAsync(block).ConfigureAwait(false);
                digest.AppendData(block);
            }
            stream.Flush(flushToDisk: true);
            writtenHash = Convert.ToHexString(digest.GetHashAndReset());
        }
        string before = await HashAsync(file).ConfigureAwait(false);
        Require(before == writtenHash && new FileInfo(file).Length == 512L * 1024 * 1024,
            "The independently read source differs from the 512 MiB of newly written deterministic bytes.");
        Require(!Directory.Exists(destination) && !File.Exists(destination), "The fresh backup destination exists.");
        var target = new ProfileSnapshot("S-1-5-21-1-2-3-1001", source, false, false, DateTimeOffset.UtcNow.AddDays(-120), false);
        var host = new WindowsProfileBackupHost();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var watcherStop = new CancellationTokenSource();
        var watcherReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NativeProcess? observed = null;
        Process? heldProcess = null;
        bool liveAtCancellation = false, canceled = false, exitedAtReturn = false;
        Task watcher = Task.Run(async () =>
        {
            watcherReady.TrySetResult();
            var watch = Stopwatch.StartNew();
            while (!watcherStop.IsCancellationRequested && watch.Elapsed < TimeSpan.FromSeconds(45))
            {
                NativeProcess[] rows = QueryRobocopy();
                if (watcherStop.IsCancellationRequested) return;
                foreach (var row in rows.Where(row => IsOwned(row, source, destination)))
                {
                    var process = Process.GetProcessById(row.Pid);
                    try
                    {
                        _ = process.Handle;
                        // Bind the held PID to the same live management identity before canceling.
                        if (process.HasExited || !QueryRobocopy().Any(current => current == row)) continue;
                        if (watcherStop.IsCancellationRequested) return;
                        heldProcess = process; process = null!;
                        observed = row;
                        liveAtCancellation = !heldProcess.HasExited;
                        if (!liveAtCancellation) return;
                        cancellation.Cancel();
                        return;
                    }
                    finally { process?.Dispose(); }
                }
                await Task.Delay(5, watcherStop.Token).ConfigureAwait(false);
            }
            if (!watcherStop.IsCancellationRequested) cancellation.Cancel();
        });
        await watcherReady.Task.ConfigureAwait(false);
        Exception? copyError = null;
        bool copyReturned = false;
        try
        {
            Task copy = host.CopyAsync(target, destination, null, cancellation.Token);
            try { await AwaitProtectedCopyAsync(copy, cancellation.Cancel, TimeSpan.FromSeconds(90)).ConfigureAwait(false); }
            catch (OperationCanceledException) { canceled = true; }
            catch (Exception error) { copyError = error; }
            copyReturned = copyError is not TimeoutException;
            // Observe termination before any subsequent hashing, artifact reading or delay.
            exitedAtReturn = heldProcess is not null && heldProcess.HasExited;
        }
        finally
        {
            watcherStop.Cancel();
            cancellation.Cancel();
            try { await watcher.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { throw new BlockedException("The read-only management watcher did not finish within its bound. Containment is unverified; restore the snapshot after coordinator reconciliation."); }
        }
        using var observedLease = heldProcess;
        if (copyError is not null) throw new InvalidOperationException("CopyAsync failed before cancellation acceptance could be established. Containment must be reviewed.", copyError);
        if (observed is null || !liveAtCancellation)
            throw new BlockedException("No exact owned live Robocopy was observed before copying completed or the deadline. This timing-sensitive observation is not a pass.");
        Require(copyReturned && canceled && exitedAtReturn, "Cancellation did not throw OperationCanceledException after the observed process exited.");
        var remaining = await Task.Run(QueryRobocopy).WaitAsync(TimeSpan.FromSeconds(10));
        Require(!remaining.Any(row => IsOwned(row, source, destination)), "An exact owned Robocopy remains after CopyAsync returned.");
        string after = await HashAsync(file).ConfigureAwait(false);
        Require(before == after, "Cancellation changed the synthetic source bytes.");
        var proof = await host.VerifyAsync(target, destination, CancellationToken.None).ConfigureAwait(false);
        Require(!proof.Verified && proof.Receipt is null, "Canceled copying exposed completed proof.");
        string receipt = Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName);
        Require(!File.Exists(receipt), "A cancellation during native copying unexpectedly published a completed receipt.");
        Require(Directory.Exists(destination), "The observed native copy did not retain its reserved destination for containment review.");
        string dacl = ReadContainedDacl(destination);
        return new
        {
            bytes = 512L * 1024 * 1024, sourceSha256Before = before, sourceSha256After = after,
            observed, liveAtCancellation, exitedAtCopyReturn = exitedAtReturn,
            operationCanceled = canceled, remainingOwnedRobocopyCount = 0,
            completedProof = proof.Verified, receiptAbsent = true, partialRootDacl = dacl,
            observedProcessStartUtc = observedLease!.StartTime.ToUniversalTime()
        };
    }

    internal static async Task AwaitProtectedCopyAsync(Task copy, Action requestCancellation, TimeSpan deadline)
    {
        try { await copy.WaitAsync(deadline).ConfigureAwait(false); }
        catch (TimeoutException timeout)
        {
            // A deadline is a failed observation, never permission to release protection around an active copy.
            try { requestCancellation(); }
            catch { /* Still wait for the owned copy if a cancellation callback fails. */ }
            try { await copy.ConfigureAwait(false); }
            catch { /* Completion errors cannot turn the original deadline failure into success. */ }
            // An outer owned guest process-tree watchdog is the final bound if native termination stalls.
            throw new TimeoutException("The protected copy exceeded its deadline. Cancellation was requested and the underlying copy has now completed.", timeout);
        }
    }

    private static bool IsOwned(NativeProcess row, string source, string destination)
    {
        if (row.Parent != Environment.ProcessId || !row.Executable.Equals(Path.Combine(Environment.SystemDirectory, "robocopy.exe"), StringComparison.OrdinalIgnoreCase)) return false;
        string[] args = Arguments(row.CommandLine);
        return args.Length >= 4 && args[0].Equals(row.Executable, StringComparison.OrdinalIgnoreCase) &&
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[1])).Equals(source, StringComparison.OrdinalIgnoreCase) &&
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[2])).Equals(destination, StringComparison.OrdinalIgnoreCase) &&
            args[3] == FileName;
    }

    private static NativeProcess[] QueryRobocopy()
    {
        object? locator = null, services = null, result = null;
        var rows = new List<NativeProcess>();
        try
        {
            locator = Activator.CreateInstance(Type.GetTypeFromProgID("WbemScripting.SWbemLocator", throwOnError: true)!);
            services = ((dynamic)locator!).ConnectServer(".", @"root\cimv2");
            result = ((dynamic)services).ExecQuery("SELECT ProcessId, ParentProcessId, ExecutablePath, CommandLine, CreationDate FROM Win32_Process WHERE Name='robocopy.exe'", "WQL", 0);
            foreach (object item in (System.Collections.IEnumerable)result)
            {
                try
                {
                    dynamic row = item;
                    if (rows.Count >= 64) throw new BlockedException("Robocopy management inventory exceeds its 64-process bound.");
                    string command = (string?)row.CommandLine ?? "";
                    if (command.Length > 32767) throw new BlockedException("A management command line exceeds its bound.");
                    rows.Add(new(Convert.ToInt32(row.ProcessId), Convert.ToInt32(row.ParentProcessId),
                        (string?)row.ExecutablePath ?? "", command, (string?)row.CreationDate ?? ""));
                }
                finally { Release(item); }
            }
            return rows.ToArray();
        }
        finally { Release(result); Release(services); Release(locator); }
        static void Release(object? value) { if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
    }

    private static string[] Arguments(string command)
    {
        IntPtr list = CommandLineToArgv(command, out int count);
        if (list == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            if (count is < 1 or > 128) throw new BlockedException("The management command argument count exceeds its bound.");
            return Enumerable.Range(0, count).Select(i => Marshal.PtrToStringUni(Marshal.ReadIntPtr(list, i * IntPtr.Size)) ?? "").ToArray();
        }
        finally { LocalFree(list); }
    }

    private static string ReadContainedDacl(string destination)
    {
        var security = new DirectoryInfo(destination).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
        Require(security.AreAccessRulesProtected && rules.Length == 3 &&
            security.GetOwner(typeof(SecurityIdentifier))?.Value == "S-1-5-32-544", "The partial root owner or protected DACL differs.");
        foreach (string sid in new[] { "S-1-5-18", "S-1-5-32-544", "S-1-3-4" })
        {
            var matches = rules.Where(rule => rule.IdentityReference.Value == sid).ToArray();
            FileSystemRights expected = sid == "S-1-3-4" ? FileSystemRights.ReadPermissions : FileSystemRights.FullControl;
            Require(matches.Length == 1 && !matches[0].IsInherited && matches[0].AccessControlType == AccessControlType.Allow &&
                matches[0].FileSystemRights == expected && matches[0].PropagationFlags == PropagationFlags.None &&
                matches[0].InheritanceFlags == (InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit),
                "The partial root DACL did not retain exact BA/SY full control and Owner Rights read control.");
        }
        return security.GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner);
    }

    private static async Task<string> HashAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream).ConfigureAwait(false));
    }

    private static string RequireGuest()
    {
        if (!OperatingSystem.IsWindows() || Environment.MachineName != "KILLERLAB-W11" ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable" ||
            Environment.ProcessPath != @"C:\Program Files\dotnet\dotnet.exe")
            throw new InvalidOperationException("Native cancellation requires the exact admitted guest, runtime and disposable marker before writes.");
        using var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        if (!Equals(bios?.GetValue("SystemManufacturer"), "Microsoft Corporation") || !Equals(bios?.GetValue("SystemProductName"), "Virtual Machine"))
            throw new InvalidOperationException("Native cancellation requires Microsoft Virtual Machine BIOS identity.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) throw new InvalidOperationException("Native cancellation requires an elevated administrator.");
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        string name = Path.GetFileName(full);
        if (!string.Equals(Path.GetDirectoryName(full), GuestParent, StringComparison.OrdinalIgnoreCase) ||
            !name.StartsWith("green", StringComparison.Ordinal) || name.Length <= 5 || !name[5..].All(char.IsAsciiDigit))
            throw new InvalidOperationException("Native cancellation requires the exact admitted green-number package directory.");
        for (string? path = full; path is not null; path = Path.GetDirectoryName(path))
            if (!Directory.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("The admitted package and ancestors must be existing nonredirected directories.");
        if (!new DriveInfo(Path.GetPathRoot(full)!).DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Native cancellation requires NTFS.");
        return full;
    }

    private static void CreateProtectedDirectory(string path)
    {
        if (Directory.Exists(path) || File.Exists(path)) throw new IOException("The unique owned fixture path already exists.");
        if (!ConvertStringSecurityDescriptorToSecurityDescriptor("O:BAG:BAD:P(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)", 1, out var descriptor, out _)) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var attributes = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor };
            if (!CreateDirectory(path, ref attributes)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally { LocalFree(descriptor); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool Inherit; }
    [DllImport("kernel32.dll", EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateDirectory(string path, ref SecurityAttributes attributes);
    [DllImport("advapi32.dll", EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string text, uint revision, out IntPtr descriptor, out uint length);
    [DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CommandLineToArgv(string command, out int count);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
}
