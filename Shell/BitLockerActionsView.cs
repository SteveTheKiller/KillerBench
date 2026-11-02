using System.IO;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private static string BitLockerFolder(string name) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", name);

    private string SelectedBitLockerAction => (BitLockerActionChoice?.SelectedItem as ComboBoxItem)?.Tag as string ?? "";

    private static readonly (string Source, string Key)[] BitLockerResultText =
    [
        ("BitLocker operation ended without a verified outcome. Review the selected operation and saved journal before retrying.", "Str_BitResult_Unverified"),
        ("Store the verified recovery backup securely before making another BitLocker change.", "Str_BitResult_SecureBackup"),
        ("Restart Windows, then return to this report and reconcile the selected operation.", "Str_BitResult_RestartReconcile"),
        ("Confirm BitLocker protection is active before making another BitLocker change.", "Str_BitResult_ProtectionActive"),
        ("Monitor encryption progress in Windows before making another BitLocker change.", "Str_BitResult_Monitor"),
        ("Keep the drive's recovery password backed up before relying on auto-unlock.", "Str_BitResult_AutoUnlock"),
        ("Encrypt the drive again with the required method once decryption finishes.", "Str_BitResult_AfterDecrypt"),
        ("Back up the new recovery password before making another BitLocker change.", "Str_BitResult_NewRecovery"),
        ("Protector discovery complete. Select a recovery protector before backup.", "Str_BitResult_DiscoveryComplete"),
        ("Review the local change journal and current state before retrying.", "Str_BitResult_ReviewJournal"),
        ("Confirm the TPM protector before making another BitLocker change.", "Str_BitResult_Tpm"),
        ("Protector discovery incomplete. Backup selection unavailable.", "Str_BitResult_DiscoveryIncomplete"),
    ];

    private static string LocalizeBitLockerResultText(string text)
    {
        foreach (var (source, key) in BitLockerResultText)
            text = text.Replace(source, Controls.LocalizedText.Get(key), StringComparison.Ordinal);
        const string directoryStart = "Confirm the recovery password appears in ";
        const string directoryEnd = " before relying on it.";
        if (text.StartsWith(directoryStart, StringComparison.Ordinal) && text.EndsWith(directoryEnd, StringComparison.Ordinal))
            return Controls.LocalizedText.Format("Str_BitResult_Directory", text[directoryStart.Length..^directoryEnd.Length]);
        foreach (var (source, key) in new[]
        {
            ("BitLocker recovery protectors", "Str_BitLocker_FindProtectors"),
            ("BitLocker recovery backup", "Str_BitLocker_RecoveryBackup"),
            ("BitLocker directory backup", "Str_BitLocker_RecoveryBackup"),
            ("BitLocker auto-unlock", "Str_BitLocker_ActionAutoUnlock"),
            ("BitLocker decryption", "Str_BitLocker_ActionDecrypt"),
            ("BitLocker encryption", "Str_BitLocker_Encryption"),
            ("BitLocker protection", "Str_Choice_BitLockerOperation_ResumeProtection"),
            ("BitLocker protector", "Str_BitResult_Protector"),
            ("BitLocker operation", "Str_BitLocker_Operation")
        })
        {
            string title = "BitLocker " + Controls.LocalizedText.Get(key);
            if (text == source) return title;
            if (text.StartsWith(source + " / ", StringComparison.Ordinal))
                return title + text[source.Length..];
        }
        return text switch
        {
            "BitLocker protector discovery completed" => Controls.LocalizedText.Get("Str_BitLocker_FindProtectors") + ": " + Controls.LocalizedText.Get("Str_ProfileResult_Complete"),
            "BitLocker protector discovery incomplete" => Controls.LocalizedText.Get("Str_BitLocker_FindProtectors") + ": " + Controls.LocalizedText.Get("Str_Report_Incomplete"),
            "BitLocker operation not completed" => "BitLocker " + Controls.LocalizedText.Get("Str_BitLocker_Operation") + ": " + Controls.LocalizedText.Get("Str_Report_Incomplete"),
            "Verified" or "Guarded action verified" => Controls.LocalizedText.Get("Str_Tkp_Verified"),
            "Review required" or "Guarded action requires review" => Controls.LocalizedText.Get("Str_Status_NeedsAttention"),
            "Restart required" => Controls.LocalizedText.Get("Str_Recipe_RestartRequired"),
            _ => text
        };
    }


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
                : Controls.LocalizedText.Get("Str_BitLocker_FindProtectors"),
            "decrypt" => BitLockerDecryption.Preview(volume).Detail,
            _ => null
        };
        Report(detail ?? Controls.LocalizedText.Format("Str_Ui_SelectOne", Controls.LocalizedText.Get("Str_BitLocker_Action")));
    }

    private async void OnBitLockerActionRun(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string action = SelectedBitLockerAction;
        string volume;
        try { volume = BitLockerProtectionResume.NormalizeVolume(BitLockerVolume.Text.Trim()); }
        catch (ArgumentException error) { Report(error.Message); return; }
        if (action.Length == 0) { Report(Controls.LocalizedText.Format("Str_Ui_SelectOne", Controls.LocalizedText.Get("Str_BitLocker_Action"))); return; }
        if (BitLockerActionConsent.IsChecked != true) { Report(Controls.LocalizedText.Format("Str_Ui_ConfirmFirst", Controls.LocalizedText.Get("Str_BitLocker_ActionConsent"))); return; }
        if (action == "decrypt" && BitLockerDecryptConsent.IsChecked != true)
        { Report(Controls.LocalizedText.Format("Str_Ui_ConfirmFirst", Controls.LocalizedText.Get("Str_BitLocker_DecryptConsent"))); return; }
        Guid? protector = SelectedRecoveryProtector(volume);
        if (action.StartsWith("escrow", StringComparison.Ordinal) && protector is null)
        { Report(Controls.LocalizedText.Get("Str_BitLocker_FindProtectors")); return; }
        if (!WindowsState.IsAdministrator()) { Report(Controls.LocalizedText.Get("Str_Ui_AdminRequired")); return; }
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
