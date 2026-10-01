using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class IpInterfaceConfigurationChecks
{
    internal static async Task RunAsync()
    {
        object Row(int index = 7, string alias = "Ethernet", string family = "IPv4", string connection = "Connected",
            string dhcp = "Enabled", long mtu = 1500, string forwarding = "Disabled", string advertising = "Disabled",
            string discovery = "ControlledByDHCP", string automaticMetric = "Enabled", long metric = 25) => new
            {
                interfaceIndex = index, interfaceAlias = alias, addressFamily = family, connectionState = connection,
                dhcp, nlMtu = mtu, forwarding, advertising, routerDiscovery = discovery, automaticMetric, interfaceMetric = metric
            };
        ToolResult Evidence(object[] interfaces, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, interfaces }), "", false);

        var clearEvidence = Evidence([Row(), Row(family: "IPv6", dhcp: "Disabled", discovery: "Enabled")]);
        var clear = IpInterfaceConfigurationDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) &&
            clear.Detail.Contains("2 IP interface configurations", StringComparison.Ordinal),
            "Valid interface configuration remains informational and retains evidence");
        Require(IpInterfaceConfigurationDiagnostics.Interpret(Evidence([Row(mtu: 575)])).State == CheckState.Information,
            "Unusual IPv4 MTU remains evidence without an invented universal minimum");
        Require(IpInterfaceConfigurationDiagnostics.Interpret(Evidence([Row(family: "IPv6", mtu: 1279)])).State == CheckState.Attention,
            "Connected IPv6 MTU below its protocol minimum requests attention");
        Require(IpInterfaceConfigurationDiagnostics.Interpret(Evidence([Row(connection: "Disconnected", mtu: 500)])).State == CheckState.Information,
            "Disconnected low-MTU configuration does not claim a current path fault");
        Require(IpInterfaceConfigurationDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "No interfaces remains informational without inventing a repair");

        foreach (var invalid in new[]
        {
            Evidence([Row()], false), Evidence([Row(mtu: 0)]), Evidence([Row(family: "Other")]),
            Evidence([Row(connection: "Unknown")]), Evidence([Row(dhcp: "Maybe")]),
            Evidence([Row(discovery: "FutureValue")]), Evidence([Row(), Row(alias: "Renamed")]),
            Evidence([.. Enumerable.Range(0, 513).Select(i => Row(index: i + 1))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { Error = "denied" },
            clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }
        })
            Require(IpInterfaceConfigurationDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, invalid, duplicate, excessive, malformed and failed evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await IpInterfaceConfigurationDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(clearEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Get-NetIPInterface", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("$_.NlMtu", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-NetIPInterface", StringComparison.Ordinal),
            "Native plan reads bounded interface configuration without changing it");
        Require((await IpInterfaceConfigurationDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await IpInterfaceConfigurationDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: IP interface configuration, protocol MTU bounds and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
