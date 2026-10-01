using System.Runtime.CompilerServices;

namespace KillerBench.Engine.Evidence;

public sealed record EvidenceImportResult(string Path, TextEvidence? Evidence, InvestigationReport? Report, string? Error);

public static class EvidenceBatch
{
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
