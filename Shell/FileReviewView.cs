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
                CompleteFileReview("Duplicate file review", complete, summary,
                    result.FilesExamined, "Duplicate groups", result.Groups.Count);
            }
            else
            {
                LargeFileReviewResult result = await LargeFileReview.ScanAsync(folder, token: stop.Token);
                complete = result.Complete;
                var text = new StringBuilder("LARGE FILE REVIEW\nFolder: " + result.Root + "\nFiles examined: " + result.FilesExamined + "\nLarge files: " + result.Files.Count + "\n\n");
                foreach (LargeFile file in result.Files) text.AppendLine(file.Length + " bytes | " + file.LastWriteTimeUtc.ToString("O") + " | " + file.Path);
                summary = text.AppendLine().Append(result.Detail).ToString();
                CompleteFileReview("Large file review", complete, summary,
                    result.FilesExamined, "Large files", result.Files.Count);
            }
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

    private void CompleteFileReview(string title, bool complete, string rawOutput, int filesExamined,
        string resultLabel, int resultCount)
    {
        AppendWorkflowOutput(rawOutput);
        var checks = new[]
        {
            new WorkflowCheck("Files examined", CheckState.Information, filesExamined + " files were examined in the selected folder."),
            new WorkflowCheck(resultLabel, resultCount == 0 ? CheckState.Clear : CheckState.Attention,
                resultCount == 0 ? "No matching files were identified." : resultCount + " matching result" + (resultCount == 1 ? " was" : "s were") + " identified. Review the raw output before choosing any action."),
            new WorkflowCheck("Review coverage", complete ? CheckState.Clear : CheckState.Unknown,
                complete ? "The selected folder review completed within its safety limits." : "The selected folder review was incomplete. Results do not establish that every file was examined.")
        };
        string status = complete ? title + " complete. No files changed." : title + " is incomplete. No files changed.";
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Storage, Environment.MachineName, DateTimeOffset.UtcNow,
            status, checks, [], "Read-only file review. No journal was written."), title);
        Report(status);
        SetScanLight(complete ? "ready" : "incomplete", status);
    }
}
