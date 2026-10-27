using System;
using System.IO;
using System.Linq;
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
        TestHooksCompiledOut();
        UserCleanupResultChecked();
        RetainedDirectoriesSurfaced(scratch);
    }

    /// <summary>Setup is a windowless WinExe and the elevated /silent child returns only an exit code, so retained
    /// recovery, previous and staging folders must travel in the failure message and be found from the install folder.</summary>
    private static void RetainedDirectoriesSurfaced(string scratch)
    {
        string folder = Path.Combine(scratch, "retained-" + Guid.NewGuid().ToString("N"));
        string destination = Path.Combine(folder, "app"), staging = destination + ".staging-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(destination);
        Directory.CreateDirectory(staging);
        File.WriteAllText(Path.Combine(destination, "KillerBench.exe"), "Old payload");
        var retained = new System.Collections.Generic.List<string>();
        Exception? surfaced = null;
        try
        {
            InstallationOwnership.PublishPreparedDirectory(destination, staging, _ => { }, () => { }, retained.Add,
                (source, target) =>
                {
                    if (source != destination) throw new IOException("Substituted move failure.");
                    Directory.Move(source, target);
                });
        }
        catch (IOException error) { surfaced = InstallationOwnership.WithRetainedDirectories(error, retained, list => "Kept for recovery:" + Environment.NewLine + list); }
        string backup = Directory.GetDirectories(folder, "app.previous-*").Single();
        if (surfaced is not InvalidOperationException || surfaced.InnerException is not IOException ||
            !surfaced.Message.StartsWith("Substituted move failure.", StringComparison.Ordinal) ||
            !surfaced.Message.Contains(staging) || !surfaced.Message.Contains(backup))
            throw new Exception("A failed setup did not surface its retained staging and previous folders: " + surfaced?.Message);
        var found = InstallationOwnership.FindRetainedDirectories(destination);
        if (found.Count != 2 || !found.Contains(staging) || !found.Contains(backup))
            throw new Exception("Retained folders beside the install folder were not found: " + string.Join(", ", found));
        var untouched = new IOException("Nothing retained.");
        if (!ReferenceEquals(InstallationOwnership.WithRetainedDirectories(untouched, [], list => list), untouched))
            throw new Exception("A failure with nothing retained changed its exception.");

        string launcher = File.ReadAllText(CheckSource.Find(Path.Combine("Packaging", "KillerLauncher", "Program.cs")));
        string wizard = File.ReadAllText(CheckSource.Find(Path.Combine("Packaging", "KillerLauncher", "InstallerWizard.xaml.cs")));
        if (launcher.Contains("directory retained: ") || !launcher.Contains("InstallationOwnership.WithRetainedDirectories(") ||
            !wizard.Contains("InstallationOwnership.FindRetainedDirectories(installDirectory)"))
            throw new Exception("Setup still reports retained folders only to a console the windowless launcher does not have.");
        Console.WriteLine("PASS: a failed setup names its retained recovery, previous and staging folders in the wizard and the /silent failure message.");
    }

    /// <summary>A machine install removes the per-user copy it supersedes. When that removal fails, setup must
    /// fail rather than report success with both installations registered.</summary>
    private static void UserCleanupResultChecked()
    {
        string source = File.ReadAllText(CheckSource.Find(Path.Combine("Packaging", "KillerLauncher", "Program.cs")));
        var tested = System.Text.RegularExpressions.Regex.Match(source,
            @"int (\w+) = RunMaintenance\(destination, ""/remove-user-install""\);\s*if \(\1 != 0\)\s*throw new System\.ComponentModel\.Win32Exception\(\1,\s*LauncherStrings\.Format\(""UserCleanupFailed"", \1\)\);");
        if (!tested.Success || source.Split(new[] { "\"/remove-user-install\"" }, StringSplitOptions.None).Length != 2)
            throw new Exception("Setup ignores the /remove-user-install result after a machine install.");
        Console.WriteLine("PASS: a machine install fails when the superseded per-user copy cannot be removed.");
    }

    /// <summary>The disposable install root and skipped registration exist for the package build's smoke test.
    /// A shipping setup must not honour them, so every read sits behind INSTALLER_TEST_HOOKS, which only the
    /// unsigned smoke-test build defines.</summary>
    private static void TestHooksCompiledOut()
    {
        string[] lines = File.ReadAllLines(CheckSource.Find(Path.Combine("Packaging", "KillerLauncher", "Program.cs")));
        bool guarded = false;
        int reads = 0;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line == "#if INSTALLER_TEST_HOOKS") guarded = true;
            else if (guarded && (line == "#else" || line == "#endif")) guarded = false;
            else if (line.Contains("GetEnvironmentVariable(TestInstallRootEnvironmentVariable)") ||
                line.Contains("GetEnvironmentVariable(SkipRegistrationEnvironmentVariable)") ||
                line.Contains("\"KILLERBENCH_TEST_INSTALL_ROOT\"") && !line.Contains("const string") ||
                line.Contains("\"KILLERBENCH_SKIP_REGISTRATION\"") && !line.Contains("const string"))
            {
                if (!guarded) throw new Exception("Shipping setup reads a test hook outside INSTALLER_TEST_HOOKS: " + line);
                reads++;
            }
        }
        if (reads != 2) throw new Exception("Expected the two guarded test hook reads in the launcher, found " + reads + ".");
        string project = File.ReadAllText(CheckSource.Find(Path.Combine("Packaging", "KillerLauncher", "KillerLauncher.csproj")));
        string build = File.ReadAllText(CheckSource.Find(Path.Combine("Packaging", "build-packages.ps1")));
        if (!project.Contains("'$(InstallerTestHooks)' == 'true'\">$(DefineConstants);INSTALLER_TEST_HOOKS<") ||
            !build.Contains("-p:InstallerTestHooks=\"$(!$RequireSignature)\"") || !build.Contains("if ($isInstaller -and -not $RequireSignature)"))
            throw new Exception("The package smoke test no longer builds the launcher with INSTALLER_TEST_HOOKS, or a signed build would.");
        Console.WriteLine("PASS: setup honours the install-root and skip-registration test hooks only in the unsigned smoke-test build.");
    }
}
