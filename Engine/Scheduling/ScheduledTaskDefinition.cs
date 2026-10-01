using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Scheduling;

public sealed record ScheduledTaskDefinitionResult(ScheduledTaskIdentity Target, string? Xml, string Detail, ToolResult Evidence)
{
    public bool Observed => Xml is not null;
#pragma warning disable CA1822 // Instance property is serialized into exported inspection reports.
    public string Limitation => "Read-only definition snapshot. Task execution, permissions, schema validity and agreement with a proposed plan are not verified. Unavailable evidence does not establish absence.";
#pragma warning restore CA1822
}

public static class ScheduledTaskDefinition
{
    public const int MaximumCharacters = 1024 * 1024;

    public static RepairStep CreateReadStep(ScheduledTaskIdentity target)
    {
        ArgumentNullException.ThrowIfNull(target);
        static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$folder=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + Encode(target.Path) + "')); " +
            "$name=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + Encode(target.Name) + "')); " +
            "$scheduler=New-Object -ComObject Schedule.Service; $scheduler.Connect(); " +
            "$task=$scheduler.GetFolder($folder).GetTask($name); $xml=$task.Xml; " +
            "if($xml.Length -gt 1048576){throw 'Task definition exceeds the inspection limit'}; " +
            "[pscustomobject]@{path=$task.Path;xml=$xml}|ConvertTo-Json -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static async Task<ScheduledTaskDefinitionResult> ReadAsync(ScheduledTaskIdentity target, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Task definition collection requires Windows.");
        return Interpret(target, await BoundedTool.RunAsync(CreateReadStep(target), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false));
    }

    public static ScheduledTaskDefinitionResult Interpret(ScheduledTaskIdentity target, ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(evidence);
        ScheduledTaskDefinitionResult Unknown() => new(target, null, "Definition unavailable or incomplete. The task may be absent, inaccessible or changed; no absence was inferred.", evidence);
        if (!TaskRegistration.Complete(evidence) || evidence.Output.Length > MaximumCharacters * 6 + 4096) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                root.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != 2 ||
                !string.Equals(root.GetProperty("path").GetString(), target.FullName, StringComparison.OrdinalIgnoreCase)) return Unknown();
            string? xml = root.GetProperty("xml").GetString();
            if (string.IsNullOrWhiteSpace(xml) || xml.Length > MaximumCharacters) return Unknown();
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumCharacters });
            var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            if (document.Root?.Name != XName.Get("Task", ScheduledTaskXml.Namespace)) return Unknown();
            return new(target, xml, "Exact task path returned a readable Task XML snapshot. No task was registered, changed or run.", evidence);
        }
        catch (Exception error) when (error is JsonException or XmlException or InvalidOperationException or KeyNotFoundException)
        { return Unknown(); }
    }
}
