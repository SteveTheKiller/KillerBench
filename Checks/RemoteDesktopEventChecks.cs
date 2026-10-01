using KillerBench.Engine;

internal static class RemoteDesktopEventChecks
{
    internal static void Run()
    {
        static string Entry(string provider, int id, int level = 2, ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='User'>PRIVATE\\User</Data><Data Name='Address'>10.1.2.3</Data><Data Name='SessionID'>42</Data></EventData></Event>";
        foreach (var source in new[]
        {
            (RemoteDesktopEvents.ConnectionChannel, RemoteDesktopEvents.ConnectionProvider, "Connection history"),
            (RemoteDesktopEvents.SessionChannel, RemoteDesktopEvents.SessionProvider, "Session history")
        })
        {
            var evidence = new ToolResult(0, "<Events>" + Entry(source.Item2, 100, record: 1) + Entry(source.Item2, 100, record: 2) + Entry(source.Item2, 101, 4, 3) + "</Events>", "", false);
            var check = RemoteDesktopEvents.Interpret(source.Item1, source.Item2, source.Item3, evidence);
            Require(check.State == CheckState.Attention && check.Repair is null && ReferenceEquals(check.Evidence, evidence) &&
                check.Detail.Contains("3 records retained; 2 warning") && check.Detail.Contains("Event 100, level 2: 2 records") &&
                check.Detail.Contains("account, client address, session") && !check.Detail.Contains("PRIVATE") && !check.Detail.Contains("10.1.2.3"),
                "Remote Desktop history groups failures without copying private payload into summaries");
            Require(RemoteDesktopEvents.Interpret(source.Item1, source.Item2, source.Item3, evidence with { Output = "<Events/>" }).State == CheckState.Information,
                "Empty complete Remote Desktop history remains informational");
            foreach (var invalid in new[] { evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Output = "<Events>" }, evidence with { Output = "<Events>" + Entry("Other", 100) + "</Events>" }, evidence with { Output = "<Events>" + Entry(source.Item2, 100) + Entry(source.Item2, 100) + "</Events>" }, evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(source.Item2, 100, record: (ulong)index))) + "</Events>" } })
                Require(RemoteDesktopEvents.Interpret(source.Item1, source.Item2, source.Item3, invalid).State == CheckState.Unknown,
                    "Incomplete Remote Desktop history remains unknown");
        }
        try { RemoteDesktopEvents.Interpret(RemoteDesktopEvents.ConnectionChannel, RemoteDesktopEvents.ConnectionProvider, "fixture", new(0, "<Events/>", "", false), new CancellationToken(true)); throw new Exception("Remote Desktop history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Remote Desktop connection and session event grouping, privacy, incomplete evidence and cancellation; no remote-access changes.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
