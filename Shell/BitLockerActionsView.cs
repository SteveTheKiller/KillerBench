using System.IO;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private static string BitLockerFolder(string name) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", name);

    private string SelectedBitLockerAction => (BitLockerActionChoice?.SelectedItem as ComboBoxItem)?.Tag as string ?? "";

    private void OnBitLockerActionChanged(object sender, RoutedEventArgs e)
    {
        BitLockerActionConsent?.IsChecked = false;
        if (BitLockerDecryptConsent is not null)
        {
            BitLockerDecryptConsent.IsChecked = false;
            BitLockerDecryptConsent.Visibility = SelectedBitLockerAction == "decrypt" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnBitLockerActionPreview(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string volume;
        try { volume = BitLockerProtectionResume.NormalizeVolume(BitLockerVolume.Text.Trim()); }
        catch (ArgumentException error) { Report(error.Message); return; }
        string? detail = SelectedBitLockerAction switch
        {
            "auto-unlock" => BitLockerAutoUnlock.Preview(volume).Detail,
            "protector-tpm" => BitLockerProtectorCreation.Preview(volume, BitLockerNewProtector.Tpm).Detail,
            "protector-recovery" => BitLockerProtectorCreation.Preview(volume, BitLockerNewProtector.RecoveryPassword).Detail,
            "escrow-ad" or "escrow-entra" => SelectedRecoveryProtector(volume) is Guid protector
                ? BitLockerEscrow.Preview(volume, EscrowTarget(), protector).Detail
                : "Find and select the recovery protector to back up first.",
            "decrypt" => BitLockerDecryption.Preview(volume).Detail,
            _ => null
        };
        Report(detail ?? "Choose a BitLocker action first.");
    }

    private async void OnBitLockerActionRun(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string action = SelectedBitLockerAction;
        string volume;
        try { volume = BitLockerProtectionResume.NormalizeVolume(BitLockerVolume.Text.Trim()); }
        catch (ArgumentException error) { Report(error.Message); return; }
        if (action.Length == 0) { Report("Choose a BitLocker action first."); return; }
        if (BitLockerActionConsent.IsChecked != true) { Report("Confirm the change on the selected drive first."); return; }
        if (action == "decrypt" && BitLockerDecryptConsent.IsChecked != true)
        { Report("Decryption leaves the drive unprotected. Confirm that separately first."); return; }
        Guid? protector = SelectedRecoveryProtector(volume);
        if (action.StartsWith("escrow", StringComparison.Ordinal) && protector is null)
        { Report("Find and select the recovery protector to back up first."); return; }
        if (!WindowsState.IsAdministrator()) { Report("Run as administrator to change BitLocker."); return; }
        BitLockerActionConsent.IsChecked = false;
        BitLockerDecryptConsent.IsChecked = false;
        var progress = new Progress<string>(AppendWorkflowOutput);
        await RunBitLockerOperationAsync(async token => action switch
        {
            "auto-unlock" => await AutoUnlockAsync(volume, progress, token),
            "protector-tpm" => await AddProtectorAsync(volume, BitLockerNewProtector.Tpm, progress, token),
            "protector-recovery" => await AddProtectorAsync(volume, BitLockerNewProtector.RecoveryPassword, progress, token),
            "decrypt" => await DecryptAsync(volume, progress, token),
            _ => await EscrowAsync(volume, EscrowTarget(), protector!.Value, progress, token)
        });
    }

    private BitLockerEscrowTarget EscrowTarget() =>
        SelectedBitLockerAction == "escrow-ad" ? BitLockerEscrowTarget.ActiveDirectory : BitLockerEscrowTarget.EntraId;

    private Guid? SelectedRecoveryProtector(string volume) =>
        string.Equals(protectorVolume, volume, StringComparison.OrdinalIgnoreCase) && BitLockerProtectorChoice.SelectedItem is Guid id ? id : null;

    private static async Task<BitLockerOperationOutcome> AutoUnlockAsync(string volume, IProgress<string> progress, CancellationToken token)
    {
        string folder = BitLockerFolder("BitLockerAutoUnlock");
        var host = new WindowsBitLockerAutoUnlockHost();
        var result = File.Exists(Path.Combine(folder, "bitlocker-autounlock.pending.json"))
            ? await BitLockerAutoUnlock.ReconcileAsync(folder, host, progress, token)
            : await BitLockerAutoUnlock.RunAsync(volume, true, folder, host, progress, token);
        return new BitLockerOperationOutcome(result.Detail, result.Outcome == BitLockerAutoUnlockOutcome.Enabled,
            ["Volume: " + result.Volume, "Outcome: " + result.Outcome, "Journal: " + (result.JournalPath ?? "none")],
            "BitLocker auto-unlock", "BitLocker auto-unlock / " + result.Outcome, result.JournalPath ?? "No operation journal created.",
            "Keep the drive's recovery password backed up before relying on auto-unlock.");
    }

    private static async Task<BitLockerOperationOutcome> AddProtectorAsync(string volume, BitLockerNewProtector kind, IProgress<string> progress, CancellationToken token)
    {
        string folder = BitLockerFolder("BitLockerProtector");
        var host = new WindowsBitLockerProtectorCreationHost();
        var result = File.Exists(Path.Combine(folder, "bitlocker-protector.pending.json"))
            ? await BitLockerProtectorCreation.ReconcileAsync(folder, host, progress, token)
            : await BitLockerProtectorCreation.RunAsync(volume, kind, true, folder, host, progress, token: token);
        return new BitLockerOperationOutcome(result.Detail, result.Outcome == BitLockerProtectorCreationOutcome.Verified,
            ["Volume: " + result.Volume, "Protector: " + result.Kind, "Outcome: " + result.Outcome,
                "New protector: " + (result.Created?.ToString() ?? "none"), "Journal: " + (result.JournalPath ?? "none")],
            "BitLocker protector", "BitLocker protector / " + result.Outcome, result.JournalPath ?? "No operation journal created.",
            kind == BitLockerNewProtector.RecoveryPassword ? "Back up the new recovery password before making another BitLocker change."
                : "Confirm the TPM protector before making another BitLocker change.");
    }

    private static async Task<BitLockerOperationOutcome> DecryptAsync(string volume, IProgress<string> progress, CancellationToken token)
    {
        string folder = BitLockerFolder("BitLockerDecrypt");
        var host = new WindowsBitLockerDecryptionHost();
        var result = File.Exists(Path.Combine(folder, "bitlocker-decrypt.pending.json"))
            ? await BitLockerDecryption.ReconcileAsync(folder, host, progress, token)
            : await BitLockerDecryption.RunAsync(volume, true, true, folder, host, progress, token: token);
        return new BitLockerOperationOutcome(result.Detail, result.Outcome == BitLockerDecryptionOutcome.Started,
            ["Volume: " + result.Volume, "Outcome: " + result.Outcome, "Journal: " + (result.JournalPath ?? "none")],
            "BitLocker decryption", "BitLocker decryption / " + result.Outcome, result.JournalPath ?? "No operation journal created.",
            "Encrypt the drive again with the required method once decryption finishes.");
    }

    private static async Task<BitLockerOperationOutcome> EscrowAsync(string volume, BitLockerEscrowTarget target, Guid protector,
        IProgress<string> progress, CancellationToken token)
    {
        var result = await BitLockerEscrow.RunAsync(volume, target, protector, true, BitLockerFolder("BitLockerEscrow"),
            new WindowsBitLockerEscrowHost(), progress, token: token);
        return new BitLockerOperationOutcome(result.Detail, result.Outcome == BitLockerEscrowOutcome.Accepted,
            ["Volume: " + result.Volume, "Destination: " + BitLockerEscrow.TargetName(target), "Protector: " + protector,
                "Outcome: " + result.Outcome, "Journal: " + (result.JournalPath ?? "none")],
            "BitLocker recovery backup", "BitLocker directory backup / " + result.Outcome, result.JournalPath ?? "No operation journal created.",
            "Confirm the recovery password appears in " + BitLockerEscrow.TargetName(target) + " before relying on it.");
    }
}
