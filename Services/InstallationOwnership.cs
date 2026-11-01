using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;

namespace KillerBench.Services;

/// <summary>Bounds installation replacement to a registered product payload with an authoritative filename inventory.</summary>
public static class InstallationOwnership
{
    /// <summary>Refuses access descriptors that allow an untrusted identity to change elevated payloads or their ancestry.</summary>
    public static void ValidateElevatedAccess(System.Security.AccessControl.FileSystemSecurity security, bool protectChildren)
    {
        if (security == null) throw new ArgumentNullException(nameof(security));
        var descriptor = new System.Security.AccessControl.RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0);
        if (descriptor.DiscretionaryAcl == null || descriptor.Owner == null || !TrustedIdentity(descriptor.Owner.Value))
            throw new InvalidOperationException("Elevated setup requires a protected directory owned by SYSTEM, Administrators, or TrustedInstaller.");
        foreach (System.Security.AccessControl.GenericAce ace in descriptor.DiscretionaryAcl)
            if (!(ace is System.Security.AccessControl.CommonAce common) || common.IsCallback)
                throw new InvalidOperationException("Elevated setup cannot establish protection for this access descriptor.");
        // Ancestors must prevent replacement and reparse/control changes. The payload and
        // its creation parent must additionally prevent new files and inherited writes.
        uint mutation = 0x10000000u | 0x40000000u | 0x00010000u | 0x00040000u | 0x00080000u | 0x40u | 0x100u | 0x10u;
        if (protectChildren) mutation |= 0x2u | 0x4u;
        foreach (System.Security.AccessControl.FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)))
        {
            if (rule.AccessControlType != System.Security.AccessControl.AccessControlType.Allow) continue;
            bool inheritOnly = (rule.PropagationFlags & System.Security.AccessControl.PropagationFlags.InheritOnly) != 0;
            if (inheritOnly && !protectChildren) continue;
            string sid = rule.IdentityReference.Value;
            if (TrustedIdentity(sid) || inheritOnly && sid == "S-1-3-0") continue;
            if ((unchecked((uint)rule.FileSystemRights) & mutation) != 0)
                throw new InvalidOperationException("Elevated setup refused a location with write or replacement access for an untrusted identity.");
        }
    }

    public static void ValidateElevatedPath(string directory, bool inspectPayload)
    {
        string root = Path.GetFullPath(directory);
        RejectRedirection(root);
        var creationParent = new DirectoryInfo(root).Parent;
        while (creationParent != null && !creationParent.Exists) creationParent = creationParent.Parent;
        if (creationParent == null) throw new InvalidOperationException("The installation parent is unavailable.");
        for (var current = new DirectoryInfo(root); current != null; current = current.Parent)
            if (current.Exists)
                ValidateElevatedAccess(current.GetAccessControl(), string.Equals(current.FullName, root, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(current.FullName, creationParent.FullName, StringComparison.OrdinalIgnoreCase));
        if (!inspectPayload || !Directory.Exists(root)) return;
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
            foreach (string path in Directory.GetFileSystemEntries(pending.Pop()))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Elevated setup refused a redirected payload entry.");
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    ValidateElevatedAccess(new DirectoryInfo(path).GetAccessControl(), true);
                    pending.Push(path);
                }
                else ValidateElevatedAccess(new FileInfo(path).GetAccessControl(), true);
            }
    }

    private static bool TrustedIdentity(string sid) => sid == "S-1-5-18" || sid == "S-1-5-32-544" ||
        sid == "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464";
    /// <summary>Publishes a prepared payload while retaining every previous or interrupted directory for recovery.</summary>
    public static void PublishPreparedDirectory(string destination, string staging, Action<string> validateDestination,
        Action register, Action<string> retained, Action<string, string>? moveDirectory = null)
    {
        if (validateDestination == null) throw new ArgumentNullException(nameof(validateDestination));
        if (register == null) throw new ArgumentNullException(nameof(register));
        if (retained == null) throw new ArgumentNullException(nameof(retained));
        var move = moveDirectory ?? Directory.Move;
        string backup = destination + ".previous-" + Guid.NewGuid().ToString("N");
        bool movedExisting = false;
        try
        {
            validateDestination(destination);
            if (Directory.Exists(destination))
            {
                move(destination, backup);
                movedExisting = true;
            }
            move(staging, destination);
            register();
            if (movedExisting) retained(backup);
        }
        catch
        {
            if (Directory.Exists(staging)) retained(staging);
            if (Directory.Exists(destination)) retained(destination);
            if (movedExisting && Directory.Exists(backup))
            {
                if (!Directory.Exists(destination))
                {
                    // A restored backup is the live install again, not recovery data, so it is not reported.
                    try { move(backup, destination); }
                    catch { retained(backup); throw; }
                }
                else retained(backup);
            }
            throw;
        }
    }

    /// <summary>Carries retained recovery folders in a failure message. Setup has no console and its elevated child
    /// returns only an exit code, so the message is where the wizard and a /silent caller can see them. The original
    /// failure stays as the inner exception, so its exit code mapping is unchanged.</summary>
    public static Exception WithRetainedDirectories(Exception error, IReadOnlyCollection<string> retained, Func<string, string> describe)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));
        if (retained == null) throw new ArgumentNullException(nameof(retained));
        if (describe == null) throw new ArgumentNullException(nameof(describe));
        if (retained.Count == 0) return error;
        return new InvalidOperationException(error.Message + Environment.NewLine + Environment.NewLine +
            describe(string.Join(Environment.NewLine, retained.Distinct(StringComparer.OrdinalIgnoreCase))), error);
    }

    /// <summary>Staging and previous folders that setup kept beside <paramref name="destination"/> for recovery.</summary>
    public static IReadOnlyList<string> FindRetainedDirectories(string destination)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        string full = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? parent = Path.GetDirectoryName(full);
        if (parent == null || !Directory.Exists(parent)) return Array.Empty<string>();
        string name = Path.GetFileName(full);
        try
        {
            return Directory.GetDirectories(parent, name + ".staging-*")
                .Concat(Directory.GetDirectories(parent, name + ".previous-*"))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    public static void ValidateDestination(string directory, IReadOnlyCollection<string> trustedPayloadFiles,
        string? registeredExecutable, Func<string, bool> isTrustedProduct, IReadOnlyCollection<string>? trustedPriorManifestHashes = null)
    {
        if (directory == null) throw new ArgumentNullException(nameof(directory));
        if (trustedPayloadFiles == null) throw new ArgumentNullException(nameof(trustedPayloadFiles));
        if (isTrustedProduct == null) throw new ArgumentNullException(nameof(isTrustedProduct));
        string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(root, Path.GetPathRoot(Path.GetFullPath(directory))?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            Refuse("A drive root cannot be an installation destination.");
        RejectRedirection(root);
        if (File.Exists(root)) Refuse("The selected destination is a file.");
        if (!Directory.Exists(root) || Directory.GetFileSystemEntries(root).Length == 0) return;
        string executable = Path.Combine(root, "KillerBench.exe");
        // Uninstall keeps the application files, so an unregistered copy is judged by its signature and inventory.
        bool registeredElsewhere = !string.IsNullOrWhiteSpace(registeredExecutable) && !RegistrationMatches(root, registeredExecutable);
        if (registeredElsewhere || !isTrustedProduct(executable))
            Refuse("The nonempty destination is not a registered, trusted KillerBench installation.");

        var allowed = new HashSet<string>(trustedPayloadFiles.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        string manifest = Path.Combine(root, "payload.manifest");
        if (!File.Exists(manifest) || (File.GetAttributes(manifest) & FileAttributes.ReparsePoint) != 0 || new FileInfo(manifest).Length > 4 * 1024 * 1024)
            Refuse("The existing payload inventory is unavailable.");
        // Read one held image. The authenticated digest and parsed entries must describe the same bytes.
        byte[] manifestBytes;
        using (var input = new FileStream(manifest, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (input.Length > 4 * 1024 * 1024) Refuse("The existing payload inventory exceeds its size limit.");
            manifestBytes = new byte[(int)input.Length];
            int offset = 0;
            while (offset < manifestBytes.Length)
            {
                int read = input.Read(manifestBytes, offset, manifestBytes.Length - offset);
                if (read == 0) Refuse("The existing payload inventory is incomplete.");
                offset += read;
            }
        }
        string inventoryDigest;
        using (var hash = SHA256.Create()) inventoryDigest = BitConverter.ToString(hash.ComputeHash(manifestBytes)).Replace("-", "");
        bool authenticatedPrior = trustedPriorManifestHashes != null && trustedPriorManifestHashes.Any(digest =>
            string.Equals(digest, inventoryDigest, StringComparison.OrdinalIgnoreCase));
        var owned = new Dictionary<string, Tuple<string, long>>(StringComparer.OrdinalIgnoreCase);
        using var manifestReader = new StreamReader(new MemoryStream(manifestBytes), System.Text.Encoding.UTF8, true);
        string? line;
        while ((line = manifestReader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
            string[] parts = line.Split(new[] { '\t' }, 3);
            long size;
            if (parts.Length != 3 || parts[0].Length != 64 || !parts[0].All(Uri.IsHexDigit) ||
                !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out size) || size < 0)
                throw new InvalidOperationException("The existing payload inventory is invalid. Its files were preserved.");
            string name = Normalize(parts[2]);
            if ((!allowed.Contains(name) && !authenticatedPrior) || owned.ContainsKey(name)) Refuse("The existing inventory claims an unknown or duplicate payload path.");
            owned.Add(name, Tuple.Create(parts[0], size));
        }
        if (!owned.ContainsKey("KillerBench.exe")) Refuse("The existing inventory does not own the product executable.");
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in owned.Keys)
        {
            int slash = name.LastIndexOf('/');
            while (slash >= 0) { directories.Add(name.Substring(0, slash)); slash = name.LastIndexOf('/', slash - 1); }
            string path = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
            RejectRedirection(path);
            if (!File.Exists(path) || new FileInfo(path).Length != owned[name].Item2 ||
                !string.Equals(Hash(path), owned[name].Item1, StringComparison.OrdinalIgnoreCase))
                Refuse("An existing payload file is missing or differs from its recorded inventory.");
        }
        var pending = new Stack<string>();
        pending.Push(root);
        string prefix = root + Path.DirectorySeparatorChar;
        while (pending.Count > 0)
        {
            foreach (string path in Directory.GetFileSystemEntries(pending.Pop()))
            {
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) Refuse("The destination contains a redirected entry.");
                string name = path.Substring(prefix.Length).Replace(Path.DirectorySeparatorChar, '/');
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (!directories.Contains(name)) Refuse("The destination contains an unowned folder.");
                    pending.Push(path);
                }
                else if (!string.Equals(name, "payload.manifest", StringComparison.OrdinalIgnoreCase) && !owned.ContainsKey(name))
                    Refuse("The destination contains an unowned file.");
            }
        }
    }

    public static bool RegistrationMatches(string directory, string? registeredExecutable)
        => !string.IsNullOrWhiteSpace(registeredExecutable) && string.Equals(
            Path.GetFullPath(Path.Combine(directory, "KillerBench.exe")), Path.GetFullPath(registeredExecutable), StringComparison.OrdinalIgnoreCase);

    /// <summary>True when no per-user copy is registered, or the registered one was removed.</summary>
    public static bool UserCopyCleared(string? registeredExecutable, Func<bool> remove)
        => string.IsNullOrWhiteSpace(registeredExecutable) || remove();

    /// <summary>Removes matching installation metadata only. The installation directory is retained.</summary>
    public static bool UnregisterPreservingFiles(string directory, string? registeredExecutable, Action removeMetadata)
    {
        if (removeMetadata == null) throw new ArgumentNullException(nameof(removeMetadata));
        if (!RegistrationMatches(directory, registeredExecutable)) return false;
        removeMetadata();
        return true;
    }

    private static string Normalize(string path)
    {
        string name = path.Replace('\\', '/');
        if (Path.IsPathRooted(path) || name.Contains(":") || name.Split('/').Any(part => string.IsNullOrWhiteSpace(part) || part == "." || part == ".."))
            throw new InvalidOperationException("The payload inventory contains an unsafe path. Existing files were preserved.");
        return name;
    }

    private static void RejectRedirection(string path)
    {
        for (var current = new DirectoryInfo(path); current != null; current = current.Parent)
            if ((Directory.Exists(current.FullName) || File.Exists(current.FullName)) && (File.GetAttributes(current.FullName) & FileAttributes.ReparsePoint) != 0)
                Refuse("The destination or an ancestor is redirected.");
    }

    private static string Hash(string path)
    {
        using (var input = File.OpenRead(path))
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(input)).Replace("-", "");
    }

    private static void Refuse(string reason) => throw new InvalidOperationException(reason + " Choose an empty folder. Existing files were preserved.");
}
