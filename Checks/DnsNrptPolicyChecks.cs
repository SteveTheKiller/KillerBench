using System.Text.Json;
using KillerBench.Engine;

internal static class DnsNrptPolicyChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(object[] rules, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, rules }, Capture.JsonOptions), "", false);
        object Rule(string[] namespaces, string[]? servers = null, string[]? directServers = null,
            bool? direct = null, bool? directIpsec = null, bool? dnsSecIpsec = null, bool? validation = null,
            string query = "", string fallback = "") => new
            {
                namespaces,
                nameServers = servers ?? [],
                directAccessDnsServers = directServers ?? [],
                directAccessEnabled = direct,
                directAccessIpsecRequired = directIpsec,
                dnsSecIpsecRequired = dnsSecIpsec,
                dnsSecValidationRequired = validation,
                queryPolicy = query,
                secureFallback = fallback
            };

        var empty = DnsNrptPolicyDiagnostics.Interpret(Evidence([]));
        Require(empty is { State: CheckState.Information, Repair: null } && empty.Detail.Contains("No effective", StringComparison.Ordinal),
            "An empty complete effective policy remains informational");
        var configured = DnsNrptPolicyDiagnostics.Interpret(Evidence([
            Rule([".corp.example"], directServers: ["2001:db8::1"], direct: true, directIpsec: true, query: "QueryIPv6Only"),
            Rule(["secure.example"], servers: ["192.0.2.53"], dnsSecIpsec: true, validation: true, fallback: "FallbackPrivate")
        ]));
        Require(configured is { State: CheckState.Information, Repair: null } && configured.Evidence is not null &&
            configured.Detail.Contains("2 effective DNS policy rules", StringComparison.Ordinal) &&
            configured.Detail.Contains("1 require DNSSEC validation", StringComparison.Ordinal) &&
            !configured.Detail.Contains("corp.example", StringComparison.Ordinal),
            "Effective policy is summarized without exposing namespace or server values");

        foreach (var invalid in new[]
        {
            Evidence([], false), Evidence([Rule([])]),
            Evidence([Rule([".corp.example"]), Rule([".CORP.EXAMPLE"])]),
            Evidence([new { namespaces = new[] { ".corp.example" } }]),
            Evidence([Rule([" bad.example"])]),
            Evidence([Rule([".corp.example"], servers: ["192.0.2.53", "192.0.2.53"])]),
            new ToolResult(1, "{}", "failed", false), new ToolResult(0, "{}", "", true)
        }) Require(DnsNrptPolicyDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Incomplete, malformed, duplicate, failed and truncated effective policy remains unknown");

        var capped = Evidence(Enumerable.Range(1, 257).Select(index => Rule(["rule" + index + ".example"])).ToArray());
        Require(DnsNrptPolicyDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized effective policy cannot claim complete coverage");

        RepairStep? observed = null;
        var collected = await DnsNrptPolicyDiagnostics.CheckAsync(execute: (step, _) =>
        {
            observed = step;
            return Task.FromResult(Evidence([]));
        });
        Require(observed is { Verification: true } && observed.Tool == PrintRecovery.Tool &&
            observed.Arguments.Last().Contains("Get-DnsClientNrptPolicy -Effective", StringComparison.Ordinal) &&
            observed.Arguments.Last().Contains("Select-Object -First 257", StringComparison.Ordinal) &&
            collected.State == CheckState.Information,
            "Native plan is bounded, effective-policy scoped and read only");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DnsNrptPolicyDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("provider should not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents effective DNS policy collection");
        Console.WriteLine("PASS: bounded effective DNS policy, privacy summary, malformed evidence and cancellation; fixtures only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
