namespace KillerBench.Engine.BitLocker;

public static class BitLockerFileBackup
{
    public static RepairStep Step(BitLockerOptions options, Guid protector)
    {
        options.Validate();
        if (options.Backup != RecoveryDestination.File || protector == Guid.Empty) throw new ArgumentException("Select a file destination and recovery protector.");
        string path = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(options.BackupFile!)));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; try { " +
            "$path=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + path + "')); " +
            "$parent=[IO.DirectoryInfo]::new([IO.Path]::GetDirectoryName($path)); while($null -ne $parent){if(!$parent.Exists -or ($parent.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Invalid destination'}; $parent=$parent.Parent}; " +
            "$b=Get-BitLockerVolume -MountPoint '" + options.Volume.ToUpperInvariant() + "' -ErrorAction Stop; " +
            "if(@($b).Count -ne 1 -or $b.MountPoint -ne '" + options.Volume.ToUpperInvariant() + "'){throw 'Volume mismatch'}; " +
            "$keys=@($b.KeyProtector | Where-Object { [guid]$_.KeyProtectorId -eq [guid]'" + protector.ToString("D") + "' -and [string]$_.KeyProtectorType -eq 'RecoveryPassword' }); " +
            "if($keys.Count -ne 1 -or $keys[0].RecoveryPassword -notmatch '^([0-9]{6}-){7}[0-9]{6}$'){throw 'Recovery protector unavailable'}; " +
            "$content='BitLocker recovery information'+[Environment]::NewLine+'Protector: " + protector.ToString("D") + "'+[Environment]::NewLine+'Recovery password: '+$keys[0].RecoveryPassword+[Environment]::NewLine; " +
            "$acl=[Security.AccessControl.FileSecurity]::new(); $acl.SetAccessRuleProtection($true,$false); " +
            "$owners=@([Security.Principal.WindowsIdentity]::GetCurrent().User,[Security.Principal.SecurityIdentifier]::new('S-1-5-18'),[Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')); " +
            "foreach($sid in $owners){$acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($sid,[Security.AccessControl.FileSystemRights]::FullControl,[Security.AccessControl.AccessControlType]::Allow))}; " +
            "$bytes=[Text.Encoding]::UTF8.GetBytes($content); $f=[IO.FileStream]::new($path,[IO.FileMode]::CreateNew,[Security.AccessControl.FileSystemRights]::FullControl,[IO.FileShare]::None,4096,[IO.FileOptions]::None,$acl); " +
            "try {$f.Write($bytes,0,$bytes.Length); $f.Flush($true); $f.Position=0; $read=[byte[]]::new($bytes.Length); $offset=0; while($offset -lt $read.Length){$n=$f.Read($read,$offset,$read.Length-$offset); if($n -le 0){throw 'Incomplete verification'}; $offset+=$n}; if([Convert]::ToBase64String($read) -ne [Convert]::ToBase64String($bytes)){throw 'Verification failed'}} finally {$f.Dispose()}; " +
            "'BACKUP_VERIFIED:" + protector.ToString("D") + "' " +
            "} catch { [Console]::Error.WriteLine('Recovery backup failed or could not be verified. Existing files were not overwritten. Review the chosen destination.'); exit 1 }";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], false);
    }

    public static async Task<bool> BackupAsync(BitLockerOptions options, Guid protector, CancellationToken token = default)
    {
        var result = await BoundedTool.RunAsync(Step(options, protector), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return IsVerified(result, protector);
    }

    public static bool IsVerified(ToolResult result, Guid protector)
    {
        ArgumentNullException.ThrowIfNull(result);
        return protector != Guid.Empty && result.ExitCode == 0 && !result.Truncated && !result.TimedOut &&
            string.IsNullOrWhiteSpace(result.Error) && result.Output.Trim() == "BACKUP_VERIFIED:" + protector.ToString("D");
    }

    public static async Task<BitLockerBackupFileState> InspectAsync(BitLockerOptions options, Guid protector,
        CancellationToken token = default)
    {
        var result = await BoundedTool.RunAsync(InspectionStep(options, protector), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error))
            return BitLockerBackupFileState.Unavailable;
        return result.Output.Trim() switch
        {
            "BACKUP_VERIFIED" => BitLockerBackupFileState.Verified,
            "BACKUP_MISSING" => BitLockerBackupFileState.Missing,
            "BACKUP_INVALID" => BitLockerBackupFileState.Invalid,
            _ => BitLockerBackupFileState.Unavailable
        };
    }

    public static RepairStep InspectionStep(BitLockerOptions options, Guid protector)
    {
        options.Validate();
        if (options.Backup != RecoveryDestination.File || protector == Guid.Empty)
            throw new ArgumentException("Select a file destination and recovery protector.");
        string path = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(options.BackupFile!)));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$path=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + path + "')); " +
            "if(!(Test-Path -LiteralPath $path -PathType Leaf)){'BACKUP_MISSING';exit 0}; " +
            "$item=Get-Item -LiteralPath $path -Force -ErrorAction Stop; if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or $item.Length -gt 4096){'BACKUP_INVALID';exit 0}; " +
            "$acl=[Security.AccessControl.FileSecurity]::new($path,[Security.AccessControl.AccessControlSections]::Access); $allowed=@([Security.Principal.WindowsIdentity]::GetCurrent().User.Value,'S-1-5-18','S-1-5-32-544'); " +
            "$rules=@($acl.GetAccessRules($true,$true,[Security.Principal.SecurityIdentifier])); $seen=@($rules|ForEach-Object{$_.IdentityReference.Value}|Sort-Object -Unique); " +
            "if(!$acl.AreAccessRulesProtected -or $seen.Count -ne 3 -or @($allowed|Where-Object{$_ -notin $seen}).Count -ne 0 -or @($rules|Where-Object{$_.AccessControlType -ne 'Allow' -or $_.IdentityReference.Value -notin $allowed -or ($_.FileSystemRights -band [Security.AccessControl.FileSystemRights]::FullControl) -ne [Security.AccessControl.FileSystemRights]::FullControl}).Count -ne 0){'BACKUP_INVALID';exit 0}; " +
            "$b=@(Get-BitLockerVolume -MountPoint '" + options.Volume.ToUpperInvariant() + "' -ErrorAction Stop); if($b.Count -ne 1 -or $b[0].MountPoint -ne '" + options.Volume.ToUpperInvariant() + "'){'BACKUP_INVALID';exit 0}; " +
            "$keys=@($b[0].KeyProtector|Where-Object{[guid]$_.KeyProtectorId -eq [guid]'" + protector.ToString("D") + "' -and [string]$_.KeyProtectorType -eq 'RecoveryPassword'}); " +
            "if($keys.Count -ne 1 -or $keys[0].RecoveryPassword -notmatch '^([0-9]{6}-){7}[0-9]{6}$'){'BACKUP_INVALID';exit 0}; " +
            "$expected='BitLocker recovery information'+[Environment]::NewLine+'Protector: " + protector.ToString("D") + "'+[Environment]::NewLine+'Recovery password: '+$keys[0].RecoveryPassword+[Environment]::NewLine; " +
            "$actual=[IO.File]::ReadAllBytes($path); $wanted=[Text.Encoding]::UTF8.GetBytes($expected); " +
            "if([Convert]::ToBase64String($actual) -ceq [Convert]::ToBase64String($wanted)){'BACKUP_VERIFIED'}else{'BACKUP_INVALID'}";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }
}

public enum BitLockerBackupFileState { Missing, Verified, Invalid, Unavailable }
