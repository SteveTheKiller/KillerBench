using System.Net;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class RoutePriorityDiagnostics
{
    private const int MaximumRoutes = 128;
    private const string Scope = " Default-route metrics are point-in-time configuration evidence. Equal priorities can be intentional for resilient or specialized networks. This check does not prove which path carried traffic, gateway reachability or Internet access. No route, interface, metric, address, gateway, VPN or policy setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$r=@(Get-NetRoute -ErrorAction Stop|Where-Object{$_.DestinationPrefix-in @('0.0.0.0/0','::/0')}|Sort-Object AddressFamily,CompartmentId,RouteMetric,InterfaceIndex|Select-Object -First 129);" +
        "$i=@(Get-NetIPInterface -ErrorAction Stop|Where-Object{[int]$_.AddressFamily-in @(2,23)}|Select-Object -First 513);$complete=($r.Count-le 128-and $i.Count-le 512);$map=@{};" +
        "foreach($x in @($i|Select-Object -First 512)){$k=([int]$x.AddressFamily).ToString()+':'+([int64]$x.InterfaceIndex).ToString();if($map.ContainsKey($k)){$complete=$false}else{$map[$k]=$x}};" +
        "$rows=@();foreach($x in @($r|Select-Object -First 128)){$f=[int]$x.AddressFamily;$n=[int64]$x.InterfaceIndex;$k=$f.ToString()+':'+$n.ToString();if(-not $map.ContainsKey($k)){$complete=$false;continue};$ip=$map[$k];$rm=[uint64]$x.RouteMetric;$im=[uint64]$ip.InterfaceMetric;" +
        "$rows+=[pscustomobject]@{family=$f;interfaceIndex=$n;nextHop=[string]$x.NextHop;routeMetric=$rm;interfaceMetric=$im;effectiveMetric=($rm+$im);automaticMetric=[string]$ip.AutomaticMetric;state=[string]$x.State;protocol=[string]$x.Protocol}};" +
        "[pscustomobject]@{complete=$complete;routes=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Default-route priority inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Route-priority provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Route-priority provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Default-route priority inspection exceeded its overall deadline.",
                new(-1, "", "Route-priority inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Default-route priority inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Default-route priority collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) ||
                !root.TryGetProperty("routes", out var routes) || routes.ValueKind != JsonValueKind.Array ||
                routes.GetArrayLength() > MaximumRoutes || !complete)
                return Unknown("Default-route and interface-metric evidence was malformed, incomplete or exceeded its bound.", evidence);

            var rows = new List<RouteRow>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var route in routes.EnumerateArray())
            {
                if (!Unique(route, 9) || !Integer(route, "family", 2, 23, out long family) || family is not (2 or 23) ||
                    !Integer(route, "interfaceIndex", 1, int.MaxValue, out long index) ||
                    !Text(route, "nextHop", 128, out string nextHop) || !IPAddress.TryParse(nextHop, out var address) ||
                    (family == 2) != (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) ||
                    !Integer(route, "routeMetric", 0, uint.MaxValue, out long routeMetric) ||
                    !Integer(route, "interfaceMetric", 0, uint.MaxValue, out long interfaceMetric) ||
                    !Integer(route, "effectiveMetric", 0, (long)uint.MaxValue * 2, out long effectiveMetric) ||
                    effectiveMetric != routeMetric + interfaceMetric ||
                    !Text(route, "automaticMetric", 16, out string automaticMetric) || automaticMetric is not ("Enabled" or "Disabled") ||
                    !Text(route, "state", 32, out string state) || !Text(route, "protocol", 64, out string protocol) ||
                    !identities.Add(family + "\0" + index + "\0" + nextHop + "\0" + routeMetric + "\0" + protocol))
                    return Unknown("Default-route priority evidence contained an invalid or duplicate route.", evidence);
                rows.Add(new((int)family, (int)index, nextHop, routeMetric, interfaceMetric, effectiveMetric,
                    automaticMetric == "Enabled", state, protocol));
            }
            if (rows.Count == 0)
                return new("Default route priority", CheckState.Information,
                    "No IPv4 or IPv6 default routes were reported, so no route priority could be compared." + Scope, Evidence: evidence);

            var inactive = rows.Where(row => !row.State.Equals("Alive", StringComparison.OrdinalIgnoreCase)).ToArray();
            var ties = rows.Where(row => row.State.Equals("Alive", StringComparison.OrdinalIgnoreCase))
                .GroupBy(row => row.Family).Select(group =>
                {
                    long minimum = group.Min(row => row.EffectiveMetric);
                    return (Family: group.Key, Rows: group.Where(row => row.EffectiveMetric == minimum).ToArray());
                }).Where(group => group.Rows.Length > 1).ToArray();
            string detail = $"Reviewed {rows.Count} default route{(rows.Count == 1 ? "" : "s")}. " +
                string.Join("; ", rows.Take(16).Select(row =>
                    $"IPv{(row.Family == 2 ? 4 : 6)} interface {row.InterfaceIndex} via {row.NextHop}: route {row.RouteMetric} + interface {row.InterfaceMetric} = {row.EffectiveMetric}, automatic metric {(row.AutomaticMetric ? "on" : "off")}, state {row.State}, protocol {row.Protocol}")) +
                (rows.Count > 16 ? "; additional routes remain in evidence." : ".");
            if (ties.Length > 0)
                detail += " Multiple live default routes share the lowest effective metric for " +
                    string.Join(" and ", ties.Select(group => "IPv" + (group.Family == 2 ? "4" : "6"))) +
                    ". Review whether equal path priority is intended before changing metrics.";
            if (inactive.Length > 0)
                detail += $" {inactive.Length} retained default route{(inactive.Length == 1 ? " is" : "s are")} not reported Alive and need current-path review.";
            var stateResult = ties.Length > 0 || inactive.Length > 0 ? CheckState.Attention : CheckState.Information;
            return new("Default route priority", stateResult, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Default-route priority evidence could not be interpreted.", evidence);
        }
    }

    private sealed record RouteRow(int Family, int InterfaceIndex, string NextHop, long RouteMetric,
        long InterfaceMetric, long EffectiveMetric, bool AutomaticMetric, string State, string Protocol);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Default route priority", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, long minimum, long maximum, out long value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt64(out value) && value >= minimum && value <= maximum;
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String ||
            item.GetString() is not { } text || text.Length is < 1 || text.Length > maximum ||
            text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
