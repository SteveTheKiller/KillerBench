using System.Runtime.CompilerServices;

namespace KillerBench.Engine.Evidence;

public sealed record EvidenceImportResult(string Path, TextEvidence? Evidence, InvestigationReport? Report, string? Error);

public static class EvidenceBatch
{
    public static (TextEvidence Overview, WorkflowResult Report) Summarize(IReadOnlyList<InvestigationReport> imported, int failed)
    {
        ArgumentNullException.ThrowIfNull(imported);
        ArgumentOutOfRangeException.ThrowIfNegative(failed);
        int loaded = imported.Count;
        int partial = imported.Count(report => report.Partial);
        bool incomplete = failed > 0 || partial > 0;
        var findings = imported.SelectMany(report => report.Findings).ToArray();
        var checks = new List<WorkflowCheck>
        {
            new("Imported sources", incomplete ? CheckState.Unknown : CheckState.Information,
                loaded + " source" + (loaded == 1 ? " was" : "s were") + " imported. " + failed + " failed. " +
                partial + " imported source" + (partial == 1 ? " has" : "s have") + " incomplete evidence.")
        };
        checks.AddRange(findings.Select(finding => new WorkflowCheck(finding.Title,
            finding.Severity == "Information" ? CheckState.Information : CheckState.Attention,
            finding.Explanation + " Next: " + finding.NextStep)));
        var summary = new WorkflowResult(WorkflowKind.WindowsUpdate, Environment.MachineName, DateTimeOffset.UtcNow,
            incomplete ? "Imported evidence review incomplete" : findings.Length == 0 ? "Imported evidence reviewed" : "Imported evidence needs review",
            checks, [], "Imported evidence only. No repair journal was written.");
        var overview = new TextEvidence("Imported evidence summary", ["IMPORTED EVIDENCE", loaded + " loaded", failed + " failed", partial + " imported sources incomplete"], incomplete);
        return (overview, summary);
    }

    public static async IAsyncEnumerable<EvidenceImportResult> ReadAsync(IReadOnlyList<string> paths,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var selected = paths.ToArray();
        if (selected.Length is < 1 or > 12 || selected.Any(string.IsNullOrWhiteSpace) ||
            selected.Any(path => System.IO.Path.GetExtension(path).Equals(".kbench", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Choose 1 to 12 evidence files. Open saved captures separately.", nameof(paths));
        foreach (var path in selected)
        {
            token.ThrowIfCancellationRequested();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            EvidenceImportResult result;
            try
            {
                bool archive = System.IO.Path.GetExtension(path).Equals(".evtx", StringComparison.OrdinalIgnoreCase);
                var source = archive ? await EventArchive.ReadAsync(path, deadline.Token).ConfigureAwait(false)
                    : await TextEvidence.ReadAsync(path, deadline.Token).ConfigureAwait(false);
                InvestigationReport report;
                if (System.IO.Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    var xml = EventXmlImport.Import(source, deadline.Token);
                    source = xml.Evidence;
                    report = xml.Report;
                }
                else report = archive ? EventArchive.Analyze(source, deadline.Token) : Investigation.Analyze(source, deadline.Token);
                deadline.Token.ThrowIfCancellationRequested();
                result = new(path, source, report, null);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
            { result = new(path, null, null, "File import exceeded 15 seconds."); }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            { result = new(path, null, null, error.Message); }
            token.ThrowIfCancellationRequested();
            yield return result;
        }
    }
}
