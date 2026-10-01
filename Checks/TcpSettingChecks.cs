using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class TcpSettingChecks
{
    internal static async Task RunAsync()
    {
        object Row(string name = "Internet", int local = 3, int effective = 3, int congestion = 0,
            int ecn = 0, int timestamps = 0) => new
            {
                settingName = name, minRto = 300, initialCongestionWindow = 10, congestionProvider = congestion,
                autoTuningLocal = local, autoTuningEffective = effective, ecnCapability = ecn, timestamps,
                initialRto = 3000, scalingHeuristics = 0, maxSynRetransmissions = 2
            };
        ToolResult Evidence(object[] settings, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, settings }), "", false);

        var evidence = Evidence([Row(), Row("DatacenterCustom", local: 4, effective: 2, congestion: 3, ecn: 1, timestamps: 1)]);
        var result = TcpSettingDiagnostics.Interpret(evidence);
        Require(result is { State: CheckState.Information, Repair: null } && ReferenceEquals(result.Evidence, evidence) &&
            result.Detail.Contains("Reviewed 2 TCP templates", StringComparison.Ordinal) &&
            result.Detail.Contains("1 have different local and effective", StringComparison.Ordinal) &&
            result.Detail.Contains("Numeric enum values are retained exactly", StringComparison.Ordinal) &&
            result.Detail.Contains("No TCP template, connection, port range", StringComparison.Ordinal),
            "Valid TCP templates remain configuration context without selecting a change");

        foreach (var invalid in new[]
        {
            Evidence([], true), Evidence([Row()], false), Evidence([Row(), Row("internet")]),
            Evidence([Row(local: 256)]),
            Evidence([.. Enumerable.Range(0, 65).Select(index => Row("Template" + index))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            evidence with { ExitCode = 5 }, evidence with { Error = "denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true }
        })
            Require(TcpSettingDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Empty, incomplete, duplicate, invalid, excessive, malformed and failed TCP evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await TcpSettingDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(evidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") &&
            planned.Arguments.Last().Contains("Get-NetTCPSetting", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Select-Object -First 65", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-NetTCPSetting", StringComparison.Ordinal),
            "Native plan reads bounded TCP settings without changing them");
        Require((await TcpSettingDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "TCP setting provider failure remains unknown");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await TcpSettingDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents TCP setting collection");
        Console.WriteLine("PASS: bounded TCP setting templates, exact enum evidence, incomplete handling and read-only collection; fixtures only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
