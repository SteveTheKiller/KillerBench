using System.Net;
using System.Net.Sockets;
using KillerBench.Engine;

internal static class TcpChecks
{
    internal static async Task RunAsync()
    {
        Require(new WindowsWorkflowHost().NetworkTimeout == TimeSpan.FromSeconds(10), "Workflow network timeout defaults to ten seconds");
        foreach (var invalidLimit in new[] { TimeSpan.Zero, TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(61) })
        {
            bool rejected = false;
            try { _ = new WindowsWorkflowHost { NetworkTimeout = invalidLimit }; }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "Workflow host rejects unbounded network waits before collection");
        }
        Require(new WindowsWorkflowHost { NetworkTimeout = TimeSpan.FromMinutes(1) }.NetworkTimeout == TimeSpan.FromMinutes(1), "Workflow accepts the maximum network wait");
        bool missingTaskRejected = false;
        try { await TcpDiagnostics.CheckAsync("example.test", 443, connector: (_, _, _) => null!); }
        catch (InvalidOperationException) { missingTaskRejected = true; }
        Require(missingTaskRejected, "Missing TCP provider task cannot establish a connection");
        Require(new WindowsWorkflowHost().NetworkEndpoint is null, "TCP is optional by default");
        foreach (var (code, phrase) in new[] { (SocketError.ConnectionRefused, "actively refused"), (SocketError.HostNotFound, "DNS namespace"),
            (SocketError.TryAgain, "temporary failure"), (SocketError.NetworkUnreachable, "usable route"), (SocketError.NetworkDown, "subsystem"),
            (SocketError.AccessDenied, "permissions"), (SocketError.TimedOut, "timeout"), (SocketError.ConnectionReset, "was reset"), (SocketError.Fault, "reported socket error") })
        {
            var failure = await TcpDiagnostics.CheckAsync("example.test", 443, TimeSpan.FromSeconds(3), connector: (_, _, _) => Task.FromException(new SocketException((int)code)));
            Require(failure.State == TcpCheckState.Failed && failure.ErrorCode == code && failure.Detail.Contains(phrase) &&
                TcpDiagnostics.ToWorkflowCheck(failure).Repair is null, "TCP failures retain specific evidence without granting repair eligibility");
            var json = TcpDiagnostics.ToWorkflowCheck(failure).Evidence!.Output;
            var restored = System.Text.Json.JsonSerializer.Deserialize<TcpCheckResult>(json, Capture.JsonOptions)!;
            Require(restored.ErrorCode == code && restored.Timeout == TimeSpan.FromSeconds(3),
                "TCP error code and configured timeout survive workflow evidence serialization");
        }
        try { _ = new WindowsWorkflowHost { NetworkEndpoint = ("localhost", 0) }; throw new Exception("Invalid endpoint accepted"); }
        catch (ArgumentException) { }
        foreach (var state in Enum.GetValues<TcpCheckState>())
        {
            var check = TcpDiagnostics.ToWorkflowCheck(new("example.test", 443, state, "Fixture"));
            Require(check.Repair is null && check.State == (state == TcpCheckState.Connected ? CheckState.Clear : state == TcpCheckState.Failed ? CheckState.Attention : CheckState.Unknown), "TCP outcomes never grant repair eligibility");
            var retained = System.Text.Json.JsonSerializer.Deserialize<TcpCheckResult>(check.Evidence!.Output, Capture.JsonOptions)!;
            Require(retained.Host == "example.test" && retained.Port == 443 && retained.State == state &&
                check.Evidence.TimedOut == (state == TcpCheckState.TimedOut), "Workflow retains TCP target, port, result and timeout state");
        }
        Require((await TcpDiagnostics.CheckAsync("example.test", 443, connector: (_, _, _) => Task.CompletedTask)).State == TcpCheckState.Connected, "TCP success is distinct");
        Require((await TcpDiagnostics.CheckAsync("example.test", 443, connector: (_, _, _) => Task.FromException(new SocketException((int)SocketError.ConnectionRefused)))).State == TcpCheckState.Failed, "Connection refusal is not success");
        var timedOut = await TcpDiagnostics.CheckAsync("example.test", 443, TimeSpan.FromMilliseconds(20), connector: (_, _, _) => new TaskCompletionSource().Task);
        Require(timedOut.State == TcpCheckState.TimedOut && timedOut.Timeout == TimeSpan.FromMilliseconds(20), "Unresponsive connector is bounded and retains its actual configured limit");
        using var stop = new CancellationTokenSource();
        try { await TcpDiagnostics.CheckAsync("example.test", 443, token: stop.Token, connector: (_, _, _) => { stop.Cancel(); return Task.CompletedTask; }); throw new Exception("Cancellation lost"); }
        catch (OperationCanceledException) { }
        foreach (int port in new[] { 0, -1, 65536 })
        { try { TcpDiagnostics.Validate("localhost", port); throw new Exception("Invalid port accepted"); } catch (ArgumentException) { } }
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int localPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var accept = listener.AcceptTcpClientAsync(deadline.Token);
        Require((await TcpDiagnostics.CheckAsync("127.0.0.1", localPort, token: deadline.Token)).State == TcpCheckState.Connected, "Native loopback handshake completes");
        using var peer = await accept;
        Require(await peer.GetStream().ReadAsync(new byte[1], deadline.Token) == 0, "Probe closes without sending application data");
        using var reserved = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ExclusiveAddressUse = true };
        reserved.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var refused = await TcpDiagnostics.CheckAsync("127.0.0.1", ((IPEndPoint)reserved.LocalEndPoint!).Port, token: deadline.Token);
        Require(refused.State == TcpCheckState.Failed && refused.ErrorCode == SocketError.ConnectionRefused &&
            refused.Detail.Contains("actively refused") && TcpDiagnostics.ToWorkflowCheck(refused).Repair is null,
            "Native bound non-listening loopback socket retains refusal without repair eligibility");
        Console.WriteLine("PASS: TCP outcomes, bounded waits, cancellation, port validation and native loopback without payload.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
