using System.Net;
using System.Net.Sockets;

namespace KillerBench.Engine.Network;

public enum DnsLookupState { Resolved, NoAddresses, Failed, TimedOut }
public sealed record DnsLookupResult(string Host, DnsLookupState State, string[] Addresses, string Detail)
{
    public SocketError? ErrorCode { get; init; }
    public TimeSpan? Timeout { get; init; }
}

public static class DnsDiagnostics
{
    public static WorkflowCheck ToWorkflowCheck(DnsLookupResult result) => new(
        "Hostname lookup: " + result.Host,
        result.State == DnsLookupState.Resolved ? CheckState.Clear :
            result.State == DnsLookupState.TimedOut ? CheckState.Unknown : CheckState.Attention,
        result.Detail + (result.Addresses.Length == 0 ? "" : " Answers: " + string.Join(", ", result.Addresses)),
        Evidence: new ToolResult(result.State == DnsLookupState.Resolved ? 0 : (int?)result.ErrorCode ?? -1,
            System.Text.Json.JsonSerializer.Serialize(result, Capture.JsonOptions), "", false)
            { TimedOut = result.State == DnsLookupState.TimedOut || result.ErrorCode == SocketError.TimedOut });

    public static string ValidateHost(string host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (host.Length is < 1 or > 253 || host != host.Trim() || IPAddress.TryParse(host, out _) ||
            Uri.CheckHostName(host) != UriHostNameType.Dns)
            throw new ArgumentException("Enter a hostname, without a URL, port or IP address.", nameof(host));
        return host;
    }

    public static async Task<DnsLookupResult> CheckAsync(string host, TimeSpan? timeout = null,
        CancellationToken token = default, Func<string, CancellationToken, Task<IPAddress[]>>? resolver = null)
    {
        host = ValidateHost(host);
        var limit = timeout ?? TimeSpan.FromSeconds(10);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        token.ThrowIfCancellationRequested();
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(token);
        bounded.CancelAfter(limit);
        try
        {
            var pending = (resolver ?? ResolveAsync)(host, bounded.Token)
                ?? throw new InvalidOperationException("DNS provider returned no query task.");
            var addresses = await pending.WaitAsync(bounded.Token)
                ?? throw new InvalidOperationException("DNS provider returned no address result.");
            token.ThrowIfCancellationRequested();
            if (addresses.Any(address => address is null)) throw new InvalidOperationException("DNS provider returned a missing address.");
            var values = addresses.Select(address => address.ToString()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            return new(host, values.Length == 0 ? DnsLookupState.NoAddresses : DnsLookupState.Resolved, values,
                values.Length == 0 ? "The configured resolver returned no addresses. This does not identify the cause." :
                "The configured system resolver returned addresses. Hosts-file and cached answers may be included; server and Internet reachability were not tested.") { Timeout = limit };
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && bounded.IsCancellationRequested)
        { return new(host, DnsLookupState.TimedOut, [], "Name resolution exceeded the time limit. No DNS settings were changed.") { Timeout = limit }; }
        catch (SocketException error)
        {
            token.ThrowIfCancellationRequested();
            string guidance = error.SocketErrorCode switch
            {
                SocketError.HostNotFound => "The resolver did not recognize this hostname. Check spelling, the full domain name and the intended DNS namespace.",
                SocketError.NoData => "The resolver reported no requested address data. Check the hostname's address records in the intended namespace.",
                SocketError.TryAgain => "The resolver reported a temporary failure. Review resolver availability and retry after the underlying condition changes.",
                SocketError.NetworkDown or SocketError.SystemNotReady => "The network subsystem was unavailable. Review adapter and service state before changing DNS configuration.",
                SocketError.NetworkUnreachable or SocketError.HostUnreachable => "The socket provider reported an unreachable network or host. Review the intended route, VPN and resolver path.",
                SocketError.AccessDenied => "The lookup was denied by local socket permissions. Review access controls without disabling security protections.",
                SocketError.TimedOut => "The socket provider reported a timeout. Review resolver connectivity; a timeout alone does not identify the failing component.",
                _ => "Review the reported socket error and resolver configuration. This result does not identify a repair."
            };
            return new(host, DnsLookupState.Failed, [], $"Name resolution failed ({error.SocketErrorCode}). {guidance} No DNS settings were changed.")
                { ErrorCode = error.SocketErrorCode, Timeout = limit };
        }
    }

    private static Task<IPAddress[]> ResolveAsync(string host, CancellationToken token) =>
        Dns.GetHostAddressesAsync(host, token);
}
