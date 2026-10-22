using System.IO;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private async void OnViewDefenderOverview(object sender, System.Windows.RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        readCancellation = stop;
        SetCaptureBusy(true);
        ModeLabel.Text = "DEFENDER / OVERVIEW";
        BeginWorkflowOutput(WorkflowKind.Defender);
        AppendWorkflowOutput("Reading Defender protection, scan timing and aggregate threat history. No scan or security setting will change.");
        try
        {
            Task<IReadOnlyList<WorkflowCheck>> protection = DefenderProtectionDiagnostics.CheckAsync(stop.Token);
            Task<WorkflowCheck> scanHistory = DefenderScanHistory.CheckAsync(stop.Token);
            Task<WorkflowCheck> threatHistory = DefenderThreatSummary.CheckAsync(stop.Token);
            await Task.WhenAll(protection, scanHistory, threatHistory);
            WorkflowCheck[] checks = [.. protection.Result, scanHistory.Result, threatHistory.Result];
            AppendWorkflowOutput(string.Join("\n\n", checks.Select(check => check.Name + " / " + check.State + "\n" + check.Detail)));
            bool needsAttention = checks.Any(check => check.State is CheckState.Attention or CheckState.Unknown);
            string summary = needsAttention ? "Defender overview needs review" : "Defender overview collected";
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Defender, Environment.MachineName, DateTimeOffset.UtcNow,
                summary, checks, [], "Read-only Defender overview. No scan, remediation, exclusion, policy or security setting was changed."),
                "Microsoft Defender overview");
            Report(summary);
            SetScanLight(needsAttention ? "attention" : "ready", summary);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException or TimeoutException)
        {
            AppendWorkflowOutput("Defender overview could not complete: " + error.Message);
            SetScanLight("incomplete", "Defender overview is incomplete.");
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            ModeLabel.Text = "WORKFLOW ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private DefenderScanType SelectedDefenderScan() => DefenderScanChoice.SelectedIndex switch
    {
        1 => DefenderScanType.Quick,
        2 => DefenderScanType.Full,
        _ => throw new InvalidOperationException("Select Quick or Full scan.")
    };

    private Func<IDefenderScanHost> createDefenderScanHost = () => new WindowsDefenderScanHost();

    private async Task RunDefenderScanAsync(bool execute)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        readCancellation = stop;
        // A confirmed request holds close and self-update until its immediate outcome is recorded; the scan itself may continue in Defender.
        repairRunning = execute;
        SetCaptureBusy(true);
        ModeLabel.Text = execute ? "DEFENDER SCAN / STARTING" : "DEFENDER SCAN / PREVIEW";
        BeginWorkflowOutput(WorkflowKind.Defender);
        DefenderScanType type = SelectedDefenderScan();
        AppendWorkflowOutput("Checking " + type + " scan eligibility. No exclusion or policy setting will change.");
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "Repairs", "DefenderScan");
            var host = createDefenderScanHost();
            DefenderScanResult result = execute
                ? await DefenderScanExecution.ExecuteAsync(folder, type, true, host, stop.Token)
                : await DefenderScanExecution.PreviewAsync(type, host, stop.Token);
            string output = "MICROSOFT DEFENDER / " + type.ToString().ToUpperInvariant() + " SCAN\n" +
                "Outcome: " + result.Outcome + "\n" + result.Detail +
                (string.IsNullOrWhiteSpace(result.PendingPath) ? "" : "\nRecovery record: " + result.PendingPath) +
                "\n\nThreat identities, paths, exclusions and file contents are not included. KillerBench does not change remediation settings.";
            AppendWorkflowOutput(output);
            CheckState state = result.Outcome is DefenderScanOutcome.Preview or DefenderScanOutcome.Verified ? CheckState.Clear :
                result.Outcome is DefenderScanOutcome.Blocked or DefenderScanOutcome.Inconclusive ? CheckState.Unknown : CheckState.Information;
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Defender, Environment.MachineName, DateTimeOffset.UtcNow,
                result.Detail,
                [new WorkflowCheck(type + " scan outcome", state, result.Detail),
                 new WorkflowCheck("Remediation boundary", CheckState.Information,
                    "KillerBench did not change Defender remediation settings, exclusions, policy, threats, or files.")], [],
                "Defender scan result. Review the Defender console for threat details when applicable."), "Microsoft Defender scan");
            Report(result.Detail);
            SetScanLight(result.Outcome is DefenderScanOutcome.Preview or DefenderScanOutcome.Verified ? "ready" :
                result.Outcome == DefenderScanOutcome.Started ? "running" : "attention", result.Detail);
        }
        catch (OperationCanceledException)
        {
            AppendWorkflowOutput("Defender scan operation stopped. Review any retained recovery record before another request.");
            SetScanLight("incomplete", "Defender scan operation stopped. Completion is unverified.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        {
            AppendWorkflowOutput("Defender scan operation could not complete: " + error.Message);
            SetScanLight("incomplete", "Defender scan completion is unverified.");
        }
        finally
        {
            readCancellation = null;
            repairRunning = false;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            DefenderScanConsent.IsChecked = false;
            ModeLabel.Text = "WORKFLOW ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private async void OnRecoverDefenderScan(object sender, System.Windows.RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        readCancellation = stop;
        SetCaptureBusy(true);
        ModeLabel.Text = "DEFENDER SCAN / RECOVERY";
        BeginWorkflowOutput(WorkflowKind.Defender);
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "Repairs", "DefenderScan");
            var result = await DefenderScanExecution.ReconcileAsync(folder, new WindowsDefenderScanHost(), stop.Token);
            string output = "MICROSOFT DEFENDER / SCAN RECOVERY\nOutcome: " + result.Outcome + "\n" + result.Detail;
            AppendWorkflowOutput(output);
            CheckState state = result.Outcome == DefenderScanOutcome.Verified ? CheckState.Clear :
                result.Outcome == DefenderScanOutcome.Started ? CheckState.Information : CheckState.Unknown;
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Defender, Environment.MachineName, DateTimeOffset.UtcNow,
                result.Detail,
                [new WorkflowCheck("Recovery result", state, result.Detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "This recovery check did not start another scan or change Defender remediation settings, exclusions, policy, threats, or files.")], [],
                "Read-only Defender scan recovery check. No additional scan was requested."), "Microsoft Defender scan recovery");
            Report(result.Detail);
            SetScanLight(result.Outcome == DefenderScanOutcome.Verified ? "ready" :
                result.Outcome == DefenderScanOutcome.Started ? "running" : "attention", result.Detail);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException or TimeoutException)
        {
            AppendWorkflowOutput("Defender scan recovery could not complete: " + error.Message);
            SetScanLight("incomplete", "Defender scan recovery is incomplete.");
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            ModeLabel.Text = "WORKFLOW ENDED";
        }
    }
}
