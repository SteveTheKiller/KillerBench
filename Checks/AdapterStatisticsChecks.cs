using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class AdapterStatisticsChecks
{
    internal static async Task RunAsync()
    {
        object Row(string name = "Ethernet", int index = 7, string status = "Up", bool hardware = true,
            ulong rxErrors = 0, ulong txErrors = 0, ulong rxDiscards = 0, ulong txDiscards = 0) => new
            { name, interfaceIndex = index, status, hardware, receivedBytes = 1000UL, sentBytes = 500UL,
                receivedErrors = rxErrors, outboundErrors = txErrors, receivedDiscards = rxDiscards, outboundDiscards = txDiscards };
        ToolResult Evidence(object[] adapters, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, adapters }), "", false);

        var clearEvidence = Evidence([Row()]);
        var clear = AdapterStatisticsDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence), "Zero error and discard counters remain informational");
        Require(AdapterStatisticsDiagnostics.Interpret(Evidence([Row(rxErrors: 1)])).State == CheckState.Attention, "Received errors request review");
        Require(AdapterStatisticsDiagnostics.Interpret(Evidence([Row(txDiscards: 2)])).State == CheckState.Attention, "Outbound discards request review");
        Require(AdapterStatisticsDiagnostics.Interpret(Evidence([])).State == CheckState.Information, "Empty visible inventory remains scoped information");
        foreach (var invalid in new[]
        {
            Evidence([Row()], false), Evidence([Row(), Row("ethernet", 8)]), Evidence([Row(), Row("Wi-Fi", 7)]),
            Evidence(Enumerable.Range(0, 257).Select(i => Row("Adapter " + i, i + 1)).ToArray()),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            new ToolResult(0, "{\"complete\":true,\"adapters\":[{\"name\":\"Ethernet\",\"interfaceIndex\":7,\"status\":\"Up\",\"hardware\":true,\"receivedBytes\":-1,\"sentBytes\":0,\"receivedErrors\":0,\"outboundErrors\":0,\"receivedDiscards\":0,\"outboundDiscards\":0}]}", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { Error = "denied" }, clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }
        }) Require(AdapterStatisticsDiagnostics.Interpret(invalid).State == CheckState.Unknown, "Incomplete, duplicate, excessive, malformed and failed evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await AdapterStatisticsDiagnostics.CheckAsync(execute: (step, token) => { planned = step; bounded = token.CanBeCanceled; return Task.FromResult(clearEvidence); });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Last().Contains("Get-NetAdapterStatistics", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Restart-NetAdapter", StringComparison.Ordinal) && !planned.Arguments.Last().Contains("Disable-NetAdapter", StringComparison.Ordinal),
            "Native plan reads bounded counters without changing an adapter");
        Require((await AdapterStatisticsDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown, "Provider failure remains unknown");
        using var stop = new CancellationTokenSource(); stop.Cancel();
        bool canceled = false;
        try { await AdapterStatisticsDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); } catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: bounded adapter byte, error and discard counters remain read only; fixtures only.");
    }

    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
