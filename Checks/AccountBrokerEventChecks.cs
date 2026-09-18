using KillerBench.Engine;

internal static class AccountBrokerEventChecks
{
    public static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-AAD", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><Version>0</Version><Level>{level}</Level><Task>103</Task><Opcode>0</Opcode><Keywords>0x10</Keywords><TimeCreated SystemTime='2026-09-14T00:00:00Z'/><EventRecordID>{record}</EventRecordID><Correlation ActivityID='{{PRIVATE-ACTIVITY}}'/><Execution ProcessID='42' ThreadID='7'/><Computer>PRIVATE-PC</Computer><Security UserID='S-1-5-21-PRIVATE'/></System><EventData><Data Name='Account'>PRIVATE\\User</Data><Data Name='Token'>PRIVATE-TOKEN</Data><Data Name='Uri'>https://private.invalid</Data></EventData></Event>";
        var raw = new ToolResult(0, "<Events>" + Entry(1097, record: 1) + Entry(1097, record: 2) + Entry(1006, 4, record: 3) + "</Events>", "", false);
        var sanitized = AccountBrokerEvents.Sanitize(raw);
        Require(sanitized.Output.Contains("Microsoft-Windows-AAD", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("PRIVATE", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("EventData", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("Correlation", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("Execution", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("Security", StringComparison.Ordinal),
            "Account broker evidence retains only bounded system metadata");
        var result = AccountBrokerEvents.Interpret(sanitized);
        Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, sanitized) &&
            result.Detail.Contains("3 records retained; 2 warning", StringComparison.Ordinal) &&
            result.Detail.Contains("Event 1097, level 2: 2 records", StringComparison.Ordinal) &&
            result.Detail.Contains("payload, computer identity", StringComparison.Ordinal) &&
            !result.Detail.Contains("PRIVATE", StringComparison.Ordinal),
            "Sanitized broker history groups problems without exposing private payload");
        Require(AccountBrokerEvents.Interpret(AccountBrokerEvents.Sanitize(raw with { Output = "<Events/>" })).State == CheckState.Information,
            "Empty complete account broker history remains informational");
        foreach (var invalid in new[]
        {
            sanitized with { ExitCode = 5 }, sanitized with { Error = "denied" }, sanitized with { TimedOut = true },
            sanitized with { Truncated = true }, AccountBrokerEvents.Sanitize(raw with { Output = "<Events>" }),
            AccountBrokerEvents.Sanitize(raw with { Output = "<Events>" + Entry(1097, provider: "Other") + "</Events>" }),
            AccountBrokerEvents.Sanitize(raw with { Output = "<Events>" + Entry(1097) + Entry(1097) + "</Events>" }),
            AccountBrokerEvents.Sanitize(raw with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(1097, record: (ulong)index))) + "</Events>" })
        })
            Require(AccountBrokerEvents.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, duplicate, unexpected and capped account broker history remains unknown");
        try { AccountBrokerEvents.Interpret(sanitized, new CancellationToken(true)); throw new Exception("Account broker history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: sanitized account broker event grouping, payload removal, incomplete evidence and cancellation; no account changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
