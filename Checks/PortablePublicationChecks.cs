using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using KillerBench.Services;

internal static class PortablePublicationChecks
{
    internal static void Run(string scratch)
    {
        foreach (string mode in new[] { "held", "wrong-publisher", "unsigned", "swapped" })
        {
            string root = Path.Combine(scratch, "portable-" + mode + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string download = Path.Combine(root, "download.exe"), target = Path.Combine(root, "app", "portable.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            byte[] bytes = mode == "unsigned" ? [1, 2, 3] : File.ReadAllBytes(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
            File.WriteAllBytes(download, bytes);
            UpdateImagePublication.RememberVerified(download, Convert.ToHexString(SHA256.HashData(bytes)));
            string script = UpdateImagePublication.Script(download, target, false, "https://example.invalid", "Fixture");
            string marker = Path.Combine(root, "relaunch.txt");
            string hook = """
$ops.Relaunch = { param($plan)
    $blocked = 0
    try { [IO.File]::WriteAllText($plan.Target, 'replacement') } catch [IO.IOException] { $blocked++ }
    try { [IO.File]::Move($plan.Target, $plan.Target + '.renamed') } catch [IO.IOException] { $blocked++ }
    try { [IO.Directory]::Move([IO.Path]::GetDirectoryName($plan.Target), [IO.Path]::GetDirectoryName($plan.Target) + '.renamed') } catch [IO.IOException] { $blocked++ }
    [IO.File]::WriteAllText($plan.Report + '.launched', [string]$blocked)
}
""";
            // Replace only the UI/launch boundary and fixture publisher. Production trust and file protection run.
            string signerHook = mode == "wrong-publisher" ? "$plan | Add-Member Signer 'Not The Publisher' -Force\n" : "$plan | Add-Member Signer 'Microsoft' -Force\n";
            script = script.Replace("if (Invoke-UpdateImagePublication $plan $ops)", signerHook + hook + "\nif (Invoke-UpdateImagePublication $plan $ops)", StringComparison.Ordinal)
                .Replace("try { Start-Process $plan.Releases } catch { }", "", StringComparison.Ordinal);
            if (mode == "swapped") File.AppendAllText(download, "changed after verification");
            var start = UpdateHelperLaunch.StartInfo(script, elevate: false);
            start.UseShellExecute = false;
            start.RedirectStandardError = true;
            start.RedirectStandardOutput = true;
            start.Environment["PSModulePath"] = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\Modules");
            using var child = Process.Start(start) ?? throw new Exception("Portable fixture helper did not start.");
            var output = child.StandardOutput.ReadToEndAsync();
            var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(60_000)) { child.Kill(true); throw new Exception("Portable publication fixture timed out."); }
            string launched = download + ".recovery.txt.launched";
            if (mode == "held")
            {
                if (child.ExitCode != 0 || !File.Exists(launched) || File.ReadAllText(launched) != "3")
                    throw new Exception("Portable relaunch must hold verified image and its directory against replacement: " + child.ExitCode + " " + error.GetAwaiter().GetResult());
            }
            else if (child.ExitCode == 0 || File.Exists(launched))
                throw new Exception("Portable publication executed an unverified " + mode + " image.");
            Console.WriteLine("PASS: portable publication " + mode + "; real signature/hash/hold, launch substituted.");
        }
    }
}
