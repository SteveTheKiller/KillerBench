namespace KillerBench.Engine.Storage;

public static class PhysicalDiskCorrelation
{
    private const string InventoryPrefix = "Physical disk: ";
    private const string ReliabilityPrefix = "Storage reliability: ";

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> inventory,
        IReadOnlyList<WorkflowCheck> reliability, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(reliability);
        const string boundary = " Provider IDs are point-in-time identities and are not mapped to drive letters. This correlation is not a surface test, SMART result, backup check, root-cause diagnosis or repair authorization. No disk, pool, volume, file, driver or storage setting changed.";
        static WorkflowCheck Unknown(string detail) => new("Physical disk evidence correlation", CheckState.Unknown, detail + boundary);
        if (inventory.Count is < 1 or > 65 || reliability.Count is < 1 or > 65)
            return Unknown("Physical-disk inventory or reliability coverage was absent or outside its supported bound.");

        if (!Read(inventory, InventoryPrefix, out var disks) || disks.Count == 0)
            return Unknown("Complete exact physical-disk identities are required for correlation.");
        if (reliability.Count == 1 && reliability[0].Name == "Storage reliability")
            return Unknown("Storage reliability counters were unavailable or incomplete.");
        if (!Read(reliability, ReliabilityPrefix, out var counters))
            return Unknown("Complete exact reliability-counter identities are required for correlation.");
        var unmatched = counters.Keys.Where(id => !disks.ContainsKey(id)).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        if (unmatched.Length > 0)
            return Unknown("Reliability counters referenced physical-disk identities absent from the current inventory: " + string.Join(", ", unmatched) + ".");

        var all = disks.Values.Concat(counters.Values).ToArray();
        int incomplete = all.Count(check => check.EffectiveState == CheckState.Unknown);
        var attentionIds = disks.Where(pair => pair.Value.EffectiveState == CheckState.Attention)
            .Select(pair => pair.Key).Concat(counters.Where(pair => pair.Value.EffectiveState == CheckState.Attention)
                .Select(pair => pair.Key)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        int withoutCounters = disks.Keys.Count(id => !counters.ContainsKey(id));
        CheckState state = incomplete > 0 ? CheckState.Unknown : attentionIds.Length > 0 ? CheckState.Attention : CheckState.Information;
        string detail = $"Correlated {counters.Count} reliability-counter record{(counters.Count == 1 ? "" : "s")} with {disks.Count} current physical-disk record{(disks.Count == 1 ? "" : "s")}. {withoutCounters} disk{(withoutCounters == 1 ? " has" : "s have")} no provider reliability counters; unavailable counters are not treated as zero. ";
        detail += state switch
        {
            CheckState.Attention => "Current provider evidence requests review for disk IDs: " + string.Join(", ", attentionIds) + ". Confirm backups and vendor diagnostics before repair writes.",
            CheckState.Unknown => $"{incomplete} source result{(incomplete == 1 ? " is" : "s are")} incomplete. Recollect before drawing a combined conclusion.",
            _ => "No correlated source requested review, but this point-in-time result does not establish physical health."
        };
        return new("Physical disk evidence correlation", state, detail + boundary);
    }

    private static bool Read(IReadOnlyList<WorkflowCheck> checks, string prefix,
        out Dictionary<string, WorkflowCheck> values)
    {
        values = new(StringComparer.OrdinalIgnoreCase);
        foreach (var check in checks)
        {
            if (check is null || !check.Name.StartsWith(prefix, StringComparison.Ordinal) ||
                !DriverReference.ValidText(check.Name[prefix.Length..], 128) ||
                !values.TryAdd(check.Name[prefix.Length..], check))
                return false;
        }
        return true;
    }
}
