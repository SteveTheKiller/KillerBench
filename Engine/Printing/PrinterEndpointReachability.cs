using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace KillerBench.Engine;

public static class PrinterEndpointReachability
{
    private const int MaximumInventoryItems = 256;
    private const int MaximumTargets = 32;
    private static readonly TimeSpan ConnectionLimit = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan OverallLimit = TimeSpan.FromSeconds(12);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<string, int, CancellationToken, Task<TcpCheckResult>>? probe = null)
    {
        var configuration = await PrinterEndpointDiagnostics.CheckAsync(token).ConfigureAwait(false);
        return await CheckAsync(configuration, token, probe).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(IReadOnlyList<WorkflowCheck> configuration,
        CancellationToken token = default, Func<string, int, CancellationToken, Task<TcpCheckResult>>? probe = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        token.ThrowIfCancellationRequested();
        if (!TryReadTargets(configuration, out var targets, out var evidence, out string error))
            return [Unknown(error, evidence)];
        if (targets.Count == 0)
            return [new("Printer endpoint reachability", CheckState.Information,
                "No valid, assigned Standard TCP/IP printer endpoint was available to test. Remote, pooled, USB, WSD, file and vendor ports were not contacted. No discovery scan, SNMP request, print data or setting change occurred.", Evidence: evidence)];
        if (targets.Count > MaximumTargets)
            return [Unknown($"The snapshot contains {targets.Count} distinct assigned endpoints, exceeding the {MaximumTargets}-endpoint connection limit. No endpoint was contacted.", evidence)];

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(OverallLimit);
        var checks = await Task.WhenAll(targets.Select(target => ProbeAsync(target, evidence, probe, token, deadline.Token))).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return System.Array.AsReadOnly(checks);
    }

    private static async Task<WorkflowCheck> ProbeAsync(Target target, ToolResult evidence,
        Func<string, int, CancellationToken, Task<TcpCheckResult>>? probe, CancellationToken callerToken, CancellationToken deadlineToken)
    {
        string name = $"Printer endpoint reachability: {target.Host}:{target.Port}";
        string protocol = target.Protocol == 1 ? "RAW" : target.Protocol == 2 ? "LPR" : "TCP/IP";
        string scope = $" Configured {protocol} endpoint used by {target.QueueCount} local queue{(target.QueueCount == 1 ? "" : "s")}. No application data, SNMP request or print job was sent. No setting changed.";
        try
        {
            var pending = (probe is null
                ? TcpDiagnostics.CheckAsync(target.Host, target.Port, ConnectionLimit, deadlineToken)
                : probe(target.Host, target.Port, deadlineToken))
                ?? throw new InvalidOperationException("Printer endpoint probe returned no task.");
            var result = await pending.WaitAsync(deadlineToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Printer endpoint probe returned no result.");
            callerToken.ThrowIfCancellationRequested();
            if (!Exact(result, target))
                return new(name, CheckState.Unknown, "The connection provider returned mismatched or invalid endpoint evidence." + scope,
                    Evidence: ProbeEvidence(result, false));
            var state = result.State == TcpCheckState.Connected ? CheckState.Information : CheckState.Attention;
            string detail = result.State switch
            {
                TcpCheckState.Connected => "A TCP connection was accepted. This confirms a listener at the configured address, but does not establish printer identity, queue acceptance or physical output.",
                TcpCheckState.TimedOut => "The configured endpoint did not accept a TCP connection within three seconds. Review printer power, address, network path and filtering before changing the queue.",
                _ => "The configured endpoint did not accept a TCP connection. " + result.Detail
            };
            return new(name, state, detail + scope, Evidence: ProbeEvidence(result, result.State == TcpCheckState.TimedOut));
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested && deadlineToken.IsCancellationRequested)
        {
            return new(name, CheckState.Unknown, "The endpoint connection did not complete within the overall twelve-second limit." + scope,
                Evidence: new(-1, "", "Printer endpoint connection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException or SocketException or
            TimeoutException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            callerToken.ThrowIfCancellationRequested();
            return new(name, CheckState.Unknown, "The endpoint connection provider failed, so reachability could not be established." + scope,
                Evidence: new(-1, "", exception.Message, false) { TimedOut = exception is TimeoutException });
        }
    }

    private static ToolResult ProbeEvidence(TcpCheckResult result, bool timedOut) => new(
        result.State == TcpCheckState.Connected ? 0 : (int?)result.ErrorCode ?? -1,
        JsonSerializer.Serialize(result, Capture.JsonOptions), "", false) { TimedOut = timedOut };

    private static bool Exact(TcpCheckResult result, Target target) => result.Host == target.Host && result.Port == target.Port &&
        Enum.IsDefined(result.State) && result.Detail is { Length: > 0 and <= 2048 } && !result.Detail.Any(char.IsControl);

    private static bool TryReadTargets(IReadOnlyList<WorkflowCheck> configuration, out List<Target> targets,
        out ToolResult evidence, out string error)
    {
        targets = [];
        evidence = new(-1, "", "Printer endpoint configuration evidence is unavailable.", false);
        error = "Printer endpoint configuration evidence was missing or incomplete. No endpoint was contacted.";
        if (configuration.Count == 0 || configuration[0].Evidence is not { } first ||
            configuration.Any(check => check.Evidence is null || check.Evidence != first)) return false;
        evidence = first;
        if (first.ExitCode != 0 || first.TimedOut || first.Truncated || !string.IsNullOrWhiteSpace(first.Error)) return false;
        try
        {
            using var document = JsonDocument.Parse(first.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !JsonArray(root, "queues", out var queues) || !JsonArray(root, "ports", out var ports) ||
                queues.GetArrayLength() > MaximumInventoryItems || ports.GetArrayLength() > MaximumInventoryItems)
                throw new InvalidDataException("Printer endpoint inventory is incomplete.");
            var endpoints = new Dictionary<string, Endpoint>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ports.EnumerateArray())
            {
                if (!Unique(item, 5) || Text(item, "name", 512) is not { } name || Text(item, "host", 253, true) is not { } host ||
                    !Integer(item, "port", 0, 65535, out int port) || !Integer(item, "protocol", 0, int.MaxValue, out int protocol) ||
                    !Boolean(item, "snmpEnabled", out _) || !endpoints.TryAdd(name, new(host, port, protocol)))
                    throw new InvalidDataException("Printer endpoint inventory is malformed.");
            }
            var queuesSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var grouped = new Dictionary<string, Target>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in queues.EnumerateArray())
            {
                if (!Unique(item, 3) || Text(item, "name", 512) is not { } queue || !queuesSeen.Add(queue) ||
                    Text(item, "portName", 512) is not { } portName || !Integer(item, "type", 0, 1, out int type))
                    throw new InvalidDataException("Printer queue inventory is malformed.");
                if (type != 0 || portName.Contains(',') || !endpoints.TryGetValue(portName, out var endpoint) || !Valid(endpoint)) continue;
                string key = endpoint.Host + "\0" + endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
                grouped[key] = grouped.TryGetValue(key, out var existing) ? existing with
                {
                    Protocol = existing.Protocol == endpoint.Protocol ? existing.Protocol : 0,
                    QueueCount = existing.QueueCount + 1
                } :
                    new(endpoint.Host, endpoint.Port, endpoint.Protocol, 1);
            }
            targets = grouped.Values.OrderBy(item => item.Host, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Port).ToList();
            error = "";
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException)
        {
            error = "Printer endpoint configuration evidence was malformed or ambiguous. No endpoint was contacted.";
            return false;
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Printer endpoint reachability", CheckState.Unknown,
        detail + " No discovery scan, SNMP request, print data or setting change occurred.", Evidence: evidence);
    private sealed record Endpoint(string Host, int Port, int Protocol);
    private sealed record Target(string Host, int Port, int Protocol, int QueueCount);
    private static bool Valid(Endpoint endpoint)
    {
        if (endpoint.Port is < 1 or > 65535 || endpoint.Protocol is not (1 or 2) || endpoint.Host.Length == 0) return false;
        if (IPAddress.TryParse(endpoint.Host, out _)) return true;
        try { DnsDiagnostics.ValidateHost(endpoint.Host); return true; }
        catch (ArgumentException) { return false; }
    }
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool JsonArray(JsonElement owner, string name, out JsonElement value) => owner.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Array;
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
