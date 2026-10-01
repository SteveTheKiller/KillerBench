using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Evidence;

public static class EventChannelDiagnostics
{
    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Event channel inspection requires Windows.");
        return await CheckAsync(async (channel, stop) =>
        {
            var command = new RepairStep("wevtutil.exe", ["gl", channel, "/f:xml"], true);
            return await BoundedTool.RunAsync(command, TimeSpan.FromSeconds(15), token: stop).ConfigureAwait(false);
        }, token).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(
        Func<string, CancellationToken, Task<ToolResult>> collect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(collect);
        var checks = new List<WorkflowCheck>();
        foreach (string channel in new[] { "System", "Application", "Setup", "Microsoft-Windows-WindowsUpdateClient/Operational" })
        {
            token.ThrowIfCancellationRequested();
            ToolResult evidence;
            try { evidence = await collect(channel, token).ConfigureAwait(false); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            { evidence = new(-1, "", error.Message, false); }
            token.ThrowIfCancellationRequested();
            checks.Add(Interpret(channel, evidence));
        }
        return checks;
    }

    public static WorkflowCheck Interpret(string channel, ToolResult evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentNullException.ThrowIfNull(evidence);
        WorkflowCheck Unknown() => new("Event channel: " + channel, CheckState.Unknown,
            "Channel configuration is unavailable or incomplete. Enabled state and retention could not be verified. No logging settings changed.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var reader = XmlReader.Create(new StringReader(evidence.Output), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 });
            var root = XDocument.Load(reader).Root;
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events";
            if (root?.Name != ns + "channel" || !string.Equals((string?)root.Attribute("name"), channel, StringComparison.OrdinalIgnoreCase)) return Unknown();
            var logging = root.Elements(ns + "logging").ToArray();
            if (logging.Length != 1) return Unknown();
            string? Field(string name)
            {
                var fields = logging[0].Elements(ns + name).ToArray();
                return fields.Length == 1 ? fields[0].Value : null;
            }
            if (!bool.TryParse((string?)root.Attribute("enabled"), out bool enabled) ||
                !bool.TryParse(Field("retention"), out bool retention) ||
                !bool.TryParse(Field("autoBackup"), out bool backup) ||
                !ulong.TryParse(Field("maxSize"), NumberStyles.None, CultureInfo.InvariantCulture, out ulong maximum) || maximum == 0 ||
                (backup && !retention)) return Unknown();
            string mode = !retention ? "overwrite oldest records" : backup ? "retain with automatic archive" : "retain without automatic archive";
            string detail = $"Enabled: {enabled}. Maximum size: {maximum} bytes. Retention: {mode}.\n" +
                (!enabled ? "This channel is disabled; required diagnostic events may be missing. Review its intended policy before enabling it.\n" : "") +
                (retention && !backup ? "New records can be lost when the log fills. Current fullness is not measured; review the collection and archive policy.\n" : "") +
                "Configuration is a snapshot, not proof of successful event recording. Log contents, access permissions and managed settings are preserved.";
            return new("Event channel: " + channel, enabled ? CheckState.Information : CheckState.Attention, detail, Evidence: evidence);
        }
        catch (XmlException) { return Unknown(); }
    }
}
