using System.Net.NetworkInformation;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record AdapterSnapshot(string Name, string[] Addresses, string[] Gateways, string[] Dns);

public static class NetworkSnapshot
{
    public static IReadOnlyList<WorkflowCheck> Check(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(adapter => adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
        var checks = new List<WorkflowCheck>();
        var links = new List<WorkflowCheck>();
        foreach (var adapter in adapters)
        {
            token.ThrowIfCancellationRequested();
            var status = OperationalStatus.Unknown;
            links.Add(ReadLink(adapter.Name, () => status = adapter.OperationalStatus));
            if (status != OperationalStatus.Up) continue;
            checks.Add(WindowsWorkflowHost.ReadNetworkConfiguration(adapter.Name, () =>
            {
                var properties = adapter.GetIPProperties();
                return (properties.UnicastAddresses.Select(a => a.Address.ToString()).ToArray(),
                    properties.GatewayAddresses.Select(a => a.Address.ToString()).ToArray(),
                    properties.DnsAddresses.Select(a => a.ToString()).ToArray());
            }));
        }
        if (checks.Count == 0)
            checks.Add(links.Any(link => link.State == CheckState.Unknown)
                ? new("Network", CheckState.Unknown, "No active adapter was confirmed, but link collection is incomplete. Review the unavailable adapter evidence.")
                : new("Network", CheckState.Attention, "No active non-loopback adapter. Check the cable, Wi-Fi connection, or VPN."));
        else if (checks.All(check => check.State == CheckState.Information))
            checks.Add(new("Network", CheckState.Attention, "No active adapter has an IP address. Check the intended connection."));
        checks.AddRange(links);
        return checks.AsReadOnly();
    }

    public static WorkflowCheck ReadLink(string name, Func<OperationalStatus> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        try { return AssessLink(name, read()); }
        catch (Exception error) when (error is NetworkInformationException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException)
        {
            return new(name + " / Link", CheckState.Unknown,
                "Link state could not be read. The adapter may have changed during collection. No adapter settings changed.",
                Evidence: new ToolResult(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck AssessLink(string name, OperationalStatus status)
    {
        var evidence = new ToolResult(0, JsonSerializer.Serialize(new { Name = name, Status = status.ToString() }, Capture.JsonOptions), "", false);
        string detail = status switch
        {
            OperationalStatus.Up => "Link is up. This does not establish DNS, routing or Internet connectivity.",
            OperationalStatus.Down => "Link is down. This can be intentional; if this is the intended connection, review its cable, Wi-Fi association and administrative state.",
            OperationalStatus.Dormant => "Link is waiting for an external event. Review the intended connection or VPN session before changing settings.",
            OperationalStatus.LowerLayerDown => "An underlying interface is down. Review the parent connection used by this adapter.",
            OperationalStatus.NotPresent => "An interface component is absent. Review whether its device or virtual connection is expected to be present.",
            OperationalStatus.Testing => "The interface is in a testing state; normal traffic availability is not established.",
            _ => "Link state is unavailable or unrecognized. No connectivity verdict is available."
        };
        var state = status == OperationalStatus.Up ? CheckState.Clear :
            !Enum.IsDefined(status) || status == OperationalStatus.Unknown ? CheckState.Unknown : CheckState.Information;
        return new(name + " / Link", state, detail + " No adapter settings changed.", Evidence: evidence);
    }

    public static WorkflowCheck Assess(AdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
        if (snapshot.Addresses is null || snapshot.Gateways is null || snapshot.Dns is null)
            return new(snapshot.Name, CheckState.Unknown, "Adapter configuration is incomplete. No repair selected.", Evidence: evidence);
        return WindowsWorkflowHost.AssessNetwork(snapshot.Name, snapshot.Addresses, snapshot.Gateways, snapshot.Dns) with { Evidence = evidence };
    }
}
