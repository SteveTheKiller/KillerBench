using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class AdapterBindingChecks
{
    internal static async Task RunAsync()
    {
        object Binding(string component, bool enabled) => new { component, enabled };
        object Adapter(string name = "Ethernet", int index = 7, string status = "Up", bool hardware = true,
            object[]? bindings = null) => new { name, interfaceIndex = index, status, hardware,
                bindings = bindings ?? [Binding("ms_tcpip", true), Binding("ms_tcpip6", true), Binding("ms_msclient", true), Binding("ms_server", false)] };
        ToolResult Evidence(object[] adapters, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, adapters }), "", false);

        var clearEvidence = Evidence([Adapter()]);
        var clear = AdapterBindingDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) &&
            clear.Detail.Contains("file sharing is explicitly disabled on 1", StringComparison.Ordinal),
            "Optional file-sharing state remains informational while IP protocol evidence is complete");
        var ipv4Only = Adapter(bindings: [Binding("ms_tcpip", true), Binding("ms_tcpip6", false)]);
        Require(AdapterBindingDiagnostics.Interpret(Evidence([ipv4Only])).State == CheckState.Information,
            "One intentionally disabled IP family does not invent a fault");
        var noIp = Adapter(bindings: [Binding("ms_tcpip", false), Binding("ms_tcpip6", false)]);
        Require(AdapterBindingDiagnostics.Interpret(Evidence([noIp])).State == CheckState.Attention,
            "An active hardware adapter with both IP protocols disabled requests review");
        Require(AdapterBindingDiagnostics.Interpret(Evidence([Adapter(status: "Disconnected", bindings: [])])).State == CheckState.Information,
            "Inactive adapters do not require IP binding evidence");
        Require(AdapterBindingDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "Empty visible adapter inventory remains scoped information");

        foreach (var invalid in new[]
        {
            Evidence([Adapter()], false), Evidence([Adapter(bindings: [Binding("ms_tcpip", true)])]),
            Evidence([Adapter(), Adapter(name: "ethernet", index: 8)]), Evidence([Adapter(), Adapter(name: "Wi-Fi", index: 7)]),
            Evidence([Adapter(bindings: [Binding("ms_tcpip", true), Binding("ms_tcpip", false)])]),
            Evidence([Adapter(bindings: [Binding("vendor_filter", true)])]),
            Evidence(Enumerable.Range(0, 257).Select(i => Adapter("Adapter " + i, i + 1, "Disconnected", false, [])).ToArray()),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { Error = "denied" },
            clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }
        })
            Require(AdapterBindingDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, missing, duplicate, unsupported, excessive, malformed and failed evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await AdapterBindingDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(clearEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Get-NetAdapterBinding", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Enable-NetAdapterBinding", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Disable-NetAdapterBinding", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-NetAdapterBinding", StringComparison.Ordinal),
            "Native plan reads bounded bindings without changing an adapter");
        Require((await AdapterBindingDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await AdapterBindingDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: bounded adapter protocol bindings, active dual-IP review and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
