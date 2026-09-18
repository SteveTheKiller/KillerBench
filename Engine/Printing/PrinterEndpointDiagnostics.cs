using System.Net;
using System.Text.Json;

namespace KillerBench.Engine;

public static class PrinterEndpointDiagnostics
{
    private const int MaximumQueues = 256;
    private const int MaximumPorts = 256;

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Printer endpoint inventory requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$queues=@(Get-Printer -ErrorAction Stop|Sort-Object Name|Select-Object -First 257|ForEach-Object{[pscustomobject]@{name=[string]$_.Name;portName=[string]$_.PortName;type=[int]$_.Type}});" +
            "$ports=@(Get-CimInstance Win32_TCPIPPrinterPort -ErrorAction Stop|Sort-Object Name|Select-Object -First 257|ForEach-Object{[pscustomobject]@{" +
            "name=[string]$_.Name;host=[string]$_.HostAddress;port=[int]$_.PortNumber;protocol=[int]$_.Protocol;snmpEnabled=[bool]$_.SNMPEnabled}});" +
            "[pscustomobject]@{complete=($queues.Count-le 256-and$ports.Count-le 256);queues=$queues;ports=$ports}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(35), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence)
    {
        const string scope = " Configuration was inspected without resolving names or connecting to an endpoint. SNMP community strings are excluded. No queue, port, network, SNMP or printer setting changed.";
        WorkflowCheck Unknown(string detail) => new("Printer TCP/IP endpoints", CheckState.Unknown, detail + scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("Printer TCP/IP endpoint collection failed or was incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !Array(root, "queues", out var queues) || !Array(root, "ports", out var ports) ||
                !complete || queues.GetArrayLength() > MaximumQueues || ports.GetArrayLength() > MaximumPorts)
                return [Unknown("Printer TCP/IP endpoint inventory was malformed or exceeded its evidence bound.")];
            var endpoints = new Dictionary<string, Endpoint>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ports.EnumerateArray())
            {
                if (!Unique(item, 5) || Text(item, "name", 512) is not { Length: > 0 } name || !endpoints.TryAdd(name, ParseEndpoint(item)))
                    return [Unknown("Printer TCP/IP port identities were missing or duplicated.")];
            }
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var checks = new List<WorkflowCheck>();
            foreach (var item in queues.EnumerateArray())
            {
                if (!Unique(item, 3) || Text(item, "name", 512) is not { Length: > 0 } name || !seen.Add(name) ||
                    Text(item, "portName", 512) is not { Length: > 0 } portName || !Integer(item, "type", 0, 1, out int type))
                    return [Unknown("Printer queue identities or endpoint assignments were malformed or duplicated.")];
                if (type == 1)
                {
                    checks.Add(new("Printer endpoint: " + name, CheckState.Information,
                        "Remote queue endpoint configuration belongs to its print server and was not inspected." + scope, Evidence: evidence));
                    continue;
                }
                if (portName.Contains(','))
                {
                    checks.Add(new("Printer endpoint: " + name, CheckState.Unknown,
                        "The queue reports a pooled port assignment. Individual endpoint selection cannot be established from this snapshot." + scope, Evidence: evidence));
                    continue;
                }
                if (!endpoints.TryGetValue(portName, out var endpoint))
                {
                    checks.Add(new("Printer endpoint: " + name, CheckState.Information,
                        "The local queue does not reference a registered Standard TCP/IP port. USB, WSD, file and vendor port types require their own diagnostics. Reported port: " + portName + "." + scope, Evidence: evidence));
                    continue;
                }
                string protocol = endpoint.Protocol == 1 ? "RAW" : endpoint.Protocol == 2 ? "LPR" : "unsupported protocol " + endpoint.Protocol;
                bool valid = ValidHost(endpoint.Host) && endpoint.Port is >= 1 and <= 65535 && endpoint.Protocol is 1 or 2;
                string detail = $"Registered endpoint: {endpoint.Host}:{endpoint.Port}, protocol {protocol}, SNMP {(endpoint.SnmpEnabled ? "enabled" : "disabled")}. ";
                detail += valid
                    ? "The endpoint syntax is valid. Reachability, printer identity, SNMP response, queue acceptance and physical output remain untested."
                    : "The host, TCP port or protocol is invalid or unsupported. Recollect and compare the registered port before changing queue configuration.";
                checks.Add(new("Printer endpoint: " + name, valid ? CheckState.Information : CheckState.Attention, detail + scope, Evidence: evidence));
            }
            if (checks.Count == 0)
                checks.Add(new("Printer TCP/IP endpoints", CheckState.Information,
                    $"No queues were reported. {endpoints.Count} registered TCP/IP port{(endpoints.Count == 1 ? " remains" : "s remain")} unassigned or outside this process account's visible queues." + scope, Evidence: evidence));
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return [Unknown("Printer TCP/IP endpoint evidence could not be interpreted.")];
        }
    }

    private sealed record Endpoint(string Host, int Port, int Protocol, bool SnmpEnabled);
    private static Endpoint ParseEndpoint(JsonElement item)
    {
        if (Text(item, "host", 253, true) is not { } host || !Integer(item, "port", 0, 65535, out int port) ||
            !Integer(item, "protocol", 0, int.MaxValue, out int protocol) || !Boolean(item, "snmpEnabled", out bool snmp))
            throw new InvalidDataException("Printer endpoint is invalid.");
        return new(host, port, protocol, snmp);
    }
    private static bool ValidHost(string host)
    {
        if (host.Length == 0) return false;
        if (IPAddress.TryParse(host, out _)) return true;
        try { DnsDiagnostics.ValidateHost(host); return true; }
        catch (ArgumentException) { return false; }
    }
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Array(JsonElement owner, string name, out JsonElement value) => owner.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Array;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.TryGetInt32(out value) && value >= minimum && value <= maximum;
    }
    private static string? Text(JsonElement owner, string name, int maximum, bool allowEmpty = false) => owner.TryGetProperty(name, out var item) &&
        item.ValueKind == JsonValueKind.String && item.GetString() is { } text && text == text.Trim() && text.Length <= maximum &&
        !text.Any(char.IsControl) && (allowEmpty || text.Length > 0) ? text : null;
}
