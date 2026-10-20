using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using KillerBench.Services;

internal static class InstallationProtectionChecks
{
    internal static void Run(string scratch)
    {
        var admin = new SecurityIdentifier("S-1-5-32-544");
        var users = new SecurityIdentifier("S-1-5-32-545");
        DirectorySecurity Acl(FileSystemRights rights, PropagationFlags propagation = PropagationFlags.None)
        {
            var security = new DirectorySecurity();
            security.SetOwner(admin);
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(admin, FileSystemRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(users, rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, propagation, AccessControlType.Allow));
            return security;
        }
        void Refuses(Action action, string name)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new Exception("Elevated setup accepted " + name + ".");
        }
        foreach (var right in new[] { FileSystemRights.WriteData, FileSystemRights.CreateDirectories, FileSystemRights.DeleteSubdirectoriesAndFiles,
            FileSystemRights.Delete, FileSystemRights.ChangePermissions, FileSystemRights.TakeOwnership, FileSystemRights.WriteAttributes, FileSystemRights.WriteExtendedAttributes })
            Refuses(() => InstallationOwnership.ValidateElevatedAccess(Acl(right), true), "untrusted " + right);
        InstallationOwnership.ValidateElevatedAccess(Acl(FileSystemRights.CreateDirectories), false);
        Refuses(() => InstallationOwnership.ValidateElevatedAccess(Acl(FileSystemRights.WriteAttributes), false), "ancestor reparse mutation");
        Refuses(() => InstallationOwnership.ValidateElevatedAccess(Acl(FileSystemRights.WriteData, PropagationFlags.InheritOnly), true), "inherited payload writes");
        var untrustedOwner = Acl(FileSystemRights.ReadAndExecute);
        untrustedOwner.SetOwner(users);
        Refuses(() => InstallationOwnership.ValidateElevatedAccess(untrustedOwner, true), "an untrusted owner");
        var nullDacl = new DirectorySecurity();
        nullDacl.SetSecurityDescriptorSddlForm("O:BAG:BAD:NO_ACCESS_CONTROL");
        Refuses(() => InstallationOwnership.ValidateElevatedAccess(nullDacl, true), "a null DACL");
        InstallationOwnership.ValidateElevatedPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KillerBench-acl-probe-" + Guid.NewGuid().ToString("N")), false);
        string custom = Path.Combine(scratch, "writable-custom-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(custom);
        string sentinel = Path.Combine(custom, "keep.txt");
        File.WriteAllText(sentinel, "Preserved ACL fixture");
        Refuses(() => InstallationOwnership.ValidateElevatedPath(custom, true), "a user-owned custom destination");
        if (File.ReadAllText(sentinel) != "Preserved ACL fixture") throw new Exception("ACL refusal changed unrelated data.");
    }
}
