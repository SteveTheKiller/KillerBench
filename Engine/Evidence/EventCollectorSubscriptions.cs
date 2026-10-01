namespace KillerBench.Engine.Evidence;

public sealed record EventCollectorSubscriptionInventory(IReadOnlyList<string> Names, bool Complete, ToolResult Evidence);

public static class EventCollectorSubscriptions
{
    public static async Task<EventCollectorSubscriptionInventory> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Event Collector subscription inventory requires Windows.");
        ToolResult evidence;
        try
        {
            evidence = await BoundedTool.RunAsync(new("wecutil.exe", ["enum-subscription"], true),
                TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            evidence = new(-1, "", error.Message, false);
        }
        return Interpret(evidence, token);
    }

    public static EventCollectorSubscriptionInventory Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new([], false, evidence);
        var names = new List<string>();
        foreach (string line in evidence.Output.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            token.ThrowIfCancellationRequested();
            string name = line.Trim();
            if (name.Length == 0) continue;
            if (name.Length > 512 || name.Any(char.IsControl) || names.Count >= 256 ||
                names.Contains(name, StringComparer.OrdinalIgnoreCase)) return new([], false, evidence);
            names.Add(name);
        }
        return new(Array.AsReadOnly(names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()), true, evidence);
    }

    public static WorkflowCheck Describe(EventCollectorSubscriptionInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var observed = Interpret(inventory.Evidence);
        if (!observed.Complete)
            return new("Event Collector subscriptions", CheckState.Unknown,
                "Subscription inventory is unavailable or incomplete. No subscription, credential, forwarded event or channel setting changed.", Evidence: inventory.Evidence);
        var displayed = observed.Names.Take(20).ToArray();
        string detail = observed.Names.Count == 0
            ? "No Windows Event Collector subscriptions were reported. This does not prove that event forwarding is required or correctly configured."
            : observed.Names.Count + " Windows Event Collector subscriptions reported: " + string.Join(", ", displayed) +
              (observed.Names.Count > displayed.Length ? $". {observed.Names.Count - displayed.Length} additional names remain in the retained evidence" : "") +
              ". Inventory does not verify source reachability, authentication, delivery or event freshness.";
        return new("Event Collector subscriptions", CheckState.Information,
            detail + " No subscription, credential, forwarded event or channel setting changed.", Evidence: inventory.Evidence);
    }
}
