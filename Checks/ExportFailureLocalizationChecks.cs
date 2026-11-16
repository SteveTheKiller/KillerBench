using System.IO;
using System.Windows;
using System.Text.RegularExpressions;
using KillerBench.Services;

internal static class ExportFailureLocalizationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var application = Application.Current ?? new Application();
        var german = new ResourceDictionary { Source = new Uri("/KillerBench;component/Strings/de-DE.xaml", UriKind.Relative) };
        application.Resources.MergedDictionaries.Add(german);
        try
        {
            string message = DesktopReportExport.DescribeFailure(new ReportPublicationException(ReportPublicationFailure.ChooseDestinationAgain));
            if (message != "Wählen Sie das Exportziel vor dem Speichern erneut aus.")
                throw new InvalidOperationException("Structured export failures must use German presentation without parsing English messages.");
        }
        finally { application.Resources.MergedDictionaries.Remove(german); }
        string[] locales = ["en-US", "bn", "cs-CZ", "de-DE", "es", "fr-FR", "hu-HU", "it-IT", "ja-JP", "kk-KZ", "nb-NO", "pl-PL", "pt-BR", "ru-RU", "tr-TR", "uk-UA", "vi-VN", "zh-CN", "zh-TW", "sl-SI"];
        (ReportPublicationFailure Reason, string Key)[] cases =
        [
            (ReportPublicationFailure.ChooseDestinationAgain, "Str_ExportFailure_ChooseAgain"),
            (ReportPublicationFailure.DestinationChanged, "Str_ExportFailure_Changed"),
            (ReportPublicationFailure.DestinationTooLarge, "Str_ExportFailure_TooLarge"),
            (ReportPublicationFailure.NotPublishedPriorRetained, "Str_ExportFailure_NotPublished"),
            (ReportPublicationFailure.SavedPriorRetained, "Str_ExportFailure_SavedPriorRetained")
        ];
        const string recoveryPath = "C:\\Fixture\\prior {0}, report.txt";
        foreach (string locale in locales)
        {
            var dictionary = new ResourceDictionary { Source = new Uri("/KillerBench;component/Strings/" + locale + ".xaml", UriKind.Relative) };
            application.Resources.MergedDictionaries.Add(dictionary);
            try
            {
                foreach (var item in cases)
                {
                    string format = (string)dictionary[item.Key];
                    bool hasPath = item.Reason is ReportPublicationFailure.NotPublishedPriorRetained or ReportPublicationFailure.SavedPriorRetained;
                    string[] tokens = Regex.Matches(format, @"\{\d+\}").Select(match => match.Value).ToArray();
                    Require(tokens.SequenceEqual(hasPath ? new[] { "{0}" } : []), locale + " export placeholder parity");
                    var error = new ReportPublicationException(item.Reason, hasPath ? recoveryPath : null);
                    string result = DesktopReportExport.DescribeFailure(error);
                    Require(result == (hasPath ? string.Format(format, recoveryPath) : format), locale + " structured error presentation");
                    Require(error.Saved == (item.Reason == ReportPublicationFailure.SavedPriorRetained), "Saved flag distinguishes successful publication with retained prior file");
                    if (hasPath) Require(result.Contains(recoveryPath, StringComparison.Ordinal), "Recovery path stays verbatim without parsing message prose");
                    if (locale != "en-US") Require(result != error.Message, locale + " must not silently use English exception prose");
                }
                Require(((string)dictionary["Str_ExportFailure_TooLarge"]).Contains("128 MiB", StringComparison.Ordinal), locale + " retains the exact technical bound");
                if (locale == "fr-FR") Require(DesktopReportExport.DescribeFailure(new ReportPublicationException(ReportPublicationFailure.ChooseDestinationAgain)) ==
                    "Sélectionnez à nouveau la destination d’exportation avant d’enregistrer.", "French export instruction retains its meaning");
            }
            finally { application.Resources.MergedDictionaries.Remove(dictionary); }
        }
        Require(DesktopReportExport.DescribeFailure(new IOException("Fixture OS detail")) == "Fixture OS detail", "Operating-system detail remains available to the localized wrapper");
        Directory.CreateDirectory(scratch);
        var export = new DesktopReportExport();
        IOException? failure = null;
        try { await export.WriteAsync(Path.Combine(scratch, "unconfirmed.txt"), "new report"); }
        catch (IOException error) { failure = error; }
        if (failure is not ReportPublicationException { Reason: ReportPublicationFailure.ChooseDestinationAgain, RecoveryPath: null, Saved: false })
            throw new InvalidOperationException("Unconfirmed export must expose a structured destination-choice failure.");
        string changed = Path.Combine(scratch, "changed.txt");
        await File.WriteAllTextAsync(changed, "confirmed file");
        export.ObserveDestination(changed);
        await File.WriteAllTextAsync(changed, "newer file");
        try { await export.WriteAsync(changed, "new report"); throw new InvalidOperationException("Changed destination accepted"); }
        catch (ReportPublicationException error) { Require(error.Reason == ReportPublicationFailure.DestinationChanged && !error.Saved && File.ReadAllText(changed) == "newer file", "Changed destination metadata and bytes"); }
        string large = Path.Combine(scratch, "oversized.txt");
        try
        {
            using (var file = File.Create(large)) file.SetLength(128L * 1024 * 1024 + 1);
            try { export.ObserveDestination(large); throw new InvalidOperationException("Oversized destination accepted"); }
            catch (ReportPublicationException error) { Require(error.Reason == ReportPublicationFailure.DestinationTooLarge && !error.Saved, "Oversized destination metadata"); }
            application.Resources.MergedDictionaries.Add(german);
            try
            {
                try { export.ObserveDestinationForPicker(large); throw new InvalidOperationException("Oversized picker destination accepted"); }
                catch (IOException error) { Require(error.InnerException is ReportPublicationException { Reason: ReportPublicationFailure.DestinationTooLarge } &&
                    error.Message == (string)german["Str_ExportFailure_TooLarge"], "Picker boundary localizes structured oversized errors"); }
            }
            finally { application.Resources.MergedDictionaries.Remove(german); }
        }
        finally { File.Delete(large); }
        Console.WriteLine("PASS: 100 export error presentations across 20 locales, German/French meanings, path/token parity, 3 actual structured destination failures and localized picker rejection.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
