using System.Text.Json;

namespace KillerBench.Engine;

public static class WinsockReadinessCorrelation
{
    private const string Name = "Winsock name-resolution readiness";
    private const string Boundary = " This verdict covers only complete native catalog structure at one point in time. It does not query a name, open a socket, load a provider or prove application connectivity. No provider, catalog entry, cache, resolver, network setting or service changed.";

    public static WorkflowCheck Check(WorkflowCheck protocols, WorkflowCheck namespaces, WorkflowCheck providerFiles,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(protocols);
        ArgumentNullException.ThrowIfNull(namespaces);
        ArgumentNullException.ThrowIfNull(providerFiles);
        WorkflowCheck Unknown(string detail) => new(Name, CheckState.Unknown, detail + Boundary,
            Evidence: protocols.Evidence ?? namespaces.Evidence ?? providerFiles.Evidence);
        if (protocols.Name != "Winsock protocol catalog" || namespaces.Name != "Winsock namespace providers" ||
            providerFiles.Name != "Winsock protocol provider files")
            return Unknown("Winsock correlation inputs were missing or carried an unexpected identity.");
        if (protocols.EffectiveState != CheckState.Information || protocols.Evidence is null ||
            namespaces.EffectiveState != CheckState.Information || namespaces.Evidence is null ||
            providerFiles.EffectiveState == CheckState.Unknown || providerFiles.Evidence is null)
            return Unknown("Complete protocol, namespace-provider and provider-file inventories are required before Winsock readiness can be correlated.");
        if (providerFiles.EffectiveState is not (CheckState.Information or CheckState.Attention))
            return Unknown("Provider-file evidence carried an unsupported readiness state.");
        try
        {
            var transports = ReadTransports(protocols.Evidence.Output);
            bool dns = ReadActiveDnsNamespace(namespaces.Evidence.Output);
            bool ipv4Ready = transports.Contains((2, 1, 6)) && transports.Contains((2, 2, 17));
            bool ipv6Ready = transports.Contains((23, 1, 6)) && transports.Contains((23, 2, 17));
            var missing = new List<string>();
            if (!dns) missing.Add("an active DNS namespace provider");
            if (!ipv4Ready && !ipv6Ready) missing.Add("a complete IPv4 or IPv6 TCP and UDP base-provider pair");
            if (providerFiles.EffectiveState == CheckState.Attention) missing.Add("complete present and Valid registered protocol provider files");
            if (missing.Count > 0)
                return new(Name, CheckState.Attention,
                    "The complete catalogs did not report " + string.Join(" and ", missing) + ". Review installed networking or security software and compare native catalog evidence before considering any reset." + Boundary,
                    Evidence: protocols.Evidence);
            string families = ipv4Ready && ipv6Ready ? "IPv4 and IPv6" : ipv4Ready ? "IPv4" : "IPv6";
            return new(Name, CheckState.Clear,
                "The complete catalogs report an active DNS namespace provider and base TCP and UDP registrations for " + families + ". This is a structural readiness result, not a resolution or connectivity test." + Boundary,
                Evidence: protocols.Evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Protocol, namespace-provider and provider-file evidence could not be correlated safely.");
        }
    }

    private static HashSet<(int AddressFamily, int SocketType, int Protocol)> ReadTransports(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 5 });
        var root = document.RootElement;
        if (!Unique(root, 3) || !True(root, "complete") || !Int32(root, "error", out int error) || error != 0 ||
            !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() is 0 or > 512)
            throw new InvalidDataException("Protocol catalog evidence is incomplete.");
        var ids = new HashSet<uint>();
        var result = new HashSet<(int, int, int)>();
        foreach (var item in items.EnumerateArray())
        {
            if (!Unique(item, 6) || !UInt32(item, "catalogId", out uint id) || id == 0 || !ids.Add(id) ||
                !GuidText(item, "provider") || !Int32(item, "chainLength", out int chain) || chain is < 0 or > 7 ||
                !Int32(item, "addressFamily", out int family) || family < 0 || !Int32(item, "socketType", out int socket) || socket < 0 ||
                !Int32(item, "protocol", out int protocol)) throw new InvalidDataException("Protocol catalog record is invalid.");
            if (chain == 1) result.Add((family, socket, protocol));
        }
        return result;
    }

    private static bool ReadActiveDnsNamespace(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 5 });
        var root = document.RootElement;
        if (!Unique(root, 3) || !True(root, "complete") || !Int32(root, "error", out int error) || error != 0 ||
            !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() is 0 or > 128)
            throw new InvalidDataException("Namespace-provider evidence is incomplete.");
        var providers = new HashSet<Guid>();
        bool activeDns = false;
        foreach (var item in items.EnumerateArray())
        {
            if (!Unique(item, 4) || !GuidValue(item, "provider", out Guid provider) || !providers.Add(provider) ||
                !UInt32(item, "nameSpace", out uint nameSpace) || !Boolean(item, "active", out bool active) ||
                !UInt32(item, "version", out _)) throw new InvalidDataException("Namespace-provider record is invalid.");
            if (nameSpace == 12 && active) activeDns = true;
        }
        return activeDns;
    }

    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool True(JsonElement owner, string name) => owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.True;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Int32(JsonElement owner, string name, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value);
    }
    private static bool UInt32(JsonElement owner, string name, out uint value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt32(out value);
    }
    private static bool GuidText(JsonElement owner, string name) => GuidValue(owner, name, out _);
    private static bool GuidValue(JsonElement owner, string name, out Guid value)
    {
        value = Guid.Empty;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
            Guid.TryParseExact(item.GetString(), "D", out value) && value != Guid.Empty;
    }
}
