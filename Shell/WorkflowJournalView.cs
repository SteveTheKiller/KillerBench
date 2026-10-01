using System.IO;
using System.Text.Json;
using System.Windows;
using KillerBench.Engine;
using Microsoft.Win32;

namespace KillerBench;

public partial class MainWindow
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextEvidence, WorkflowJournalSnapshot> historicalWorkflows = [];

    private static WorkflowResult BuildHistoricalWorkflowReport(WorkflowJournalSnapshot review)
    {
        var kind = Enum.Parse<WorkflowKind>(review.Kind);
        string coverage = review.CheckCount + " recorded check" + (review.CheckCount == 1 ? "" : "s") +
            " and " + review.RepairCount + " recorded repair" + (review.RepairCount == 1 ? "" : "s") + ".";
        return new WorkflowResult(kind, review.Machine, review.StartedUtc,
            "Historical evidence - current verification required",
            [
                new WorkflowCheck("Current health", CheckState.Unknown, WorkflowJournalSnapshot.Scope),
                new WorkflowCheck("Recorded status", CheckState.Information, "The saved workflow recorded: " + review.RecordedStatus + "."),
                new WorkflowCheck("Recorded coverage", CheckState.Information, coverage),
                new WorkflowCheck("Source file", CheckState.Information, review.Source)
            ], [], review.Source);
    }

    private async void OnOpenWorkflowJournal(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new OpenFileDialog { Title = "Open saved workflow", Filter = "Workflow journal (*.json)|*.json", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        readCancellation = stop;
        SetCaptureBusy(true);
        Report("Reading saved workflow. No checks or repairs will run.");
        try
        {
            var review = await Task.Run(() => WorkflowJournalReview.ReadAsync(picker.FileName, stop.Token), stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            string text = "SAVED WORKFLOW / HISTORICAL EVIDENCE\n" + WorkflowJournalSnapshot.Scope +
                "\nRecorded machine: " + review.Machine + "\nWorkflow: " + review.Kind +
                "\nRecorded status: " + review.RecordedStatus + "\nStarted: " + review.StartedUtc.ToString("O") +
                "\nRecorded checks: " + review.CheckCount + " | Recorded repairs: " + review.RepairCount +
                "\nSource: " + review.Source + "\n\n" + JsonSerializer.Serialize(review.Record, Capture.JsonOptions);
            var source = new TextEvidence(review.Source, text.Split('\n'), false);
            var report = BuildHistoricalWorkflowReport(review);
            historicalWorkflows.Add(source, review);
            workflowSummaries.Add(source, report);
            workflowReportTitles.Add(source, "Saved workflow");
            RegisterSource(source);
            ShowEvidence();
            ModeLabel.Text = "SAVED WORKFLOW / HISTORICAL";
            RenderNamedWorkflowReport(report, "Saved workflow");
            SetOutputView(report: true);
            Report("Saved workflow loaded. Recorded outcomes have not been reverified.");
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or InvalidDataException or
            UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            Report(error is OperationCanceledException ? "Saved workflow review stopped or timed out." : "Saved workflow unavailable: " + error.Message);
        }
        finally
        {
            readCancellation = null;
            SetCaptureBusy(false);
            SetScanLight("incomplete", "Historical workflow review does not establish current health.");
            if (closeAfterOperation) Close();
        }
    }
}
