using KillerBench.Engine;

internal static class WiredAuthenticationEventChecks
{
    internal static void Run()
    {
        static string Entry(int id, int level = 2, string provider = "Microsoft-Windows-Wired-AutoConfig", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='Identity'>PRIVATE\\User</Data><Data Name='ProfileName'>PRIVATE-PROFILE</Data><Data Name='InterfaceGuid'>{{11111111-2222-3333-4444-555555555555}}</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(15500, record: 1) + Entry(15500, record: 2) + Entry(15501, 4, record: 3) + "</Events>", "", false);
        var result = WiredAuthenticationEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, evidence) &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 15500, level 2: 2 records") &&
            result.Detail.Contains("adapter, profile, account") && !result.Detail.Contains("PRIVATE") && !result.Detail.Contains("11111111"),
            "Wired authentication history groups failures without copying identity payload into summaries");
        Require(WiredAuthenticationEvents.Interpret(evidence with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty complete wired authentication history remains informational");
        foreach (var invalid in new[] { evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Output = "<Events>" }, evidence with { Output = "<Events>" + Entry(15500, provider: "Other") + "</Events>" }, evidence with { Output = "<Events>" + Entry(15500) + Entry(15500) + "</Events>" }, evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(15500, record: (ulong)index))) + "</Events>" } })
            Require(WiredAuthenticationEvents.Interpret(invalid).State == CheckState.Unknown, "Incomplete wired authentication history remains unknown");
        try { WiredAuthenticationEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Wired authentication history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: wired authentication event grouping, identity privacy, incomplete evidence and cancellation; no network changes.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
