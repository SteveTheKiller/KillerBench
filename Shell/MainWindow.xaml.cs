using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow : Window
{
    private TextEvidence? evidence;
    private CancellationTokenSource? readCancellation;
    private readonly KnowledgeCatalogCollection sessionKnowledgeCatalogs = new();
    private bool closeAfterOperation;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTaskDraftConsent();
        if (!Engine.ReleaseFeatures.ThirdPartyUpdates)
        {
            UpdateThirdParty.Visibility = Visibility.Collapsed;
            ThirdPartyUpdateSection.Visibility = Visibility.Collapsed;
        }
        new KillerFamily.Accessibility.OverlayFocus(this).Track(ShortcutsOverlay, AboutOverlay);
        _about = new Features.About.AboutController(this);   // Features\About
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
            Report(Controls.LocalizedText.Get("Str_Ui_CloseWaiting"));
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
        RefreshRecipeSelectionText();
        RefreshUpdateActions();
        RefreshThirdPartyUpdateActions();
        RefreshNativeCapabilityStatus();
        RefreshPrivacyLanguage();
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
        ModeLabel.Text = Controls.LocalizedText.Get("Str_Text_SAMPLE_MODE");
        RegisterSource(evidence);
        SearchBox.Clear();
        ShowEvidence();
        Report(Controls.LocalizedText.Get("Str_Ui_SampleLoaded"));
    }

    private async Task OpenEvidenceAsync()
    {
        if (readCancellation is not null) return;
        string path = PathBox.Text.Trim().Trim('"');
        if (Path.GetExtension(path).Equals(".kbench", StringComparison.OrdinalIgnoreCase))
        { await OpenCaptureAsync(path); return; }
        if (path.Length == 0)
        {
            var picker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Filter = "LOG, TXT, EVTX, XML|*.log;*.txt;*.evtx;*.xml|" + Controls.LocalizedText.Get("Str_Dlg_AllFiles") + "|*.*", Title = Controls.LocalizedText.Get("Str_Shortcuts_OpenEvidence"), Multiselect = true };
            if (picker.ShowDialog(this) != true) return;
            if (picker.FileNames.Length > 1) { await OpenEvidenceBatchAsync(picker.FileNames); return; }
            path = picker.FileName;
            PathBox.Text = path;
        }
        if (!File.Exists(path)) { StatusLabel.Text = Controls.LocalizedText.Get("Str_Ui_ExistingLogPath"); return; }
        bool archive = Path.GetExtension(path).Equals(".evtx", StringComparison.OrdinalIgnoreCase);
        bool eventXml = Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        readCancellation = cancellation;
        OpenButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        Report(Controls.LocalizedText.Get(archive ? "Str_Ui_EventsReading" : "Str_Ui_LogReading"));
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
            ModeLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "Str_Mode_Imported");
            SearchBox.Clear();
            ShowEvidence();
            OnFindingsTab(this, new RoutedEventArgs());
            Report(Controls.LocalizedText.Format("Str_Ui_ImportedSource", loaded.Source));
        }
        catch (OperationCanceledException) { Report(Controls.LocalizedText.Get("Str_Ui_ReadStopped")); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Get("Str_Shortcuts_OpenEvidence"), error.Message)); }
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
        SetOutputOpen(true);
        EvidenceTitle.Text = evidence.Source == "Synthetic demonstration" ? Controls.LocalizedText.Get("Str_Text_Evidence") : Path.GetFileName(evidence.Source);
        if (historicalWorkflows.TryGetValue(evidence, out var historical))
        {
            ModeLabel.Text = Controls.LocalizedText.Get("Str_Mode_Historical");
            EvidenceBox.Text = evidence.Excerpt(SearchBox.Text, out int historicalMatches);
            CoverageLabel.Text = Controls.LocalizedText.Format("Str_Ui_HistoricalCoverage", LocalizedStatus(historical.RecordedStatus), historicalMatches);
            SourceInfo.Text = historical.Source + "\n\n" + WorkflowJournalSnapshot.Scope;
            shownReport = null;
            FindingCards.Children.Clear();
            FindingsSummary.Text = Controls.LocalizedText.Get("Str_Ui_SavedWorkflow") + "\n" + WorkflowJournalSnapshot.Scope;
            return;
        }
        SourceInfo.Text = evidence.Source + "\n\n" + Controls.LocalizedText.Format("Str_Ui_LoadedLines", evidence.Lines.Length) + "\n" +
            Controls.LocalizedText.Get(evidence.Truncated ? "Str_Ui_SourcePartial" : "Str_Ui_SourceComplete") + "\n\n" +
            Controls.LocalizedText.Get("Str_RecordedEvidence");
        var investigation = reports.GetValue(evidence, item => Investigation.Analyze(item));
        var analysis = investigation.Codes;
        SourceInfo.Text += "\n\n" + Controls.LocalizedText.Format("Str_Ui_CodeObservations", analysis.Observations.Count) + "\n" +
            string.Join("\n", analysis.Observations.Select(code => Controls.LocalizedText.Format("Str_Ui_CodeOccurrence", code.Code, code.Occurrences, code.Evidence[0].LineNumber))) +
            (analysis.ObservationsTruncated ? "\n" + Controls.LocalizedText.Get("Str_Ui_AnalysisLimited") : "") + "\n\n" + investigation.Format + "\n" +
            Controls.LocalizedText.Format("Str_Ui_EvidenceCounts", investigation.ParsedLines, investigation.LinesAnalyzed) + "\n" + investigation.Limitation;
        RefreshFindings(investigation);
        EvidenceBox.Text = evidence.Excerpt(SearchBox.Text, out int matches);
        CoverageLabel.Text = Controls.LocalizedText.Format("Str_Ui_EvidenceCoverage", matches, investigation.Findings.Count) +
            (investigation.Partial ? " " + Controls.LocalizedText.Get("Str_Ui_PartialEvidence") : "");
        if (workflowSummaries.TryGetValue(evidence, out var workflow))
        {
            CoverageLabel.Text = Controls.LocalizedText.Format("Str_Output_WorkflowCoverage", LocalizedStatus(workflow.Status), workflow.Checks.Count(check => check.Name != NativeCapabilityCheckName),workflow.Repairs.Count, matches.ToString("N0"));
            SourceInfo.Text = $"{evidence.Source}\n\nWorkflow: {workflow.Kind}\nOutcome: {workflow.Status}\nJournal: {workflow.JournalPath}\n\n" +
                Controls.LocalizedText.Get("Str_Ui_RecordedWorkflow");
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
        Report(Controls.LocalizedText.Get("Str_Ui_StopRequested"));
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
        if (HandleZoomKey(e.Key, Keyboard.Modifiers)) { e.Handled = true; return; }   // Shell/Workbench.cs
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
