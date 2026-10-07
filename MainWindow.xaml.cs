using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow : Window
{
    private TextEvidence? evidence;
    private CancellationTokenSource? readCancellation;
    private readonly KnowledgeCatalogCollection sessionKnowledgeCatalogs = new();
    private bool closeAfterOperation;

    public MainWindow()
    {
        InitializeComponent();
        var titleFrames = System.Windows.Media.Imaging.BitmapDecoder.Create(
            new Uri("pack://application:,,,/KillerBench;component/Assets/AppIcon.ico"),
            System.Windows.Media.Imaging.BitmapCreateOptions.None,
            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames;
        void RefreshTitleIcon() => TitlebarIcon.Source = titleFrames
            .OrderBy(frame => Math.Abs(frame.PixelWidth - TitlebarIcon.ActualWidth * System.Windows.Media.VisualTreeHelper.GetDpi(TitlebarIcon).DpiScaleX))
            .First();
        TitlebarIcon.Loaded += (_, _) => RefreshTitleIcon();
        TitlebarIcon.SizeChanged += (_, _) => RefreshTitleIcon();
        ApplyWorkflowOptionCatalog();
        InitializeShell();
        InitializeNavigation();
        ShowWelcome();
        if (App.DemoMode)
        {
            ModeLabel.Text = "DEMO MODE / SYNTHETIC MACHINE";
            Report("Demo mode is active. GO uses fabricated evidence and never runs a Windows provider or repair command.");
        }
        ApplyDisplay();
        SetDetailsOpen(false);
        ActivityPanel.Visibility = Visibility.Collapsed;
        Closed += (_, _) => readCancellation?.Cancel();
        Closing += (_, e) =>
        {
            if (!repairRunning) return;
            e.Cancel = true;
            closeAfterOperation = true;
            OnCancel(this, new RoutedEventArgs());
            Report("Closing after the active operation stops. An actual repair must finish its current command.");
        };
    }

    private void ShowWelcome()
    {
        evidence = null;
        sources.Clear();
        SourcesList.Items.Clear();
        SearchBox.Clear();
        EvidenceTitle.Text = "Start here";
        EvidenceBox.Text = "KILLERBENCH / START HERE\n\n" +
            "1. Full system scan is selected by default and starts with read-only checks.\n" +
            "2. Select Run diagnosis.\n" +
            "3. Read the readable report for problems and the next safe step.\n" +
            "4. Open Repair only when you are ready to review a focused recovery option.\n\n" +
            "Raw output appears here while work is running. Repair remains off until you explicitly approve it.";
        CoverageLabel.Text = "No scan has run. A clean report starts with the selected diagnostic workflow.";
        ModeLabel.Text = "READY / DIAGNOSTICS";
        SessionLabel.Text = "No sources yet";
        CaptureInfo.Text = "Run diagnosis to collect current evidence, or load a saved capture or sample.";
        SourceInfo.Text = "No evidence loaded. Run diagnosis, open a saved capture, or load sample evidence.";
        FindingsSummary.Text = "No findings yet. A report appears after the selected diagnostic workflow finishes.";
        FindingCards.Children.Clear();
        Report("Ready. Run diagnosis to collect current evidence.");
    }

    private void LoadSample()
    {
        evidence = new TextEvidence("Synthetic demonstration", [
            "KILLERBENCH / EVIDENCE CONSOLE",
            "SYNTHETIC SAMPLE. No data has been collected from this machine.",
            "",
            "[10:42:01] Sample servicing session started.",
            "[10:42:03] Sample package applicability check completed.",
            "[10:42:06] Sample operation returned an error.",
            "",
            "An error line is evidence, not a diagnosis.",
            "Open a .log or .txt file to inspect your own evidence.",
            "Filter lines with FIND. Original line numbers remain visible.",
            "",
            "DISPLAY READY / LOAD DEMO TO TRY LOG INVESTIGATION"
        ], false);
        ModeLabel.Text = "SAMPLE MODE";
        RegisterSource(evidence);
        SearchBox.Clear();
        ShowEvidence();
        Report("Synthetic sample loaded. No system scan performed.");
    }

    private async Task OpenEvidenceAsync()
    {
        if (readCancellation is not null) return;
        string path = PathBox.Text.Trim().Trim('"');
        if (Path.GetExtension(path).Equals(".kbench", StringComparison.OrdinalIgnoreCase))
        { await OpenCaptureAsync(path); return; }
        if (path.Length == 0)
        {
            var picker = new Microsoft.Win32.OpenFileDialog { Filter = "Logs and event files|*.log;*.txt;*.evtx;*.xml|All files|*.*", Title = "Open evidence", Multiselect = true };
            if (picker.ShowDialog(this) != true) return;
            if (picker.FileNames.Length > 1) { await OpenEvidenceBatchAsync(picker.FileNames); return; }
            path = picker.FileName;
            PathBox.Text = path;
        }
        if (!File.Exists(path)) { StatusLabel.Text = "Enter an existing local text-log path."; return; }
        bool archive = Path.GetExtension(path).Equals(".evtx", StringComparison.OrdinalIgnoreCase);
        bool eventXml = Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        readCancellation = cancellation;
        OpenButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        Report(archive ? "Reading saved events. Newest 200 records, no date filter; 15-second time limit." : "Reading text evidence. Maximum 1,000,000 characters; 15-second time limit.");
        try
        {
            var loaded = await Task.Run(async () =>
            {
                var source = archive ? await EventArchive.ReadAsync(path, cancellation.Token) : await TextEvidence.ReadAsync(path, cancellation.Token);
                if (eventXml)
                {
                    var imported = EventXmlImport.Import(source, cancellation.Token);
                    reports.Add(imported.Evidence, imported.Report);
                    return imported.Evidence;
                }
                reports.Add(source, archive ? EventArchive.Analyze(source, cancellation.Token) : Investigation.Analyze(source, cancellation.Token));
                return source;
            });
            evidence = loaded;
            RegisterSource(loaded);
            ModeLabel.Text = archive || eventXml ? "IMPORTED EVENTS / RULES EVALUATED" : "IMPORTED TEXT / RULES EVALUATED";
            SearchBox.Clear();
            ShowEvidence();
            OnFindingsTab(this, new RoutedEventArgs());
            Report("Imported: " + loaded.Source);
        }
        catch (OperationCanceledException) { Report("Read stopped or timed out. Previous evidence retained."); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report("Cannot open evidence: " + error.Message); }
        finally
        {
            readCancellation = null;
            OpenButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }

    private void ShowEvidence()
    {
        if (workflowOutputActive) return;
        if (evidence is null) return;
        EvidenceTitle.Text = evidence.Source == "Synthetic demonstration" ? "Sample evidence" : Path.GetFileName(evidence.Source);
        if (historicalWorkflows.TryGetValue(evidence, out var historical))
        {
            ModeLabel.Text = "SAVED WORKFLOW / HISTORICAL";
            EvidenceBox.Text = evidence.Excerpt(SearchBox.Text, out int historicalMatches);
            CoverageLabel.Text = $"Recorded status: {historical.RecordedStatus} | {historicalMatches:N0} matching lines. No current-health verdict.";
            SourceInfo.Text = historical.Source + "\n\n" + WorkflowJournalSnapshot.Scope;
            shownReport = null;
            FindingCards.Children.Clear();
            FindingsSummary.Text = "Saved workflow record. Diagnostic rules were not rerun against the journal text.\n" + WorkflowJournalSnapshot.Scope;
            return;
        }
        SourceInfo.Text = $"{evidence.Source}\n\n{evidence.Lines.Length:N0} loaded lines\n" + (evidence.Truncated ? "Partial source: input limit reached" : "Source read completed") + "\n\nEvidence inspection only";
        var investigation = reports.GetValue(evidence, item => Investigation.Analyze(item));
        var analysis = investigation.Codes;
        SourceInfo.Text += $"\n\n{analysis.Observations.Count} distinct code observations\n" +
            string.Join("\n", analysis.Observations.Select(code => $"{code.Code}: {code.Occurrences} occurrences (line {code.Evidence[0].LineNumber})")) +
            (analysis.ObservationsTruncated ? "\nAnalysis limited." : "") + $"\n\nFormat: {investigation.Format}\n{investigation.ParsedLines} parsed entries.\n{investigation.Limitation}";
        RefreshFindings(investigation);
        EvidenceBox.Text = evidence.Excerpt(SearchBox.Text, out int matches);
        CoverageLabel.Text = $"{matches:N0} matching lines in loaded portion. " +
            (investigation.Partial ? "PARTIAL ANALYSIS. " : "") + $"{investigation.Findings.Count} findings. No current-health verdict.";
        if (workflowSummaries.TryGetValue(evidence, out var workflow))
        {
            CoverageLabel.Text = $"{workflow.Status} | {workflow.Checks.Count} checks | {workflow.Repairs.Count} repairs run | {matches:N0} matching report lines. Applies only to checked scopes.";
            SourceInfo.Text = $"{evidence.Source}\n\nWorkflow: {workflow.Kind}\nOutcome: {workflow.Status}\nJournal: {workflow.JournalPath}\n\n" +
                "Recorded workflow results. Filtering report lines does not change the outcome or rerun diagnostics.";
        }
    }

    private void ApplyDisplay()
    {
        if (EvidenceBox is null || SizeSlider is null) return;
        EvidenceBox.FontSize = SizeSlider.Value * appScale;
        SaveDisplayPreferences();
    }

    private async void OnOpen(object sender, RoutedEventArgs e) => await OpenEvidenceAsync();
    private void OnFind(object sender, RoutedEventArgs e) => ShowEvidence();
    private void OnSample(object sender, RoutedEventArgs e) { if (readCancellation is null) LoadSample(); }
    private void OnCancel(object sender, RoutedEventArgs e)
    {
        readCancellation?.Cancel();
        Report("Stop requested. Diagnostic checks stop now; an actual repair finishes its current command.");
        CancelButton.IsEnabled = false;
    }
    private void OnDisplayChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ApplyDisplay();
    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (readCancellation is not null) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
        {
            if (paths.Length == 1) { PathBox.Text = paths[0]; await OpenEvidenceAsync(); }
            else await OpenEvidenceBatchAsync(paths);
        }
    }
    private async void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (HandleShellKey(e)) return;
        if (e.Key == Key.Escape) { readCancellation?.Cancel(); e.Handled = true; }
        if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
        { e.Handled = true; await OpenEvidenceAsync(); }
    }
}
