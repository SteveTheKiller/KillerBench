using KillerBench.Engine;

internal static class WirelessEventChecks
{
    internal static void Run()
    {
        static string Entry(int id, int level = 2, string provider = "Microsoft-Windows-WLAN-AutoConfig", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='SSID'>PRIVATE-NETWORK</Data><Data Name='ProfileName'>PRIVATE-PROFILE</Data><Data Name='InterfaceGuid'>{{11111111-2222-3333-4444-555555555555}}</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(8002, record: 1) + Entry(8002, record: 2) + Entry(8001, 4, record: 3) + "</Events>", "", false);
        var result = WirelessEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, evidence) &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 8002, level 2: 2 records") &&
            result.Detail.Contains("adapter, profile, network") && !result.Detail.Contains("PRIVATE-NETWORK") && !result.Detail.Contains("PRIVATE-PROFILE"),
            "Wireless history groups failures without copying network identities into summaries");
        Require(WirelessEvents.Interpret(evidence with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty complete wireless history remains informational");
        foreach (var invalid in new[] { evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Output = "<Events>" }, evidence with { Output = "<Events>" + Entry(8002, provider: "Other") + "</Events>" }, evidence with { Output = "<Events>" + Entry(8002) + Entry(8002) + "</Events>" }, evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(8002, record: (ulong)index))) + "</Events>" } })
            Require(WirelessEvents.Interpret(invalid).State == CheckState.Unknown, "Incomplete wireless history remains unknown");
        var many = WirelessEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"), "Wireless summaries remain bounded");
        try { WirelessEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Wireless history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: wireless event grouping, network identity privacy, incomplete evidence and cancellation; no network changes.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
