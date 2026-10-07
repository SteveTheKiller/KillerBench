using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KillerBench.Controls;
using KillerBench.Services;

internal static class PickerDialogChecks
{
    public static void Run()
    {
        IsolatedDesktop.Verify();
        var owner = Application.Current.Windows.OfType<KillerBench.MainWindow>()
            .FirstOrDefault(window => window.IsVisible)
            ?? throw new InvalidOperationException("The picker check needs the isolated KillerBench owner window.");

        string scratch = Path.Combine(Path.GetTempPath(), "KillerBench-picker-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        string first = Path.Combine(scratch, "one.txt");
        string second = Path.Combine(scratch, "two.txt");
        string existing = Path.Combine(scratch, "existing.csv");
        string child = Path.Combine(scratch, "child");
        File.WriteAllText(first, "one");
        File.WriteAllText(second, "two");
        File.WriteAllText(existing, "original");
        Directory.CreateDirectory(child);

        ThemeManager.Apply(Theme.Dark);
        var canceledOpen = new FileDialog(FileDialogMode.Open)
        {
            Title = "Open fixture",
            InitialDirectory = scratch,
            Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
            FilterIndex = 2,
            Multiselect = true
        };
        bool? openResult = Show(owner, canceledOpen, dialog =>
        {
            var filter = (ComboBox)dialog.FindName("FilterCombo");
            var list = (ListBox)dialog.FindName("FileList");
            Require(filter.SelectedIndex == 1 && dialog.FilterIndex == 2,
                "Open picker retains its selected filter");
            Require(list.SelectionMode == SelectionMode.Extended,
                "Open picker permits multiple file selection");
            var tree = (TreeView)dialog.FindName("FolderTreeCtl");
            var treeScroll = FindDescendant<ScrollViewer>(tree);
            Require(VirtualizingStackPanel.GetIsVirtualizing(tree) &&
                    VirtualizingStackPanel.GetVirtualizationMode(tree) == VirtualizationMode.Recycling &&
                    treeScroll?.CanContentScroll == true &&
                    ScrollViewer.GetCanContentScroll(list),
                "Picker lists stay virtualized when a folder contains many entries");
            Capture(dialog, Path.Combine(scratch, "open-dark.png"));
            ((Button)dialog.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
        Require(openResult != true && canceledOpen.FileNames.Length == 0,
            "Open picker cancel returns no files");

        var multiselect = new FileDialog(FileDialogMode.Open)
        {
            Title = "Open two fixtures",
            InitialDirectory = scratch,
            Filter = "Text files (*.txt)|*.txt",
            Multiselect = true
        };
        bool? multiResult = Show(owner, multiselect, dialog =>
        {
            var list = (ListBox)dialog.FindName("FileList");
            var files = list.Items.OfType<PickerEntry>().Where(item => !item.IsFolder).ToArray();
            Require(files.Length == 2, "Text filter presents exactly two fixture files");
            list.SelectedItems.Add(files[0]);
            list.SelectedItems.Add(files[1]);
            ((Button)dialog.FindName("AcceptButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
        Require(multiResult == true &&
                multiselect.FileNames.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    .SetEquals([first, second]),
            "Open picker returns both selected files");

        ThemeManager.Apply(Theme.Retro98);
        var folder = new FileDialog(FileDialogMode.Folder)
        {
            Title = "Choose fixture folder",
            InitialDirectory = scratch
        };
        bool? folderResult = Show(owner, folder, dialog =>
        {
            Require(((Grid)dialog.FindName("FileFields")).Visibility == Visibility.Collapsed,
                "Folder picker hides file name and filter fields");
            Require(dialog.Entries.All(item => item.IsFolder),
                "Folder picker does not present files as selectable entries");
            Require(((Button)dialog.FindName("AcceptButton")).IsDefault,
                "Folder picker accepts with Enter");
            Capture(dialog, Path.Combine(scratch, "folder-98se.png"));
            ((Button)dialog.FindName("AcceptButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
        Require(folderResult == true && string.Equals(folder.FileName, scratch, StringComparison.OrdinalIgnoreCase),
            "Folder picker returns the current folder");

        ThemeManager.Apply(Theme.Dark);
        bool overwritePromptSeen = false;
        var save = new FileDialog(FileDialogMode.Save)
        {
            Title = "Save fixture",
            InitialDirectory = scratch,
            FileName = "existing.csv",
            Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
            FilterIndex = 2,
            OverwritePrompt = true
        };
        try
        {
            bool? saveResult = Show(owner, save, dialog =>
            {
                Require(((ComboBox)dialog.FindName("FilterCombo")).SelectedIndex == 1,
                    "Save picker retains its selected filter");
                Capture(dialog, Path.Combine(scratch, "save-dark.png"));
                dialog.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var confirm = Application.Current.Windows.OfType<ConfirmDialog>()
                        .FirstOrDefault(window => window.IsVisible);
                    if (confirm is not null)
                    {
                        overwritePromptSeen = true;
                        ((Button)confirm.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    dialog.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (dialog.IsVisible) dialog.Close();
                    }), DispatcherPriority.Normal);
                }), DispatcherPriority.Normal);
                ((Button)dialog.FindName("AcceptButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            });
            Require(overwritePromptSeen && saveResult != true && File.ReadAllText(existing) == "original",
                "Canceling themed overwrite leaves the existing file untouched");
        }
        finally
        {
            ThemeManager.Apply(Theme.Dark);
        }

        Console.WriteLine("PASS: KillerUI Open, Save, and Folder picker behavior. Captures: " + scratch);
    }

    private static bool? Show(Window owner, FileDialog dialog, Action<FileDialog> inspect)
    {
        Exception? failure = null;
        var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        timeout.Tick += (_, _) =>
        {
            timeout.Stop();
            failure ??= new TimeoutException("The picker check did not close its modal dialog.");
            foreach (var confirm in Application.Current.Windows.OfType<ConfirmDialog>()
                         .Where(window => window.IsVisible).ToArray())
                confirm.Close();
            if (dialog.IsVisible) dialog.Close();
        };
        dialog.Loaded += (_, _) => dialog.Dispatcher.BeginInvoke(new Action(() =>
        {
            try { inspect(dialog); }
            catch (Exception error)
            {
                failure = error;
                foreach (var confirm in Application.Current.Windows.OfType<ConfirmDialog>()
                             .Where(window => window.IsVisible).ToArray())
                    confirm.Close();
                if (dialog.IsVisible) dialog.Close();
            }
        }), DispatcherPriority.Normal);
        timeout.Start();
        try
        {
            bool? result = dialog.ShowDialog(owner);
            if (failure is not null) throw new InvalidOperationException("Picker check failed.", failure);
            return result;
        }
        finally { timeout.Stop(); }
    }

    private static void Capture(FrameworkElement visual, string path)
    {
        if (visual is FileDialog dialog && dialog.FindName("RootBorder") is UIElement card)
        {
            card.BeginAnimation(UIElement.OpacityProperty, null);
            card.Opacity = 1;
        }
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth),
            (int)Math.Ceiling(visual.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var nested = FindDescendant<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }
}
