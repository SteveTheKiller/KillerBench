using KillerBench.Engine;

internal static class StorageVolumeCapacityCorrelationChecks
{
    public static void Run()
    {
        const ulong gib = 1073741824;
        WorkflowCheck Topology(string volumes, string complete = "true", CheckState state = CheckState.Information) => new("Storage topology", state, "fixture",
            Evidence: new(0, $"{{\"complete\":{complete},\"disks\":[],\"partitions\":[],\"volumes\":{volumes}}}", "", false));
        string Volume(string letter, ulong? size, ulong? remaining) =>
            $"{{\"letter\":\"{letter}\",\"fileSystem\":\"NTFS\",\"health\":\"Healthy\",\"operational\":[\"OK\"],\"size\":{(size?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null")},\"remaining\":{(remaining?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null")}}}";
        var healthy = StorageVolumeCapacityCorrelation.Check(Topology("[" + Volume("C", 100 * gib, 40 * gib) + "," + Volume("D", 200 * gib, 20 * gib) + "]"));
        Require(healthy.State == CheckState.Information && healthy.Repair is null && healthy.Detail.Contains("C: 40.0 GiB", StringComparison.Ordinal) &&
            healthy.Detail.Contains("D: 20.0 GiB", StringComparison.Ordinal) && !healthy.Detail.Contains("Below 10%", StringComparison.Ordinal),
            "All drive-letter capacities are evaluated and the threshold boundary remains healthy");
        var lowTopology = Topology("[" + Volume("C", 100 * gib, 5 * gib) + "," + Volume("", gib, 5) + "]");
        var low = StorageVolumeCapacityCorrelation.Check(lowTopology);
        Require(low.State == CheckState.Attention && low.Repair is null && low.Detail.Contains("Below 10%: C:", StringComparison.Ordinal) &&
            low.Detail.Contains("1 unlettered volume was", StringComparison.Ordinal) && low.Evidence == lowTopology.Evidence,
            "Low lettered volume requests review while unlettered recovery capacity is excluded");
        var unavailable = StorageVolumeCapacityCorrelation.Check(Topology("[" + Volume("E", null, null) + "]"));
        Require(unavailable.State == CheckState.Unknown && unavailable.Repair is null && unavailable.Detail.Contains("unavailable or inconsistent", StringComparison.Ordinal),
            "Unavailable lettered capacity keeps all-volume coverage unknown");
        foreach (var invalid in new[]
        {
            StorageVolumeCapacityCorrelation.Check(Topology("[]", state: CheckState.Unknown)),
            StorageVolumeCapacityCorrelation.Check(Topology("[]", "false")),
            StorageVolumeCapacityCorrelation.Check(Topology("[" + Volume("CC", gib, gib) + "]")),
            StorageVolumeCapacityCorrelation.Check(Topology("[" + Volume("C", gib, gib) + "," + Volume("c", gib, gib) + "]")),
            StorageVolumeCapacityCorrelation.Check(new("Storage topology", CheckState.Information, "fixture", Evidence: new(0, "broken", "", false)))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Incomplete, malformed, duplicate and inconsistent topology cannot produce capacity coverage");
        try { StorageVolumeCapacityCorrelation.Check(Topology("[]"), new CancellationToken(true)); throw new Exception("Capacity cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: all drive-letter capacity thresholds, unavailable evidence and unlettered exclusions; no cleanup or storage changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
