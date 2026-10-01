using KillerBench.Engine;

internal static class FirewallEventChecks
{
    internal static async Task RunAsync()
    {
        static string Entry(int id, int level = 2, ulong record = 1,
            string provider = "Microsoft-Windows-Windows Firewall With Advanced Security") =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-16T00:00:00Z'/></System><EventData><Data Name='ApplicationPath'>PRIVATE-APPLICATION</Data><Data Name='RemoteAddress'>192.0.2.44</Data><Data Name='RemotePort'>445</Data><Data Name='RuleName'>PRIVATE-RULE</Data></EventData></Event>";

        var evidence = new ToolResult(0,
            "<Events>" + Entry(2004, record: 1) + Entry(2004, record: 2) + Entry(2011, 3, 3) + "</Events>", "", false);
        var result = FirewallEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, evidence) &&
            result.Detail.Contains("3 warning, error or critical records", StringComparison.Ordinal) &&
            result.Detail.Contains("Event 2004, level 2: 2 records", StringComparison.Ordinal) &&
            result.Detail.Contains("Ordinary summaries exclude those values", StringComparison.Ordinal) &&
            !result.Detail.Contains("PRIVATE-", StringComparison.Ordinal) && !result.Detail.Contains("192.0.2.44", StringComparison.Ordinal),
            "Firewall event history groups patterns without copying sensitive payload fields into summaries");
        Require(FirewallEvents.Interpret(evidence with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty complete firewall operational history remains informational");
        foreach (var invalid in new[]
        {
            evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true },
            evidence with { Truncated = true }, evidence with { Output = "<Events>" },
            evidence with { Output = "<Events>" + Entry(2004, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(2004, level: 4) + "</Events>" },
            evidence with { Output = "<Events>" + Entry(2004) + Entry(2004) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201)
                .Select(index => Entry(2004, record: (ulong)index))) + "</Events>" }
        })
            Require(FirewallEvents.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, duplicate or out-of-scope firewall operational history remains unknown");

        RepairStep? planned = null;
        var collected = await FirewallEvents.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            Require(token.CanBeCanceled, "Firewall event provider must be bounded");
            return Task.FromResult(new ToolResult(0, "<Events/>", "", false));
        });
        Require(collected.State == CheckState.Information && planned is { Tool: "wevtutil.exe", Verification: true } &&
            planned.Arguments[1] == FirewallEvents.Channel && planned.Arguments.Contains("/c:201") &&
            planned.Arguments.Contains("/rd:true") &&
            planned.Arguments.Single(argument => argument.StartsWith("/q:", StringComparison.Ordinal))
                .Contains("Level=1 or Level=2 or Level=3", StringComparison.Ordinal),
            "Native firewall event plan is bounded, newest first and read only");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await RequireCanceled(() => FirewallEvents.CheckAsync(stop.Token,
            (_, _) => throw new Exception("Canceled firewall collection must not invoke its provider.")));
        Console.WriteLine("PASS: Windows Firewall operational event grouping, payload privacy, bounds and cancellation; no firewall changes.");
    }

    private static async Task RequireCanceled(Func<Task<WorkflowCheck>> action)
    {
        try
        {
            await action();
            throw new Exception("Canceled firewall event collection completed unexpectedly.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
