using System.Text.Json;

namespace KillerBench.Engine;

public static class NetworkFirewallCorrelation
{
    private const string Scope = " This correlation does not establish effective rule enforcement, third-party filtering, policy ownership or traffic reachability. No profile, firewall rule, service or network setting is changed.";

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> networkProfiles,
        IReadOnlyList<WorkflowCheck> firewallProfiles, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(networkProfiles);
        ArgumentNullException.ThrowIfNull(firewallProfiles);
        token.ThrowIfCancellationRequested();
        if (networkProfiles.Count == 0 || firewallProfiles.Count == 0 ||
            networkProfiles.Any(item => item.State == CheckState.Unknown) || firewallProfiles.Any(item => item.State == CheckState.Unknown))
            return Unknown("Active network profile or firewall profile evidence was incomplete.");

        try
        {
            var networkEvidence = networkProfiles[0].Evidence;
            var firewallEvidence = firewallProfiles[0].Evidence;
            if (networkEvidence is null || firewallEvidence is null) return Unknown("Required profile evidence was not retained.");
            using var networkDocument = JsonDocument.Parse(networkEvidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            using var firewallDocument = JsonDocument.Parse(firewallEvidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var networks = ReadNetworks(networkDocument.RootElement);
            var firewalls = ReadFirewalls(firewallDocument.RootElement);
            token.ThrowIfCancellationRequested();
            if (networks is null || firewalls is null) return Unknown("Retained profile evidence could not be correlated safely.");
            if (networks.Count == 0)
                return new("Active network firewall alignment", CheckState.Information,
                    "No active Windows network profiles were reported, so no profile-to-firewall alignment was available." + Scope);

            var matches = networks.Select(item => new Alignment(item.Index, item.Category,
                firewalls.TryGetValue(item.Category == "DomainAuthenticated" ? "Domain" : item.Category, out bool enabled) && enabled)).ToArray();
            int disabled = matches.Count(item => !item.Enabled);
            string detail = string.Join("; ", matches.Take(8).Select(item =>
                $"interface {item.InterfaceIndex} uses {item.Category} with its matching firewall profile {(item.Enabled ? "enabled" : "disabled")}"));
            if (matches.Length > 8) detail += $"; {matches.Length - 8} additional profile assignments retained in evidence";
            var evidence = new ToolResult(0, JsonSerializer.Serialize(new { Alignments = matches }, Capture.JsonOptions), "", false);
            return new("Active network firewall alignment", disabled == 0 ? CheckState.Information : CheckState.Attention,
                (disabled == 0 ? "Every active network profile has its matching Windows firewall profile enabled. " :
                    $"{disabled} active network profile assignment(s) have a matching Windows firewall profile configured disabled. Review effective policy and other security products before changing it. ") +
                detail + "." + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Unknown("Retained profile evidence could not be interpreted: " + error.Message);
        }
    }

    private static List<NetworkAssignment>? ReadNetworks(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
            !root.TryGetProperty("profiles", out var profiles) || profiles.ValueKind != JsonValueKind.Array || profiles.GetArrayLength() > 32)
            return null;
        var result = new List<NetworkAssignment>();
        var indexes = new HashSet<int>();
        foreach (var profile in profiles.EnumerateArray())
        {
            if (profile.ValueKind != JsonValueKind.Object || profile.EnumerateObject().Count() != 5 ||
                !profile.TryGetProperty("InterfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index <= 0 || !indexes.Add(index) ||
                !profile.TryGetProperty("NetworkCategory", out var categoryValue) || categoryValue.ValueKind != JsonValueKind.String ||
                categoryValue.GetString() is not ("Public" or "Private" or "DomainAuthenticated")) return null;
            result.Add(new(index, categoryValue.GetString()!));
        }
        return result;
    }

    private static Dictionary<string, bool>? ReadFirewalls(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
            !root.TryGetProperty("profiles", out var profiles) || profiles.ValueKind != JsonValueKind.Array || profiles.GetArrayLength() != 3)
            return null;
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var profile in profiles.EnumerateArray())
        {
            if (profile.ValueKind != JsonValueKind.Object || profile.EnumerateObject().Count() != 2 ||
                !profile.TryGetProperty("Name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                nameValue.GetString() is not ("Domain" or "Private" or "Public") ||
                !profile.TryGetProperty("Enabled", out var enabledValue) || enabledValue.ValueKind != JsonValueKind.String ||
                enabledValue.GetString() is not ("True" or "False") || !result.TryAdd(nameValue.GetString()!, enabledValue.GetString() == "True")) return null;
        }
        return result;
    }

    private sealed record NetworkAssignment(int Index, string Category);
    private sealed record Alignment(int InterfaceIndex, string Category, bool Enabled);
    private static WorkflowCheck Unknown(string detail) =>
        new("Active network firewall alignment", CheckState.Unknown, detail + Scope);
}
