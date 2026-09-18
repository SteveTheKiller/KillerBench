using System.Text.Json;
using KillerBench.Engine;

internal static class StorageSpacesChecks
{
    public static void Run()
    {
        var healthy = StorageSpacesDiagnostics.Interpret(Result(new
        {
            pools = new[] { Pool("pool-1", "Lab pool", "Healthy", "OK", false, 2_000_000_000_000UL, 1_000_000_000_000UL) },
            virtualDisks = new[] { VirtualDisk("disk-1", "Files", "Healthy", "OK", "Mirror", 900_000_000_000UL, 1_800_000_000_000UL) },
            poolError = (string?)null,
            virtualDiskError = (string?)null
        }));
        Require(healthy.Count == 2 && healthy.All(check => check.State == CheckState.Clear) &&
            healthy[0].Detail.Contains("1 reported; 0 need attention", StringComparison.Ordinal) &&
            healthy[0].Detail.Contains("allocated", StringComparison.Ordinal) &&
            healthy[1].Detail.Contains("resiliency Mirror", StringComparison.Ordinal),
            "Healthy pool and virtual disk evidence reports capacity, allocation and resiliency without inventing member relationships");

        var degraded = StorageSpacesDiagnostics.Interpret(Result(new
        {
            pools = new[] { Pool("pool-1", "Archive", "Warning", "Degraded", true, 4_000_000_000_000UL, 3_000_000_000_000UL) },
            virtualDisks = new[] { VirtualDisk("disk-1", "Archive mirror", "Unhealthy", "Detached", "Mirror", 1_500_000_000_000UL, 3_000_000_000_000UL) },
            poolError = (string?)null,
            virtualDiskError = (string?)null
        }));
        Require(degraded.All(check => check.State == CheckState.Attention) &&
            degraded[0].Detail.Contains("read-only", StringComparison.Ordinal) && degraded[1].Detail.Contains("Detached", StringComparison.Ordinal) &&
            degraded.All(check => check.Repair is null),
            "Degraded and detached Storage Spaces evidence requests review without selecting repair");

        var empty = StorageSpacesDiagnostics.Interpret(Result(new
        {
            pools = Array.Empty<object>(), virtualDisks = Array.Empty<object>(), poolError = (string?)null, virtualDiskError = (string?)null
        }));
        Require(empty.All(check => check.State == CheckState.Information), "No configured Storage Spaces is a normal informational result");

        var partial = StorageSpacesDiagnostics.Interpret(Result(new
        {
            pools = new[] { Pool("pool-1", "Lab", "Healthy", "OK", false, 100UL, 50UL) },
            virtualDisks = Array.Empty<object>(), poolError = (string?)null, virtualDiskError = "access denied"
        }));
        Require(partial[0].State == CheckState.Clear && partial[1].State == CheckState.Unknown &&
            partial[1].Detail.Contains("access denied", StringComparison.Ordinal),
            "Independent Storage Spaces provider failures retain successful evidence");

        var duplicate = StorageSpacesDiagnostics.Interpret(Result(new
        {
            pools = new[] { Pool("same", "One", "Healthy", "OK", false, 100UL, 50UL), Pool("same", "Two", "Healthy", "OK", false, 100UL, 50UL) },
            virtualDisks = Array.Empty<object>(), poolError = (string?)null, virtualDiskError = (string?)null
        }));
        Require(duplicate[0].State == CheckState.Unknown, "Duplicate Storage Spaces identities cannot establish health");

        var capped = StorageSpacesDiagnostics.Interpret(Result(new
        {
            pools = Enumerable.Range(0, 33).Select(index => Pool("p" + index, "Pool " + index, "Healthy", "OK", false, 100UL, 50UL)).ToArray(),
            virtualDisks = Array.Empty<object>(), poolError = (string?)null, virtualDiskError = (string?)null
        }));
        Require(capped[0].State == CheckState.Unknown && capped[0].Detail.Contains("coverage was capped", StringComparison.Ordinal),
            "Storage Spaces inventory cap remains incomplete");

        foreach (var bad in new[]
        {
            new ToolResult(1, "", "provider failure", false),
            new ToolResult(0, "{}", "", false),
            new ToolResult(0, "not json", "", false),
            new ToolResult(0, "{}", "", true),
            new ToolResult(0, "{}", "", false) { TimedOut = true }
        })
            Require(StorageSpacesDiagnostics.Interpret(bad).All(check => check.State == CheckState.Unknown),
                "Failed, malformed, truncated and timed-out Storage Spaces evidence remains unknown");

        Console.WriteLine("PASS: Storage Spaces pool and virtual-disk health, partial evidence, bounds and read-only classification.");
    }

    private static object Pool(string identity, string name, string health, string operation, bool readOnly, ulong size, ulong allocated) =>
        new { identity, name, health, operation, readOnly, size, allocated };

    private static object VirtualDisk(string identity, string name, string health, string operation, string resiliency, ulong size, ulong footprint) =>
        new { identity, name, health, operation, resiliency, size, footprint };

    private static ToolResult Result(object value) => new(0, JsonSerializer.Serialize(value), "", false);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
