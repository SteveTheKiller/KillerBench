using System.IO;
using System.Windows;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private string? protectorVolume;

    private void OnChooseRecoveryBackup(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Choose a new recovery backup file outside the protected volume",
            Filter = "Text files (*.txt)|*.txt", DefaultExt = ".txt", AddExtension = true,
            CheckPathExists = true, OverwritePrompt = false
        };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            new BitLockerOptions(BitLockerVolume.Text.Trim(), BitLockerOperation.BackupRecovery,
                Backup: RecoveryDestination.File, BackupFile: picker.FileName).Validate();
            if (File.Exists(picker.FileName) || Directory.Exists(picker.FileName))
            { Report("Choose a new backup filename. Existing files are preserved."); return; }
            BitLockerBackupPath.Text = picker.FileName;
            BitLockerDestination.SelectedItem = RecoveryDestination.File;
            BitLockerBackupConsent.IsChecked = false;
            Report("Backup destination selected. No recovery information written.");
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        { Report("Choose an available new file outside the protected volume."); }
    }

    private void OnBitLockerBackupChanged(object sender, RoutedEventArgs e)
    {
        BitLockerBackupConsent?.IsChecked = false;
        BitLockerEncryptConsent?.IsChecked = false;
        BitLockerResumeConsent?.IsChecked = false;
        BitLockerActionConsent?.IsChecked = false;
        BitLockerDecryptConsent?.IsChecked = false;
    }

    private void OnBitLockerResumeConsentChanged(object sender, RoutedEventArgs e)
    {
        if (BitLockerResumeConsent.IsChecked == true && BitLockerBackupConsent is not null)
            BitLockerBackupConsent.IsChecked = false;
        if (BitLockerResumeConsent.IsChecked == true && BitLockerEncryptConsent is not null)
            BitLockerEncryptConsent.IsChecked = false;
    }

    private void OnBitLockerEncryptConsentChanged(object sender, RoutedEventArgs e)
    {
        if (BitLockerEncryptConsent.IsChecked != true) return;
        BitLockerBackupConsent?.IsChecked = false;
        BitLockerResumeConsent?.IsChecked = false;
    }

    private async void OnFindRecoveryProtectors(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        BitLockerBackupConsent.IsChecked = false;
        BitLockerProtectorChoice.ItemsSource = null;
        protectorVolume = null;
        string volume = BitLockerVolume.Text.Trim();
        try { new BitLockerOptions(volume).Validate(); }
        catch (ArgumentException error) { Report(error.Message); return; }
        if (!WindowsState.IsAdministrator()) { Report("Run as administrator to inspect recovery protectors."); return; }
        await RunBitLockerOperationAsync(async token =>
        {
            AppendWorkflowOutput("Reading recovery protector identities. Passwords are not displayed.");
            var inventory = await BitLockerProtectors.InspectAsync(volume, token);
            if (inventory.Complete)
            {
                protectorVolume = volume;
                BitLockerProtectorChoice.ItemsSource = inventory.Protectors.Where(p => p.Type == "RecoveryPassword").Select(p => p.Id).ToArray();
            }
            return new BitLockerOperationOutcome(inventory.Complete ? "Protector discovery complete. Select a recovery protector before backup." : "Protector discovery incomplete. Backup selection unavailable.",
                inventory.Complete, [.. inventory.Protectors.Select(p => p.Id + " / " + p.Type)]);
        });
    }

    private async Task RunBitLockerBackupAsync(BitLockerOptions options)
    {
        if (options.Operation != BitLockerOperation.BackupRecovery || options.Backup != RecoveryDestination.File ||
            !string.Equals(protectorVolume, options.Volume, StringComparison.OrdinalIgnoreCase) ||
            BitLockerProtectorChoice.SelectedItem is not Guid protector)
        { Report("Choose BackupRecovery, File and a discovered protector for the selected volume."); return; }
        if (!WindowsState.IsAdministrator()) { Report("Run as administrator to back up recovery information."); return; }
        BitLockerBackupConsent.IsChecked = false;
        await RunBitLockerOperationAsync(async token =>
        {
            AppendWorkflowOutput("Verifying the selected volume and protector, then writing a new recovery backup file.");
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitLockerBackup");
            var host = new WindowsBitLockerBackupHost();
            if (File.Exists(Path.Combine(folder, "backup.pending.json")))
            {
                AppendWorkflowOutput("A previous backup needs recovery. Checking the existing file without writing it again.");
                var recovery = await BitLockerBackupReconciliation.RunAsync(options, protector, folder, host, new Progress<string>(AppendWorkflowOutput),
                    token: token);
                protectorVolume = null;
                BitLockerProtectorChoice.ItemsSource = null;
                return new BitLockerOperationOutcome(recovery.Detail, recovery.Outcome == BitLockerBackupReconciliationOutcome.Verified,
                    ["Protector: " + protector, "Record: " + (recovery.ArchivePath ?? recovery.PendingPath), "No recovery file was written during this check."],
                    "BitLocker recovery backup", "BitLocker recovery backup / " + recovery.Outcome,
                    recovery.ArchivePath ?? recovery.PendingPath,
                    "Store the verified recovery backup securely before making another BitLocker change.");
            }
            var result = await BitLockerBackupExecution.RunAsync(options, protector, folder, host, new Progress<string>(AppendWorkflowOutput), token: token);
            protectorVolume = null;
            BitLockerProtectorChoice.ItemsSource = null;
            return new BitLockerOperationOutcome(result.Status, result.Receipt is { Verified: true },
                ["Protector: " + protector, "Journal: " + result.JournalPath],
                "BitLocker recovery backup", "BitLocker recovery backup", result.JournalPath,
                "Store the verified recovery backup securely before making another BitLocker change.");
        });
    }

    private async Task RunBitLockerResumeAsync(BitLockerOptions options)
    {
        if (options.Operation != BitLockerOperation.ResumeProtection)
        { Report("Select ResumeProtection before confirming the protection change."); return; }
        BitLockerResumeConsent.IsChecked = false;
        await RunBitLockerOperationAsync(async token =>
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "BitLockerResume");
            var host = new WindowsBitLockerProtectionResumeHost();
            BitLockerProtectionResumeResult result;
            if (File.Exists(Path.Combine(folder, "bitlocker-resume.pending.json")))
            {
                AppendWorkflowOutput("A previous resume request needs recovery. Checking protection without repeating the request.");
                result = await BitLockerProtectionResume.ReconcileAsync(folder, host,
                    new Progress<string>(AppendWorkflowOutput), token);
            }
            else
            {
                AppendWorkflowOutput("Checking the exact volume, protectors and restart readiness before resuming protection once.");
                result = await BitLockerProtectionResume.RunAsync(options.Volume, true, folder, host,
                    new Progress<string>(AppendWorkflowOutput), token: token);
            }
            string before = result.Before is null ? "Before: unavailable" :
                "Before: " + result.Before.VolumeStatus + ", protection " + result.Before.ProtectionStatus +
                ", " + result.Before.Protectors.Count + " protector identities";
            string after = result.After is null ? "After: unavailable" :
                "After: " + result.After.VolumeStatus + ", protection " + result.After.ProtectionStatus +
                ", " + result.After.Protectors.Count + " protector identities";
            return new BitLockerOperationOutcome(result.Detail, result.Outcome == BitLockerProtectionResumeOutcome.Verified,
                ["Volume: " + result.Volume, "Outcome: " + result.Outcome, before, after,
                    "Journal: " + (result.JournalPath ?? "none"),
                    "No encryption method, protector, recovery destination, policy or restart setting was changed."],
                "BitLocker protection", "BitLocker protection / " + result.Outcome,
                result.JournalPath ?? "No operation journal created.",
                "Confirm BitLocker protection is active before making another BitLocker change.");
        });
    }

    private async Task RunBitLockerEncryptionAsync(BitLockerOptions options)
    {
        if (options.Operation != BitLockerOperation.Encrypt || options.Backup != RecoveryDestination.File ||
            !string.Equals(protectorVolume, options.Volume, StringComparison.OrdinalIgnoreCase) ||
            BitLockerProtectorChoice.SelectedItem is not Guid protector)
        { Report("Choose Encrypt, File and a discovered recovery-password protector for the selected volume."); return; }
        if (!WindowsState.IsAdministrator()) { Report("Run as administrator to start BitLocker encryption."); return; }
        BitLockerEncryptConsent.IsChecked = false;
        await RunBitLockerOperationAsync(async token =>
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "BitLockerEncryption");
            var host = new WindowsBitLockerEncryptionHost();
            BitLockerEncryptionResult result;
            if (File.Exists(Path.Combine(folder, "bitlocker-encryption.pending.json")))
            {
                AppendWorkflowOutput("A previous encryption request needs recovery. Checking conversion without repeating the request.");
                result = await BitLockerEncryptionExecution.ReconcileAsync(folder, host,
                    new Progress<string>(AppendWorkflowOutput), token);
            }
            else
            {
                AppendWorkflowOutput("Verifying the exact volume, protectors and recovery backup before one encryption request.");
                result = await BitLockerEncryptionExecution.RunAsync(options, protector, true, folder, host,
                    new Progress<string>(AppendWorkflowOutput), token: token);
            }
            protectorVolume = null;
            BitLockerProtectorChoice.ItemsSource = null;
            string before = result.Before is null ? "Before: unavailable" :
                "Before: " + result.Before.VolumeStatus + ", " + result.Before.EncryptionPercentage + "% encrypted, " + result.Before.EncryptionMethod;
            string after = result.After is null ? "After: unavailable" :
                "After: " + result.After.VolumeStatus + ", " + result.After.EncryptionPercentage + "% encrypted, " + result.After.EncryptionMethod;
            return new BitLockerOperationOutcome(result.Detail, result.Outcome == BitLockerEncryptionOutcome.Started,
                ["Volume: " + result.Volume, "Outcome: " + result.Outcome, before, after,
                    "Recovery backup: " + (result.Backup?.Verified == true ? "verified" : "not verified"),
                    "Journal: " + (result.JournalPath ?? "none")],
                "BitLocker encryption", "BitLocker encryption / " + result.Outcome,
                result.JournalPath ?? "No operation journal created.",
                "Monitor encryption progress in Windows before making another BitLocker change.");
        });
    }

    private sealed record BitLockerOperationOutcome(string Detail, bool Verified, string[] Lines,
        string? ReportTitle = null, string? ActionName = null, string? Journal = null, string? NextAction = null);

    private async Task RunBitLockerOperationAsync(Func<CancellationToken, Task<BitLockerOperationOutcome>> operation)
    {
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        BitLockerSettings.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.BitLocker);
        ModeLabel.Text = "BITLOCKER / SELECTED OPERATION";
        try
        {
            var result = await operation(stop.Token);
            workflowOutputActive = false;
            if (result.ReportTitle is not null && result.ActionName is not null && result.Journal is not null && result.NextAction is not null)
            {
                RegisterGuardedActionReport(WorkflowKind.BitLocker, result.ReportTitle, result.ActionName,
                    string.Join("\n", new[] { result.Detail }.Concat(result.Lines)), result.Detail, result.Journal,
                    result.Verified, result.NextAction);
                SetScanLight(result.Verified ? "ready" : "incomplete", result.Detail);
                return;
            }
            AppendWorkflowOutput(string.Join("\n", new[] { result.Detail }.Concat(result.Lines)));
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.BitLocker, Environment.MachineName, DateTimeOffset.UtcNow,
                result.Verified ? "BitLocker protector discovery completed" : "BitLocker protector discovery incomplete",
                [new WorkflowCheck("BitLocker recovery protectors", result.Verified ? CheckState.Information : CheckState.Unknown, result.Detail)], [],
                "Read-only BitLocker protector discovery. No recovery password or machine setting was changed."),
                "BitLocker recovery protectors");
            SetScanLight(result.Verified ? "ready" : "incomplete", result.Detail);
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            const string detail = "BitLocker operation ended without a verified outcome. Review the selected operation and saved journal before retrying.";
            AppendWorkflowOutput(detail);
            workflowOutputActive = false;
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.BitLocker, Environment.MachineName, DateTimeOffset.UtcNow,
                "BitLocker operation not completed", [new WorkflowCheck("BitLocker operation", CheckState.Unknown, detail)], [],
                "Review the retained console output and any BitLocker operation journal before retrying."), "BitLocker operation");
            SetScanLight("incomplete", detail);
            protectorVolume = null;
            BitLockerProtectorChoice.ItemsSource = null;
        }
        finally
        {
            workflowOutputActive = false;
            readCancellation = null;
            repairRunning = false;
            BitLockerSettings.IsEnabled = true;
            BitLockerBackupConsent.IsChecked = false;
            BitLockerEncryptConsent.IsChecked = false;
            BitLockerResumeConsent.IsChecked = false;
            BitLockerActionConsent.IsChecked = false;
            BitLockerDecryptConsent.IsChecked = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            ModeLabel.Text = "BITLOCKER OPERATION ENDED";
            if (closeAfterOperation) Close();
        }
    }
}
