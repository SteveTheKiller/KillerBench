using System.Collections.Concurrent;
using System.IO;
using KillerBench.Engine;

internal static class DnsClientEventChecks
{
    private static readonly string[] ExpectedChannels = ["System", "Microsoft-Windows-DNS-Client/Operational"];

    internal static async Task RunAsync()
    {
        static string Entry(int id, int level = 2, ulong record = 1, string provider = "Microsoft-Windows-DNS-Client") =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-15T00:00:00Z'/></System><EventData><Data Name='QueryName'>PRIVATE-NAME</Data><Data Name='ServerAddress'>192.0.2.53</Data><Data Name='ProcessName'>PRIVATE-PROCESS</Data></EventData></Event>";

        Require(DnsClientEvents.Channels.SequenceEqual(ExpectedChannels),
            "DNS Client failure sources are explicit and stable");
        foreach (string channel in DnsClientEvents.Channels)
        {
            var evidence = new ToolResult(0, "<Events>" + Entry(1014, record: 1) + Entry(1014, record: 2) + Entry(3018, 3, 3) + "</Events>", "", false);
            var result = DnsClientEvents.Interpret(channel, evidence);
            Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, evidence) &&
                result.Detail.Contains("3 warning, error or critical records", StringComparison.Ordinal) &&
                result.Detail.Contains("Event 1014, level 2: 2 records", StringComparison.Ordinal) &&
                result.Detail.Contains("Ordinary summaries exclude those values", StringComparison.Ordinal) &&
                !result.Detail.Contains("PRIVATE-", StringComparison.Ordinal) && !result.Detail.Contains("192.0.2.53", StringComparison.Ordinal),
                "DNS Client failure history groups patterns without copying query or resolver identities into summaries");
            Require(DnsClientEvents.Interpret(channel, evidence with { Output = "<Events/>" }).State == CheckState.Information,
                "Empty complete DNS Client failure history remains informational");
            foreach (var invalid in new[]
            {
                evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true },
                evidence with { Truncated = true }, evidence with { Output = "<Events>" },
                evidence with { Output = "<Events>" + Entry(1014, provider: "Other") + "</Events>" },
                evidence with { Output = "<Events>" + Entry(1014, level: 4) + "</Events>" },
                evidence with { Output = "<Events>" + Entry(1014) + Entry(1014) + "</Events>" },
                evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(1014, record: (ulong)index))) + "</Events>" }
            })
                Require(DnsClientEvents.Interpret(channel, invalid).State == CheckState.Unknown,
                    "Incomplete or out-of-scope DNS Client history remains unknown");
        }

        var planned = new ConcurrentBag<RepairStep>();
        var collected = await DnsClientEvents.CheckAsync(execute: (step, token) =>
        {
            Require(token.CanBeCanceled, "DNS Client event provider receives a cancelable token");
            planned.Add(step);
            return Task.FromResult(new ToolResult(0, "<Events/>", "", false));
        });
        Require(collected.Count == 2 && collected.All(item => item.State == CheckState.Information) && planned.Count == 2 &&
            planned.All(step => step is { Tool: "wevtutil.exe", Verification: true } && step.Arguments.Contains("/c:201") &&
                step.Arguments.Contains("/rd:true") && DnsClientEvents.Channels.Contains(step.Arguments[1], StringComparer.Ordinal) &&
                step.Arguments.Single(argument => argument.StartsWith("/q:", StringComparison.Ordinal)).Contains("Level=1 or Level=2 or Level=3", StringComparison.Ordinal)),
            "Native plans query only bounded DNS Client warning and failure history newest first");
        var partial = await DnsClientEvents.CheckAsync(execute: (step, _) => step.Arguments[1] == "System"
            ? throw new IOException("fixture failure") : Task.FromResult(new ToolResult(0, "<Events/>", "", false)));
        Require(partial.Count(item => item.State == CheckState.Unknown) == 1 && partial.Count(item => item.State == CheckState.Information) == 1,
            "One unavailable DNS Client source does not erase independent evidence");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DnsClientEvents.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents DNS Client event collection");
        Console.WriteLine("PASS: DNS Client warning and failure event grouping, summary privacy, isolation and cancellation; no network changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
