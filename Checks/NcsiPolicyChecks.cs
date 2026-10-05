using System.IO;
using KillerBench.Engine;

internal static class NcsiPolicyChecks
{
    internal static async Task RunAsync()
    {
        static ToolResult Evidence(string active = "null", string passive = "null", string local = "null", bool web = false, bool dns = false) =>
            new(0, $"{{\"policyNoActiveProbe\":{active},\"policyDisablePassivePolling\":{passive},\"localEnableActiveProbing\":{local},\"customWebProbe\":{web.ToString().ToLowerInvariant()},\"customDnsProbe\":{dns.ToString().ToLowerInvariant()}}}", "", false);

        var defaults = NcsiPolicyDiagnostics.Interpret(Evidence());
        Require(defaults is { State: CheckState.Information, Repair: null } && defaults.Detail.Contains("enabled or no explicit disabling", StringComparison.Ordinal),
            "Absent NCSI overrides remain descriptive");
        var policy = NcsiPolicyDiagnostics.Interpret(Evidence("1", "0", "1", true, true));
        Require(policy.State == CheckState.Information && policy.Detail.Contains("disabled by policy", StringComparison.Ordinal) &&
            policy.Detail.Contains("web and DNS", StringComparison.Ordinal) && !policy.Detail.Contains("msft", StringComparison.OrdinalIgnoreCase),
            "Active-probe policy and private override presence are retained without destinations");
        var disabled = NcsiPolicyDiagnostics.Interpret(Evidence("1", "1", "1"));
        Require(disabled.State == CheckState.Attention && disabled.Detail.Contains("Both active and passive", StringComparison.Ordinal),
            "Disabled active and passive classification requests policy-owner review");

        foreach (var invalid in new[]
        {
            Evidence("2"), new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            defaults.Evidence! with { ExitCode = 5 }, defaults.Evidence! with { Error = "denied" },
            defaults.Evidence! with { TimedOut = true }, defaults.Evidence! with { Truncated = true }
        })
            Require(NcsiPolicyDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Unsupported, malformed, failed and incomplete NCSI evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await NcsiPolicyDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(Evidence("0", "0", "1"));
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Get-ItemProperty", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-ItemProperty", StringComparison.Ordinal),
            "Native plan reads bounded NCSI configuration without changing it");
        Require((await NcsiPolicyDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "NCSI provider failure remains unknown");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await NcsiPolicyDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents NCSI collection");
        Console.WriteLine("PASS: NCSI classification policy, privacy bounds, incomplete handling and read-only collection; fixtures only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
