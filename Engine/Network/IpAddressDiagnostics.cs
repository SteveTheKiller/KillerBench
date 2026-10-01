using System.Net;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public sealed record IpAddressObservation(int InterfaceIndex, string InterfaceAlias, int AddressFamily, string Address,
    int PrefixLength, string PrefixOrigin, string SuffixOrigin, string AddressState, bool SkipAsSource, string ConnectionState);

public static class IpAddressDiagnostics
{
    private const int MaximumAddresses = 512;

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();$states=@{};" +
            "foreach($i in @(Get-NetIPInterface -ErrorAction Stop)){if($i.AddressFamily-in @('IPv4','IPv6')){$states[([string]$i.InterfaceIndex+'|'+[string]$i.AddressFamily)]=[string]$i.ConnectionState}};" +
            "foreach($a in @(Get-NetIPAddress -ErrorAction Stop|Sort-Object InterfaceIndex,AddressFamily,IPAddress)){if($rows.Count-ge 513){break};" +
            "$key=[string]$a.InterfaceIndex+'|'+[string]$a.AddressFamily;$connectionState=if($states.ContainsKey($key)){$states[$key]}else{'Unknown'};" +
            "$rows.Add([pscustomobject]@{interfaceIndex=[int64]$a.InterfaceIndex;interfaceAlias=[string]$a.InterfaceAlias;addressFamily=[string]$a.AddressFamily;" +
            "address=[string]$a.IPAddress;prefixLength=[int64]$a.PrefixLength;prefixOrigin=[string]$a.PrefixOrigin;suffixOrigin=[string]$a.SuffixOrigin;" +
            "addressState=[string]$a.AddressState;skipAsSource=[bool]$a.SkipAsSource;connectionState=$connectionState})};" +
            "[pscustomobject]@{complete=($rows.Count-le 512);addresses=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(evidence);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? evidence)
    {
        const string scope = " Address records are point-in-time configuration evidence. No address, binding, route, adapter, DHCP, DNS or policy setting changed.";
        WorkflowCheck Unknown(string detail) => new("IP address inventory", CheckState.Unknown, detail + scope, Evidence: evidence);
        if (evidence is null || evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("The current IP address inventory was unavailable or incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var complete) ||
                complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !complete.GetBoolean() ||
                !root.TryGetProperty("addresses", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumAddresses)
                return [Unknown("The current IP address inventory exceeded its evidence bound or was malformed.")];
            var parsed = new List<IpAddressObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 10 ||
                    !row.TryGetProperty("interfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index < 1 ||
                    !row.TryGetProperty("interfaceAlias", out var aliasValue) || aliasValue.ValueKind != JsonValueKind.String ||
                    !DriverReference.ValidText(aliasValue.GetString(), 256) || aliasValue.GetString() != aliasValue.GetString()!.Trim() ||
                    !row.TryGetProperty("addressFamily", out var familyValue) || familyValue.ValueKind != JsonValueKind.String ||
                    familyValue.GetString() is not ("IPv4" or "IPv6") ||
                    !row.TryGetProperty("address", out var addressValue) || addressValue.ValueKind != JsonValueKind.String ||
                    !IPAddress.TryParse(addressValue.GetString(), out var address) ||
                    !row.TryGetProperty("prefixLength", out var prefixValue) || !prefixValue.TryGetInt32(out int prefix) ||
                    !row.TryGetProperty("prefixOrigin", out var prefixOriginValue) || prefixOriginValue.ValueKind != JsonValueKind.String ||
                    !DriverReference.ValidText(prefixOriginValue.GetString(), 64) ||
                    !row.TryGetProperty("suffixOrigin", out var suffixOriginValue) || suffixOriginValue.ValueKind != JsonValueKind.String ||
                    !DriverReference.ValidText(suffixOriginValue.GetString(), 64) ||
                    !row.TryGetProperty("addressState", out var stateValue) || stateValue.ValueKind != JsonValueKind.String ||
                    stateValue.GetString() is not ("Preferred" or "Deprecated" or "Tentative" or "Duplicate" or "Invalid") ||
                    !row.TryGetProperty("skipAsSource", out var skipValue) || skipValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("connectionState", out var connectionValue) || connectionValue.ValueKind != JsonValueKind.String ||
                    connectionValue.GetString() is not ("Connected" or "Disconnected" or "Unknown"))
                    return [Unknown("One or more current IP address identities or values were invalid.")];
                int family = familyValue.GetString() == "IPv4" ? 2 : 23;
                if (family == 2 && (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || prefix is < 0 or > 32) ||
                    family == 23 && (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6 || prefix is < 0 or > 128) ||
                    !identities.Add(index + "\0" + family + "\0" + address))
                    return [Unknown("One or more current IP address identities were mismatched or duplicated.")];
                parsed.Add(new(index, aliasValue.GetString()!, family, address.ToString(), prefix, prefixOriginValue.GetString()!,
                    suffixOriginValue.GetString()!, stateValue.GetString()!, skipValue.GetBoolean(), connectionValue.GetString()!));
            }
            if (parsed.Count == 0) return [new("IP address inventory", CheckState.Attention,
                "Windows reported no current IPv4 or IPv6 address records. Local networking may be unavailable." + scope, Evidence: evidence)];

            var duplicateAddresses = parsed.Where(row => row.ConnectionState != "Disconnected" &&
                    !IPAddress.Parse(row.Address).IsIPv6LinkLocal && !IPAddress.IsLoopback(IPAddress.Parse(row.Address)))
                .GroupBy(row => (row.AddressFamily, row.Address), StringTupleComparer.Instance)
                .Where(group => group.Select(row => row.InterfaceIndex).Distinct().Count() > 1)
                .SelectMany(group => group).ToHashSet();
            var results = new List<WorkflowCheck>();
            foreach (var row in parsed)
            {
                var address = IPAddress.Parse(row.Address);
                bool automaticPrivate = row.AddressFamily == 2 && address.GetAddressBytes() is [169, 254, _, _];
                bool badState = row.AddressState is "Deprecated" or "Tentative" or "Duplicate" or "Invalid";
                bool duplicated = duplicateAddresses.Contains(row);
                bool disconnected = row.ConnectionState == "Disconnected";
                CheckState state = !disconnected && (badState || duplicated || automaticPrivate) ? CheckState.Attention : CheckState.Information;
                string detail = $"Interface {row.InterfaceIndex} ({row.InterfaceAlias}); IPv{(row.AddressFamily == 2 ? 4 : 6)} {row.Address}/{row.PrefixLength}; connection {row.ConnectionState}; state {row.AddressState}; prefix origin {row.PrefixOrigin}; suffix origin {row.SuffixOrigin}; source use {(row.SkipAsSource ? "skipped" : "allowed")}.";
                if (disconnected && (automaticPrivate || badState)) detail += " The interface is disconnected, so its retained address or transition state does not establish a current connectivity problem.";
                if (automaticPrivate) detail += " This IPv4 address is in the automatic private range commonly used when DHCP configuration is unavailable. An isolated or intentionally link-local network can use it deliberately.";
                if (badState) detail += " Windows does not report this address as Preferred. Review current adapter and address transition state before selecting recovery.";
                if (duplicated) detail += " The same non-link-local address appears on more than one interface. Confirm whether this is intentional before changing either interface.";
                if (state == CheckState.Information) detail += " No address-level issue was identified in this record.";
                results.Add(new("IP address: " + row.InterfaceAlias + " / " + row.Address, state, detail + scope, Evidence: evidence));
            }
            return results.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return [Unknown("The current IP address inventory could not be interpreted safely.")];
        }
    }

    private sealed class StringTupleComparer : IEqualityComparer<(int Family, string Address)>
    {
        public static StringTupleComparer Instance { get; } = new();
        public bool Equals((int Family, string Address) x, (int Family, string Address) y) =>
            x.Family == y.Family && string.Equals(x.Address, y.Address, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((int Family, string Address) value) => HashCode.Combine(value.Family, StringComparer.OrdinalIgnoreCase.GetHashCode(value.Address));
    }
}
