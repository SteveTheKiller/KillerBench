using System.IO;
using System.Text;
using System.Windows;
using KillerBench.Engine;
using Microsoft.Win32;

namespace KillerBench;

public partial class MainWindow
{
    private void OnBrowseFileReviewFolder(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = "Choose one local folder to review", Multiselect = false };
        if (picker.ShowDialog(this) == true) FileReviewFolder.Text = picker.FolderName;
    }

    private async void OnReviewDuplicates(object sender, RoutedEventArgs e) =>
        await RunFileReviewAsync(duplicates: true);

    private async void OnReviewLargeFiles(object sender, RoutedEventArgs e) =>
        await RunFileReviewAsync(duplicates: false);

    private async Task RunFileReviewAsync(bool duplicates)
    {
        if (readCancellation is not null) return;
        string folder = FileReviewFolder.Text.Trim().Trim('"');
        if (folder.Length == 0) { Report("Choose one local folder to review."); return; }
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        ReviewDuplicatesButton.IsEnabled = false;
        ReviewLargeFilesButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        BeginWorkflowOutput(WorkflowKind.Storage);
        AppendWorkflowOutput(duplicates ? "Reviewing duplicate files without changing them..." : "Reviewing large files without changing them...");
        try
        {
            string summary;
            bool complete;
            if (duplicates)
            {
                DuplicateFileReviewResult result = await DuplicateFileReview.ScanAsync(folder, token: stop.Token);
                complete = result.Complete;
                var text = new StringBuilder("DUPLICATE FILE REVIEW\nFolder: " + result.Root + "\nFiles examined: " + result.FilesExamined + "\nDuplicate groups: " + result.Groups.Count + "\n\n");
                foreach (DuplicateFileGroup group in result.Groups)
                    text.AppendLine(group.Files.Count + " files | " + group.Length + " bytes | " + group.Sha256).AppendJoin('\n', group.Files.Select(file => "  " + file.Path)).AppendLine();
                summary = text.AppendLine().Append(result.Detail).ToString();
            }
            else
            {
                LargeFileReviewResult result = await LargeFileReview.ScanAsync(folder, token: stop.Token);
                complete = result.Complete;
                var text = new StringBuilder("LARGE FILE REVIEW\nFolder: " + result.Root + "\nFiles examined: " + result.FilesExamined + "\nLarge files: " + result.Files.Count + "\n\n");
                foreach (LargeFile file in result.Files) text.AppendLine(file.Length + " bytes | " + file.LastWriteTimeUtc.ToString("O") + " | " + file.Path);
                summary = text.AppendLine().Append(result.Detail).ToString();
            }
            workflowOutputActive = false;
            RegisterSource(new TextEvidence((duplicates ? "Duplicate files " : "Large files ") + DateTime.Now.ToString("HHmmss"), summary.Split('\n'), !complete));
            Report(complete ? "Folder review complete. No files changed." : "Folder review is incomplete. No files changed.");
            SetScanLight(complete ? "ready" : "incomplete", StatusLabel.Text);
        }
        catch (OperationCanceledException) { AppendWorkflowOutput("Folder review stopped. No files changed."); }
        catch (Exception error) when (error is ArgumentException or DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        { AppendWorkflowOutput("Folder review unavailable: " + error.Message); }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            ReviewDuplicatesButton.IsEnabled = true;
            ReviewLargeFilesButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }
}
