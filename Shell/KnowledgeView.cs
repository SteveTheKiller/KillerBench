using System.IO;
using System.Text.Json;
using System.Windows;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private async void OnKnowledgeCompare(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var beforePicker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Title = "Choose the current knowledge pack", Filter = "Knowledge JSON (*.json)|*.json", CheckFileExists = true };
        if (beforePicker.ShowDialog(this) != true) return;
        var afterPicker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Title = "Choose the candidate knowledge pack", Filter = "Knowledge JSON (*.json)|*.json", CheckFileExists = true };
        if (afterPicker.ShowDialog(this) != true) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        readCancellation = stop;
        SetCaptureBusy(true);
        SetScanLight("running", "Comparing local knowledge packs.");
        Report("Comparing current and candidate knowledge packs...");
        try
        {
            var comparison = await Task.Run(async () =>
            {
                var before = await KnowledgeCatalog.ReadFileAsync(beforePicker.FileName, stop.Token);
                var after = await KnowledgeCatalog.ReadFileAsync(afterPicker.FileName, stop.Token);
                return before.Compare(after, token: stop.Token);
            }, stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            var lines = new List<string>
            {
                "KNOWLEDGE / REVISION COMPARISON",
                "Current file: " + beforePicker.FileName,
                "Candidate file: " + afterPicker.FileName,
                $"Pack: {comparison.PackId} / revision {comparison.BeforeRevision} to {comparison.AfterRevision}",
                $"Added: {comparison.Added} / Removed: {comparison.Removed} / Changed: {comparison.Changed}",
                "Schema, publication or license changed: " + (comparison.MetadataChanged ? "Yes" : "No"),
                "Before metadata: " + JsonSerializer.Serialize(comparison.BeforeMetadata),
                "After metadata: " + JsonSerializer.Serialize(comparison.AfterMetadata),
                $"Showing {comparison.Changes.Count} of {comparison.Added + comparison.Removed + comparison.Changed} entry changes.",
                "Reference comparison only. Signatures, accuracy and update eligibility have not been verified. No pack installed.", ""
            };
            foreach (var change in comparison.Changes)
            {
                lines.Add(change.Kind);
                if (change.Before is not null) lines.Add("Before: " + JsonSerializer.Serialize(change.Before));
                if (change.After is not null) lines.Add("After: " + JsonSerializer.Serialize(change.After));
                lines.Add("");
            }
            RegisterSource(new TextEvidence("Pack comparison: " + comparison.PackId, [.. lines], comparison.Truncated));
            Report("Knowledge comparison ready. Source files preserved; no pack installed.");
            SetScanLight("incomplete", "Reference comparison only; machine health was not assessed.");
        }
        catch (OperationCanceledException)
        {
            Report("Knowledge comparison stopped or timed out.");
            SetScanLight("incomplete", "Knowledge comparison did not complete.");
        }
        catch (Exception error) when (error is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            Report("Knowledge comparison unavailable: " + error.Message);
            SetScanLight("incomplete", "Knowledge comparison did not complete.");
        }
        finally
        {
            readCancellation = null;
            SetCaptureBusy(false);
            if (closeAfterOperation) Close();
        }
    }

    private void OnWindowsCodeLookup(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string raw = KnowledgeCode.Text.Trim();
        bool hex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (!uint.TryParse(hex ? raw[2..] : raw,
            hex ? System.Globalization.NumberStyles.AllowHexSpecifier : System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out uint code))
        { Report("Enter a decimal or 0x hexadecimal Windows system code."); return; }
        var result = WindowsErrorReference.Lookup(code);
        var lines = new List<string> { "WINDOWS SYSTEM MESSAGE", "Code: " + code + " (0x" + code.ToString("X") + ")",
            result.Message ?? "No system message found for this code.", "Source: " + result.Source, result.Limitation };
        if (code <= int.MaxValue && WindowsInstallerErrorCatalog.Find((int)code) is { } installer)
            lines.AddRange(["", "WINDOWS INSTALLER RETURN CODE", installer.Symbol, installer.Summary,
                "Next: " + installer.DiagnosticScope, "Successful outcome: " + (installer.Success ? "Yes" : "No"),
                "Restart required: " + (installer.RestartRequired ? "Yes" : "No"),
                "Source: " + WindowsInstallerErrorCatalog.SourceUrl,
                "Reference only. The code does not identify a failing action, prove current product state or authorize repair."]);
        if (WindowsUpdateErrorCatalog.Find(code) is { } update)
            lines.AddRange(["", "WINDOWS UPDATE RESULT CODE", update.Symbol, update.Meaning,
                "Next: " + update.DiagnosticScope, "Source: " + WindowsUpdateErrorCatalog.SourceUrl,
                "Reference only. The code does not identify the failing update, prove current update state or authorize repair."]);
        RegisterSource(new TextEvidence("Win32 code: " + code, [.. lines], false));
        Report(result.Message is null ? "No Windows system message found." : "Windows system reference loaded. No diagnostic verdict.");
        SetScanLight("incomplete", "Reference only; machine health was not assessed.");
    }

    private void OnDeviceCodeLookup(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (!DeviceProblemCodeCatalog.TryParse(KnowledgeCode.Text, out uint code))
        { Report("Enter an unsigned decimal or 0x hexadecimal Device Manager code."); return; }
        var entry = DeviceProblemCodeCatalog.Find(code);
        RegisterSource(new TextEvidence("Device Manager code: " + code,
            [
                "DEVICE MANAGER PROBLEM CODE",
                "Code: " + code + " (0x" + code.ToString("X") + ")",
                "Identity: " + entry.Symbol,
                entry.Summary,
                "Next: " + entry.NextAction,
                "Restart may resolve this reported state: " + (entry.RestartMayResolve ? "Yes" : "No"),
                "Reference only. Current device state was not inspected and no repair was selected."
            ], false));
        Report(entry.Symbol == "CM_PROB_UNKNOWN" ? "Unknown Device Manager code retained for review." :
            "Device Manager reference loaded. No diagnostic verdict.");
        SetScanLight("incomplete", "Reference only; machine health was not assessed.");
    }

    private async void OnKnowledgeLookup(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string context = KnowledgeContext.Text.Trim(), code = KnowledgeCode.Text.Trim();
        bool search = sender is System.Windows.Controls.Button { Tag: "search" };
        if (context.Length == 0 || code.Length == 0)
        { Report("Enter a product context and code or search text before opening a reference pack."); return; }
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Title = "Open local knowledge pack", Filter = "Knowledge JSON (*.json)|*.json", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            Report("Reading local knowledge reference...");
            var catalog = await Task.Run(() => KnowledgeCatalog.ReadFileAsync(picker.FileName, stop.Token));
            sessionKnowledgeCatalogs.AddOrReplace(catalog);
            if (search)
            {
                var matches = await Task.Run(() => catalog.Search(context, code, token: stop.Token));
                stop.Token.ThrowIfCancellationRequested();
                RegisterSource(FormatKnowledgeSearch(catalog, context, code, matches));
                Report($"Reference search: {matches.Matches.Count} of {matches.TotalMatches} matches displayed. {catalog.PackId} joined {sessionKnowledgeCatalogs.Count} session knowledge pack(s).");
                SetScanLight("incomplete", "Reference search only; machine health was not assessed.");
                return;
            }
            var entry = catalog.Find(context, code);
            var lines = new List<string> { "KNOWLEDGE / LOCAL REFERENCE", "Pack: " + catalog.PackId + " / revision " + catalog.Revision,
                "Published: " + catalog.PublishedOn, "License: " + catalog.License, "Context: " + context, "Code: " + code, "" };
            if (entry is null) lines.Add("No entry matches this context and code.");
            else lines.AddRange([entry.Title, entry.Explanation, "Applies to: " + entry.AppliesTo,
                "Source: " + entry.SourceUrl, "Verified on: " + entry.VerifiedOn.ToString()]);
            if (entry?.Driver is { } driver)
                lines.AddRange(["", "DRIVER REFERENCE / DECLARED APPLICABILITY",
                    "Hardware ID: " + driver.HardwareId, "Architecture: " + driver.Architecture,
                    "Windows product: " + driver.WindowsProduct,
                    "Build range: " + driver.MinimumBuild + " to " + driver.MaximumBuild,
                    "Vendor: " + driver.Vendor, "Version: " + driver.DriverVersion,
                    "Download reference: " + driver.DownloadUrl,
                    "Device match and package compatibility have not been checked. No download or installation performed."]);
            lines.Add("Local reference only. Publisher signature has not been verified. This match does not diagnose the machine or authorize a repair.");
            RegisterSource(new TextEvidence("Reference: " + context + " / " + code, [.. lines], false));
            Report(entry is null
                ? $"No contextual reference found. {catalog.PackId} joined {sessionKnowledgeCatalogs.Count} session knowledge pack(s)."
                : $"Reference loaded. {catalog.PackId} joined {sessionKnowledgeCatalogs.Count} session knowledge pack(s).");
            SetScanLight("incomplete", "Reference lookup only; machine health was not assessed.");
        }
        catch (Exception error) when (error is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or ArgumentException or OperationCanceledException)
        { Report("Knowledge lookup unavailable: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private static TextEvidence FormatKnowledgeSearch(KnowledgeCatalog catalog, string context, string query, KnowledgeSearchResult result)
    {
        var lines = new List<string> { "KNOWLEDGE / TEXT SEARCH", "Pack: " + catalog.PackId + " / revision " + catalog.Revision,
            "Published: " + catalog.PublishedOn, "License: " + catalog.License, "Context: " + context, "Search: " + query,
            $"Showing {result.Matches.Count} of {result.TotalMatches} matches.", "" };
        if (result.Truncated) lines.Add("Result limit reached. Narrow the search text to see other matches.");
        if (result.TotalMatches == 0) lines.Add("No reference text matches in this context.");
        foreach (var entry in result.Matches)
            lines.AddRange([entry.Code + " / " + entry.Title, entry.Explanation, "Applies to: " + entry.AppliesTo,
                "Source: " + entry.SourceUrl, "Verified on: " + entry.VerifiedOn, ""]);
        lines.Add("Local reference only. Publisher signature has not been verified. Text matches do not diagnose the machine or authorize repairs or driver installation.");
        return new("Reference search: " + context + " / " + query, [.. lines], result.Truncated);
    }
}
