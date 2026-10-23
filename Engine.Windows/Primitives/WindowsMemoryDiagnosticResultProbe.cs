using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Requires a completion event newer than the System log watermark captured before scheduling.</summary>
public sealed class WindowsMemoryDiagnosticResultProbe : IProbePrimitive
{
    private readonly Func<string[], CancellationToken, Task<ToolResult>> run;
    public WindowsMemoryDiagnosticResultProbe(Func<string[], CancellationToken, Task<ToolResult>>? run = null) =>
        this.run = run ?? ((arguments, token) => NativeTool.RunAsync("wevtutil.exe", arguments, token: token));
    public string Name => "memory-diagnostic.result";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("afterRecordId", false, "System event record watermark captured before scheduling")];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!long.TryParse(parameters.GetValueOrDefault("afterRecordId"), NumberStyles.None, CultureInfo.InvariantCulture, out long baseline) || baseline <= 0)
            return PrimitiveResult.Unavailable("The original scheduling event watermark is unavailable. Review Memory Diagnostic results before retrying.");
        try
        {
            var result = await run(["qe", "System", "/q:*[System[Provider[@Name='Microsoft-Windows-MemoryDiagnostics-Results']]]", "/rd:true", "/c:1", "/f:xml"], token).ConfigureAwait(false);
            var system = ReadSystem(result);
            return system is null || (string?)system.Element(system.Name.Namespace + "Provider")?.Attribute("Name") != "Microsoft-Windows-MemoryDiagnostics-Results" ||
                !long.TryParse(system.Element(system.Name.Namespace + "EventRecordID")?.Value, out long id) ||
                !int.TryParse(system.Element(system.Name.Namespace + "EventID")?.Value, out int eventId)
                ? PrimitiveResult.Unavailable("No matching Memory Diagnostic completion event is available.") : Interpret(baseline, id, eventId);
        }
        catch (Exception error) when (error is XmlException or UnauthorizedAccessException or IOException or System.ComponentModel.Win32Exception)
        { return PrimitiveResult.Unavailable("Memory Diagnostic completion evidence is unavailable."); }
    }

    public static PrimitiveResult Interpret(long baseline, long completionRecordId, int eventId) =>
        baseline <= 0 || completionRecordId <= baseline || eventId is not (1101 or 1102 or 1201 or 1202)
            ? PrimitiveResult.Unavailable("The available event does not establish completion after this scheduling request.")
            : new PrimitiveResult(true, "completed", "MemoryDiagnostics-Results event " + eventId.ToString(CultureInfo.InvariantCulture))
            { Detail = eventId is 1102 or 1202 ? "The scheduled test completed and reported memory errors. Investigate the hardware; memory was not repaired." : "The scheduled test completed and reported no memory errors within its test coverage." };

    internal static async Task<long?> LatestSystemRecordIdAsync(CancellationToken token)
    {
        try
        {
            var result = await NativeTool.RunAsync("wevtutil.exe", ["qe", "System", "/rd:true", "/c:1", "/f:xml"], token: token).ConfigureAwait(false);
            var system = ReadSystem(result);
            return system is not null && long.TryParse(system.Element(system.Name.Namespace + "EventRecordID")?.Value, out long id) && id > 0 ? id : null;
        }
        catch (Exception error) when (error is XmlException or UnauthorizedAccessException or IOException or System.ComponentModel.Win32Exception) { return null; }
    }

    private static XElement? ReadSystem(ToolResult result)
    {
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || string.IsNullOrWhiteSpace(result.Output)) return null;
        using var reader = XmlReader.Create(new StringReader("<Events>" + result.Output + "</Events>"), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1_048_576 });
        var document = XDocument.Load(reader);
        var events = document.Root!.Elements().ToArray();
        return events.Length == 1 && events[0].Name.LocalName == "Event" ? events[0].Element(events[0].Name.Namespace + "System") : null;
    }
}
