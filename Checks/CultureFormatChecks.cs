using System.Globalization;
using KillerBench.Engine;
using KillerBench.Engine.Windows.Primitives;

internal static class CultureFormatChecks
{
    private static readonly string[] Cultures = ["th-TH", "fi-FI", "ar-SA", "de-DE"];

    internal static void Run()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            foreach (string name in Cultures)
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                ReportTimestamps(name);
            }
            foreach (string name in new[] { "de-DE", "fi-FI", "en-US" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                DiskBusySamples(name);
                ShadowStorageSizes(name);
            }
        }
        finally { CultureInfo.CurrentCulture = original; }
        Console.WriteLine("PASS: report timestamps and native tool numbers do not depend on the Windows region.");
    }

    private static void ReportTimestamps(string culture)
    {
        var started = new DateTimeOffset(2026, 9, 14, 14, 30, 5, TimeSpan.Zero);
        var result = new WorkflowResult(WorkflowKind.SystemFiles, "FIXTURE-PC", started, "Needs attention",
            [new("Restart", CheckState.Clear, "No restart is pending.")], [], @"C:\Users\fixture\private\journal.json");
        string expected = "2026-09-14 14:30:05 +00:00";
        Require(WorkflowReportExport.BuildText(result, WorkflowReportScope.Diagnostics).Contains("Started: " + expected, StringComparison.Ordinal),
            "Workflow text report writes the Gregorian invariant timestamp under " + culture);
        Require(WorkflowReportExport.BuildHtml(result, WorkflowReportScope.Diagnostics).Contains("</b> " + expected, StringComparison.Ordinal),
            "Workflow HTML report writes the Gregorian invariant timestamp under " + culture);

        var scenario = NativeValidationCatalog.Find("windows.system-files")!.Scenarios.Single(item => item.Kind == NativeValidationScenarioKind.Healthy);
        var record = new NativeValidationRecord(NativeValidationEvidence.SchemaVersion, scenario.Id, "windows.system-files", 1,
            NativeValidationOutcome.Passed, "Windows 11 Pro", "24H2", "26100.1", "x64", "1.0.0", started,
            [.. scenario.RequiredArtifacts.Select((kind, index) => new NativeValidationArtifact(kind, "artifact-" + index + ".json", new string('a', 64)))], "note");
        var coverage = NativeValidationEvidence.Summarize([record]);
        Require(NativeValidationReport.BuildText(coverage).Contains("Recorded: " + expected, StringComparison.Ordinal),
            "Validation text report writes the Gregorian invariant timestamp under " + culture);
        Require(NativeValidationReport.BuildHtml(coverage, "dark", "orange").Contains(">" + expected + "<", StringComparison.Ordinal),
            "Validation HTML report writes the Gregorian invariant timestamp under " + culture);
    }

    private static void DiskBusySamples(string culture)
    {
        foreach (string sample in new[] { "0,123456", "0.123456" })
        {
            string output = "\"(PDH-CSV 4.0)\",\"Disk Time\"\n\"11/02/2026 10:00:00.123\",\"" + sample + "\"\n";
            var result = WindowsDiskBusyProbe.Interpret(new ToolResult(0, output, "", false));
            bool expectedOk = sample.Contains('.') || culture != "en-US";
            Require(result.Complete == expectedOk, "Disk busy sample '" + sample + "' recognition under " + culture + " was " + result.Complete);
            if (expectedOk) Require(result.Value == "0.1", "Disk busy sample '" + sample + "' reads as 0.1 under " + culture + ", got " + result.Value);
        }
    }

    private static void ShadowStorageSizes(string culture)
    {
        foreach (string used in new[] { "1,5 GB", "1.5 GB" })
        {
            string output = "Shadow Copy Storage association\n   Used Shadow Copy Storage space: " + used + " (75%)\n   Maximum Shadow Copy Storage space: 2 GB (100%)\n";
            var result = WindowsShadowStorageProbe.Interpret(new ToolResult(0, output, "", false));
            bool expectedOk = used.Contains('.') || culture != "en-US";
            Require(result.Complete == expectedOk, "Shadow storage size '" + used + "' recognition under " + culture + " was " + result.Complete);
            if (expectedOk) Require(result.Value == "75", "Shadow storage size '" + used + "' reads as 75% under " + culture + ", got " + result.Value);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
