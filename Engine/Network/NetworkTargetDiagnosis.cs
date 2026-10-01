using System.Net.Sockets;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class NetworkTargetDiagnosis
{
    public static WorkflowCheck Check(WorkflowCheck path, WorkflowCheck gateways, WorkflowCheck? lookup, WorkflowCheck? tcp,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(gateways);
        token.ThrowIfCancellationRequested();
        if (lookup is null && tcp is null) throw new ArgumentException("Supply a hostname lookup or TCP result.");
        const string name = "Selected target diagnosis";
        const string boundary = " This layered result is limited to the selected target and point in time. It does not prove Internet health, firewall policy, TLS, authentication or application behavior. No network setting changed.";
        WorkflowCheck Result(CheckState state, string detail) => new(name, state, detail + boundary,
            Evidence: tcp?.Evidence ?? lookup?.Evidence ?? gateways.Evidence ?? path.Evidence);
        try
        {
            var dns = lookup is null ? null : ReadDns(lookup);
            var socket = tcp is null ? null : ReadTcp(tcp);
            if (dns is not null && socket is not null && !string.Equals(dns.Host, socket.Host, StringComparison.OrdinalIgnoreCase))
                return Result(CheckState.Information,
                    $"The hostname lookup targeted {dns.Host}, while the TCP check targeted {socket.Host}:{socket.Port}. They remain separate observations and cannot form one end-to-end chain.");
            if (path.EffectiveState == CheckState.Unknown)
                return Result(CheckState.Unknown, "Current route and DNS-server correlation is incomplete. Recollect the local path before attributing a target failure.");
            if (gateways.EffectiveState == CheckState.Unknown)
                return Result(CheckState.Unknown, "Default-gateway evidence is incomplete. The selected target result cannot be placed safely beyond the local path.");
            if (lookup?.EffectiveState == CheckState.Unknown || tcp?.EffectiveState == CheckState.Unknown)
                return Result(CheckState.Unknown, "The selected hostname or TCP observation timed out or is incomplete. No failing layer was established.");

            if (socket?.State == TcpCheckState.Connected)
            {
                string dnsText = dns?.State == DnsLookupState.Resolved ? " The same hostname resolved through the configured system resolver." : "";
                string gatewayText = gateways.EffectiveState == CheckState.Attention
                    ? " The destination still accepted TCP even though the exact gateway ICMP probe did not succeed; ICMP can be filtered."
                    : "";
                return Result(CheckState.Information,
                    $"TCP connection to {socket.Host}:{socket.Port} was accepted without sending application data.{dnsText}{gatewayText} The local route, resolver and transport observations contain no blocking contradiction in this selected chain.");
            }
            if (dns?.State is DnsLookupState.NoAddresses or DnsLookupState.Failed)
                return Result(CheckState.Attention,
                    $"Name resolution for {dns.Host} failed before a destination transport result could be established. Review spelling, namespace, Hosts mappings, DNS policy and resolver reachability before changing adapters or firewall rules.");
            if (socket is not null && socket.State == TcpCheckState.Failed)
            {
                string detail = socket.ErrorCode switch
                {
                    SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain =>
                        $"The TCP provider could not resolve {socket.Host}. Review the hostname and resolver path before investigating port {socket.Port}.",
                    SocketError.NetworkDown or SocketError.SystemNotReady or SocketError.NetworkUnreachable or SocketError.HostUnreachable =>
                        $"The TCP provider reported that the network or route to {socket.Host}:{socket.Port} was unavailable. Review the selected interface, VPN and default path before changing the destination service.",
                    SocketError.ConnectionRefused =>
                        $"The route reached {socket.Host}, but TCP port {socket.Port} was actively refused. Confirm the intended destination and inspect its listener and intervening rejection policy.",
                    SocketError.AccessDenied =>
                        $"Local socket access to {socket.Host}:{socket.Port} was denied. Review local policy and endpoint protection without disabling safeguards.",
                    SocketError.ConnectionReset =>
                        $"The connection to {socket.Host}:{socket.Port} was reset. Review the destination service and intervening devices; this evidence does not identify which component reset it.",
                    _ => $"TCP connection to {socket.Host}:{socket.Port} failed. Review the exact socket result, destination listener and intervening path."
                };
                if (path.EffectiveState == CheckState.Attention)
                    detail += " Local route or DNS-server configuration also needs review, so the destination is not established as the first failing layer.";
                else if (gateways.EffectiveState == CheckState.Attention)
                    detail += " The gateway probe also needs review, although ICMP filtering prevents treating it as proof of a gateway outage.";
                return Result(CheckState.Attention, detail);
            }
            if (dns?.State == DnsLookupState.Resolved)
            {
                string context = path.EffectiveState == CheckState.Attention
                    ? " Local path configuration still needs review."
                    : gateways.EffectiveState == CheckState.Attention
                        ? " The gateway ICMP result needs review, but it does not invalidate a resolver answer."
                        : " No blocking contradiction was found in the collected local path evidence.";
                return Result(CheckState.Information,
                    $"The configured system resolver returned address data for {dns.Host}.{context} Transport and application availability were not tested.");
            }
            return Result(CheckState.Unknown, "The selected target evidence did not contain a recognized complete outcome.");
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Result(CheckState.Unknown, "Selected target evidence was malformed or inconsistent. Recollect it before drawing a layered conclusion.");
        }
    }

    private static DnsLookupResult ReadDns(WorkflowCheck check)
    {
        if (check.Evidence is null) throw new InvalidDataException("Missing DNS evidence.");
        var value = JsonSerializer.Deserialize<DnsLookupResult>(check.Evidence.Output, Capture.JsonOptions)
            ?? throw new InvalidDataException("Missing DNS result.");
        DnsDiagnostics.ValidateHost(value.Host);
        if (!Enum.IsDefined(value.State) || value.Addresses.Length > 64) throw new InvalidDataException("Invalid DNS result.");
        return value;
    }

    private static TcpCheckResult ReadTcp(WorkflowCheck check)
    {
        if (check.Evidence is null) throw new InvalidDataException("Missing TCP evidence.");
        var value = JsonSerializer.Deserialize<TcpCheckResult>(check.Evidence.Output, Capture.JsonOptions)
            ?? throw new InvalidDataException("Missing TCP result.");
        TcpDiagnostics.Validate(value.Host, value.Port);
        if (!Enum.IsDefined(value.State)) throw new InvalidDataException("Invalid TCP result.");
        return value;
    }
}
