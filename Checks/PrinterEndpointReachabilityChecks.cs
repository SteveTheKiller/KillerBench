using System.IO;
using System.Net.Sockets;
using KillerBench.Engine;

internal static class PrinterEndpointReachabilityChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(string queues, string ports, string complete = "true") => new(0,
            $"{{\"complete\":{complete},\"queues\":{queues},\"ports\":{ports}}}", "", false);
        IReadOnlyList<WorkflowCheck> Configuration(ToolResult evidence, CheckState state = CheckState.Information) =>
            [new("Printer endpoint: fixture", state, "Fixture", Evidence: evidence)];
        const string queues = "[{\"name\":\"Office\",\"portName\":\"IP_Office\",\"type\":0}]";
        const string ports = "[{\"name\":\"IP_Office\",\"host\":\"192.0.2.10\",\"port\":9100,\"protocol\":1,\"snmpEnabled\":true}]";
        var evidence = Evidence(queues, ports);
        var contacted = new List<string>();
        var connected = await PrinterEndpointReachability.CheckAsync(Configuration(evidence), probe: (host, port, _) =>
        {
            contacted.Add(host + ":" + port);
            return Task.FromResult(new TcpCheckResult(host, port, TcpCheckState.Connected, "Connected fixture."));
        });
        Require(contacted.SequenceEqual(["192.0.2.10:9100"]) && connected.Single() is { State: CheckState.Information, Repair: null } &&
            connected[0].Detail.Contains("does not establish printer identity", StringComparison.Ordinal),
            "Exact configured endpoint is contacted without claiming printer health");

        var refused = await PrinterEndpointReachability.CheckAsync(Configuration(evidence), probe: (host, port, _) =>
            Task.FromResult(new TcpCheckResult(host, port, TcpCheckState.Failed, "Connection refused fixture.") { ErrorCode = SocketError.ConnectionRefused }));
        Require(refused.Single() is { State: CheckState.Attention, Evidence.ExitCode: (int)SocketError.ConnectionRefused },
            "Refused configured endpoint needs attention and retains socket evidence");
        var timedOut = await PrinterEndpointReachability.CheckAsync(Configuration(evidence), probe: (host, port, _) =>
            Task.FromResult(new TcpCheckResult(host, port, TcpCheckState.TimedOut, "Timeout fixture.")));
        Require(timedOut.Single() is { State: CheckState.Attention, Evidence.TimedOut: true }, "Endpoint timeout is visible as bounded attention");

        var duplicateQueues = Evidence(queues.Replace("]", ",{\"name\":\"Warehouse\",\"portName\":\"IP_Office\",\"type\":0}]"), ports);
        int probes = 0;
        var grouped = await PrinterEndpointReachability.CheckAsync(Configuration(duplicateQueues), probe: (host, port, _) =>
        {
            probes++;
            return Task.FromResult(new TcpCheckResult(host, port, TcpCheckState.Connected, "Connected fixture."));
        });
        Require(probes == 1 && grouped.Single().Detail.Contains("used by 2 local queues", StringComparison.Ordinal),
            "Shared exact endpoint is connected once");

        foreach (var noTarget in new[]
        {
            Evidence("[]", ports),
            Evidence("[{\"name\":\"USB\",\"portName\":\"USB001\",\"type\":0}]", ports),
            Evidence("[{\"name\":\"Shared\",\"portName\":\"server\",\"type\":1}]", ports),
            Evidence("[{\"name\":\"Pool\",\"portName\":\"IP_Office,IP_Backup\",\"type\":0}]", ports)
        })
        {
            var result = await PrinterEndpointReachability.CheckAsync(Configuration(noTarget), probe: (_, _, _) => throw new Exception("No endpoint should be contacted."));
            Require(result.Single().State == CheckState.Information, "Unassigned and unsupported port types produce no connection attempt");
        }

        foreach (var invalid in new[]
        {
            Configuration(evidence with { ExitCode = 5 }), Configuration(evidence with { Truncated = true }),
            Configuration(Evidence(queues, ports, "false")), Configuration(evidence with { Output = "{}" }),
            [new("a", CheckState.Information, "fixture", Evidence: evidence), new("b", CheckState.Information, "fixture", Evidence: evidence with { Output = "{}" })]
        })
        {
            var result = await PrinterEndpointReachability.CheckAsync(invalid, probe: (_, _, _) => throw new Exception("Invalid evidence must not be contacted."));
            Require(result.Single().State == CheckState.Unknown, "Incomplete, failed, mixed and malformed evidence blocks contact");
        }

        string manyQueues = "[" + string.Join(',', Enumerable.Range(1, 33).Select(index =>
            $"{{\"name\":\"Queue{index}\",\"portName\":\"IP_{index}\",\"type\":0}}")) + "]";
        string manyPorts = "[" + string.Join(',', Enumerable.Range(1, 33).Select(index =>
            $"{{\"name\":\"IP_{index}\",\"host\":\"192.0.2.{index}\",\"port\":9100,\"protocol\":1,\"snmpEnabled\":false}}")) + "]";
        var excessive = await PrinterEndpointReachability.CheckAsync(Configuration(Evidence(manyQueues, manyPorts)),
            probe: (_, _, _) => throw new Exception("Excessive target inventory must not be contacted."));
        Require(excessive.Single().State == CheckState.Unknown && excessive[0].Detail.Contains("32-endpoint connection limit", StringComparison.Ordinal),
            "More than 32 distinct endpoints are rejected before contact");

        var mismatch = await PrinterEndpointReachability.CheckAsync(Configuration(evidence), probe: (_, _, _) =>
            Task.FromResult(new TcpCheckResult("192.0.2.11", 9100, TcpCheckState.Connected, "Wrong target fixture.")));
        Require(mismatch.Single().State == CheckState.Unknown, "Mismatched provider identity cannot establish reachability");
        var failed = await PrinterEndpointReachability.CheckAsync(Configuration(evidence), probe: (_, _, _) => throw new IOException("Fixture provider failure"));
        Require(failed.Single() is { State: CheckState.Unknown, Evidence.Error: "Fixture provider failure" }, "Provider failure remains unknown with evidence");

        using var stop = new CancellationTokenSource();
        try
        {
            await PrinterEndpointReachability.CheckAsync(Configuration(evidence), stop.Token, (_, _, _) =>
            {
                stop.Cancel();
                return new TaskCompletionSource<TcpCheckResult>().Task;
            }).WaitAsync(TimeSpan.FromSeconds(2));
            throw new Exception("Printer endpoint reachability ignored Stop");
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        Console.WriteLine("PASS: exact configured printer endpoint reachability, deduplication, bounds, failures and cancellation; simulated connections only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
