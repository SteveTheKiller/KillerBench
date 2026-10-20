using System;
using System.IO;
using KillerLauncher;

internal static class InstallerDiscoveryChecks
{
    [STAThread]
    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "KillerBench-discovery-" + Guid.NewGuid().ToString("N"));
        string Copy(string name)
        {
            string directory = Path.Combine(root, name);
            Directory.CreateDirectory(directory);
            string executable = Path.Combine(directory, "KillerBench.exe");
            File.Copy(typeof(InstallerDiscoveryChecks).Assembly.Location, executable);
            return executable;
        }
        string machine = Copy("custom-machine"), user = Copy("custom-user");
        string machineDefault = Path.GetDirectoryName(Copy("machine-default"))!;
        string userDefault = Path.GetDirectoryName(Copy("user-default"))!;
        Program.InstalledCopy? Discover(string? machineRegistration, string? userRegistration, bool machineTrusted = true, bool userTrusted = true)
            => Program.FindInstalledCopy(scope => scope ? machineRegistration : userRegistration,
                path => (path != machine || machineTrusted) && (path != user || userTrusted),
                scope => !scope, machineDefault, userDefault);
        void Expect(Program.InstalledCopy? found, string expected, bool scope, bool shortcut)
        {
            if (found == null || !string.Equals(found.Executable, expected, StringComparison.OrdinalIgnoreCase) ||
                found.Machine != scope || found.DesktopShortcut != shortcut || string.IsNullOrWhiteSpace(found.Version))
                throw new Exception("Manual setup did not rediscover the expected registered custom path, scope, version, and shortcut.");
        }
        Expect(Discover(machine, user), machine, true, false);
        Expect(Discover(null, user), user, false, true);
        Expect(Discover(machine, user, false), user, false, true);
        foreach (string invalid in new[] { Path.Combine(root, "missing", "KillerBench.exe"), "KillerBench.exe", "C:KillerBench.exe", "bad\0path", Path.Combine(root, "custom-machine", "Other.exe") })
            Expect(Discover(invalid, user), user, false, true);
        Expect(Discover(null, null), Path.Combine(machineDefault, "KillerBench.exe"), true, false);
        Expect(Discover(machine, null, false), Path.Combine(machineDefault, "KillerBench.exe"), true, false);
        Expect(Program.FindInstalledCopy(_ => null, _ => true, scope => !scope,
            Path.Combine(root, "missing-default"), userDefault), Path.Combine(userDefault, "KillerBench.exe"), false, true);
        if (Program.FindInstalledCopy(scope => scope ? machine : user, _ => false, _ => false, machineDefault, userDefault) != null)
            throw new Exception("Setup rediscovered an untrusted registered or legacy executable.");
        Console.WriteLine("PASS actual launcher registered installation discovery and legacy fallback");
        return 0;
    }
}
