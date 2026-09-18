using KillerBench.Engine;

internal static class EventCollectorSubscriptionChecks
{
    internal static void Run()
    {
        ToolResult Evidence(string output) => new(0, output, "", false);
        var inventory = EventCollectorSubscriptions.Interpret(Evidence("Operations\r\nSecurity forwarders\r\n"));
        var check = EventCollectorSubscriptions.Describe(inventory);
        Require(inventory.Complete && inventory.Names.SequenceEqual(new[] { "Operations", "Security forwarders" }) &&
            check.State == CheckState.Information && check.Repair is null && check.Evidence == inventory.Evidence &&
            check.Detail.Contains("source reachability", StringComparison.Ordinal),
            "Complete subscription inventory retains exact identities without health or repair claims");
        var empty = EventCollectorSubscriptions.Interpret(Evidence("\r\n"));
        Require(empty.Complete && empty.Names.Count == 0 && EventCollectorSubscriptions.Describe(empty).State == CheckState.Information,
            "An empty complete inventory is distinguished from unavailable coverage");
        var boundedDisplay = EventCollectorSubscriptions.Interpret(Evidence(string.Join("\n", Enumerable.Range(0, 21).Select(index => "subscription" + index.ToString("D2")))));
        Require(EventCollectorSubscriptions.Describe(boundedDisplay).Detail.Contains("1 additional names remain in the retained evidence", StringComparison.Ordinal),
            "Large complete inventories retain all evidence while bounding displayed names");
        foreach (var evidence in new[]
        {
            Evidence("duplicate\nDUPLICATE"),
            Evidence(new string('x', 513)),
            Evidence("valid\ninvalid\u0001"),
            Evidence(string.Join("\n", Enumerable.Range(0, 257).Select(index => "subscription" + index))),
            Evidence("one") with { ExitCode = 5 },
            Evidence("one") with { Error = "unavailable" },
            Evidence("one") with { TimedOut = true },
            Evidence("one") with { Truncated = true }
        })
        {
            var incomplete = EventCollectorSubscriptions.Interpret(evidence);
            Require(!incomplete.Complete && incomplete.Names.Count == 0 &&
                EventCollectorSubscriptions.Describe(incomplete).State == CheckState.Unknown,
                "Duplicate, oversized, malformed and incomplete evidence cannot claim subscription coverage");
        }
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        try
        {
            EventCollectorSubscriptions.Interpret(Evidence("one"), stop.Token);
            throw new InvalidOperationException("Canceled subscription parsing continued");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Event Collector subscription identities, empty state, bounds, incomplete evidence and cancellation; no subscriptions changed.");
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
