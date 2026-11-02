using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Engine.Profiles;

/// <summary>Reserves the absent destination and finishes all data writes before restoring security.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class ProfileBackupTransaction
{
    private sealed record Entry(string Relative, bool Directory, SafeFileHandle Handle, byte[] Security);

    internal static async Task<ProfileBackupVerification> CopyAsync(ProfileSnapshot target, string destination,
        IProgress<string>? progress, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromHours(2));
        token = deadline.Token;
        using var identity = WindowsIdentity.GetCurrent();
        if (!DuplicateTokenEx(identity.AccessToken, 0x2E, IntPtr.Zero, 2, 2, out var privileged))
            throw Failure("The backup token could not be created.");
        using (privileged)
        {
            foreach (string name in new[] { "SeBackupPrivilege", "SeRestorePrivilege", "SeSecurityPrivilege" })
            {
                if (!LookupPrivilegeValue(null, name, out var luid)) throw Failure("A backup privilege is unavailable.");
                var privileges = new TokenPrivileges { Count = 1, Luid = luid, Attributes = 2 };
                if (!AdjustTokenPrivileges(privileged, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero) ||
                    Marshal.GetLastWin32Error() != 0) throw Failure("The account lacks a required backup privilege.");
            }
            return await WindowsIdentity.RunImpersonatedAsync(privileged,
                () => CopyProtectedAsync(target, destination, progress, token)).ConfigureAwait(false);
        }
    }

    private static async Task<ProfileBackupVerification> CopyProtectedAsync(ProfileSnapshot target, string destination,
        IProgress<string>? progress, CancellationToken token)
    {
        string source = Path.GetFullPath(target.Path).TrimEnd('\\');
        using var ancestors = ProfileBackupPathGuard.Open(source, destination);
        var entries = new List<Entry>();
        var destinationHandles = new List<(SafeFileHandle Handle, byte[] Security)>();
        try
        {
            void Capture(string path, bool directory)
            {
                token.ThrowIfCancellationRequested();
                if (entries.Count >= 100_000) throw new IOException("The profile exceeds the protected backup entry limit.");
                var handle = Open(path, 0x81020000, 1, directory, rejectEncrypted: true);
                try { entries.Add(new(Path.GetRelativePath(source, path), directory, handle, ReadSecurity(handle))); }
                catch { handle.Dispose(); throw; }
            }
            Capture(source, true);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].Directory) continue;
                string folder = Path.Combine(source, entries[i].Relative);
                foreach (string path in System.IO.Directory.EnumerateFileSystemEntries(folder))
                {
                    token.ThrowIfCancellationRequested();
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        if (!ProfileBackupPathGuard.IsOmittedLink(path))
                            throw new IOException("The profile contains an unsupported link or cloud placeholder.");
                        continue;
                    }
                    Capture(path, (attributes & FileAttributes.Directory) != 0);
                }
            }

            var filesByDirectory = entries.Where(entry => !entry.Directory)
                .GroupBy(entry => Path.GetDirectoryName(entry.Relative) ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Select(entry => Path.GetFileName(entry.Relative)).ToArray(), StringComparer.OrdinalIgnoreCase);
            progress?.Report("Copying the selected unloaded profile into the new local backup folder...");
            token.ThrowIfCancellationRequested();
            // FILE_CREATE returns the new root and its hold atomically. An existing name always fails.
            using var root = Reserve(destination);
            foreach (var directory in entries.Where(entry => entry.Directory && entry.Relative != "."))
                System.IO.Directory.CreateDirectory(Path.Combine(destination, directory.Relative));

            foreach (var directory in entries.Where(entry => entry.Directory))
            {
                token.ThrowIfCancellationRequested();
                string from = Path.Combine(source, directory.Relative), to = Path.Combine(destination, directory.Relative);
                string[] files = filesByDirectory.GetValueOrDefault(directory.Relative == "." ? "" : directory.Relative) ?? [];
                // Each call is nonrecursive and names only files whose source objects are held.
                // Empty directories must not fall back to Robocopy's default *.* selection.
                if (files.Length == 0) await CopyBatchAsync(from, to, [], token).ConfigureAwait(false);
                for (int offset = 0; offset < files.Length;)
                {
                    var batch = new List<string>();
                    int characters = from.Length + to.Length + 1024;
                    while (offset < files.Length && characters + files[offset].Length + 3 <= 28_000)
                    { characters += files[offset].Length + 3; batch.Add(files[offset++]); }
                    if (batch.Count == 0) throw new IOException("A profile copy command exceeds its safe path bound.");
                    await CopyBatchAsync(from, to, batch, token).ConfigureAwait(false);
                }
            }
            // Robocopy copies child directory streams but omits streams on its source root.
            CopyRootStreams(entries[0].Handle, root, token);
            token.ThrowIfCancellationRequested();
            progress?.Report("The copy provider completed. Verifying every regular source and destination file...");
            var verification = await ProfileBackupArtifactWriter.CreateForProtectedCopyAsync(target, destination, token).ConfigureAwait(false);
            if (!verification.Verified) throw new IOException("The protected backup could not be verified: " + verification.Detail);

            // Reserve all restoration access before releasing any object's restrictive DACL.
            foreach (var entry in entries.Where(entry => entry.Relative != ".").Reverse())
            {
                string path = Path.Combine(destination, entry.Relative);
                var attributes = File.GetAttributes(path);
                bool readOnly = !entry.Directory && (attributes & FileAttributes.ReadOnly) != 0;
                if (readOnly) File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                try { destinationHandles.Add((Open(path, 0x410E0080, 3, entry.Directory), entry.Security)); }
                finally { if (readOnly) File.SetAttributes(path, attributes); }
            }
            token.ThrowIfCancellationRequested();
            // A backup security stream restores the held object without walking descendants.
            // No content writes, pathname cleanup or recursive security operations follow this point.
            foreach (var item in destinationHandles)
            {
                RestoreSecurity(item.Handle, item.Security);
                item.Handle.Dispose();
            }
            RestoreSecurity(root, entries[0].Security);
            return verification;
        }
        finally
        {
            foreach (var item in destinationHandles) item.Handle.Dispose();
            foreach (var entry in entries) entry.Handle.Dispose();
            // Partial owned backups remain protected and available for administrator review.
        }
    }

    private static async Task CopyBatchAsync(string source, string destination, IReadOnlyList<string> files, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "robocopy.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(source); start.ArgumentList.Add(destination);
        foreach (string file in files) start.ArgumentList.Add(file);
        foreach (string option in new[] { "/COPY:DAT", "/DCOPY:DAT", "/B", "/EFSRAW", "/XJ", "/SL", "/R:1", "/W:1", "/NFL", "/NDL", "/NJH", "/NJS", "/NP" })
            start.ArgumentList.Add(option);
        if (files.Count == 0) { start.ArgumentList.Add("/XF"); start.ArgumentList.Add("*"); }
        using var process = Process.Start(start) ?? throw new IOException("The profile copy process did not start.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromHours(2));
        var output = DrainAsync(process.StandardOutput);
        var error = DrainAsync(process.StandardError);
        try { await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false); }
        catch
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            finally
            {
                // Do not release source identities or destination protection before termination.
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                await Task.WhenAll(output, error).ConfigureAwait(false);
            }
            throw;
        }
        await Task.WhenAll(output, error).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (process.ExitCode is < 0 or > 7 || output.Result || error.Result)
            throw new IOException("The profile copy did not return complete bounded Robocopy success. The protected backup was retained.");
        static async Task<bool> DrainAsync(StreamReader reader)
        {
            char[] buffer = new char[4096];
            long count = 0;
            int read;
            while ((read = await reader.ReadAsync(buffer).ConfigureAwait(false)) != 0) count += read;
            return count > 250_000;
        }
    }

    private static void CopyRootStreams(SafeFileHandle source, SafeFileHandle destination, CancellationToken token)
    {
        IntPtr readContext = IntPtr.Zero, writeContext = IntPtr.Zero;
        byte[] buffer = new byte[64 * 1024];
        long total = 0;
        int streams = 0;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                byte[] header = new byte[20];
                if (!Read(header, header.Length, true)) break;
                uint nameLength = BitConverter.ToUInt32(header, 16);
                long size = BitConverter.ToInt64(header, 8);
                if (++streams > 4096 || nameLength > 65_536 || (nameLength & 1) != 0 || size < 0)
                    throw new IOException("The profile root backup streams exceed their safe bounds.");
                byte[] name = new byte[nameLength];
                Read(name, name.Length, false);
                if (BitConverter.ToUInt32(header, 0) != 4)
                {
                    if (!BackupSeek(source, (uint)size, (uint)((ulong)size >> 32), out uint low, out uint high, ref readContext) ||
                        (((ulong)high << 32) | low) != (ulong)size)
                        throw Failure("A profile root backup stream could not be skipped.");
                    continue;
                }
                if (size > 2_147_483_648L - total)
                    throw new IOException("The profile root alternate streams exceed the backup byte limit.");
                total += size;
                Write(header, header.Length);
                Write(name, name.Length);
                while (size != 0)
                {
                    token.ThrowIfCancellationRequested();
                    int count = (int)Math.Min(size, buffer.Length);
                    Read(buffer, count, false);
                    Write(buffer, count);
                    size -= count;
                }
            }
        }
        finally
        {
            if (readContext != IntPtr.Zero) BackupRead(source, [], 0, out _, true, false, ref readContext);
            if (writeContext != IntPtr.Zero) BackupWrite(destination, [], 0, out _, true, false, ref writeContext);
        }

        bool Read(byte[] bytes, int count, bool allowEnd)
        {
            if (count == 0) return true;
            if (!BackupRead(source, bytes, (uint)count, out uint read, false, false, ref readContext))
                throw Failure("A profile root backup stream could not be read.");
            if (allowEnd && read == 0) return false;
            if (read != count) throw new IOException("A profile root backup stream was truncated.");
            return true;
        }
        void Write(byte[] bytes, int count)
        {
            if (count != 0 && (!BackupWrite(destination, bytes, (uint)count, out uint written, false, false, ref writeContext) || written != count))
                throw Failure("A profile root alternate stream could not be preserved.");
        }
    }

    private static SafeFileHandle Open(string path, uint access, uint share, bool directory, bool rejectEncrypted = false)
    {
        var handle = CreateFile(path, access, share, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw Failure("A profile object could not be held."); }
        try
        {
            if (!GetFileInformationByHandle(handle, out var info)) throw Failure("A profile object could not be inspected.");
            if (rejectEncrypted && (info.Attributes & 0x4000) != 0)
                throw new IOException("Protected profile backup does not support EFS-encrypted entries. No files were copied.");
            if ((info.Attributes & 0x400) != 0 || ((info.Attributes & 0x10) != 0) != directory)
                throw new IOException("A profile object was redirected or changed type.");
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }

    private static byte[] ReadSecurity(SafeFileHandle handle)
    {
        GetKernelObjectSecurity(handle, 15, null, 0, out uint needed);
        if (needed is 0 or > 1_048_576) throw Failure("The source security descriptor is unavailable or oversized.");
        byte[] descriptor = new byte[needed];
        if (!GetKernelObjectSecurity(handle, 15, descriptor, needed, out _)) throw Failure("The source security descriptor could not be captured.");
        return descriptor;
    }

    private static void RestoreSecurity(SafeFileHandle handle, byte[] descriptor)
    {
        byte[] stream = new byte[20 + descriptor.Length];
        BitConverter.GetBytes(3).CopyTo(stream, 0); // BACKUP_SECURITY_DATA
        BitConverter.GetBytes(2).CopyTo(stream, 4); // STREAM_CONTAINS_SECURITY
        BitConverter.GetBytes((long)descriptor.Length).CopyTo(stream, 8);
        descriptor.CopyTo(stream, 20);
        IntPtr context = IntPtr.Zero;
        try
        {
            if (!BackupWrite(handle, stream, (uint)stream.Length, out uint written, false, true, ref context) || written != stream.Length)
                throw Failure("The exact backup object's security could not be restored.");
        }
        finally
        {
            if (context != IntPtr.Zero) BackupWrite(handle, [], 0, out _, true, true, ref context);
        }
        if (new RawSecurityDescriptor(descriptor, 0).GetSddlForm(AccessControlSections.All) !=
            new RawSecurityDescriptor(ReadSecurity(handle), 0).GetSddlForm(AccessControlSections.All))
            throw new IOException("The exact backup object's security restoration did not verify.");
    }

    private static SafeFileHandle Reserve(string path)
    {
        if (!ConvertStringSecurityDescriptorToSecurityDescriptor("O:BAG:BAD:P(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)(A;OICI;RC;;;OW)", 1, out var security, out _))
            throw Failure("The protected backup security descriptor is unavailable.");
        IntPtr name = Marshal.StringToHGlobalUni("\\??\\" + Path.GetFullPath(path));
        IntPtr unicodePointer = IntPtr.Zero;
        try
        {
            int length = (4 + Path.GetFullPath(path).Length) * 2;
            var unicode = new UnicodeName { Length = checked((ushort)length), MaximumLength = checked((ushort)(length + 2)), Buffer = name };
            unicodePointer = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeName>());
            Marshal.StructureToPtr(unicode, unicodePointer, false);
            var attributes = new ObjectAttributes { Length = Marshal.SizeOf<ObjectAttributes>(), Name = unicodePointer, Attributes = 0x40, Security = security };
            // Exclude DELETE access so Robocopy can open the root without delete sharing.
            // Our share mask still denies deletion and renaming throughout the transaction.
            int status = NtCreateFile(out var handle, 0x011E0197, ref attributes, out _, IntPtr.Zero, 0x10, 3, 2, 0x00204021, IntPtr.Zero, 0);
            if (status < 0) { handle?.Dispose(); throw new IOException("The absent backup destination could not be reserved. Existing data was preserved.", new Win32Exception((int)RtlNtStatusToDosError(status))); }
            return handle;
        }
        finally
        {
            Marshal.FreeHGlobal(name);
            if (unicodePointer != IntPtr.Zero) Marshal.FreeHGlobal(unicodePointer);
            LocalFree(security);
        }
    }

    private static IOException Failure(string message) => new(message, new Win32Exception(Marshal.GetLastWin32Error()));
    [StructLayout(LayoutKind.Sequential)] private struct UnicodeName { public ushort Length, MaximumLength; public IntPtr Buffer; }
    [StructLayout(LayoutKind.Sequential)] private struct ObjectAttributes { public int Length; public IntPtr Root, Name; public uint Attributes; public IntPtr Security, Quality; }
    [StructLayout(LayoutKind.Sequential)] private struct IoStatus { public IntPtr Status, Information; }
    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct TokenPrivileges { public uint Count; public Luid Luid; public uint Attributes; }
    [StructLayout(LayoutKind.Sequential)] private struct Information
    { public uint Attributes; public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write; public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow; }
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out Information information);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetKernelObjectSecurity(SafeFileHandle handle, uint information, [Out] byte[]? descriptor, uint length, out uint needed);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string text, uint revision, out IntPtr descriptor, out uint size);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool DuplicateTokenEx(SafeAccessTokenHandle source, uint access, IntPtr attributes, int level, int kind, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool LookupPrivilegeValue(string? system, string name, out Luid luid);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool AdjustTokenPrivileges(SafeAccessTokenHandle token, bool disableAll, ref TokenPrivileges privileges, uint length, IntPtr previous, IntPtr returned);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool BackupWrite(SafeFileHandle handle, byte[] bytes, uint count, out uint written, bool abort, bool security, ref IntPtr context);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool BackupRead(SafeFileHandle handle, [Out] byte[] bytes, uint count, out uint read, bool abort, bool security, ref IntPtr context);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool BackupSeek(SafeFileHandle handle, uint low, uint high, out uint soughtLow, out uint soughtHigh, ref IntPtr context);
    [DllImport("ntdll.dll")] private static extern int NtCreateFile(out SafeFileHandle handle, uint access, ref ObjectAttributes attributes, out IoStatus status, IntPtr allocation, uint fileAttributes, uint share, uint disposition, uint options, IntPtr ea, uint eaLength);
    [DllImport("ntdll.dll")] private static extern uint RtlNtStatusToDosError(int status);
}
