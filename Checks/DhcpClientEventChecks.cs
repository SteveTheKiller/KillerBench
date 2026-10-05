using System.Collections.Concurrent;
using System.IO;
using KillerBench.Engine;

internal static class DhcpClientEventChecks
{
    internal static async Task RunAsync()
    {
        string Provider(string channel) => channel.Contains("Dhcpv6", StringComparison.Ordinal)
            ? "Microsoft-Windows-DHCPv6-Client" : "Microsoft-Windows-Dhcp-Client";
        string Entry(string channel, int id, int level = 2, ulong record = 1, string? provider = null) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider ?? Provider(channel)}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='Address'>192.0.2.10</Data><Data Name='Adapter'>PRIVATE-ADAPTER</Data><Data Name='Server'>PRIVATE-SERVER</Data><Data Name='Domain'>PRIVATE-DOMAIN</Data></EventData></Event>";

        Require(DhcpClientEvents.Channels.Count == 4 && DhcpClientEvents.Channels.Distinct(StringComparer.Ordinal).Count() == 4,
            "IPv4 and IPv6 administrative and operational channels are explicit and unique");
        foreach (string channel in DhcpClientEvents.Channels)
        {
            var evidence = new ToolResult(0, "<Events>" + Entry(channel, 1001, record: 1) + Entry(channel, 1001, record: 2) + Entry(channel, 1002, 4, record: 3) + "</Events>", "", false);
            var result = DhcpClientEvents.Interpret(channel, evidence);
            Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, evidence) &&
                result.Detail.Contains("3 records retained; 2 warning", StringComparison.Ordinal) &&
                result.Detail.Contains("Event 1001, level 2: 2 records", StringComparison.Ordinal) &&
                result.Detail.Contains("Ordinary summaries exclude those values", StringComparison.Ordinal) &&
                !result.Detail.Contains("192.0.2.10", StringComparison.Ordinal) && !result.Detail.Contains("PRIVATE-", StringComparison.Ordinal),
                "DHCP Client history groups failures without copying network identities into summaries");
            Require(DhcpClientEvents.Interpret(channel, evidence with { Output = "<Events/>" }).State == CheckState.Information,
                "Empty complete DHCP Client history remains informational");
            foreach (var invalid in new[]
            {
                evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true },
                evidence with { Truncated = true }, evidence with { Output = "<Events>" },
                evidence with { Output = "<Events>" + Entry(channel, 1001, provider: "Other") + "</Events>" },
                evidence with { Output = "<Events>" + Entry(channel, 1001) + Entry(channel, 1001) + "</Events>" },
                evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(channel, 1001, record: (ulong)index))) + "</Events>" }
            })
                Require(DhcpClientEvents.Interpret(channel, invalid).State == CheckState.Unknown,
                    "Incomplete DHCP Client history remains unknown");
        }

        var planned = new ConcurrentBag<RepairStep>();
        var collected = await DhcpClientEvents.CheckAsync(execute: (step, token) =>
        {
            Require(token.CanBeCanceled, "DHCP Client event provider receives a cancelable token");
            planned.Add(step);
            string channel = step.Arguments[1];
            return Task.FromResult(new ToolResult(0, "<Events/>", "", false));
        });
        Require(collected.Count == 4 && collected.All(item => item.State == CheckState.Information) && planned.Count == 4 &&
            planned.All(step => step is { Tool: "wevtutil.exe", Verification: true } && step.Arguments.Contains("/c:201") &&
                step.Arguments.Contains("/rd:true") && DhcpClientEvents.Channels.Contains(step.Arguments[1], StringComparer.Ordinal)),
            "Native plans query only four bounded DHCP Client channels newest first");
        var partial = await DhcpClientEvents.CheckAsync(execute: (step, _) => step.Arguments[1].EndsWith("/Admin", StringComparison.Ordinal)
            ? throw new IOException("fixture failure") : Task.FromResult(new ToolResult(0, "<Events/>", "", false)));
        Require(partial.Count(item => item.State == CheckState.Unknown) == 2 && partial.Count(item => item.State == CheckState.Information) == 2,
            "One unavailable channel does not erase independent DHCP Client evidence");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DhcpClientEvents.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents DHCP Client event collection");
        Console.WriteLine("PASS: DHCP Client IPv4 and IPv6 event grouping, summary privacy, isolation and cancellation; no network changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
