using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class RoutePriorityChecks
{
    internal static async Task RunAsync()
    {
        object Row(int family = 2, int index = 7, string nextHop = "192.0.2.1", long routeMetric = 10,
            long interfaceMetric = 25, long? effectiveMetric = null, string automaticMetric = "Enabled",
            string state = "Alive", string protocol = "Dhcp") => new
            {
                family, interfaceIndex = index, nextHop, routeMetric, interfaceMetric,
                effectiveMetric = effectiveMetric ?? routeMetric + interfaceMetric, automaticMetric, state, protocol
            };
        ToolResult Evidence(object[] routes, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, routes }), "", false);

        var clearEvidence = Evidence([Row(), Row(23, 9, "2001:db8::1", 5, 20, protocol: "RouterAdvertisement")]);
        var clear = RoutePriorityDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) &&
            clear.Detail.Contains("route 10 + interface 25 = 35", StringComparison.Ordinal),
            "Distinct lowest effective route metrics remain informational with calculation evidence");
        var tied = RoutePriorityDiagnostics.Interpret(Evidence([Row(), Row(index: 8, nextHop: "192.0.2.2", routeMetric: 20, interfaceMetric: 15)]));
        Require(tied.State == CheckState.Attention && tied.Detail.Contains("share the lowest effective metric", StringComparison.Ordinal),
            "Equal lowest effective metrics on distinct live routes request review");
        Require(RoutePriorityDiagnostics.Interpret(Evidence([Row(state: "Dead")])).State == CheckState.Attention,
            "A retained non-Alive default route requests current-path review");
        Require(RoutePriorityDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "No default routes leaves priority comparison inapplicable without inventing a repair");

        foreach (var invalid in new[]
        {
            Evidence([Row()], false), Evidence([Row(effectiveMetric: 99)]), Evidence([Row(family: 2, nextHop: "2001:db8::1")]),
            Evidence([Row(automaticMetric: "Maybe")]), Evidence([Row(), Row(protocol: "dhcp")]),
            Evidence(Enumerable.Range(0, 129).Select(i => Row(index: i + 1, nextHop: "192.0.2." + ((i % 250) + 1), routeMetric: i)).ToArray()),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { Error = "denied" },
            clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }
        })
            Require(RoutePriorityDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, inconsistent, duplicate, excessive, malformed and failed evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await RoutePriorityDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(clearEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Get-NetRoute", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Get-NetIPInterface", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-NetRoute", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-NetIPInterface", StringComparison.Ordinal),
            "Native plan reads routes and interface metrics without changing priority");
        Require((await RoutePriorityDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await RoutePriorityDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: effective default-route metrics, equal-priority review, bounds and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
