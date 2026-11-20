using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using KillerFamily.Controls;
using KillerBench.Controls;

namespace KillerBench.Shell;

// The Session sources and Knowledge pack buttons open KillerUI tile cards (CardBuilder), three tiles
// across with related tiles on their own row, built each time they open so every tile shows its
// current state and its shortcut. None of these tiles runs a diagnostic or a repair: Workflows and
// Repairs only open the Tools view, where a run still needs its own selection and consent.
public partial class MainWindow
{
    private bool openCaptureEnabled = true;
    private bool saveCaptureEnabled;

    /// <summary>Whether Open capture can run now: its tile, Ctrl+Shift+O, and the Overview and History buttons.</summary>
    internal bool OpenCaptureEnabled
    {
        get => openCaptureEnabled;
        set { openCaptureEnabled = value; CommandManager.InvalidateRequerySuggested(); }
    }

    /// <summary>Whether Save capture can run now: its tile and Ctrl+S.</summary>
    internal bool SaveCaptureEnabled
    {
        get => saveCaptureEnabled;
        set { saveCaptureEnabled = value; CommandManager.InvalidateRequerySuggested(); }
    }

    private static string? TileKeys(string labelKey) =>
        ShortcutBindings.FirstOrDefault(binding => binding.LabelKey == labelKey).Keys;

    private CardTile WorkbenchTile(string glyph, string labelKey, Action run, bool enabled = true, string? captionKey = null)
    {
        string? keys = TileKeys(labelKey);
        string label = LocalizedText.Get(labelKey);
        return new CardTile(glyph, LocalizedText.Get(captionKey ?? labelKey), keys, run, Enabled: enabled)
        { Hint = CardBuilder.Tip(label, keys) };
    }

    /// <summary>The Session sources card: open and save a capture, then the workflow journal and the
    /// sample, then the two Tools views.</summary>
    internal ContextMenu BuildSessionSourcesCard()
    {
        var card = CardBuilder.Menu(300);
        string title = LocalizedText.Get("Str_Session_Sources");
        card.Items.Add(CardBuilder.Header(title));
        RoutedEventArgs Args() => new(Button.ClickEvent, this);
        var capture = new[]
        {
            WorkbenchTile("", "Str_OpenCapture", () => OnOpenCapture(this, Args()), OpenCaptureEnabled),
            WorkbenchTile("", "Str_SaveCapture", () => OnSaveCapture(this, Args()), SaveCaptureEnabled),
        };
        var session = new[]
        {
            WorkbenchTile("", "Str_Advanced_OpenWorkflow", () => OnOpenWorkflowJournal(this, Args()), readCancellation is null),
            WorkbenchTile("", "Str_Session_LoadSample", () => OnSample(this, Args()), readCancellation is null),
        };
        var tools = new[]
        {
            WorkbenchTile("", "Str_Advanced_Workflows", () => OnWorkflows(this, Args()), !repairRunning),
            WorkbenchTile("", "Str_Advanced_Repairs", () => OnRepairs(this, Args()), !repairRunning),
        };
        card.Items.Add(CardBuilder.Slot(CardBuilder.TileGroups(card, title, new[] { capture, session, tools }), new Thickness(0)));
        return card;
    }

    /// <summary>The Knowledge pack card: look up a code, search the text, compare two revisions.</summary>
    internal ContextMenu BuildKnowledgeCard()
    {
        var card = CardBuilder.Menu(300);
        string title = LocalizedText.Get("Str_Knowledge_PackHeader");
        card.Items.Add(CardBuilder.Header(title));
        RoutedEventArgs Args() => new(Button.ClickEvent, this);
        bool idle = readCancellation is null;
        var pack = new[]
        {
            WorkbenchTile("", "Str_Knowledge_Lookup", () => OnKnowledgeLookup(KnowledgeSearchMode.Code, Args()), idle, "Str_Knowledge_LookupTile"),
            WorkbenchTile("", "Str_Knowledge_Search", () => OnKnowledgeLookup(KnowledgeSearchMode.Text, Args()), idle, "Str_Knowledge_SearchTile"),
            WorkbenchTile("", "Str_Knowledge_Compare", () => OnKnowledgeCompare(this, Args()), idle, "Str_Knowledge_CompareTile"),
        };
        card.Items.Add(CardBuilder.Slot(CardBuilder.TileGroups(card, title, new[] { pack }), new Thickness(0)));
        return card;
    }

    /// <summary>The tile on a workbench card whose action is labelKey, for the desktop checks.</summary>
    internal static Button FindCardTile(ContextMenu card, string labelKey)
    {
        string hint = LocalizedText.Get(labelKey);
        return Descendants(card).OfType<Button>().First(button =>
            button.ToolTip is string tip && (tip == hint || tip.StartsWith(hint + " (", StringComparison.Ordinal)));

        static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            yield return root;
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
                foreach (var d in Descendants(child)) yield return d;
        }
    }

    private void OnSessionSourcesMenu(object sender, RoutedEventArgs e) => OpenWorkbenchCard((FrameworkElement)sender, BuildSessionSourcesCard());

    private void OnKnowledgeMenu(object sender, RoutedEventArgs e) => OpenWorkbenchCard((FrameworkElement)sender, BuildKnowledgeCard());

    private static void OpenWorkbenchCard(FrameworkElement button, ContextMenu card)
    {
        card.PlacementTarget = button;
        card.Placement = PlacementMode.Bottom;
        card.Opened += (_, _) =>
        {
            if (Keyboard.FocusedElement is not DependencyObject focus || !card.IsAncestorOf(focus))
                card.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        };
        card.IsOpen = true;
    }

    private enum KnowledgeSearchMode { Code, Text }
}
