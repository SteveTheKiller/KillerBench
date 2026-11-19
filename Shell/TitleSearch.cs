using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KillerBench.Controls;
using KillerBench.Engine;
using KillerBench.Services;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private sealed record SearchDestination(string Group, string Title, string Detail,
        ToolCategory? Area = null, WorkflowDefinition? Tool = null, TextEvidence? Source = null)
    {
        // Area art for the row: the area itself, a tool's category, or the evidence figure for text sources.
        public string ArtId { get; init; } = Area?.Id ?? "Diagnostics";
    }

    private readonly DispatcherTimer titleSearchDelay = new() { Interval = TimeSpan.FromMilliseconds(160) };
    private CancellationTokenSource? titleSearchCancellation;
    private bool titleSearchReady;
    private int titleSearchVersion;

    private bool CanSearchWorkspace => !App.ScreenshotMode && WorkspaceNavigation.IsEnabled &&
        readCancellation is null && !workflowOutputActive &&
        AboutOverlay.Visibility != Visibility.Visible && ShortcutsOverlay.Visibility != Visibility.Visible;

    private void InitializeTitleSearch()
    {
        titleSearchReady = true;
        titleSearchDelay.Tick += async (_, _) => { titleSearchDelay.Stop(); await SearchWorkspaceAsync(); };
        BindAction(WorkbenchCommands.SearchWorkspace, "Str_Search_All", TitleSearchBox,
            (_, _) => { TitleSearchBox.Focus(); TitleSearchBox.SelectAll(); QueueTitleSearch(); });
        WorkspaceNavigation.IsEnabledChanged += (_, _) =>
        {
            TitleSearchBox.IsEnabled = WorkspaceNavigation.IsEnabled && !App.ScreenshotMode;
            if (!TitleSearchBox.IsEnabled) CloseTitleSearch();
        };
        SizeChanged += (_, _) => ReflowCaption();
        LogoBar.SizeChanged += (_, _) => ReflowCaption();
        CaptionButtons.SizeChanged += (_, _) => ReflowCaption();
        Loaded += (_, _) => ReflowCaption();
        Deactivated += (_, _) => CloseTitleSearch();
        Closed += (_, _) => CloseTitleSearch();
        AboutOverlay.IsVisibleChanged += (_, _) => { if (AboutOverlay.IsVisible) CloseTitleSearch(); };
        ShortcutsOverlay.IsVisibleChanged += (_, _) => { if (ShortcutsOverlay.IsVisible) CloseTitleSearch(); };
        ReflowCaption();
    }

    private void ReflowCaption()
    {
        if (!titleSearchReady || !double.IsFinite(ActualWidth) || ActualWidth < 1) return;
        TitleSearchHost.Width = Math.Clamp(ActualWidth * .21, 150, 240);
        double budget = Math.Max(184, ActualWidth - LogoBar.ActualWidth - CaptionButtons.ActualWidth - TitleSearchHost.Width - 76);
        WorkspaceNavigation.MaxWidth = budget;
        RestoreToolbarCaptions();
        CompactWorkspaceToolbarButtons();
        // The shared engine sheds captions only when necessary, retaining every destination.
        ShedToolbarCaptions(() =>
        {
            double width = 0;
            foreach (var (button, _, _) in ToolbarItems())
            {
                button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                width += button.DesiredSize.Width + 4;
            }
            return width <= budget;
        });
        TitleSearchCard.Width = Math.Min(464, Math.Max(300, ActualWidth - 44));
        CompactWorkspaceToolbarButtons();
    }

    private void OnTitleSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (TitleSearchHint is not null)
            TitleSearchHint.Visibility = TitleSearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        QueueTitleSearch();
    }

    private void OnTitleSearchFocus(object sender, KeyboardFocusChangedEventArgs e) => QueueTitleSearch();

    private void QueueTitleSearch()
    {
        if (!titleSearchReady) return;
        titleSearchVersion++;
        titleSearchCancellation?.Cancel();
        titleSearchDelay.Stop();
        TitleSearchResults.ItemsSource = null;
        if (!CanSearchWorkspace) { CloseTitleSearch(); return; }
        TitleSearchStatus.Text = LocalizedText.Get("Str_Search_Hint");
        if (TitleSearchBox.Text.Trim().Length == 0) { TitleSearchPopup.IsOpen = false; return; }
        titleSearchDelay.Start();
    }

    private async Task SearchWorkspaceAsync()
    {
        if (!CanSearchWorkspace) return;
        string query = TitleSearchBox.Text.Trim();
        if (query.Length == 0) return;
        int version = titleSearchVersion;
        using var cancellation = new CancellationTokenSource();
        titleSearchCancellation = cancellation;
        const int limit = 12;
        bool Matches(string value) => value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        var results = ToolCategories.Where(area => Matches(area.Title) || Matches(area.Description))
            .Take(limit).Select(area => new SearchDestination(LocalizedText.Get("Str_Search_Areas"), area.Title, area.Description, Area: area)).ToList();
        var tools = WorkflowCatalog.All.Where(tool => Matches(LocalizedText.Workflow(tool.Kind)))
            .Take(limit + 1).ToArray();
        results.AddRange(tools.Take(limit).Select(tool => new SearchDestination(LocalizedText.Get("Str_Shell_Tools"),
            LocalizedText.Workflow(tool.Kind), CategoryOf(tool.Kind).Title, Tool: tool) { ArtId = CategoryOf(tool.Kind).Id }));
        var snapshot = sources.ToArray();
        try
        {
            var evidenceHits = await Task.Run(() =>
            {
                var found = new List<TextEvidence>();
                foreach (var source in snapshot)
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    if (Matches(Path.GetFileName(source.Source))) found.Add(source);
                    else
                    {
                        for (int index = 0; index < source.Lines.Length; index++)
                        {
                            if (index % 256 == 0) cancellation.Token.ThrowIfCancellationRequested();
                            if (!Matches(source.Lines[index])) continue;
                            found.Add(source);
                            break;
                        }
                    }
                    if (found.Count > limit) break;
                }
                return found;
            }, cancellation.Token);
            if (version != titleSearchVersion || !CanSearchWorkspace) return;
            results.AddRange(evidenceHits.Take(limit).Select(source => new SearchDestination(LocalizedText.Get("Str_Text_Evidence"),
                Path.GetFileName(source.Source), LocalizedText.Format("Str_Ui_LoadedLines", source.Lines.Length), Source: source)));
            var view = new ListCollectionView(results);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SearchDestination.Group)));
            TitleSearchResults.ItemsSource = view;
            TitleSearchStatus.Text = results.Count == 0 ? LocalizedText.Get("Str_Search_NoMatches")
                : tools.Length > limit || evidenceHits.Count > limit ? LocalizedText.Format("Str_Search_Limit", limit) : "";
            TitleSearchPopup.IsOpen = true;
        }
        catch (OperationCanceledException) { }
        finally { if (ReferenceEquals(titleSearchCancellation, cancellation)) titleSearchCancellation = null; }
    }

    private void OpenSearchDestination()
    {
        if (!CanSearchWorkspace || TitleSearchResults.SelectedItem is not SearchDestination selected) return;
        string query = TitleSearchBox.Text.Trim();
        CloseTitleSearch();
        MainAutoRepair.IsChecked = false;
        if (selected.Area is { } area)
        {
            WorkspaceNavigation.SelectedIndex = 0;
            SystemAreas.SelectedItem = area;
            ShowAreaEvidence();
            ReviewAreaButton.Focus();
        }
        else if (selected.Tool is { } tool)
        {
            ShowToolsWorkspace();
            MainWorkflow.SelectedItem = tool;
            SynchronizeNavigation();
            CategoryWorkflow.Focus();
        }
        else if (selected.Source is { } source && sources.Contains(source))
        {
            WorkspaceNavigation.SelectedIndex = 2;
            SourcesList.SelectedIndex = sources.IndexOf(source);
            OnSourceSelected(this, new SelectionChangedEventArgs(Selector.SelectionChangedEvent, Array.Empty<object>(), Array.Empty<object>()));
            SearchBox.Text = source.Lines.Any(line => line.Contains(query, StringComparison.CurrentCultureIgnoreCase)) ? query : "";
            ShowEvidence();
            ShowRawOutput();
            EvidenceBox.Focus();
        }
    }

    private void OnTitleSearchKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { CloseTitleSearch(); OverviewButton.Focus(); e.Handled = true; }
        else if (e.Key is Key.Down or Key.Enter && TitleSearchResults.Items.Count > 0)
        {
            TitleSearchResults.SelectedIndex = 0;
            if (e.Key == Key.Enter) OpenSearchDestination();
            else { TitleSearchResults.Focus(); (TitleSearchResults.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus(); }
            e.Handled = true;
        }
    }

    private void OnTitleSearchResultKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { OpenSearchDestination(); e.Handled = true; }
        else if (e.Key == Key.Escape) { CloseTitleSearch(); TitleSearchBox.Focus(); e.Handled = true; }
    }

    private void OnTitleSearchResultClick(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? current = e.OriginalSource as DependencyObject;
        while (current is not null && current is not ListBoxItem)
            current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        if (current is ListBoxItem) { OpenSearchDestination(); e.Handled = true; }
    }

    private void CloseTitleSearch()
    {
        titleSearchVersion++;
        titleSearchDelay.Stop();
        titleSearchCancellation?.Cancel();
        if (TitleSearchPopup is not null) TitleSearchPopup.IsOpen = false;
    }
}
