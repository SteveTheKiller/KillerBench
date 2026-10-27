using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using KillerBench.Services;

/// <summary>
/// Runs the real installed-copy update helper from the shared update stack, unelevated, with a copy of cmd.exe
/// standing in for the installer. The stand-in only writes a marker file. Elevated handoff belongs to the
/// disposable VM. cmd.exe is catalog-signed rather than carrying its own signature, so the fixture swaps the
/// helper's embedded-signature publisher check for a catalog-aware one; the hash, hold and staging run as shipped.
/// </summary>
internal static class UpdateHelperChecks
{
    /// <summary>Fixture publisher check: a valid (catalog or embedded) signature whose subject names $plan.Signer.</summary>
    internal const string CatalogPublisher = """
$ops.Publisher = { param($path, $plan)
    $signature = Get-AuthenticodeSignature -LiteralPath $path
    $signature.Status -eq [Management.Automation.SignatureStatus]::Valid -and $null -ne $signature.SignerCertificate -and
        $signature.SignerCertificate.Subject.IndexOf($plan.Signer, [StringComparison]::OrdinalIgnoreCase) -ge 0
}
""";

    internal static readonly string Signer = new('A', 40);

    internal static void Run(string scratch)
    {
        string root = Path.Combine(scratch, "update-helper");
        Directory.CreateDirectory(root);
        PortableLauncherCases(root);
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
        var failed = Case(root, "installer-failed", installer, hash, "Microsoft", "/d /c exit 3");
        string failedReport = failed.Download + ".update-failure.txt";
        Require(failed.Exit != 0 && !failed.Relaunched && failed.ReleasesOpened && File.Exists(failed.Download) && File.Exists(failedReport)
            && File.ReadAllText(failedReport).Contains("Download: " + failed.Download, StringComparison.Ordinal),
            "An installer that exits nonzero keeps the verified download the failure report names: " + failed);
        Console.WriteLine("PASS: the installed-copy update helper rechecks SHA-256 and publisher from a held staged copy, refuses a swapped download, runs only the verified image and keeps the download when the installer fails.");

        // The wait for the portable launcher has a deadline, so a launcher that never exits cannot hold the helper forever.
        var launcherWait = typeof(PortableUpdateTarget).Assembly.GetType("KillerBench.Services.UpdateService")
            ?.GetMethod("LauncherWait", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new Exception("UpdateService.LauncherWait exists");
        string wait = (string)launcherWait.Invoke(null, [4242])!;
        Require(wait.Contains("$deadline = [DateTime]::UtcNow.Add", StringComparison.Ordinal)
            && wait.Contains("Get-Process -Id 4242", StringComparison.Ordinal)
            && wait.Contains("[DateTime]::UtcNow -lt $deadline", StringComparison.Ordinal),
            "The portable helper's launcher wait is bounded by a deadline: " + wait);
        Console.WriteLine("PASS: the portable helper waits for its launcher only until a deadline.");

        // The portable helper must still parse.
        string portableDownload = Path.Combine(root, "portable.exe");
        File.WriteAllBytes(portableDownload, installer);
        UpdateImagePublication.RememberVerified(portableDownload, hash, Signer);
        string portable = UpdateImagePublication.Script(portableDownload, Path.Combine(root, "KillerBench-Portable.exe"), false,
            "https://example.invalid/releases", "KillerBench");
        // The real launch must fit; the parse runs from a file so its wrapper cannot hit the limit itself.
        ImmutablePowerShell.Arguments(portable);
        string portableScript = Path.Combine(root, "portable-helper.ps1");
        File.WriteAllText(portableScript, portable, new System.Text.UTF8Encoding(true));
        var parse = Helper("$tokens=$null; $errors=$null; [void][System.Management.Automation.Language.Parser]::ParseFile('" +
            portableScript.Replace("'", "''") + "',[ref]$tokens,[ref]$errors); if($errors.Count){exit 1}else{exit 0}");
        using (var parser = Process.Start(parse) ?? throw new Exception("The parser did not start."))
        {
            parser.WaitForExit(60_000);
            Require(parser.ExitCode == 0, "The portable update helper script parses");
        }
        Console.WriteLine("PASS: the portable update helper parses and helpers launch PowerShell with the whole script in the command line.");
    }

    /// <summary>A portable copy overwrites the launcher named by KILLERBENCH_LAUNCHER_PATH, elevated when its folder
    /// is not writable. Only a KillerBench launcher that this copy's portable marker names, and that may replace this
    /// copy under its signature, is accepted. Anything else falls through to the installed or unsupported path.</summary>
    private static void PortableLauncherCases(string root)
    {
        string folder = Path.Combine(root, "portable-launcher");
        string extracted = Path.Combine(folder, "extracted");
        Directory.CreateDirectory(extracted);
        // An unsigned image stands in for the running copy, as a local build is.
        string current = Path.Combine(AppContext.BaseDirectory, "KillerBench.Checks.exe");
        Require(File.Exists(current), "The unsigned check host is available as the running copy");
        string launcher = Path.Combine(folder, "KillerBench-Portable.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), launcher, true);
        string marker = Path.Combine(extracted, ".killerbench-portable");
        File.WriteAllLines(marker, ["KillerBench", "1.0.0", "4242", "", "0", ""]);

        Require(PortableUpdateTarget.IsTrustedLauncher(launcher, "4242", extracted, current),
            "The launcher that started this portable copy is accepted");
        Require(!PortableUpdateTarget.IsTrustedLauncher(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "4242", extracted, current),
            "An unrelated existing file named by KILLERBENCH_LAUNCHER_PATH is never replaced");
        string notes = Path.Combine(folder, "KillerBench-notes.txt");
        File.WriteAllText(notes, "Unrelated data");
        Require(!PortableUpdateTarget.IsTrustedLauncher(notes, "4242", extracted, current), "A file that is not an executable is never replaced");
        Require(!PortableUpdateTarget.IsTrustedLauncher(launcher, "4243", extracted, current), "A launcher the portable marker does not name is refused");
        Require(!PortableUpdateTarget.IsTrustedLauncher(launcher, "4242", folder, current), "A copy with no portable marker is not portable");
        Require(!PortableUpdateTarget.IsTrustedLauncher(Path.Combine(folder, "KillerBench-Missing.exe"), "4242", extracted, current),
            "A missing launcher is refused");
        Console.WriteLine("PASS: a portable update replaces only the KillerBench launcher named by this copy's marker, never another file from KILLERBENCH_LAUNCHER_PATH.");
    }

    /// <summary>Windows PowerShell with the whole script in its command line, hidden, as the update stack starts it.</summary>
    internal static ProcessStartInfo Helper(string script) =>
        new(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"), ImmutablePowerShell.Arguments(script))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Environment.SystemDirectory
        };

    private sealed record Outcome(int Exit, bool InstallerRan, bool Relaunched, bool ReleasesOpened, bool StageEmpty, string Download);

    private static Outcome Case(string root, string name, byte[] download, string hash, string signer, string? arguments = null)
    {
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "download.exe");
        File.WriteAllBytes(path, download);
        UpdateImagePublication.RememberVerified(path, hash, Signer);
        string marker = Path.Combine(folder, "installer-ran.txt");
        string relaunched = Path.Combine(folder, "relaunched.txt");
        string releases = Path.Combine(folder, "releases.txt");
        string stage = Path.Combine(folder, "stage");
        // Relaunch and the releases page are recorded instead of opened; everything else is the production helper.
        string overrides = "$plan.Signer = '" + signer.Replace("'", "''") + "'\n" + CatalogPublisher + "\n" +
            "$ops.Relaunch = { param($plan) [IO.File]::WriteAllText('" + relaunched + "', 'yes') }\n" +
            "$ops.Releases = { param($url) [IO.File]::WriteAllText('" + releases + "', $url) }";
        string script = UpdateImagePublication.Installer(path, arguments ?? "/d /c type nul > \"" + marker + "\"", false,
            "fixture-app.exe", stage, "https://example.invalid/releases", overrides);
        using var process = Process.Start(Helper(script)) ?? throw new Exception("The update helper did not start.");
        if (!process.WaitForExit(60_000)) { process.Kill(true); throw new Exception("The update helper did not finish."); }
        bool stageEmpty = !Directory.Exists(stage) || Directory.GetFileSystemEntries(stage).Length == 0;
        return new(process.ExitCode, File.Exists(marker), File.Exists(relaunched), File.Exists(releases), stageEmpty, path);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
