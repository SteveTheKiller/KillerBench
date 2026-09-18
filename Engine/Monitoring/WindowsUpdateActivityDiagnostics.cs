using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine;

public sealed record WindowsUpdateTransition(DateTimeOffset TimeUtc, ushort EventId, string State, string Title, string ErrorCode);

public static class WindowsUpdateActivityDiagnostics
{
    private const string SystemChannel = "System";
    private static readonly XNamespace EventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";
    private static readonly IReadOnlyDictionary<ushort, string> States = new Dictionary<ushort, string>
    {
        [17] = "Ready",
        [19] = "Installed",
        [20] = "Failed",
        [43] = "Installing",
        [44] = "Downloading",
        [212] = "Reverted",
        [214] = "Rollback"
    };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Update activity collection requires Windows.");
        WorkflowCheck history = await WindowsUpdateEvents.CheckAsync(token).ConfigureAwait(false);
        var step = new RepairStep("wevtutil.exe", ["qe", SystemChannel,
            "/q:*[System[Provider[@Name='Microsoft-Windows-WindowsUpdateClient'] and (EventID=17 or EventID=19 or EventID=20 or EventID=43 or EventID=44 or EventID=212 or EventID=214) and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        ToolResult activity = await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        return Interpret(history, activity, token);
    }

    public static WorkflowCheck Interpret(WorkflowCheck history, CancellationToken token = default) =>
        Interpret(history, history.Evidence, token);

    public static WorkflowCheck Interpret(WorkflowCheck history, ToolResult? activityEvidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Update live activity";
        if (history.Name != "Recent Windows Update Client events" || activityEvidence is not { } evidence ||
            evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) ||
            history.State == CheckState.Unknown)
            return new(name, CheckState.Unknown,
                "Complete recent Windows Update Client evidence is unavailable. A live update transition was not inferred.", Evidence: activityEvidence ?? history.Evidence);

        try
        {
            var source = EventEvidence.FromXml(SystemChannel, evidence.Output);
            if (source.Status == CollectionStatus.Truncated || source.Evidence is null)
                throw new InvalidDataException("Windows Update event coverage is incomplete.");

            var transitions = new List<WindowsUpdateTransition>();
            foreach (string line in source.Evidence.Lines)
            {
                token.ThrowIfCancellationRequested();
                var transition = Parse(line);
                if (transition is not null) transitions.Add(transition);
            }
            transitions.Sort((left, right) => right.TimeUtc.CompareTo(left.TimeUtc));

            if (transitions.Count == 0)
                return new(name, history.State,
                    "No recognized download, ready, install, completion, failure or rollback transition exists in the retained seven-day Windows Update Client history. This does not prove that Windows Update is idle. " + history.Detail,
                    Evidence: evidence);

            WindowsUpdateTransition latest = transitions[0];
            string detail = $"Latest recognized transition: {latest.TimeUtc:O} | {latest.State} | {latest.Title} | Event {latest.EventId}.";
            if (latest.ErrorCode.Length > 0)
            {
                detail += " Result " + latest.ErrorCode + ".";
                if (TryCode(latest.ErrorCode, out uint code) && WindowsUpdateErrorCatalog.Find(code) is { } reference)
                    detail += $" {reference.Symbol}: {reference.Meaning} Next: {reference.DiagnosticScope}";
                else
                    detail += " No installed reference is available for this result; retain the exact code and review fresh update evidence.";
            }

            var unmatchedStarts = transitions.Where(item => item.EventId == 43 &&
                !transitions.Any(terminal => terminal.TimeUtc > item.TimeUtc && terminal.EventId is 19 or 20 &&
                    terminal.Title.Equals(item.Title, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (unmatchedStarts.Length > 0)
                detail += $" {unmatchedStarts.Length} retained install-start transition{(unmatchedStarts.Length == 1 ? " has" : "s have")} no later matching completion or failure in this bounded history. This does not prove that installation is currently active or stalled.";

            detail += $" {transitions.Count} recognized transition{(transitions.Count == 1 ? "" : "s")} retained. Titles come from local Windows event evidence; URLs, service identifiers and revision identifiers are excluded. " + history.Detail;
            CheckState state = history.State == CheckState.Attention || latest.EventId is 20 or 212 or 214
                ? CheckState.Attention : CheckState.Information;
            return new(name, state, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        {
            return new(name, CheckState.Unknown,
                "Recent Windows Update Client evidence could not be projected into a bounded live transition summary. Current update activity remains unverified.",
                Evidence: activityEvidence ?? history.Evidence);
        }
    }

    public static bool TryLatestTransition(WorkflowCheck? check, out WindowsUpdateTransition transition)
    {
        transition = null!;
        if (check is not { Name: "Windows Update live activity", State: not CheckState.Unknown, Evidence: { ExitCode: 0 } evidence } ||
            evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return false;
        try
        {
            var source = EventEvidence.FromXml(SystemChannel, evidence.Output);
            if (source.Status == CollectionStatus.Truncated || source.Evidence is null) return false;
            var latest = source.Evidence.Lines.Select(Parse).Where(item => item is not null)
                .OrderByDescending(item => item!.TimeUtc).FirstOrDefault();
            if (latest is null) return false;
            transition = latest;
            return true;
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        {
            return false;
        }
    }

    private static WindowsUpdateTransition? Parse(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 64 * 1024
        });
        XElement root = XElement.Load(reader);
        XElement? system = root.Element(EventNamespace + "System");
        if (root.Name != EventNamespace + "Event" || system is null ||
            (string?)system.Element(EventNamespace + "Provider")?.Attribute("Name") != WindowsUpdateEvents.Provider ||
            !ushort.TryParse((string?)system.Element(EventNamespace + "EventID"), NumberStyles.None, CultureInfo.InvariantCulture, out ushort eventId) ||
            !DateTimeOffset.TryParse((string?)system.Element(EventNamespace + "TimeCreated")?.Attribute("SystemTime"),
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset timeUtc))
            throw new InvalidDataException("Windows Update event identity is invalid.");
        if (!States.TryGetValue(eventId, out string? state)) return null;

        XElement? data = root.Element(EventNamespace + "EventData");
        string title = Value(data, "updateTitle");
        if (title.Length == 0) title = eventId == 44 ? "Windows Update download" : "Windows Update operation";
        title = Normalize(title, 512);
        string errorCode = NormalizeCode(FirstValue(data, "errorCode", "hResult", "hr"));
        return new(timeUtc, eventId, state, title, errorCode);
    }

    private static string Value(XElement? eventData, string name) => eventData?.Elements(EventNamespace + "Data")
        .SingleOrDefault(item => string.Equals((string?)item.Attribute("Name"), name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static string FirstValue(XElement? eventData, params string[] names)
    {
        foreach (string name in names)
        {
            string value = Value(eventData, name);
            if (value.Length > 0) return value;
        }
        return "";
    }

    private static string Normalize(string value, int limit)
    {
        string normalized = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length > limit || normalized.Any(character => char.IsControl(character)))
            throw new InvalidDataException("Windows Update event text is outside its bounds.");
        return normalized;
    }

    private static string NormalizeCode(string value)
    {
        string normalized = Normalize(value, 32);
        if (normalized.Length == 0) return "";
        if (TryCode(normalized, out uint code)) return "0x" + code.ToString("X8", CultureInfo.InvariantCulture);
        return normalized;
    }

    private static bool TryCode(string value, out uint code)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.TryParse(value.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out code);
        if (uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out code)) return true;
        if (int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int signed))
        {
            code = unchecked((uint)signed);
            return true;
        }
        code = 0;
        return false;
    }
}
