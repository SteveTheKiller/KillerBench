using KillerBench.Engine;
using Microsoft.Win32;
using System.Windows;

namespace KillerBench;

public partial class MainWindow
{
    private ProfileInventoryResult? profileInventory;
    private ProfileDependencyResult? profileDependenciesResult;

    private void OnBrowseProfileBackup(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog
        {
            Title = "Choose the existing copied profile backup",
            Multiselect = false
        };
        if (picker.ShowDialog(this) == true) ProfileBackupPath.Text = picker.FolderName;
    }

    private async void OnCreateProfileBackupArtifact(object sender, RoutedEventArgs e) =>
        await RunProfileBackupVerificationAsync(createArtifact: true);

    private async void OnVerifyProfileBackup(object sender, RoutedEventArgs e) =>
        await RunProfileBackupVerificationAsync(createArtifact: false);

    private async void OnCopyProfileBackup(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (profileInventory is null) { Report("Run Profiles first."); return; }
        var selected = ProfileSelection.SelectedItems.Cast<ProfileSnapshot>().ToArray();
        if (selected.Length != 1) { Report("Select exactly one reviewed profile."); return; }
        string destination = ProfileBackupPath.Text.Trim();
        if (destination.Length == 0) { Report("Type a new local NTFS backup folder that does not exist."); return; }
        using var stop = new CancellationTokenSource();
        BeginProfileBackupOperation(stop, "PROFILE BACKUP / COPY AND VERIFY");
        try
        {
            string folder = ProfileOperationFolder();
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => ProfileBackupExecution.RunAsync(selected[0], (int)ProfileAge.Value,
                destination, folder, new WindowsProfileBackupHost(), progress, stop.Token));
            if (result.Verification is not null)
            {
                ShowProfileBackupResult("Profile backup copy", result.Verification,
                    result.Verification.Verified
                        ? "The selected unloaded profile was copied and its regular-file set and contents were verified. The source profile was not removed."
                        : "The copy was not verified. The destination and pending evidence were preserved, and the source profile was not removed.");
                return;
            }

            string note = result.Status == "Blocked"
                ? "Fresh eligibility or destination checks blocked the backup before a verified copy completed. The source profile was not removed."
                : "The backup did not reach verified completion. The destination and pending evidence were preserved, and the source profile was not removed.";
            ShowProfileBackupIncomplete("Profile backup copy", note + " Journal: " + result.JournalPath,
                "Use Check interrupted backup before attempting another copy.");
            Report(note);
            SetScanLight("attention", note);
        }
        catch (OperationCanceledException)
        {
            ShowProfileBackupIncomplete("Profile backup copy",
                "Profile backup stopped. The destination and pending evidence were preserved. No profile was removed.",
                "Use Check interrupted backup before attempting another copy.");
            SetScanLight("incomplete", "Profile backup stopped with verification incomplete.");
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or ArgumentException)
        {
            ShowProfileBackupIncomplete("Profile backup copy", "Profile backup unavailable: " + error.Message,
                "Confirm the destination and profile evidence, then try the backup again.");
            SetScanLight("incomplete", "Profile backup unavailable.");
        }
        finally { EndProfileBackupOperation(); }
    }

    private async void OnRecoverProfileBackup(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource();
        BeginProfileBackupOperation(stop, "PROFILE BACKUP / RECOVERY CHECK");
        try
        {
            AppendWorkflowOutput("Checking the pending profile backup without repeating a copy...");
            string folder = ProfileOperationFolder();
            var result = await Task.Run(() => ProfileBackupRecovery.TryResolveAsync(folder, stop.Token));
            ShowProfileBackupResult("Profile backup recovery", result,
                result.Verified ? "The pending backup was verified and archived. No copy or removal was retried."
                    : "The backup remains unresolved. No copy or removal was retried.");
        }
        catch (OperationCanceledException)
        {
            ShowProfileBackupIncomplete("Profile backup recovery",
                "Profile backup recovery stopped. No copy or removal was retried.",
                "Use Check interrupted backup when the saved evidence is available.");
            SetScanLight("incomplete", "Backup recovery stopped.");
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or ArgumentException)
        {
            ShowProfileBackupIncomplete("Profile backup recovery", "Profile backup recovery unavailable: " + error.Message,
                "Confirm the saved backup evidence, then run the recovery check again.");
            SetScanLight("incomplete", "Backup recovery unavailable.");
        }
        finally { EndProfileBackupOperation(); }
    }

    private async Task RunProfileBackupVerificationAsync(bool createArtifact)
    {
        if (readCancellation is not null) return;
        if (profileInventory is null) { Report("Run Profiles first."); return; }
        var selected = ProfileSelection.SelectedItems.Cast<ProfileSnapshot>().Select(profile => profile.Sid).ToArray();
        if (selected.Length != 1) { Report("Select exactly one reviewed profile."); return; }
        string destination = ProfileBackupPath.Text.Trim();
        if (destination.Length == 0) { Report("Choose the existing copied profile backup folder."); return; }
        using var stop = new CancellationTokenSource();
        BeginProfileBackupOperation(stop, createArtifact ? "PROFILE BACKUP / CREATE VERIFICATION" : "PROFILE BACKUP / VERIFY");
        try
        {
            int days = (int)ProfileAge.Value;
            AppendWorkflowOutput("Refreshing the selected profile and dependency evidence...");
            var plan = await Task.Run(() => ProfileReview.RefreshAsync(selected, days, stop.Token));
            if (!plan.ReviewPassed || plan.Targets.Count != 1)
            {
                string blockers = plan.Blockers.Count == 0 ? "The selected profile is no longer eligible for backup verification." : string.Join("\n", plan.Blockers);
                ShowProfileBackupIncomplete("Profile backup verification", blockers + " No backup files were created and no profile was copied or removed.",
                    "Refresh the selected profile evidence before attempting another verification.");
                Report("Profile backup verification blocked by fresh profile evidence.");
                SetScanLight("attention", "Profile backup verification blocked.");
                return;
            }

            ProfileSnapshot target = plan.Targets[0];
            ProfileBackupVerification result;
            if (createArtifact)
            {
                AppendWorkflowOutput("Comparing the live profile with the existing copied folder and creating fixed verification files...");
                result = await Task.Run(() => ProfileBackupArtifactWriter.CreateForExistingCopyAsync(target, destination, token: stop.Token));
            }
            else
            {
                AppendWorkflowOutput("Verifying the copied folder against the selected live profile...");
                string receipt = System.IO.Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName);
                result = await Task.Run(() => ProfileBackupArtifactVerifier.VerifyAsync(target, receipt, token: stop.Token));
            }
            ShowProfileBackupResult(createArtifact ? "Profile backup artifact" : "Profile backup verification", result,
                result.Verified
                    ? createArtifact
                        ? "The existing copy was verified and its fixed verification files were created. No profile was copied or removed."
                        : "The existing copy and live source were verified. No profile was copied or removed."
                    : "The copied backup was not verified. Existing files were preserved and no profile was copied or removed.");
        }
        catch (OperationCanceledException)
        {
            ShowProfileBackupIncomplete("Profile backup verification",
                "Profile backup verification stopped. No profile was copied or removed.",
                "Check the copied backup again when its evidence is available.");
            SetScanLight("incomplete", "Backup verification stopped.");
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or ArgumentException)
        {
            ShowProfileBackupIncomplete("Profile backup verification", "Profile backup verification unavailable: " + error.Message,
                "Confirm the source and copied backup paths, then try verification again.");
            SetScanLight("incomplete", "Backup verification unavailable.");
        }
        finally { EndProfileBackupOperation(); }
    }

    private void ShowProfileBackupResult(string title, ProfileBackupVerification result, string note)
    {
        string receipt = result.Receipt is null ? "No verified receipt." :
            $"Completed: {result.Receipt.CompletedUtc:O}\nFiles: {result.Receipt.FileCount:N0}\nBytes: {result.Receipt.TotalBytes:N0}\nManifest SHA-256: {result.Receipt.ManifestSha256}";
        string summary = title.ToUpperInvariant() + " / " + (result.Verified ? "VERIFIED" : "NOT VERIFIED") +
            "\n" + result.Detail + "\n" + receipt + "\n\n" + note +
            "\nVerification does not prove permissions, alternate streams, encrypted files, application state or restore behavior.";
        AppendWorkflowOutput(summary);
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Profiles, Environment.MachineName, DateTimeOffset.UtcNow,
            result.Verified ? title + " verified" : title + " requires review",
            [new WorkflowCheck(title, result.Verified ? CheckState.Information : CheckState.Unknown, result.Detail),
             new WorkflowCheck("Change boundary", CheckState.Information,
                "No profile was removed. Verification does not prove restore behavior or every copied profile attribute.")], [],
            "Profile backup verification result. No profile removal was requested."), title);
        SetScanLight(result.Verified ? "ready" : "attention", note);
    }

    private void ShowProfileBackupIncomplete(string title, string detail, string nextStep)
    {
        AppendWorkflowOutput(title.ToUpperInvariant() + " / INCOMPLETE\n" + detail + "\n\n" + nextStep);
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Profiles, Environment.MachineName, DateTimeOffset.UtcNow,
            title + " requires review",
            [new WorkflowCheck(title, CheckState.Unknown, detail),
             new WorkflowCheck("Change boundary", CheckState.Information,
                "No profile was removed. Existing backup and pending evidence were preserved.")], [],
            "Profile backup did not reach verified completion. No profile removal was requested."), title);
    }

    private void ShowProfileReviewIncomplete(string detail, string nextStep)
    {
        AppendWorkflowOutput("PROFILE REVIEW / INCOMPLETE\n" + detail + "\n\n" + nextStep);
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Profiles, Environment.MachineName, DateTimeOffset.UtcNow,
            "Profile review requires attention",
            [new WorkflowCheck("Profile snapshot review", CheckState.Unknown, detail),
             new WorkflowCheck("Change boundary", CheckState.Information,
                "This is a review only. No profile was copied or removed.")], [],
            "Profile review did not produce a complete plan. No profile change was requested."), "Profile review");
    }

    private void BeginProfileBackupOperation(CancellationTokenSource stop, string mode)
    {
        readCancellation = stop;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        ModeLabel.Text = mode;
        BeginWorkflowOutput(WorkflowKind.Profiles);
    }

    private void EndProfileBackupOperation()
    {
        readCancellation = null;
        workflowOutputActive = false;
        SourcesList.IsEnabled = true;
        SearchBox.IsEnabled = true;
        SetCaptureBusy(false);
        RepairPanel.IsEnabled = true;
        ModeLabel.Text = "PROFILE BACKUP CHECK ENDED";
        if (closeAfterOperation) Close();
    }

    private static string ProfileOperationFolder() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Profiles");

    private async void OnPreviewProfiles(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (profileInventory is null) { Report("Run Profiles first."); return; }
        var selected = ProfileSelection.SelectedItems.Cast<ProfileSnapshot>().Select(p => p.Sid).ToArray();
        try { ProfilePlanning.ValidateSelection(selected); }
        catch (ArgumentException error) { Report(error.Message); return; }
        int days = (int)ProfileAge.Value;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.Profiles);
        try
        {
            AppendWorkflowOutput("Refreshing selected-profile evidence and service/task dependencies...");
            var plan = await Task.Run(() => ProfileReview.RefreshAsync(selected, days, stop.Token));
            string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Profiles");
            string journal = await ProfileReview.SavePlanAsync(plan, folder, days, stop.Token);
            string summary = "PROFILE PLAN / SNAPSHOT REVIEW\n" + string.Join("\n", plan.Targets.Select(p => p.Path + " | " + p.Sid)) +
                "\n\n" + (plan.ReviewPassed ? "Snapshot review passed. No deletion authorized." : string.Join("\n", plan.Blockers)) + "\n" + plan.Limitation;
            AppendWorkflowOutput(summary);
            string reviewDetail = plan.ReviewPassed
                ? "Snapshot review passed. No profile deletion was authorized."
                : plan.Blockers.Count == 0 ? "The profile review did not produce complete evidence." : string.Join(" ", plan.Blockers);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Profiles, Environment.MachineName, DateTimeOffset.UtcNow,
                plan.ReviewPassed ? "Profile review complete" : "Profile review requires attention",
                [new WorkflowCheck("Profile snapshot review", plan.ReviewPassed ? CheckState.Information : CheckState.Unknown, reviewDetail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "This is a review only. No profile was copied or removed.")], [], journal), "Profile review");
            Report("Profile plan saved: " + journal + ". No profiles changed.");
            SetScanLight(plan.ReviewPassed ? "ready" : "incomplete", "Profile review only. No profiles changed.");
        }
        catch (ArgumentException error) { Report(error.Message); }
        catch (OperationCanceledException)
        {
            ShowProfileReviewIncomplete("Profile review stopped. No profiles changed.",
                "Run the review again when the selected profile evidence is available.");
            SetScanLight("incomplete", "Review stopped.");
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException)
        {
            ShowProfileReviewIncomplete("Profile review unavailable: " + error.Message,
                "Confirm the selected profile and try the review again.");
            SetScanLight("incomplete", "Review unavailable.");
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            ModeLabel.Text = "PROFILE REVIEW ENDED";
            if (closeAfterOperation) Close();
        }
    }
}
