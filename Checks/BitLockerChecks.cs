using System.Reflection;
using KillerBench.Engine;

internal static class BitLockerChecks
{
    internal static async Task RunAsync()
    {
        await BitLockerWorkflowChecks.RunAsync();
        Guid backupProtector = Guid.NewGuid();
        var backupEvidence = new ToolResult(0, "BACKUP_VERIFIED:" + backupProtector.ToString("D"), "", false);
        Require(BitLockerFileBackup.IsVerified(backupEvidence, backupProtector), "Exact clean backup receipt verifies");
        foreach (var rejected in new[] { backupEvidence with { Error = "Fixture error" }, backupEvidence with { ExitCode = 1 },
            backupEvidence with { Truncated = true }, backupEvidence with { TimedOut = true }, backupEvidence with { Output = "BACKUP_VERIFIED:" + Guid.NewGuid() },
            backupEvidence with { Output = backupEvidence.Output + "\nUnexpected output" } })
            Require(!BitLockerFileBackup.IsVerified(rejected, backupProtector), "Incomplete or conflicting backup receipt cannot verify");
        Require(!BitLockerFileBackup.IsVerified(backupEvidence, Guid.Empty), "Empty protector cannot verify backup");
        var protectorStep = BitLockerProtectors.QueryStep("C:");
        var protectorArgs = protectorStep.Arguments.ToArray();
        protectorArgs[^1] = "function Get-BitLockerVolume($MountPoint,$ErrorAction){[pscustomobject]@{MountPoint=$MountPoint;KeyProtector=@([pscustomobject]@{KeyProtectorId='{b19a63d0-55d6-44f1-a89b-82e2f784b982}';KeyProtectorType='RecoveryPassword';RecoveryPassword='SECRET_PROTECTOR'})}}; " + protectorArgs[^1];
        var protectorOutput = await DefenderRecovery.ExecuteAsync(protectorStep with { Arguments = protectorArgs });
        Require(!protectorOutput.Output.Contains("SECRET_PROTECTOR") && BitLockerProtectors.Interpret(protectorOutput, "C:") is { Complete: true, Protectors.Count: 1 }, "Protector IDs retained without recovery secret");
        Require(!BitLockerProtectors.Interpret(protectorOutput, "D:").Complete, "Protector inventory must match selected volume");
        Require(!BitLockerProtectors.Interpret(protectorOutput with { Truncated = true }, "C:").Complete, "Partial protector inventory is not complete");
        foreach (var invalid in new[]
        {
            protectorOutput with { Error = "Provider failed after output" },
            protectorOutput with { ExitCode = 1 },
            protectorOutput with { TimedOut = true },
            protectorOutput with { Output = "{\"volume\":\"D:\",\"volume\":\"C:\",\"protectors\":[]}" },
            protectorOutput with { Output = "{\"Volume\":\"D:\",\"volume\":\"C:\",\"protectors\":[]}" },
            protectorOutput with { Output = "{\"volume\":\"C:\",\"protectors\":[],\"protectors\":[]}" },
            protectorOutput with { Output = "{\"volume\":\"C:\",\"protectors\":[{\"id\":\"b19a63d0-55d6-44f1-a89b-82e2f784b982\",\"type\":\"Tpm\",\"type\":\"RecoveryPassword\"}]}" },
            protectorOutput with { Output = "{\"volume\":\"C:\",\"protectors\":[{\"id\":\"b19a63d0-55d6-44f1-a89b-82e2f784b982\",\"type\":\"RecoveryPassword\\n\"}]}" }
        })
            Require(BitLockerProtectors.Interpret(invalid, "C:") is { Complete: false, Protectors.Count: 0 },
                "Failed or ambiguous protector output cannot qualify a backup target");
        new BitLockerOptions("C:").Validate();
        new BitLockerOptions("C:", BitLockerOperation.Encrypt, Backup: RecoveryDestination.File, BackupFile: @"D:\Recovery\key.txt").Validate();
        foreach (var invalid in new[] {
            new BitLockerOptions("C:\\"), new BitLockerOptions("C:", BitLockerOperation.Encrypt),
            new BitLockerOptions("C:", Backup: RecoveryDestination.File, BackupFile: @"C:\Recovery\key.txt"),
            new BitLockerOptions("C:", Backup: RecoveryDestination.File, BackupFile: @"D:\key.txt:secret"),
            new BitLockerOptions("C:", BackupFile: @"D:\key.txt"), new BitLockerOptions("C:", (BitLockerOperation)999) })
        {
            try { invalid.Validate(); throw new Exception("Invalid BitLocker options accepted"); }
            catch (ArgumentException) { }
        }
        Require(!new BitLockerOptions("C:", Backup: RecoveryDestination.File, BackupFile: @"D:\private\key.txt").Describe().Contains("private"), "Ordinary option report omits backup path");
        const string fixture = "function Get-BitLockerVolume($ErrorAction) { [pscustomobject]@{MountPoint='C:';VolumeStatus='FullyEncrypted';ProtectionStatus='On';EncryptionPercentage=100;LockStatus='Unlocked';KeyProtector=@{RecoveryPassword='SECRET_FIXTURE_ONLY'}} }; ";
        var method = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.NativeTool")!.GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        var args = BitLockerDiagnostics.QueryStep().Arguments.ToArray();
        args[^1] = fixture + args[^1];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var output = await (Task<ToolResult>)method.Invoke(null, new object?[] { PrintRecovery.Tool, args, false, null, timeout.Token })!;
        Require(output.ExitCode == 0 && !output.Output.Contains("SECRET") && !output.Output.Contains("KeyProtector"), "Collector exports only selected status fields");
        Require(BitLockerDiagnostics.Interpret(output).Single().State == CheckState.Clear, "Protected volume recognized");
        var selectedStep = BitLockerDiagnostics.QueryStep("D:");
        var selectedArgs = selectedStep.Arguments.ToArray();
        selectedArgs[^1] = "function Get-BitLockerVolume($MountPoint,$ErrorAction) { if($MountPoint -ne 'D:'){throw 'WRONG_VOLUME'}; [pscustomobject]@{MountPoint=$MountPoint;VolumeStatus='FullyDecrypted';ProtectionStatus='Off';EncryptionPercentage=0;LockStatus='Unlocked'} }; " + selectedArgs[^1];
        var selectedOutput = await DefenderRecovery.ExecuteAsync(selectedStep with { Arguments = selectedArgs }, token: timeout.Token);
        Require(selectedOutput.ExitCode == 0 && BitLockerDiagnostics.Interpret(selectedOutput).Single().Name == "BitLocker D:", "Selected volume controls native provider query");
        Require(BitLockerDiagnostics.InterpretVolume(selectedOutput, "D:").Single().State == CheckState.Information, "Selected-volume evidence accepted");
        Require(BitLockerDiagnostics.InterpretVolume(selectedOutput, "C:").Single().State == CheckState.Unknown, "Another volume cannot satisfy selected-volume inspection");
        Require(BitLockerDiagnostics.InterpretVolume(selectedOutput with { ExitCode = 5 }, "D:").Single().Detail.Contains("elevation"), "Provider failure preserves unavailable-status guidance");
        var suspended = output with { Output = output.Output.Replace("\"On\"", "\"Off\"") };
        Require(BitLockerDiagnostics.Interpret(suspended).Single().State == CheckState.Attention, "Suspended protection requires review");
        var paused = output with { Output = output.Output.Replace("FullyEncrypted", "EncryptionPaused") };
        Require(BitLockerDiagnostics.Interpret(paused).Single().State == CheckState.Attention, "Paused conversion requires review");
        foreach (var invalid in new[] { output with { ExitCode = 5 }, output with { TimedOut = true }, output with { Truncated = true }, output with { Output = "{\"volumes\":[]}" }, output with { Output = output.Output.Replace("100", "50") } })
            Require(BitLockerDiagnostics.Interpret(invalid).All(c => c.State == CheckState.Unknown), "Incomplete and contradictory BitLocker status cannot pass");
        Require(BitLockerDiagnostics.Interpret(suspended).All(c => c.Repair is null && c.Evidence is null), "Diagnosis cannot mutate protection or retain full provider objects");
        Console.WriteLine("PASS: BitLocker status classification and recovery-password exclusion with a substituted provider.");
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
