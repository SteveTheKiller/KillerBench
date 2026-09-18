using KillerBench.Engine;

internal static class NativeValidationReportChecks
{
    internal static void Run()
    {
        var scenario = NativeValidationCatalog.Find("windows.system-files")!.Scenarios.Single(item => item.Kind == NativeValidationScenarioKind.Healthy);
        var record = new NativeValidationRecord(NativeValidationEvidence.SchemaVersion, scenario.Id, "windows.system-files", 1,
            NativeValidationOutcome.Passed, "Windows 11 Pro", "24H2", "26100.1", "x64", "0.1.0", DateTimeOffset.UtcNow.AddMinutes(-1),
            scenario.RequiredArtifacts.Select((kind, index) => new NativeValidationArtifact(kind, "artifact-" + index + ".json", new string('a', 64))).ToArray(),
            "<script>must not render</script>");
        var coverage = NativeValidationEvidence.Summarize([record]);
        string html = NativeValidationReport.BuildHtml(coverage, "dark", "orange");
        string text = NativeValidationReport.BuildText(coverage);
        Require(html.Contains("Native validation coverage", StringComparison.Ordinal) && html.Contains("data-status='Passed'", StringComparison.Ordinal),
            "Native validation HTML includes summary and recorded status");
        Require(html.Contains("data-status='Missing'", StringComparison.Ordinal) && html.Contains("All statuses", StringComparison.Ordinal),
            "Native validation HTML exposes missing scenarios and filtering");
        Require(html.Contains("theme-se98", StringComparison.Ordinal) && html.Contains("theme-mourning", StringComparison.Ordinal),
            "Native validation HTML includes all report theme families");
        Require(!html.Contains("<script>must not render</script>", StringComparison.Ordinal),
            "Native validation HTML never emits record notes as executable markup");
        Require(text.Contains("Passed: 1", StringComparison.Ordinal) && text.Contains("Missing: " + (coverage.InstalledScenarios - 1), StringComparison.Ordinal),
            "Native validation text reports exact current coverage");
        Console.WriteLine("PASS: native validation coverage exports readable themed HTML and text without widening claims.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
