using KillerBench.Engine;

internal static class StorageTopologyChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string complete = "true", string disks = "[]", string partitions = "[]", string volumes = "[]") =>
            new(0, $"{{\"complete\":{complete},\"disks\":{disks},\"partitions\":{partitions},\"volumes\":{volumes}}}", "", false);
        const string disk = "[{\"number\":0,\"name\":\"Fixture disk\",\"style\":\"GPT\",\"health\":\"Healthy\",\"operational\":[\"Online\"],\"boot\":true,\"system\":true,\"readOnly\":false,\"offline\":false,\"size\":1000}]";
        const string partition = "[{\"disk\":0,\"number\":1,\"letter\":\"C\",\"type\":\"Basic\",\"size\":900,\"boot\":true,\"system\":true,\"hidden\":false,\"readOnly\":false}]";
        const string volume = "[{\"letter\":\"C\",\"fileSystem\":\"NTFS\",\"health\":\"Healthy\",\"operational\":[\"OK\"],\"size\":900,\"remaining\":400}]";
        var healthy = Evidence(disks: disk, partitions: partition, volumes: volume);
        var healthyResult = StorageTopologyDiagnostics.Interpret(healthy);
        Require(healthyResult.State == CheckState.Information && healthyResult.Repair is null && healthyResult.Evidence == healthy &&
            healthyResult.Detail.Contains("1 drive-letter volume mapped", StringComparison.Ordinal),
            "Healthy storage topology remains informational and non-mutating");
        var warning = StorageTopologyDiagnostics.Interpret(Evidence(disks: disk.Replace("Healthy", "Warning"), partitions: partition, volumes: volume));
        Require(warning.State == CheckState.Attention && warning.Repair is null && warning.Detail.Contains("Disk 0 health: Warning", StringComparison.Ordinal),
            "Reported storage health warnings request review without selecting repair");
        foreach (var invalid in new[]
        {
            healthy with { ExitCode = 1 }, healthy with { Error = "denied" }, healthy with { TimedOut = true }, healthy with { Truncated = true }, Evidence("false"),
            Evidence(disks: "[{}]"), Evidence(disks: disk, partitions: partition.Replace("\"disk\":0", "\"disk\":1"), volumes: volume),
            Evidence(disks: disk, partitions: partition.Replace("\"letter\":\"C\"", "\"letter\":\"\\u0000\""), volumes: volume),
            Evidence(disks: disk, partitions: partition, volumes: volume.Replace("\"remaining\":400", "\"remaining\":901")),
            Evidence(disks: disk.Replace("\"GPT\"", "\"Future\"")),
            Evidence(disks: disk[..^1] + ",{\"number\":0,\"name\":\"Duplicate\",\"style\":\"GPT\",\"health\":\"Healthy\",\"operational\":[\"Online\"],\"boot\":false,\"system\":false,\"readOnly\":false,\"offline\":false,\"size\":1000}]")
        }) Require(StorageTopologyDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed, inconsistent and duplicate storage topology remains unknown");
        var unavailableCapacity = StorageTopologyDiagnostics.Interpret(Evidence(disks: disk, partitions: partition,
            volumes: volume.Replace("\"size\":900,\"remaining\":400", "\"size\":null,\"remaining\":null")));
        Require(unavailableCapacity.State == CheckState.Information && unavailableCapacity.Repair is null,
            "Unavailable volume capacity remains explicit without invalidating complete topology identity evidence");
        var capped = Evidence(disks: "[" + string.Join(",", Enumerable.Range(0, 65).Select(index =>
            $"{{\"number\":{index},\"name\":\"Disk{index}\",\"style\":\"GPT\",\"health\":\"Healthy\",\"operational\":[\"Online\"],\"boot\":false,\"system\":false,\"readOnly\":false,\"offline\":false,\"size\":1000}}")) + "]");
        Require(StorageTopologyDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized storage topology cannot claim complete coverage");
        Console.WriteLine("PASS: bounded disk, partition and volume topology, health observations and incomplete evidence; no storage changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
