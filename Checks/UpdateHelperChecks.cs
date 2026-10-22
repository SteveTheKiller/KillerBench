using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using KillerBench.Services;

/// <summary>
/// Runs the real installed-copy update helper, unelevated, with a copy of cmd.exe standing in for the installer.
/// The stand-in only writes a marker file. Elevated handoff belongs to the disposable VM.
/// </summary>
internal static class UpdateHelperChecks
{
    internal static void Run(string scratch)
    {
        string root = Path.Combine(scratch, "update-helper");
        Directory.CreateDirectory(root);
        byte[] installer = File.ReadAllBytes(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
        string hash = Convert.ToHexString(SHA256.HashData(installer));

        var ran = Case(root, "verified", installer, hash, "Microsoft");
        Require(ran.Exit == 0 && ran.InstallerRan && ran.Relaunched && !ran.ReleasesOpened && !File.Exists(ran.Download) && ran.StageEmpty,
            "A verified, publisher-signed installer runs from the staged copy, relaunches and cleans up: " + ran);

        var swapped = Case(root, "swapped", [.. installer, 0], hash, "Microsoft");
        Require(swapped.Exit != 0 && !swapped.InstallerRan && !swapped.Relaunched && swapped.ReleasesOpened && swapped.StageEmpty,
            "A download replaced after verification is refused before anything runs: " + swapped);

        var unsigned = Case(root, "wrong-publisher", installer, hash, "Fixture Publisher That Did Not Sign This");
        Require(unsigned.Exit != 0 && !unsigned.InstallerRan && unsigned.ReleasesOpened && unsigned.StageEmpty,
            "An installer not signed by the publisher is refused at the execution boundary: " + unsigned);
        Console.WriteLine("PASS: the installed-copy update helper rechecks SHA-256 and publisher from a held staged copy, refuses a swapped download and runs only the verified image.");

        // The portable helper keeps its existing publication logic; it now launches the same way and must still parse.
        string portableDownload = Path.Combine(root, "portable.exe");
        File.WriteAllBytes(portableDownload, installer);
        UpdateImagePublication.RememberVerified(portableDownload, hash);
        string portable = UpdateHelperLaunch.WaitFor(0, -1) +
            UpdateImagePublication.Script(portableDownload, Path.Combine(root, "KillerBench-Portable.exe"), false, "https://example.invalid/releases", "KillerBench");
        var parse = UpdateHelperLaunch.StartInfo("$tokens=$null; $errors=$null; [void][System.Management.Automation.Language.Parser]::ParseInput([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" +
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(portable)) + "')),[ref]$tokens,[ref]$errors); if($errors.Count){exit 1}else{exit 0}", elevate: false);
        using (var parser = Process.Start(parse) ?? throw new Exception("The parser did not start."))
        {
            parser.WaitForExit(60_000);
            Require(parser.ExitCode == 0, "The portable update helper script parses");
        }
        Require(UpdateHelperLaunch.StartInfo(portable, elevate: true) is { Verb: "runas", UseShellExecute: true } start &&
            start.FileName.EndsWith(@"WindowsPowerShell\v1.0\powershell.exe", StringComparison.OrdinalIgnoreCase) && !start.Arguments.Contains(".bat", StringComparison.OrdinalIgnoreCase),
            "An elevated helper starts PowerShell directly with its script in the command line");
        Console.WriteLine("PASS: the portable update helper parses and both helpers launch without a writable batch file.");
    }

    private sealed record Outcome(int Exit, bool InstallerRan, bool Relaunched, bool ReleasesOpened, bool StageEmpty, string Download);

    private static Outcome Case(string root, string name, byte[] download, string hash, string signer)
    {
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "download.exe");
        File.WriteAllBytes(path, download);
        string marker = Path.Combine(folder, "installer-ran.txt");
        string relaunched = Path.Combine(folder, "relaunched.txt");
        string releases = Path.Combine(folder, "releases.txt");
        string stage = Path.Combine(folder, "stage");
        // Relaunch and the releases page are recorded instead of opened; everything else is the production helper.
        string overrides = "$ops.Relaunch = { param($plan) [IO.File]::WriteAllText('" + relaunched + "', 'yes') }\n" +
            "$ops.Releases = { param($url) [IO.File]::WriteAllText('" + releases + "', $url) }";
        string script = UpdateHelperLaunch.Installer(path, hash, signer, "/d /c type nul > \"" + marker + "\"", false,
            "fixture-app.exe", stage, "https://example.invalid/releases", overrides);
        var start = UpdateHelperLaunch.StartInfo(script, elevate: false);
        using var process = Process.Start(start) ?? throw new Exception("The update helper did not start.");
        if (!process.WaitForExit(60_000)) { process.Kill(true); throw new Exception("The update helper did not finish."); }
        bool stageEmpty = !Directory.Exists(stage) || Directory.GetFileSystemEntries(stage).Length == 0;
        return new(process.ExitCode, File.Exists(marker), File.Exists(relaunched), File.Exists(releases), stageEmpty, path);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
