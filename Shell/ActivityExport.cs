using System.IO;
using System.Text;
using System.Windows;
using KillerBench.Engine;
using Microsoft.Win32;

namespace KillerBench;

public partial class MainWindow
{
    private async void OnExportActivity(object sender, RoutedEventArgs e)
    {
        var snapshot = activity.ToArray();
        var picker = new SaveFileDialog
        {
            Title = "Export retained session activity (up to 150 entries)",
            Filter = "Text file (*.txt)|*.txt|CSV file (*.csv)|*.csv|HTML page (*.html)|*.html",
            DefaultExt = ".txt", AddExtension = true, OverwritePrompt = true,
            FileName = "KillerBench-activity-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")
        };
        if (picker.ShowDialog(this) != true) return;
        ExportActivityButton.IsEnabled = false;
        try
        {
            var format = picker.FilterIndex switch { 2 => SessionActivityFormat.Csv, 3 => SessionActivityFormat.Html, _ => SessionActivityFormat.Text };
            await File.WriteAllTextAsync(picker.FileName, SessionActivityExport.Format(snapshot, format), new UTF8Encoding(true));
            Report($"Exported {snapshot.Length} retained activity entries: {picker.FileName}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report("Activity export failed: " + error.Message); }
        finally { ExportActivityButton.IsEnabled = true; }
    }
}
