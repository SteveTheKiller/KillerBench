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

        // The portable helper must still parse.
        string portableDownload = Path.Combine(root, "portable.exe");
        File.WriteAllBytes(portableDownload, installer);
        UpdateImagePublication.RememberVerified(portableDownload, hash, Signer);
        string portable = UpdateImagePublication.Script(portableDownload, Path.Combine(root, "KillerBench-Portable.exe"), false,
            "https://example.invalid/releases", "KillerBench");
        var parse = Helper("$tokens=$null; $errors=$null; [void][System.Management.Automation.Language.Parser]::ParseInput([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" +
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(portable)) + "')),[ref]$tokens,[ref]$errors); if($errors.Count){exit 1}else{exit 0}");
        using (var parser = Process.Start(parse) ?? throw new Exception("The parser did not start."))
        {
            parser.WaitForExit(60_000);
            Require(parser.ExitCode == 0, "The portable update helper script parses");
        }
        Console.WriteLine("PASS: the portable update helper parses and helpers launch PowerShell with the whole script in the command line.");
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

    private static Outcome Case(string root, string name, byte[] download, string hash, string signer)
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
        string script = UpdateImagePublication.Installer(path, "/d /c type nul > \"" + marker + "\"", false,
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
