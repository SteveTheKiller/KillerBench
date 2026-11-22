using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KillerBench.Services;

internal static class ShortcutCommandChecks
{
    internal static void Run(KillerBench.Shell.MainWindow window)
    {
        (RoutedUICommand Command, Key Key, ModifierKeys Modifiers, string Control)[] actions =
        [
            (WorkbenchCommands.OpenCapture, Key.O, ModifierKeys.Control | ModifierKeys.Shift, "OpenCaptureButton"),
            (WorkbenchCommands.SaveCapture, Key.S, ModifierKeys.Control, "SaveCaptureButton"),
            (WorkbenchCommands.ExportReport, Key.E, ModifierKeys.Control, "ExportReportButton"),
            (WorkbenchCommands.ExportActivity, Key.E, ModifierKeys.Control | ModifierKeys.Shift, "ExportActivityButton"),
            (WorkbenchCommands.FindEvidence, Key.F, ModifierKeys.Control, "SearchBox"),
            (WorkbenchCommands.SearchWorkspace, Key.K, ModifierKeys.Control, "TitleSearchBox")
        ];
        foreach (var action in actions)
        {
            Require(window.InputBindings.OfType<KeyBinding>().Count(binding => binding.Command == action.Command &&
                binding.Key == action.Key && binding.Modifiers == action.Modifiers) == 1,
                "Public command is bound to its documented chord: " + action.Control);
            // The capture actions are tiles on the Session sources card, so their availability lives on the window.
            if (action.Control is "OpenCaptureButton" or "SaveCaptureButton")
            {
                bool open = action.Control == "OpenCaptureButton";
                bool was = open ? window.OpenCaptureEnabled : window.SaveCaptureEnabled;
                try
                {
                    if (open) window.OpenCaptureEnabled = false; else window.SaveCaptureEnabled = false;
                    Require(!action.Command.CanExecute(null, window), "Disabled action rejects shortcut: " + action.Control);
                }
                finally { if (open) window.OpenCaptureEnabled = was; else window.SaveCaptureEnabled = was; }
                continue;
            }
            var control = (UIElement)window.FindName(action.Control);
            bool enabled = control.IsEnabled;
            try
            {
                control.IsEnabled = false;
                Require(!action.Command.CanExecute(null, window), "Disabled action rejects shortcut: " + action.Control);
            }
            finally { control.IsEnabled = enabled; }
        }
        var overlay = (Border)window.FindName("ShortcutsOverlay");
        var previous = overlay.Visibility;
        bool wasTerminalOpen = ((FrameworkElement)window.FindName("TerminalPanel")).Visibility == Visibility.Visible;
        bool wasRawView = ((FrameworkElement)window.FindName("ConsolePane")).Visibility == Visibility.Visible;
        bool wasMapView = ((FrameworkElement)window.FindName("KsMapHost")).Visibility == Visibility.Visible;
        string? savedMapPreference = Preferences.Get("ShortcutsMapView");
        try
        {
            overlay.Visibility = Visibility.Visible;
            Require(actions.All(action => !action.Command.CanExecute(null, window)), "Overlay blocks workspace shortcut dispatch");
            overlay.Visibility = Visibility.Collapsed;
            var search = (TextBox)window.FindName("SearchBox");
            Require(WorkbenchCommands.FindEvidence.CanExecute(null, window), "Evidence search shortcut is available");
            WorkbenchCommands.FindEvidence.Execute(null, window);
            window.UpdateLayout();
            Require(((FrameworkElement)window.FindName("ConsolePane")).Visibility == Visibility.Visible && search.IsVisible,
                "Evidence-search public command opens the raw-output search surface");
            ((Button)window.FindName("ShortcutButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((Button)window.FindName("KsViewKeyboard")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Button CtrlShiftTab() => ((StackPanel)window.FindName("KsLayerTabs")).Children.OfType<Button>().Single(tab => Equals(tab.Content, "CTRL+SHIFT"));
            CtrlShiftTab().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Require(CtrlShiftTab().Tag?.ToString() == "on",
                "Ctrl+Shift has its own keyboard-map layer");
        }
        finally
        {
            ((Button)window.FindName(wasMapView ? "KsViewKeyboard" : "KsViewList"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Preferences.Set("ShortcutsMapView", savedMapPreference ?? "0");
            Preferences.Save();
            ((Button)window.FindName(wasRawView ? "ConsoleViewButton" : "ReportViewButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if ((((FrameworkElement)window.FindName("TerminalPanel")).Visibility == Visibility.Visible) != wasTerminalOpen)
                window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.F2) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            overlay.Visibility = previous;
            window.UpdateLayout();
        }
        Console.WriteLine("PASS: 6 public shortcut commands, 6 disabled-action guards, overlay guard, evidence search and Ctrl+Shift map layer.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
