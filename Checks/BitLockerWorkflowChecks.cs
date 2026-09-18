using KillerBench.Engine;
using System.IO;

internal static class BitLockerWorkflowChecks
{
    internal static async Task RunAsync()
    {
        await BitLockerBackupChecks.RunAsync();
        string backupPath = Path.Combine(Path.GetTempPath(), "KillerBench-fake-recovery-" + Guid.NewGuid().ToString("N") + ".txt");
        var protector = Guid.Parse("b19a63d0-55d6-44f1-a89b-82e2f784b982");
        var backupStep = BitLockerFileBackup.Step(new("Z:", BitLockerOperation.BackupRecovery, Backup: RecoveryDestination.File, BackupFile: backupPath), protector);
        var backupArgs = backupStep.Arguments.ToArray();
        backupArgs[^1] = "function Get-BitLockerVolume($MountPoint,$ErrorAction){[pscustomobject]@{MountPoint=$MountPoint;KeyProtector=@([pscustomobject]@{KeyProtectorId='b19a63d0-55d6-44f1-a89b-82e2f784b982';KeyProtectorType='RecoveryPassword';RecoveryPassword='000000-000000-000000-000000-000000-000000-000000-000000'})}}; " + backupArgs[^1];
        var backupResult = await DefenderRecovery.ExecuteAsync(backupStep with { Arguments = backupArgs });
        if (backupResult.ExitCode != 0 || !File.Exists(backupPath) || backupResult.Output.Contains("000000")) throw new Exception("Fixture recovery file was not verified without secret output: " + backupResult.Error);
        string retained = File.ReadAllText(backupPath);
        var acl = System.IO.FileSystemAclExtensions.GetAccessControl(new FileInfo(backupPath));
        if (!acl.AreAccessRulesProtected) throw new Exception("Recovery backup inherited directory permissions");
        var permitted = new[] { System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value, "S-1-5-18", "S-1-5-32-544" };
        foreach (System.Security.AccessControl.FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)))
            if (!permitted.Contains(rule.IdentityReference.Value)) throw new Exception("Recovery backup grants an unexpected identity access");
        var repeatedBackup = await DefenderRecovery.ExecuteAsync(backupStep with { Arguments = backupArgs });
        if (repeatedBackup.ExitCode == 0 || File.ReadAllText(backupPath) != retained) throw new Exception("Recovery backup overwrote an existing file");
        var inspectionStep = BitLockerFileBackup.InspectionStep(new("Z:", BitLockerOperation.BackupRecovery, Backup: RecoveryDestination.File, BackupFile: backupPath), protector);
        var inspectionArgs = inspectionStep.Arguments.ToArray();
        inspectionArgs[^1] = backupArgs[^1][..backupArgs[^1].IndexOf("$ErrorActionPreference", StringComparison.Ordinal)] + inspectionArgs[^1];
        var inspection = await DefenderRecovery.ExecuteAsync(inspectionStep with { Arguments = inspectionArgs });
        if (inspection.ExitCode != 0 || inspection.Output.Trim() != "BACKUP_VERIFIED" || inspection.Output.Contains("000000"))
            throw new Exception("Fixture recovery file inspection was not exact and private: " + inspection.Error);
        await File.AppendAllTextAsync(backupPath, "changed");
        var changedInspection = await DefenderRecovery.ExecuteAsync(inspectionStep with { Arguments = inspectionArgs });
        if (changedInspection.ExitCode != 0 || changedInspection.Output.Trim() != "BACKUP_INVALID")
            throw new Exception("Changed recovery file was accepted by reconciliation inspection.");
        var options = new BitLockerOptions("C:", BitLockerOperation.Encrypt, Backup: RecoveryDestination.EntraId);
        const string providers = """
            function Get-BitLockerVolume($MountPoint,$ErrorAction) {
                if($MountPoint -ne 'C:'){throw 'WRONG_VOLUME'};
                [pscustomobject]@{MountPoint='C:';VolumeType='OperatingSystem';VolumeStatus='FullyDecrypted';LockStatus='Unlocked';ProtectionStatus='Off';EncryptionPercentage=0;KeyProtector=@{RecoveryPassword='SECRET_FIXTURE'}}
            };
            function Get-Volume($DriveLetter,$ErrorAction) {if($DriveLetter -ne 'C'){throw 'WRONG_DRIVE'}; [pscustomobject]@{UniqueId='volume-1';FileSystemType='NTFS'}};
            function Get-Tpm($ErrorAction) {[pscustomobject]@{TpmReady=$true;OwnerAuth='SECRET_FIXTURE'}};
            function Test-Path($Path,$ErrorAction) {if($Path -ne 'HKLM:\SOFTWARE\Policies\Microsoft\FVE'){throw 'WRONG_POLICY_PATH'}; $script:hasPolicy};
            """;
        foreach (bool policy in new[] { false, true })
        {
            var step = BitLockerReadinessChecks.QueryStep("C:");
            var args = step.Arguments.ToArray();
            args[^1] = "$script:hasPolicy=$" + policy.ToString().ToLowerInvariant() + "; " + providers + args[^1];
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var result = await DefenderRecovery.ExecuteAsync(step with { Arguments = args }, token: stop.Token);
            if (result.ExitCode != 0 || result.Output.Contains("SECRET") || BitLockerReadinessChecks.Interpret(result, "C:").CanEncrypt == policy)
                throw new Exception("Native BitLocker readiness collector failed: " + result.Error);
        }
        const string json = "{\"mount\":\"C:\",\"identity\":\"volume-1\",\"filesystem\":\"NTFS\",\"type\":\"OperatingSystem\",\"status\":\"FullyDecrypted\",\"protection\":\"Off\",\"percent\":0,\"locked\":\"Unlocked\",\"tpm\":true,\"policy\":false}";
        if (!BitLockerReadinessChecks.Interpret(new(0, json, "", false), "C:").CanEncrypt) throw new Exception("Initial readiness not recognized");
        foreach (string invalid in new[] { json.Replace("\"policy\":false", "\"policy\":true"), json.Replace("\"tpm\":true", "\"tpm\":false"), json.Replace("FullyDecrypted", "FullyEncrypted"), json.Replace("NTFS", "FAT32"), "{}" })
            if (BitLockerReadinessChecks.Interpret(new(0, invalid, "", false), "C:").CanEncrypt) throw new Exception("Blocked readiness accepted");
        foreach (string scenario in new[] { "success", "backup-failed", "wrong-volume", "changed", "blocked" })
        {
            var host = new Fixture(scenario);
            var result = await BitLockerWorkflow.EncryptAsync(options, host);
            if (result.Started != (scenario == "success") || host.Starts != (scenario == "success" ? 1 : 0))
                throw new Exception("BitLocker backup ordering failed: " + scenario);
        }
        Console.WriteLine("PASS: BitLocker verified-backup sequencing and changed-volume blockers (substituted providers only).");
    }
    private sealed class Fixture(string scenario) : IBitLockerWorkflowHost
    {
        private int inspections;
        private bool backedUp;
        internal int Starts;
        public Task<BitLockerReadiness> InspectAsync(BitLockerOptions options, CancellationToken token)
        {
            inspections++;
            return Task.FromResult(new BitLockerReadiness("C:", scenario == "changed" && inspections > 1 ? "different" : "volume-1", scenario != "blocked", "Fixture readiness"));
        }
        public Task<RecoveryBackupReceipt> PrepareAndVerifyBackupAsync(BitLockerOptions options, BitLockerReadiness readiness, CancellationToken token)
        {
            backedUp = true;
            return Task.FromResult(new RecoveryBackupReceipt(scenario == "wrong-volume" ? "volume-2" : "volume-1", Guid.NewGuid(), options.Backup, scenario != "backup-failed"));
        }
        public Task<BitLockerStartResult> StartEncryptionAsync(BitLockerOptions options, RecoveryBackupReceipt receipt, CancellationToken token)
        {
            if (!backedUp || inspections != 2) throw new Exception("Encryption preceded backup and fresh readiness");
            Starts++;
            return Task.FromResult(new BitLockerStartResult(true, "Fixture started"));
        }
    }
}
