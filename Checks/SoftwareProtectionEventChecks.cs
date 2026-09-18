using KillerBench.Engine;

internal static class SoftwareProtectionEventChecks
{
    public static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-Security-SPP", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/><Computer>PRIVATE-PC</Computer></System><EventData><Data Name='License'>PRIVATE-LICENSE</Data><Data Name='PartialProductKey'>ABCDE</Data><Data Name='Account'>PRIVATE\\User</Data></EventData></Event>";
        var raw = new ToolResult(0, "<Events>" + Entry(8200, record: 1) + Entry(8200, record: 2) + Entry(1003, 4, record: 3) + "</Events>", "", false);
        var sanitized = SoftwareProtectionEvents.Sanitize(raw);
        Require(sanitized.ExitCode == 0 && string.IsNullOrEmpty(sanitized.Error) && sanitized.Output.Contains("PRIVATE-PC", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("PRIVATE-LICENSE", StringComparison.Ordinal) && !sanitized.Output.Contains("ABCDE", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("PRIVATE\\User", StringComparison.Ordinal) && !sanitized.Output.Contains("EventData", StringComparison.Ordinal),
            "Software Protection evidence retains system metadata and removes event payload before storage");
        var result = SoftwareProtectionEvents.Interpret(sanitized);
        Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, sanitized) &&
            result.Detail.Contains("3 records retained; 2 warning", StringComparison.Ordinal) &&
            result.Detail.Contains("Event 8200, level 2: 2 records", StringComparison.Ordinal) &&
            result.Detail.Contains("Event payload is removed", StringComparison.Ordinal) &&
            !result.Detail.Contains("PRIVATE", StringComparison.Ordinal) && !result.Detail.Contains("ABCDE", StringComparison.Ordinal),
            "Software Protection history groups failures without exposing activation payload");
        var empty = SoftwareProtectionEvents.Sanitize(raw with { Output = "<Events/>" });
        Require(SoftwareProtectionEvents.Interpret(empty).State == CheckState.Information,
            "Empty complete Software Protection history remains informational");
        var malformed = SoftwareProtectionEvents.Sanitize(raw with { Output = "<Events>" });
        Require(malformed.Output.Length == 0 && malformed.Error.Length > 0 && SoftwareProtectionEvents.Interpret(malformed).State == CheckState.Unknown,
            "Malformed native event XML is discarded instead of retaining unsanitized payload");
        foreach (var invalid in new[]
        {
            sanitized with { ExitCode = 5 }, sanitized with { Error = "denied" }, sanitized with { TimedOut = true }, sanitized with { Truncated = true },
            SoftwareProtectionEvents.Sanitize(raw with { Output = "<Events>" + Entry(8200, provider: "Other") + "</Events>" }),
            SoftwareProtectionEvents.Sanitize(raw with { Output = "<Events>" + Entry(8200) + Entry(8200) + "</Events>" }),
            SoftwareProtectionEvents.Sanitize(raw with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(8200, record: (ulong)index))) + "</Events>" })
        })
            Require(SoftwareProtectionEvents.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, duplicate, unexpected and capped Software Protection history remains unknown");
        try { SoftwareProtectionEvents.Interpret(sanitized, new CancellationToken(true)); throw new Exception("Software Protection history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: sanitized Software Protection event grouping, payload removal, incomplete evidence and cancellation; no activation changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
