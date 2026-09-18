using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine;

public static class ServiceEventCorrelation
{
    private static readonly XNamespace EventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static WorkflowCheck Check(WorkflowCheck inventory, IReadOnlyList<WorkflowCheck> current, WorkflowCheck history, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();
        const string name = "Service failure correlation";
        const string scope = " Historical events do not prove a current failure or authorize a service start, stop, restart, startup change, dependency change or recovery action. No service configuration changed.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + scope, Evidence: history.Evidence ?? inventory.Evidence);
        if (inventory.EffectiveState == CheckState.Unknown || inventory.Evidence is null)
            return Unknown("A complete installed service inventory is required before event identities can be correlated.");
        if (history.EffectiveState == CheckState.Unknown || history.Evidence is null)
            return Unknown("Complete recent Service Control Manager evidence is required before service identities can be correlated.");
        if (current.Count > 128)
            return Unknown("The focused current service observations exceeded their evidence bound.");

        try
        {
            var catalog = ServiceInventoryDiagnostics.Parse(inventory.Evidence.Output);
            if (!catalog.Complete) return Unknown("The installed service inventory is incomplete.");
            var entries = catalog.Services.ToDictionary(service => service.Name, StringComparer.OrdinalIgnoreCase);
            var currentStates = new Dictionary<string, WorkflowCheck>(StringComparer.OrdinalIgnoreCase);
            foreach (var check in current)
            {
                token.ThrowIfCancellationRequested();
                if (check?.Evidence is not { } evidence) return Unknown("A focused current service observation is incomplete.");
                string[] matches = entries.Keys.Where(service => ServiceRecovery.MatchesService(evidence, service)).ToArray();
                if (matches.Length != 1 || !currentStates.TryAdd(matches[0], check))
                    return Unknown("A focused current service identity is missing or duplicated.");
            }

            var identities = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var service in catalog.Services)
            {
                AddIdentity(service.Name, service.Name);
                AddIdentity(service.DisplayName, service.Name);
            }
            var source = EventEvidence.FromXml("System", history.Evidence.Output);
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int unresolved = 0;
            foreach (string line in source.Evidence!.Lines)
            {
                token.ThrowIfCancellationRequested();
                var root = XElement.Parse(line, LoadOptions.None);
                var system = root.Element(EventNamespace + "System") ?? throw new InvalidDataException("Event metadata is missing.");
                string? provider = (string?)system.Element(EventNamespace + "Provider")?.Attribute("Name");
                string? level = system.Element(EventNamespace + "Level")?.Value;
                if (!string.Equals(provider, "Service Control Manager", StringComparison.Ordinal) || level is not ("1" or "2"))
                    throw new InvalidDataException("Unexpected service event record.");
                string[] matches = (root.Element(EventNamespace + "EventData")?.Elements(EventNamespace + "Data") ?? [])
                    .Select(field => field.Value.Trim())
                    .Concat((root.Element(EventNamespace + "UserData")?.Descendants() ?? []).Where(field => !field.HasElements).Select(field => field.Value.Trim()))
                    .Where(value => identities.TryGetValue(value, out string? service) && service is not null)
                    .Select(value => identities[value]!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (matches.Length != 1) { unresolved++; continue; }
                counts[matches[0]] = counts.GetValueOrDefault(matches[0]) + 1;
            }

            var matched = counts.OrderBy(pair => entries[pair.Key].DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
            bool unconfirmed = matched.Any(pair => currentStates.TryGetValue(pair.Key, out var check) && check.EffectiveState == CheckState.Unknown);
            bool attention = matched.Any(pair => currentStates.TryGetValue(pair.Key, out var check)
                ? check.EffectiveState is CheckState.Attention or CheckState.Repairable
                : entries[pair.Key].State == "Stopped" && entries[pair.Key].StartMode == "Auto");
            string detail = matched.Length == 0
                ? $"Reviewed {source.Evidence.Lines.Length} recent Service Control Manager failure record{(source.Evidence.Lines.Length == 1 ? "" : "s")}. No event payload value uniquely matched an installed service name or display name."
                : "Exact installed-service matches: " + string.Join("; ", matched.Select(pair =>
                {
                    var service = entries[pair.Key];
                    string state = currentStates.TryGetValue(pair.Key, out var check) ? check.EffectiveState.ToString() : service.State + ", " + service.StartMode;
                    return $"{service.DisplayName} [{service.Name}]: {pair.Value} historical record{(pair.Value == 1 ? "" : "s")}, current state {state}";
                })) + ".";
            if (unresolved > 0)
                detail += $" {unresolved} record{(unresolved == 1 ? " has" : "s have")} no single unique installed-service identity. Missing fields and display-name collisions remain unresolved.";
            detail += " Current states are point-in-time observations and do not establish complete feature health." + scope;
            return new(name, unconfirmed ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Information, detail, Evidence: history.Evidence);

            void AddIdentity(string value, string service)
            {
                if (!identities.TryGetValue(value, out string? existing)) identities[value] = service;
                else if (!string.Equals(existing, service, StringComparison.OrdinalIgnoreCase)) identities[value] = null;
            }
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or InvalidOperationException or ArgumentException)
        {
            return Unknown("Installed service and event evidence could not be correlated safely.");
        }
    }
}
