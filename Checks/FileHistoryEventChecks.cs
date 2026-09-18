using KillerBench.Engine;

internal static class FileHistoryEventChecks
{
    internal static async Task RunAsync()
    {
        var now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        const string unavailable = "{\"channelAvailable\":false,\"complete\":true,\"retained\":0,\"overflow\":false,\"critical\":0,\"errors\":0,\"warnings\":0,\"newestUtc\":null,\"newestEventId\":null,\"newestLevel\":null}";
        const string empty = "{\"channelAvailable\":true,\"complete\":true,\"retained\":0,\"overflow\":false,\"critical\":0,\"errors\":0,\"warnings\":0,\"newestUtc\":null,\"newestEventId\":null,\"newestLevel\":null}";
        const string events = "{\"channelAvailable\":true,\"complete\":true,\"retained\":3,\"overflow\":false,\"critical\":0,\"errors\":1,\"warnings\":2,\"newestUtc\":\"2026-09-15T10:00:00.0000000+00:00\",\"newestEventId\":201,\"newestLevel\":2}";
        foreach (string informational in new[] { unavailable, empty })
        {
            var check = FileHistoryEvents.Interpret(Result(informational), now);
            Require(check.State == CheckState.Information && check.Repair is null && check.Evidence?.Output == informational,
                "Absent or empty File History evidence claimed health, failure or repair eligibility");
        }
        var attention = FileHistoryEvents.Interpret(Result(events), now);
        Require(attention.State == CheckState.Attention && attention.Repair is null && attention.Detail.Contains("event 201") &&
            !attention.Detail.Contains(@"C:\Users", StringComparison.OrdinalIgnoreCase) && !attention.Detail.Contains("ConfigFilePath", StringComparison.OrdinalIgnoreCase),
            "File History severity summary lost identity, exposed payload data or authorized repair");

        string overflow = events.Replace("\"retained\":3", "\"retained\":200").Replace("\"overflow\":false", "\"overflow\":true")
            .Replace("\"errors\":1", "\"errors\":100").Replace("\"warnings\":2", "\"warnings\":100");
        Require(FileHistoryEvents.Interpret(Result(overflow), now).State == CheckState.Unknown,
            "Bounded File History overflow was treated as complete");
        foreach (string invalid in new[]
        {
            events.Replace("\"complete\":true", "\"complete\":false"),
            events.Replace("\"retained\":3", "\"retained\":4"),
            events.Replace("\"newestLevel\":2", "\"newestLevel\":4"),
            events.Replace("\"newestEventId\":201", "\"newestEventId\":65536"),
            events.Replace("2026-09-15T10:00:00.0000000+00:00", "2026-09-16T10:00:00.0000000+00:00"),
            unavailable.Replace("\"retained\":0", "\"retained\":1"),
            empty[..^1] + ",\"payload\":\"private\"}"
        })
            Require(FileHistoryEvents.Interpret(Result(invalid), now).State == CheckState.Unknown,
                "Malformed or contradictory File History evidence was accepted");
        foreach (var invalid in new[] { Result(events) with { TimedOut = true }, Result(events) with { Truncated = true }, Result(events) with { Error = "Denied" }, Result(events) with { ExitCode = 1 } })
            Require(FileHistoryEvents.Interpret(invalid, now).State == CheckState.Unknown,
                "Incomplete File History command evidence was accepted");

        RepairStep? requested = null;
        var injected = await FileHistoryEvents.CheckAsync(execute: (step, _) =>
        {
            requested = step;
            return Task.FromResult(Result(empty));
        });
        Require(injected.State == CheckState.Information && requested?.Tool == PrintRecovery.Tool && requested.Verification &&
            requested.Arguments.Any(argument => argument.Contains("Microsoft-Windows-FileHistory-Engine/BackupLog", StringComparison.Ordinal)) &&
            requested.Arguments.All(argument => !argument.Contains("Message", StringComparison.OrdinalIgnoreCase) && !argument.Contains("Properties", StringComparison.OrdinalIgnoreCase)),
            "Collector did not use one bounded privacy-projected read-only query");
        try
        {
            await FileHistoryEvents.CheckAsync(new CancellationToken(true), (_, _) => Task.FromResult(Result(empty)));
            throw new Exception("File History event review ignored cancellation");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: File History channel availability, severity projection, privacy, bounds, malformed evidence and substituted collection; no backup changed.");
    }

    private static ToolResult Result(string output) => new(0, output, "", false);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
