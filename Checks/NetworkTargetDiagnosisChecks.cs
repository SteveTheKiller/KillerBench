using System.Net;
using System.Net.Sockets;
using KillerBench.Engine;

internal static class NetworkTargetDiagnosisChecks
{
    internal static void Run()
    {
        WorkflowCheck State(string name, CheckState state) => new(name, state, "fixture", Evidence: new ToolResult(0, "{}", "", false));
        var path = State("Network path correlation", CheckState.Information);
        var gateway = State("Default gateway reachability", CheckState.Information);
        WorkflowCheck Dns(string host, DnsLookupState state, params string[] addresses) => DnsDiagnostics.ToWorkflowCheck(
            new(host, state, addresses, "fixture") { ErrorCode = state == DnsLookupState.Failed ? SocketError.HostNotFound : null });
        WorkflowCheck Tcp(string host, int port, TcpCheckState state, SocketError? error = null) => TcpDiagnostics.ToWorkflowCheck(
            new(host, port, state, "fixture") { ErrorCode = error });

        var connected = NetworkTargetDiagnosis.Check(path, gateway, Dns("server.example", DnsLookupState.Resolved, "192.0.2.10"),
            Tcp("server.example", 443, TcpCheckState.Connected));
        Require(connected is { State: CheckState.Information, Repair: null } && connected.Detail.Contains("accepted", StringComparison.Ordinal) &&
            connected.Detail.Contains("does not prove Internet health", StringComparison.Ordinal),
            "Matching DNS and accepted TCP evidence produce a bounded layered target verdict");
        var dnsFailure = NetworkTargetDiagnosis.Check(path, gateway, Dns("missing.example", DnsLookupState.Failed), null);
        Require(dnsFailure.State == CheckState.Attention && dnsFailure.Detail.Contains("failed before a destination transport result", StringComparison.Ordinal),
            "Hostname failure identifies the resolution layer without selecting repair");
        var refused = NetworkTargetDiagnosis.Check(path, gateway, null,
            Tcp("192.0.2.20", 8443, TcpCheckState.Failed, SocketError.ConnectionRefused));
        Require(refused.State == CheckState.Attention && refused.Detail.Contains("actively refused", StringComparison.Ordinal) && refused.Repair is null,
            "Connection refusal points to the destination listener or rejection policy");
        var routeFailure = NetworkTargetDiagnosis.Check(State("Network path correlation", CheckState.Attention), gateway, null,
            Tcp("192.0.2.20", 443, TcpCheckState.Failed, SocketError.NetworkUnreachable));
        Require(routeFailure.State == CheckState.Attention && routeFailure.Detail.Contains("Local route or DNS-server configuration also needs review", StringComparison.Ordinal),
            "Local path findings prevent blaming the destination as the first failed layer");
        var timedOut = NetworkTargetDiagnosis.Check(path, gateway, null, Tcp("192.0.2.20", 443, TcpCheckState.TimedOut));
        Require(timedOut.State == CheckState.Unknown && timedOut.Detail.Contains("No failing layer was established", StringComparison.Ordinal),
            "Timeout remains ambiguous instead of being labeled a firewall failure");
        var distinct = NetworkTargetDiagnosis.Check(path, gateway, Dns("one.example", DnsLookupState.Resolved, "192.0.2.1"),
            Tcp("two.example", 443, TcpCheckState.Connected));
        Require(distinct.State == CheckState.Information && distinct.Detail.Contains("cannot form one end-to-end chain", StringComparison.Ordinal),
            "Different selected targets remain separate observations");
        var malformed = Dns("bad.example", DnsLookupState.Resolved, "192.0.2.1") with { Evidence = new ToolResult(0, "broken", "", false) };
        Require(NetworkTargetDiagnosis.Check(path, gateway, malformed, null).State == CheckState.Unknown,
            "Malformed selected-target evidence fails closed");
        try { NetworkTargetDiagnosis.Check(path, gateway, null, null); throw new Exception("Missing target accepted"); }
        catch (ArgumentException) { }
        Console.WriteLine("PASS: selected network target diagnosis orders path, gateway, DNS and TCP evidence without widening repairs.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
