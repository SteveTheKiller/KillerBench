using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace KillerBench.Engine;

public static class DnsServerDiagnostics
{
    private const int MaximumRows = 512;
    private const int MaximumServersPerRow = 16;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($entry in @(Get-DnsClientServerAddress -ErrorAction Stop|Sort-Object InterfaceIndex,AddressFamily)){if($rows.Count-ge 513){break};" +
            "$servers=@($entry.ServerAddresses|ForEach-Object{[string]$_});" +
            "$rows.Add([pscustomobject]@{interfaceIndex=[int]$entry.InterfaceIndex;interfaceAlias=[string]$entry.InterfaceAlias;addressFamily=[int]$entry.AddressFamily;serverAddresses=$servers})};" +
            "[pscustomobject]@{complete=($rows.Count-le 512);rows=$rows}|ConvertTo-Json -Depth 5 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "DNS server configuration";
        WorkflowCheck Unknown(string detail = "DNS server configuration is unavailable or incomplete. No adapter or resolver setting changed.") =>
            new(title, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var completeValue) ||
                completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("rows", out var rowsValue) || rowsValue.ValueKind != JsonValueKind.Array) return Unknown();
            if (!completeValue.GetBoolean() || rowsValue.GetArrayLength() > MaximumRows)
                return Unknown("The DNS server inventory exceeded its evidence bound. Coverage is incomplete and no change was selected.");
            var rows = new List<(int Index, string Alias, int Family, string[] Servers)>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rowsValue.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object ||
                    !row.TryGetProperty("interfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index < 1 ||
                    !row.TryGetProperty("interfaceAlias", out var aliasValue) || aliasValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("addressFamily", out var familyValue) || !familyValue.TryGetInt32(out int family) || family is not (2 or 23) ||
                    !row.TryGetProperty("serverAddresses", out var serversValue) || serversValue.ValueKind != JsonValueKind.Array ||
                    serversValue.GetArrayLength() > MaximumServersPerRow) return Unknown();
                string alias = aliasValue.GetString()!;
                if (!DriverReference.ValidText(alias, 256) || alias != alias.Trim() || !seen.Add(index + ":" + family)) return Unknown();
                var servers = new List<string>();
                var uniqueServers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var serverValue in serversValue.EnumerateArray())
                {
                    if (serverValue.ValueKind != JsonValueKind.String) return Unknown();
                    string server = serverValue.GetString()!;
                    if (server != server.Trim() || !IPAddress.TryParse(server, out var address) ||
                        (family == 2 && address.AddressFamily != AddressFamily.InterNetwork) ||
                        (family == 23 && address.AddressFamily != AddressFamily.InterNetworkV6) || !uniqueServers.Add(server)) return Unknown();
                    servers.Add(server);
                }
                rows.Add((index, alias, family, servers.ToArray()));
            }
            int configuredRows = rows.Count(row => row.Servers.Length > 0);
            int serverCount = rows.SelectMany(row => row.Servers.Select(server => row.Index + ":" + server)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            string detail = $"Reviewed {rows.Count} interface and address-family row{(rows.Count == 1 ? "" : "s")}. {configuredRows} reported DNS servers; {serverCount} distinct interface and server pair{(serverCount == 1 ? "" : "s")}.";
            detail += rows.Count == 0 || configuredRows == 0
                ? " No DNS server was reported. Name resolution may use a local resolver or configuration not exposed by this provider; correlate with an active adapter and a bounded lookup."
                : " Server registration does not establish reachability, response correctness or which interface Windows will select for a lookup.";
            detail += " No DNS server, suffix, adapter, route, policy or resolver cache changed.";
            return new(title, rows.Count == 0 || configuredRows == 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }
}
