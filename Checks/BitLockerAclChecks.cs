using System.Diagnostics;
using System.IO;
using KillerBench.Engine.BitLocker;

internal static class BitLockerAclChecks
{
    internal static async Task RunAsync()
    {
        var options = new BitLockerOptions("Z:", BitLockerOperation.BackupRecovery,
            Backup: RecoveryDestination.File, BackupFile: Path.Combine(Path.GetTempPath(), "unused-backup.txt"));
        string script = BitLockerFileBackup.InspectionStep(options, Guid.Parse("11111111-1111-1111-1111-111111111111")).Arguments[^1];
        int start = script.IndexOf("$allowed=", StringComparison.Ordinal);
        int end = script.IndexOf("$b=@(Get-BitLockerVolume", start, StringComparison.Ordinal);
        if (start < 0 || end < 0) throw new Exception("The production inspection ACL boundary could not be isolated.");
        string predicate = script[start..end].Replace("[Security.Principal.WindowsIdentity]::GetCurrent().User.Value", "$identity");
        foreach (string identity in new[] { "S-1-5-18", "S-1-5-21-1-2-3-1001" })
        foreach (string scenario in new[] { "valid", "duplicate", "unexpected", "missing", "rights", "deny", "inherited", "unprotected" })
        {
            string expected = scenario is "valid" or "duplicate" ? "ACL_ACCEPTED" : "BACKUP_INVALID";
            string setup = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$identity='" + identity + "';$scenario='" + scenario + "';" +
                "$acl=[Security.AccessControl.FileSecurity]::new();$acl.SetAccessRuleProtection(($scenario -ne 'unprotected'),$false);" +
                "$owners=@($identity,'S-1-5-18','S-1-5-32-544')|Sort-Object -Unique;" +
                "foreach($sid in $owners){if($scenario -eq 'missing' -and $sid -eq 'S-1-5-32-544'){continue};" +
                "$rights=if($scenario -eq 'rights' -and $sid -eq 'S-1-5-32-544'){'Read'}else{'FullControl'};" +
                "$acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new($sid),$rights,'Allow'))};" +
                "if($scenario -eq 'duplicate'){$acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new($identity),'FullControl','Allow'))};" +
                "if($scenario -eq 'unexpected'){$acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new('S-1-1-0'),'FullControl','Allow'))};" +
                "if($scenario -eq 'deny'){$acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new($identity),'Read','Deny'))};" +
                "if($scenario -eq 'inherited'){$sddl=$acl.GetSecurityDescriptorSddlForm('Access');" +
                "$acl.SetSecurityDescriptorSddlForm($sddl.Replace('(A;;','(A;ID;'),'Access')};";
            var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell", "v1.0", "powershell.exe"))
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add("-NoProfile"); info.ArgumentList.Add("-NonInteractive"); info.ArgumentList.Add("-EncodedCommand");
            info.ArgumentList.Add(Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(setup + predicate + "'ACL_ACCEPTED'")));
            info.Environment["PSModulePath"] = Environment.GetEnvironmentVariable("PSModulePath", EnvironmentVariableTarget.Machine);
            using var process = Process.Start(info) ?? throw new Exception("ACL fixture did not start.");
            Task<string> output = process.StandardOutput.ReadToEndAsync(), error = process.StandardError.ReadToEndAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try { await process.WaitForExitAsync(deadline.Token); }
            catch { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } throw; }
            string actual = (await output).Trim(), errors = await error;
            if (process.ExitCode != 0 || errors.Length != 0 || actual != expected)
                throw new Exception($"Production ACL inspection failed {identity}/{scenario}: expected {expected}, got {actual}; exit {process.ExitCode}; {errors}");
        }
        Console.WriteLine("PASS: production ACL predicate accepts ordinary and SYSTEM SID sets and duplicate grants; rejects unexpected, missing, weak, deny, inherited and unprotected access.");
    }
}
