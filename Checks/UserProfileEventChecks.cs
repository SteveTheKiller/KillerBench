using KillerBench.Engine;

internal static class UserProfileEventChecks
{
    internal static void Run()
    {
        static string Entry(int id, int level = 2, string provider = "Microsoft-Windows-User Profiles Service", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='LocalPath'>C:\\Users\\Fixture</Data><Data Name='Sid'>S-1-5-21-fixture</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(1511, record: 1) + Entry(1511, record: 2) + Entry(2, level: 4, record: 3) + "</Events>", "", false);
        var result = UserProfileEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 1511, level 2: 2 records.") &&
            result.Detail.Contains("account identifiers and profile paths") && !result.Detail.Contains("C:\\Users\\Fixture") &&
            result.Detail.Contains("No profile operation or repair is selected"),
            "User profile history retains grouped errors without copying account or profile payload into the summary");
        var empty = UserProfileEvents.Interpret(evidence with { Output = "<Events/>" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("0 records retained"),
            "Empty complete user profile history remains informational rather than a health verdict");
        foreach (var bad in new[]
        {
            evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "Access denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "<Events>" + Entry(1511, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(1511, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + Entry(1511) + Entry(1511) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(1511, record: (ulong)index))) + "</Events>" }
        })
        {
            var unknown = UserProfileEvents.Interpret(bad);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == bad,
                "Incomplete user profile history cannot establish current profile state or repair eligibility");
        }
        var many = UserProfileEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "User profile summaries retain bounded pattern output");
        try { UserProfileEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("User profile history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: user profile event grouping, payload privacy, incomplete evidence and cancellation; no profile changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
