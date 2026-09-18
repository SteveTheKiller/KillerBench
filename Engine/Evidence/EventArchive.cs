using System.Xml;

namespace KillerBench.Engine;

public static class EventArchive
{
    public static RepairStep PlanRead(string path)
    {
        path = Path.GetFullPath(path);
        if (!Path.GetExtension(path).Equals(".evtx", StringComparison.OrdinalIgnoreCase) || path.StartsWith(@"\\", StringComparison.Ordinal) ||
            path.Any(char.IsControl)) throw new ArgumentException("Choose a local EVTX file.", nameof(path));
        return new("wevtutil.exe", ["qe", path, "/lf:true", "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
    }

    public static async Task<TextEvidence> ReadAsync(string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("EVTX import requires Windows.");
        var step = PlanRead(path);
        if (!File.Exists(step.Arguments[1])) throw new FileNotFoundException("Event archive was not found.", step.Arguments[1]);
        var result = await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
        return FromResult(step.Arguments[1], result);
    }

    public static TextEvidence FromResult(string path, ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            throw new InvalidDataException("Event archive query failed or exceeded its limits. " + result.Error);
        try
        {
            var source = EventEvidence.FromXml(path, result.Output).Evidence!;
            return new(path, source.Lines, source.Truncated);
        }
        catch (XmlException error) { throw new InvalidDataException("Event archive returned invalid XML.", error); }
    }

    public static InvestigationReport Analyze(TextEvidence evidence, CancellationToken token = default)
    {
        var report = EventEvidence.Analyze(evidence, token);
        return report with { Limitation = report.Limitation + " Imported archive: newest 200 records, with no date filter. Older records may be omitted. The archive may describe another machine or an earlier state." };
    }
}
