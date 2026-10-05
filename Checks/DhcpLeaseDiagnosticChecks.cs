using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class DhcpLeaseDiagnosticChecks
{
    internal static async Task RunAsync()
    {
        const string captured = "2026-09-15T12:00:00.0000000+00:00";
        object Row(int index = 7, string setting = "11111111-1111-1111-1111-111111111111",
            bool usable = true, bool server = true, string? obtained = "2026-09-15T10:00:00.0000000+00:00",
            string? expires = "2026-09-16T10:00:00.0000000+00:00") => new
            { interfaceIndex = index, settingId = setting, hasUsableIpv4 = usable, hasServer = server, leaseObtainedUtc = obtained, leaseExpiresUtc = expires };
        ToolResult Evidence(object[] adapters, bool complete = true, string capturedUtc = captured) =>
            new(0, JsonSerializer.Serialize(new { complete, capturedUtc, adapters }), "", false);

        var ordinaryEvidence = Evidence([Row()]);
        var ordinary = DhcpLeaseDiagnostics.Interpret(ordinaryEvidence);
        Require(ordinary is { State: CheckState.Information, Repair: null } && ReferenceEquals(ordinary.Evidence, ordinaryEvidence) &&
            ordinary.Detail.Contains("1 connected physical IPv4 DHCP adapters", StringComparison.Ordinal) &&
            ordinary.Detail.Contains("does not prove DNS, gateway or Internet connectivity", StringComparison.Ordinal),
            "A current coherent lease remains informational and retains its limits");
        Require(DhcpLeaseDiagnostics.Interpret(Evidence([Row(usable: false)])).State == CheckState.Attention,
            "A DHCP adapter without a usable IPv4 address requests attention without selecting renewal");
        Require(DhcpLeaseDiagnostics.Interpret(Evidence([Row(expires: "2026-09-15T11:59:59.0000000+00:00")])).State == CheckState.Attention,
            "An expired lease requests attention");
        Require(DhcpLeaseDiagnostics.Interpret(Evidence([Row(server: false)])).State == CheckState.Attention,
            "Missing DHCP server evidence requests attention");
        Require(DhcpLeaseDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "No eligible adapters remains informational");
        Require(DhcpLeaseDiagnostics.Interpret(Evidence([Row(obtained: null, expires: null)])).State == CheckState.Unknown,
            "Missing lease timestamps remains unknown");
        Require(DhcpLeaseDiagnostics.Interpret(Evidence([Row(expires: "2026-09-15T09:00:00.0000000+00:00")])).State == CheckState.Unknown,
            "An expiry before acquisition remains unknown");

        foreach (var invalid in new[]
        {
            Evidence([Row()], false), Evidence([Row(obtained: null)]), Evidence([Row(expires: null)]),
            Evidence([Row(setting: "invalid")]), Evidence([Row(), Row(index: 7, setting: "22222222-2222-2222-2222-222222222222")]),
            Evidence([Row(), Row(index: 8)]), Evidence([Row(obtained: "2026-09-15T10:00:00-07:00")]),
            Evidence([.. Enumerable.Range(0, 257).Select(index => Row(index + 1, Guid.NewGuid().ToString()))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            ordinaryEvidence with { ExitCode = 5 }, ordinaryEvidence with { Error = "denied" },
            ordinaryEvidence with { TimedOut = true }, ordinaryEvidence with { Truncated = true }
        })
            Require(DhcpLeaseDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Incomplete, invalid, duplicate, excessive, malformed and failed lease evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await DhcpLeaseDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(ordinaryEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("DHCPLeaseObtained", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("DHCPLeaseExpires", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("RenewDHCPLease", StringComparison.Ordinal),
            "The native plan reads bounded lease state without renewing or changing it");
        Require((await DhcpLeaseDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DhcpLeaseDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: DHCP lease timestamps, expiry and server evidence; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
