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
        if (BitLockerBackupConsent is not null) BitLockerBackupConsent.IsChecked = false;
        if (BitLockerEncryptConsent is not null) BitLockerEncryptConsent.IsChecked = false;
        if (BitLockerResumeConsent is not null) BitLockerResumeConsent.IsChecked = false;
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
        if (BitLockerBackupConsent is not null) BitLockerBackupConsent.IsChecked = false;
        if (BitLockerResumeConsent is not null) BitLockerResumeConsent.IsChecked = false;
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
            return (inventory.Complete ? "Protector discovery complete. Select a recovery protector before backup." : "Protector discovery incomplete. Backup selection unavailable.",
                inventory.Complete, inventory.Protectors.Select(p => p.Id + " / " + p.Type).ToArray());
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
                var recovery = await BitLockerBackupReconciliation.RunAsync(options, protector, folder, host, token,
                    new Progress<string>(AppendWorkflowOutput));
                protectorVolume = null;
                BitLockerProtectorChoice.ItemsSource = null;
                return (recovery.Detail, recovery.Outcome == BitLockerBackupReconciliationOutcome.Verified,
                    new[] { "Protector: " + protector, "Record: " + (recovery.ArchivePath ?? recovery.PendingPath), "No recovery file was written during this check." });
            }
            var result = await BitLockerBackupExecution.RunAsync(options, protector, folder, host, token, new Progress<string>(AppendWorkflowOutput));
            protectorVolume = null;
            BitLockerProtectorChoice.ItemsSource = null;
            return (result.Status, result.Receipt is { Verified: true }, new[] { "Protector: " + protector, "Journal: " + result.JournalPath });
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
                    new Progress<string>(AppendWorkflowOutput), token);
            }
            string before = result.Before is null ? "Before: unavailable" :
                "Before: " + result.Before.VolumeStatus + ", protection " + result.Before.ProtectionStatus +
                ", " + result.Before.Protectors.Count + " protector identities";
            string after = result.After is null ? "After: unavailable" :
                "After: " + result.After.VolumeStatus + ", protection " + result.After.ProtectionStatus +
                ", " + result.After.Protectors.Count + " protector identities";
            return (result.Detail, result.Outcome == BitLockerProtectionResumeOutcome.Verified,
                ["Volume: " + result.Volume, "Outcome: " + result.Outcome, before, after,
                    "Journal: " + (result.JournalPath ?? "none"),
                    "No encryption method, protector, recovery destination, policy or restart setting was changed."]);
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
                    new Progress<string>(AppendWorkflowOutput), token);
            }
            protectorVolume = null;
            BitLockerProtectorChoice.ItemsSource = null;
            string before = result.Before is null ? "Before: unavailable" :
                "Before: " + result.Before.VolumeStatus + ", " + result.Before.EncryptionPercentage + "% encrypted, " + result.Before.EncryptionMethod;
            string after = result.After is null ? "After: unavailable" :
                "After: " + result.After.VolumeStatus + ", " + result.After.EncryptionPercentage + "% encrypted, " + result.After.EncryptionMethod;
            return (result.Detail, result.Outcome == BitLockerEncryptionOutcome.Started,
                ["Volume: " + result.Volume, "Outcome: " + result.Outcome, before, after,
                    "Recovery backup: " + (result.Backup?.Verified == true ? "verified" : "not verified"),
                    "Journal: " + (result.JournalPath ?? "none")]);
        });
    }

    private async Task RunBitLockerOperationAsync(Func<CancellationToken, Task<(string Detail, bool Verified, string[] Lines)>> operation)
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
            RegisterSource(new TextEvidence("BitLocker " + DateTime.Now.ToString("HHmmss"),
                new[] { result.Detail }.Concat(result.Lines).ToArray(), false));
            Report(result.Detail);
            SetScanLight(result.Verified ? "ready" : "incomplete", result.Detail);
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            const string detail = "BitLocker operation ended without a verified outcome. Review the selected operation and saved journal before retrying.";
            workflowOutputActive = false;
            RegisterSource(new TextEvidence("BitLocker interruption " + DateTime.Now.ToString("HHmmss"), [detail], false));
            Report(detail);
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
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            ModeLabel.Text = "BITLOCKER OPERATION ENDED";
            if (closeAfterOperation) Close();
        }
    }
}
