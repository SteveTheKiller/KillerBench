using System.Net;
using System.Net.Sockets;

namespace KillerBench.Engine.Network;

public enum TcpCheckState { Connected, Failed, TimedOut }
public sealed record TcpCheckResult(string Host, int Port, TcpCheckState State, string Detail)
{
    public SocketError? ErrorCode { get; init; }
    public TimeSpan? Timeout { get; init; }
}

public static class TcpDiagnostics
{
    public static WorkflowCheck ToWorkflowCheck(TcpCheckResult result) => new(
        $"TCP connection: {result.Host}:{result.Port}",
        result.State == TcpCheckState.Connected ? CheckState.Clear :
            result.State == TcpCheckState.Failed ? CheckState.Attention : CheckState.Unknown,
        result.Detail,
        Evidence: new ToolResult(result.State == TcpCheckState.Connected ? 0 : (int?)result.ErrorCode ?? -1,
            System.Text.Json.JsonSerializer.Serialize(result, Capture.JsonOptions), "", false)
            { TimedOut = result.State == TcpCheckState.TimedOut || result.ErrorCode == SocketError.TimedOut });

    public static void Validate(string host, int port)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (host != host.Trim() || host.Length is < 1 or > 253) throw new ArgumentException("Enter a hostname or IP address.", nameof(host));
        if (!IPAddress.TryParse(host, out _)) DnsDiagnostics.ValidateHost(host);
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port), "TCP port must be 1 through 65535.");
    }

    public static async Task<TcpCheckResult> CheckAsync(string host, int port, TimeSpan? timeout = null,
        CancellationToken token = default, Func<string, int, CancellationToken, Task>? connector = null)
    {
        Validate(host, port);
        var limit = timeout ?? TimeSpan.FromSeconds(10);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(1)) throw new ArgumentOutOfRangeException(nameof(timeout));
        token.ThrowIfCancellationRequested();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(limit);
        try
        {
            var pending = (connector ?? ConnectAsync)(host, port, stop.Token)
                ?? throw new InvalidOperationException("TCP provider returned no connection task.");
            await pending.WaitAsync(stop.Token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return new(host, port, TcpCheckState.Connected, "A TCP connection was accepted. No application data was sent. Authentication, TLS and application health were not tested.") { Timeout = limit };
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && stop.IsCancellationRequested)
        { return new(host, port, TcpCheckState.TimedOut, "The connection did not complete within the limit. This does not identify a firewall or service fault.") { Timeout = limit }; }
        catch (SocketException error)
        {
            token.ThrowIfCancellationRequested();
            string guidance = error.SocketErrorCode switch
            {
                SocketError.ConnectionRefused => "The connection was actively refused. Confirm the destination and port, then check the listener and any device that can reject the connection.",
                SocketError.HostNotFound or SocketError.NoData => "The destination name could not be resolved to an address. Check the full hostname and intended DNS namespace before investigating the port.",
                SocketError.TryAgain => "Name resolution reported a temporary failure. Review the resolver path before testing the destination service.",
                SocketError.NetworkUnreachable or SocketError.HostUnreachable => "The socket provider reported no usable route. Review the intended interface, gateway and VPN route.",
                SocketError.NetworkDown or SocketError.SystemNotReady => "The local network subsystem was unavailable. Review adapter and service state.",
                SocketError.AccessDenied => "Local socket permissions denied the connection. Review policy and access controls without disabling protections.",
                SocketError.TimedOut => "The socket provider reported a timeout. This alone does not identify a firewall, route or service fault.",
                SocketError.ConnectionReset => "The connection was reset. Review the destination service and intervening devices; a reset does not establish which component caused it.",
                _ => "Review the reported socket error, destination and service state. This result does not identify a repair."
            };
            return new(host, port, TcpCheckState.Failed, $"TCP connection failed ({error.SocketErrorCode}). {guidance} No network settings were changed.")
                { ErrorCode = error.SocketErrorCode, Timeout = limit };
        }
    }

    private static async Task ConnectAsync(string host, int port, CancellationToken token)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, token).ConfigureAwait(false);
    }
}
