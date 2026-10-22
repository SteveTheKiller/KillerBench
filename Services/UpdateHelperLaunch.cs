using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace KillerBench.Services
{
    /// <summary>
    /// Starts the self-update helper as one PowerShell process whose whole script travels in its command line, so no
    /// file the user account can write is read after the helper (possibly elevated) starts. The installed-copy helper
    /// stages the verified installer in a fresh folder, holds it open against writes, renames and deletion, rechecks its
    /// SHA-256 and publisher signature through that hold, and runs it from there.
    /// </summary>
    internal static class UpdateHelperLaunch
    {
        internal static ProcessStartInfo StartInfo(string script, bool elevate)
        {
            using var compressed = new MemoryStream();
            byte[] source = Encoding.UTF8.GetBytes(script);
            using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                gzip.Write(source, 0, source.Length);
            string bootstrap = "$stream=[IO.MemoryStream]::new([Convert]::FromBase64String('" + Convert.ToBase64String(compressed.ToArray()) + "'));" +
                "$reader=[IO.StreamReader]::new([IO.Compression.GZipStream]::new($stream,[IO.Compression.CompressionMode]::Decompress));" +
                "try{&([scriptblock]::Create($reader.ReadToEnd()))}finally{$reader.Dispose();$stream.Dispose()}";
            string arguments = "-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand " +
                Convert.ToBase64String(Encoding.Unicode.GetBytes(bootstrap));
            if (arguments.Length > 30000) throw new IOException("The update helper command is too long.");
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe"), arguments)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System)
            };
            if (elevate) start.Verb = "runas";
            return start;
        }

        /// <summary>Waits for each process id to exit. Zero and negative ids are skipped.</summary>
        internal static string WaitFor(params int[] processIds)
        {
            var script = new StringBuilder("$ErrorActionPreference = 'Stop'\n");
            foreach (int id in processIds)
                if (id > 0) script.Append("while (Get-Process -Id ").Append(id).Append(" -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 500 }\n");
            return script.ToString();
        }

        /// <summary>
        /// The installed-copy helper. <paramref name="overrides"/> replaces individual operations and exists for checks only.
        /// </summary>
        internal static string Installer(string download, string sha256, string signer, string arguments, bool elevated,
            string relaunch, string stageRoot, string releasesUrl, string? overrides = null)
        {
            if (sha256.Length != 64 || Array.Exists(sha256.ToCharArray(), c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException("The verified update hash is invalid.");
            var script = new StringBuilder(InstallerFunction);
            script.AppendLine("$plan = [pscustomobject]@{");
            Value(script, "Download", Path.GetFullPath(download)); Value(script, "Hash", sha256.ToUpperInvariant());
            Value(script, "Signer", signer); Value(script, "Arguments", arguments);
            Value(script, "Relaunch", relaunch); Value(script, "StageRoot", Path.GetFullPath(stageRoot));
            Value(script, "Releases", releasesUrl); Value(script, "Report", Path.GetFullPath(download) + ".update-failure.txt");
            Value(script, "Explorer", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"));
            script.AppendLine("Elevated = " + (elevated ? "$true" : "$false")).AppendLine("}");
            script.AppendLine(InstallerOperations);
            if (overrides is not null) script.AppendLine(overrides);
            script.AppendLine("if (Invoke-UpdateInstaller $plan $ops) { exit 0 } else { exit 1 }");
            return script.ToString();
        }

        private static void Value(StringBuilder script, string name, string value) =>
            script.AppendLine(name + " = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('"
                + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "'))");

        private const string InstallerFunction = """
$ErrorActionPreference = 'Stop'
function Invoke-UpdateInstaller {
    param($plan, $ops)
    $stage = $null
    $hold = $null
    try {
        $bytes = [IO.File]::ReadAllBytes($plan.Download)
        if ((& $ops.Hash $bytes) -ne $plan.Hash) { throw 'The download no longer matches its verified SHA-256.' }
        $stage = & $ops.StageFolder $plan
        $staged = Join-Path $stage 'KillerBench-Setup.exe'
        $write = New-Object IO.FileStream($staged, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None, 65536, [IO.FileOptions]::WriteThrough)
        try { $write.Write($bytes, 0, $bytes.Length); $write.Flush($true) } finally { $write.Dispose() }
        # From here until the installer exits, nothing can write, rename or delete the staged image.
        $hold = New-Object IO.FileStream($staged, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $stagedHash = [BitConverter]::ToString($sha.ComputeHash($hold)).Replace('-', '') } finally { $sha.Dispose() }
        if ($stagedHash -ne $plan.Hash) { throw 'The staged installer failed SHA-256 verification.' }
        if (-not (& $ops.Publisher $staged $plan.Signer)) { throw 'The staged installer is not signed by the publisher.' }
        $code = & $ops.Run $staged $plan.Arguments
        if ($code -ne 0) { throw ('The installer exited with code ' + $code + '.') }
    }
    catch {
        $message = 'Update installation failed: ' + $_.Exception.Message + [Environment]::NewLine + 'Download: ' + $plan.Download
        try { & $ops.Report $plan.Report $message } catch { [Console]::Error.WriteLine($message) }
        try { & $ops.Releases $plan.Releases } catch { }
        return $false
    }
    finally {
        if ($null -ne $hold) { $hold.Dispose() }
        if ($null -ne $stage) { try { [IO.Directory]::Delete($stage, $true) } catch { } }
        try { [IO.File]::Delete($plan.Download) } catch { }
    }
    try { & $ops.Relaunch $plan } catch { }
    return $true
}

""";

        private const string InstallerOperations = """
$ops = @{
    Hash = { param($bytes)
        $hash = [Security.Cryptography.SHA256]::Create()
        try { [BitConverter]::ToString($hash.ComputeHash([byte[]]$bytes)).Replace('-', '') }
        finally { $hash.Dispose() }
    }
    StageFolder = { param($plan)
        $path = Join-Path $plan.StageRoot ([Guid]::NewGuid().ToString('N'))
        if ([IO.Directory]::Exists($path)) { throw 'The staging folder already exists.' }
        [void][IO.Directory]::CreateDirectory($plan.StageRoot)
        if ($plan.Elevated) {
            # Only SYSTEM and Administrators may change what an elevated helper is about to run.
            $security = New-Object Security.AccessControl.DirectorySecurity
            $security.SetAccessRuleProtection($true, $false)
            foreach ($rule in @(@('S-1-5-18', 'FullControl'), @('S-1-5-32-544', 'FullControl'), @('S-1-5-32-545', 'ReadAndExecute'))) {
                $security.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule(
                    (New-Object Security.Principal.SecurityIdentifier($rule[0])), $rule[1], 'ContainerInherit, ObjectInherit', 'None', 'Allow')))
            }
            [void][IO.Directory]::CreateDirectory($path, $security)
        }
        else { [void][IO.Directory]::CreateDirectory($path) }
        $path
    }
    Publisher = { param($path, $signer)
        $signature = Get-AuthenticodeSignature -LiteralPath $path
        $signature.Status -eq [Management.Automation.SignatureStatus]::Valid -and $null -ne $signature.SignerCertificate -and
            $signature.SignerCertificate.Subject.IndexOf($signer, [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
    Run = { param($path, $arguments)
        $start = New-Object Diagnostics.ProcessStartInfo
        $start.FileName = $path
        $start.Arguments = $arguments
        $start.UseShellExecute = $true
        $process = [Diagnostics.Process]::Start($start)
        if ($null -eq $process) { throw 'The installer did not start.' }
        try { $process.WaitForExit(); $process.ExitCode } finally { $process.Dispose() }
    }
    Report = { param($path, $message) [IO.File]::WriteAllText($path, $message) }
    Releases = { param($url) Start-Process $url }
    Relaunch = { param($plan)
        $start = New-Object Diagnostics.ProcessStartInfo
        $start.UseShellExecute = $true
        if ($plan.Elevated) { $start.FileName = $plan.Explorer; $start.Arguments = '"' + $plan.Relaunch + '"' }
        else { $start.FileName = $plan.Relaunch }
        $process = [Diagnostics.Process]::Start($start)
        if ($null -ne $process) { $process.Dispose() }
    }
}
""";
    }
}
