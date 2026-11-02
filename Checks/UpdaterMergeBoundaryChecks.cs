using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using KillerBench.Services;

internal static class UpdaterMergeBoundaryChecks
{
    internal static void Run(string scratch)
    {
        string parent = Path.GetFullPath(scratch);
        string allowed = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scratch")) + Path.DirectorySeparatorChar;
        if (!parent.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(parent))
            throw new InvalidOperationException("An existing owned scratch directory is required.");
        string root = Path.Combine(parent, "updater-merge-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        byte[] image = [1, 7, 3, 9, 2];
        string download = Path.Combine(root, "download.bin");
        File.WriteAllBytes(download, image);
        UpdateImagePublication.RememberVerified(download, Convert.ToHexString(SHA256.HashData(image)), new string('A', 40));
        // Obtain the same emitted command artifact the updater dispatches. The script fixture replaces
        // OS boundaries before executing its publication core and operation table.
        string script = UpdateImagePublication.Script(download, Path.Combine(root, "target.bin"), true,
            "https://example.invalid/owned-fixture", "Synthetic fixture", originalHash: new string('B', 64), originalSigner: new string('C', 40));
        string emitted = Path.Combine(root, "emitted-publication.ps1");
        File.WriteAllText(emitted, script, new System.Text.UTF8Encoding(true));
        string installer = Path.Combine(root, "emitted-installer.ps1");
        File.WriteAllText(installer, UpdateImagePublication.Installer(download, "/owned-fixture-only", true,
            Path.Combine(root, "installed-target.bin"), Path.Combine(root, "installer-stage"), "https://example.invalid/owned-fixture"), new System.Text.UTF8Encoding(true));
        string checks = Path.Combine(Environment.CurrentDirectory, "Checks", "Test-UpdaterMergeBoundaries.ps1");
        if (!File.Exists(checks)) throw new InvalidOperationException("Run the check with the KillerBench repository as working directory.");
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", checks,
            "-EmittedScriptPath", emitted, "-InstallerScriptPath", installer, "-WorkRoot", root }) start.ArgumentList.Add(argument);
        start.Environment["TEMP"] = root;
        start.Environment["TMP"] = root;
        start.Environment["PSModulePath"] = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\Modules");
        using var child = Process.Start(start) ?? throw new InvalidOperationException("Updater merge check process did not start.");
        var output = child.StandardOutput.ReadToEndAsync();
        var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(120_000))
        {
            child.Kill(true);
            child.WaitForExit();
            Task.WhenAll(output, error).GetAwaiter().GetResult();
            throw new InvalidOperationException("Updater merge check exceeded its deadline. Evidence: " + root);
        }
        string stdout = output.GetAwaiter().GetResult(), stderr = error.GetAwaiter().GetResult();
        File.WriteAllText(Path.Combine(root, "stdout.log"), stdout);
        File.WriteAllText(Path.Combine(root, "stderr.log"), stderr);
        if (stdout.Length > 65536 || stderr.Length > 65536) throw new InvalidOperationException("Updater merge check output exceeded its bound. Evidence: " + root);
        Console.Write(stdout);
        if (child.ExitCode != 0) throw new InvalidOperationException("Updater merge boundaries failed with exit " + child.ExitCode + ". Evidence: " + root + "\n" + stderr);
        if (!stdout.Contains("PASS:", StringComparison.Ordinal)) throw new InvalidOperationException("Updater merge check selected no scenarios.");
    }
}
