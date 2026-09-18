namespace KillerBench.Engine;

public static class StorageSpacesOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck pools, WorkflowCheck virtualDisks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(pools);
        ArgumentNullException.ThrowIfNull(virtualDisks);
        token.ThrowIfCancellationRequested();
        const string name = "Storage Spaces operational conclusion";
        const string boundary = " Provider results do not verify pool accessibility, redundancy, data integrity, backup recoverability, physical-disk health, volume health or application I/O. No pool, virtual disk, physical disk, volume, allocation, attachment, repair, optimization, resiliency or setting operation is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        if (pools.Name != "Storage pools" || virtualDisks.Name != "Storage virtual disks")
            return Unknown("Storage Spaces evidence carried an unexpected identity.");
        var evidence = new[] { pools, virtualDisks };
        if (evidence.Any(check => check.EffectiveState == CheckState.Unknown))
            return Unknown("Pool or virtual-disk evidence was incomplete. Recollect both inventories before drawing a combined conclusion.");
        int attention = evidence.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        if (attention > 0)
            return new(name, CheckState.Attention,
                $"Storage Spaces inventory contains {attention} source{(attention == 1 ? "" : "s")} requiring review. Use the exact pool and virtual-disk identities above for narrower storage diagnosis." + boundary);
        if (evidence.All(check => check.EffectiveState == CheckState.Information))
            return new(name, CheckState.Information,
                "No non-primordial Storage Spaces pool or virtual disk was reported. This is a complete empty inventory, not a general storage-health verdict." + boundary);
        return new(name, CheckState.Clear,
            "All reported Storage Spaces pools and virtual disks were classified without a health or operational-state concern in the bounded inventory." + boundary);
    }
}
