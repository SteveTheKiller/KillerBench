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
        if (!Engine.ReleaseFeatures.ThirdPartyUpdates)
        {
            UpdateThirdParty.Visibility = Visibility.Collapsed;
            ThirdPartyUpdateSection.Visibility = Visibility.Collapsed;
        }
        new KillerFamily.Accessibility.OverlayFocus(this).Track(ShortcutsOverlay, AboutOverlay);
        about = new Features.About.AboutController(this);   // Features\About
        var titleFrames = System.Windows.Media.Imaging.BitmapDecoder.Create(
            new Uri("pack://application:,,,/KillerBench;component/Assets/AppIcon.ico"),
            System.Windows.Media.Imaging.BitmapCreateOptions.None,
            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames;
        TitlebarIcon.Source = titleFrames[0];
        ApplyWorkflowOptionCatalog();
        InitializeShell();
        InitializeNavigation();
        ShowWelcome();
        if (App.DemoMode)
        {
            ModeLabel.Text = Controls.LocalizedText.Get(App.ScreenshotMode ? "Str_Mode_DemoScreenshot" : "Str_Mode_DemoLive");
            Report(Controls.LocalizedText.Get(App.ScreenshotMode ? "Str_Demo_ScreenshotActive" : "Str_Demo_Active"));
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
        // After the first paint, so an update offer never lands before the window (Shell\Updates.cs).
        Loaded += (_, _) => Dispatcher.BeginInvoke(new Action(CheckForUpdatesOnStartup),
            System.Windows.Threading.DispatcherPriority.Background);
        if (App.ScreenshotMode)
            Loaded += async (_, _) =>
            {
                await RunDemoWorkflowAsync(WorkflowKind.CommonProblems, simulateRepair: false, screenshotPreset: true);
                LockScreenshotPresentation();
            };
    }

    private void ShowWelcome()
    {
        evidence = null;
        sources.Clear();
        SourcesList.Items.Clear();
        SearchBox.Clear();
        FindingCards.Children.Clear();
        ApplyWelcomeText();
        Report(welcomeStatus);
    }

    // The start screen text, kept so a language change can replace it while it is still showing.
    private string welcomeText = "", welcomeStatus = "";

    private void ApplyWelcomeText()
    {
        static string L(string key) => Controls.LocalizedText.Get(key);
        welcomeText = L("Str_Welcome_Heading") + "\n\n" +
            L("Str_Welcome_Step1") + "\n" + L("Str_Welcome_Step2") + "\n" +
            L("Str_Welcome_Step3") + "\n" + L("Str_Welcome_Step4") + "\n\n" + L("Str_Welcome_Footer");
        welcomeStatus = L("Str_Welcome_Status");
        EvidenceTitle.Text = L("Str_Welcome_OutputTitle");
        EvidenceBox.Text = welcomeText;
        CoverageLabel.Text = L("Str_Welcome_Coverage");
        ModeLabel.Text = L("Str_Mode_ReadyDiagnostics");
        SessionLabel.Text = L("Str_Session_NoSources");
        CaptureInfo.Text = L("Str_Welcome_CaptureInfo");
        SourceInfo.Text = L("Str_Welcome_SourceInfo");
        FindingsSummary.Text = L("Str_Welcome_Findings");
    }

    // Text that code sets does not follow DynamicResource, so a language change sets it again.
    private bool refreshingLanguage;

    private void RefreshLanguage()
    {
        if (evidence is null && !workflowOutputActive && EvidenceBox.Text == welcomeText)
        {
            bool statusWasWelcome = StatusLabel.Text == welcomeStatus;
            ApplyWelcomeText();
            if (statusWasWelcome) StatusLabel.Text = welcomeStatus;
        }
        CategoryList.Items.Refresh();
        CategoryWorkflow.Items.Refresh();
        BitLockerOperationChoice.Items.Refresh();
        BitLockerEncryptionChoice.Items.Refresh();
        BitLockerDestination.Items.Refresh();
        // A closed combo box keeps the old text for its selection until the selection changes.
        refreshingLanguage = true;
        try
        {
            foreach (var box in new ComboBox[] { CategoryWorkflow, BitLockerOperationChoice, BitLockerEncryptionChoice, BitLockerDestination })
            {
                int index = box.SelectedIndex;
                if (index < 0) continue;
                box.SelectedIndex = -1;
                box.SelectedIndex = index;
            }
        }
        finally
        {
            refreshingLanguage = false;
        }
        SynchronizeNavigation();
        UpdateMainActionLabel();
        RefreshWorkflowReportLanguage();
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
            var picker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Filter = "Logs and event files|*.log;*.txt;*.evtx;*.xml|All files|*.*", Title = "Open evidence", Multiselect = true };
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
            CoverageLabel.Text = Controls.LocalizedText.Format("Str_Output_WorkflowCoverage", LocalizedStatus(workflow.Status), workflow.Checks.Count, workflow.Repairs.Count, matches.ToString("N0"));
            SourceInfo.Text = $"{evidence.Source}\n\nWorkflow: {workflow.Kind}\nOutcome: {workflow.Status}\nJournal: {workflow.JournalPath}\n\n" +
                "Recorded workflow results. Filtering report lines does not change the outcome or rerun diagnostics.";
        }
    }

    private void ApplyDisplay()
    {
        if (EvidenceBox is null || SizeSlider is null) return;
        EvidenceBox.FontSize = SizeSlider.Value;
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
        if (App.ScreenshotMode) { e.Effects = DragDropEffects.None; e.Handled = true; return; }
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (App.ScreenshotMode) { e.Handled = true; return; }
        if (readCancellation is not null) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
        {
            if (paths.Length == 1) { PathBox.Text = paths[0]; await OpenEvidenceAsync(); }
            else await OpenEvidenceBatchAsync(paths);
        }
    }
    private async void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (App.ScreenshotMode) { e.Handled = true; return; }
        if (HandleShellKey(e)) return;
        // Alt+Shift+G, the family Film Grain chord. Alt chords arrive as Key.System.
        if (e.Key == Key.System && e.SystemKey == Key.G && Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift))
        {
            if (!e.IsRepeat) ToggleFilmGrain();   // Shell/FamilyMenus.cs
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape) { readCancellation?.Cancel(); e.Handled = true; }
        if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
        { e.Handled = true; await OpenEvidenceAsync(); }
    }
}
