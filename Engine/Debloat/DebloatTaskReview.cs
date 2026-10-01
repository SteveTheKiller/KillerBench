namespace KillerBench.Engine.Debloat;

public sealed record DebloatTaskCandidate(DebloatAreas Area, ScheduledTaskSnapshot Task, string SourcePattern);

public sealed record DebloatTaskEvidence(IReadOnlyList<DebloatTaskCandidate> Matches, bool Complete,
    ToolResult Evidence, string Detail);

public static class DebloatTaskReview
{
    private static readonly Dictionary<string, DebloatAreas> ExactTargets =
        new Dictionary<string, DebloatAreas>(StringComparer.OrdinalIgnoreCase)
        {
            [@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"] = DebloatAreas.TelemetryAndReporting,
            [@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"] = DebloatAreas.TelemetryAndReporting,
            [@"\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask"] = DebloatAreas.TelemetryAndReporting,
            [@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"] = DebloatAreas.TelemetryAndReporting
        };

    private static readonly (DebloatAreas Area, string Prefix, string Pattern, string[] Manufacturers)[] OemPaths =
    [
        (DebloatAreas.HpSoftware, @"\HP", @"\HP*", ["hp", "hewlett-packard", "hewlett packard"]),
        (DebloatAreas.DellSoftware, @"\Dell", @"\Dell*", ["dell"]),
        (DebloatAreas.AsusAcerSoftware, @"\ASUS", @"\ASUS*", ["asus", "asustek"]),
        (DebloatAreas.AsusAcerSoftware, @"\Acer", @"\Acer*", ["acer"])
    ];

    public static DebloatTaskEvidence Review(DebloatOptions options, DebloatEnvironmentEvidence environment,
        ScheduledTaskInventoryResult inventory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(environment);
        options.Validate();
        var matches = new List<DebloatTaskCandidate>();
        foreach (var task in inventory.Tasks)
        {
            if (ExactTargets.TryGetValue(task.Identity.FullName, out var area) && options.Areas.HasFlag(area))
                matches.Add(new(area, task, task.Identity.FullName));
            foreach (var (oemArea, prefix, pattern, _) in OemPaths.Where(target => options.Areas.HasFlag(target.Area) &&
                ManufacturerMatches(environment, target.Manufacturers)))
                if (task.Identity.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    matches.Add(new(oemArea, task, pattern));
        }
        return new(Array.AsReadOnly(matches.DistinctBy(match => match.Area + "\0" + match.Task.Identity.FullName,
                StringComparer.OrdinalIgnoreCase).OrderBy(match => match.Task.Identity.FullName, StringComparer.Ordinal).ToArray()), inventory.Complete, inventory.Evidence,
            "Exact telemetry identities and source OEM path patterns were correlated while retaining each exact discovered identity. State, action count and runtime history are evidence only. No task was enabled, disabled, started, stopped, changed or deleted.");
    }

    public static async Task<DebloatTaskEvidence> ReadAsync(DebloatOptions options,
        DebloatEnvironmentEvidence environment, CancellationToken token)
    {
        var inventory = await ScheduledTaskInventory.ReadAsync(token).ConfigureAwait(false);
        return Review(options, environment, inventory);
    }

    private static bool ManufacturerMatches(DebloatEnvironmentEvidence environment, IReadOnlyList<string> names) =>
        environment.Complete && environment.Manufacturer is not null &&
        names.Any(name => environment.Manufacturer.Contains(name, StringComparison.OrdinalIgnoreCase));
}
