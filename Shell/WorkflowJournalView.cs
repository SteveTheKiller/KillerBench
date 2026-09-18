using System.IO;
using System.Text.Json;
using System.Windows;
using KillerBench.Engine;
using Microsoft.Win32;

namespace KillerBench;

public partial class MainWindow
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextEvidence, WorkflowJournalSnapshot> historicalWorkflows = new();
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
            string text = "SAVED WORKFLOW / HISTORICAL EVIDENCE\n" + review.Scope +
                "\nRecorded machine: " + review.Machine + "\nWorkflow: " + review.Kind +
                "\nRecorded status: " + review.RecordedStatus + "\nStarted: " + review.StartedUtc.ToString("O") +
                "\nRecorded checks: " + review.CheckCount + " | Recorded repairs: " + review.RepairCount +
                "\nSource: " + review.Source + "\n\n" + JsonSerializer.Serialize(review.Record, Capture.JsonOptions);
            var source = new TextEvidence(review.Source, text.Split('\n'), false);
            historicalWorkflows.Add(source, review);
            RegisterSource(source);
            ShowEvidence();
            ModeLabel.Text = "SAVED WORKFLOW / HISTORICAL";
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
