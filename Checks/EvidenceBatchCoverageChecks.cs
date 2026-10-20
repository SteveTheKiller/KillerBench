using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class EvidenceBatchCoverageChecks
{
    internal static async Task RunAsync(string scratch)
    {
        const string entry = "2026-10-20 12:00:00, Info CBS Synthetic recorded event";
        var complete = Investigation.Analyze(new TextEvidence("complete.log", [entry], false));
        string cappedPath = Path.Combine(scratch, "batch-size-cap-" + Guid.NewGuid().ToString("N") + ".log");
        await File.WriteAllTextAsync(cappedPath, new string('x', TextEvidence.MaximumCharacters + 1));
        InvestigationReport sourceCapped;
        try { sourceCapped = Investigation.Analyze(await TextEvidence.ReadAsync(cappedPath, default)); }
        finally { File.Delete(cappedPath); }
        var parserCapped = Investigation.Analyze(new TextEvidence("parser-cap.log", Enumerable.Repeat(entry, Investigation.MaximumEntries + 1).ToArray(), false));
        if (complete.Partial || !sourceCapped.Partial || !parserCapped.Partial || parserCapped.Timeline.Count != Investigation.MaximumEntries)
            throw new Exception("Batch coverage fixtures did not establish complete, source-capped and parser-capped evidence.");
        (InvestigationReport[] Reports, int Failed, bool Incomplete)[] cases =
        [
            ([complete], 0, false), ([complete, sourceCapped], 0, true), ([complete, parserCapped], 0, true),
            ([complete], 1, true), ([complete, sourceCapped, parserCapped], 1, true)
        ];
        foreach (var item in cases)
        {
            var (overview, report) = EvidenceBatch.Summarize(item.Reports, item.Failed);
            if ((report.Summary.Incomplete > 0) != item.Incomplete || overview.Truncated != item.Incomplete)
                throw new Exception("Batch overview lost incomplete imported evidence coverage.");
            string next = WorkflowReport.NextAction(report);
            string readable = WorkflowReport.Format(report);
            string html = WorkflowReportExport.BuildHtml(report, WorkflowReportScope.Diagnostics);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
            if (json.RootElement.GetProperty("Summary").GetProperty("Incomplete").GetInt32() != report.Summary.Incomplete ||
                !readable.Contains("Incomplete: " + report.Summary.Incomplete, StringComparison.Ordinal) ||
                !html.Contains("<strong>" + report.Summary.Incomplete + "</strong>Incomplete</div>", StringComparison.Ordinal))
                throw new Exception("Readable, JSON and HTML batch coverage summaries disagree.");
            if (item.Incomplete && (next.Contains("checks finished", StringComparison.OrdinalIgnoreCase) ||
                html.Contains("The selected checks finished", StringComparison.Ordinal) || !report.Status.Contains("incomplete", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Incomplete batch evidence received a completed-checks recommendation or status.");
        }
    }
}
