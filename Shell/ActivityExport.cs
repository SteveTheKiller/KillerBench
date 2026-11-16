using System.Globalization;
using System.IO;
using System.Windows;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private async void OnExportActivity(object sender, RoutedEventArgs e)
    {
        var snapshot = activity.ToArray();
        var export = new Services.DesktopReportExport();
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Save)
        {
            Title = Controls.LocalizedText.Get("Str_ExportActivityHint"),
            Filter = "TXT (*.txt)|*.txt|CSV (*.csv)|*.csv|HTML (*.html)|*.html",
            DefaultExt = ".txt", AddExtension = true, OverwritePrompt = true,
            FileName = "KillerBench-activity-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture),
            FileExistsProbe = export.ObserveDestinationForPicker
        };
        if (picker.ShowDialog(this) != true) return;
        if (Services.DesktopReportExport.ConflictsWithEvidence(picker.FileName, sources.Select(source => source.Source).Append(capturePath)))
        { Report(Controls.LocalizedText.Get("Str_Ui_ExportSeparateDest")); return; }
        ExportActivityButton.IsEnabled = false;
        try
        {
            var format = picker.FilterIndex switch { 2 => SessionActivityFormat.Csv, 3 => SessionActivityFormat.Html, _ => SessionActivityFormat.Text };
            await export.WriteAsync(picker.FileName, SessionActivityExport.Format(snapshot, format), emitBom: true);
            Report($"Exported {snapshot.Length} retained activity entries: {picker.FileName}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Get("Str_ExportActivityHint"), Services.DesktopReportExport.DescribeFailure(error))); }
        finally { ExportActivityButton.IsEnabled = true; }
    }
}
