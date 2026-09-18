using KillerBench.Engine;

internal static class ServiceDependencyConfigurationChecks
{
    internal static void Run()
    {
        var installed = new ServiceInventorySnapshot(true,
        [
            new("root", "Root", "Running", "Auto", false, "Own Process"),
            new("left", "Left", "Running", "Manual", false, "Own Process"),
            new("right", "Right", "Running", "Manual", false, "Own Process")
        ]);
        ServiceDependencyConfigurationSnapshot Snapshot(params ServiceDependencyConfigurationEntry[] entries) => new(true, entries);
        var clear = ServiceDependencyConfigurationDiagnostics.Assess(installed: installed, snapshot: Snapshot(
            new("root", true, ["left", "right"], [], false), new("left", true, [], [], true), new("right", true, [], [], false)));
        Require(clear.State == CheckState.Information && clear.Repair is null && clear.Detail.Contains("2 direct service dependency references", StringComparison.Ordinal) &&
            clear.Detail.Contains("1 service has load-order group dependencies", StringComparison.Ordinal),
            "Coherent direct and load-order group dependency configuration remains informational");
        var missing = ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(
            new("root", true, ["missing"], ["missing"], false), new("left", true, [], [], false), new("right", true, [], [], false)), installed);
        Require(missing.State == CheckState.Attention && missing.Repair is null && missing.Detail.Contains("1 references to absent", StringComparison.Ordinal),
            "A missing direct dependency identity requests package review without selecting a change");
        var self = ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(
            new("root", true, ["root"], [], false), new("left", true, [], [], false), new("right", true, [], [], false)), installed);
        Require(self.State == CheckState.Attention && self.Detail.Contains("1 self-dependencies", StringComparison.Ordinal),
            "A direct self-dependency requests review");
        var cycle = ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(
            new("root", true, ["left"], [], false), new("left", true, ["right"], [], false), new("right", true, ["root"], [], false)), installed);
        Require(cycle.State == CheckState.Attention && cycle.Detail.Contains("3 services participating", StringComparison.Ordinal),
            "Every member of a direct dependency cycle is counted once");
        foreach (var invalid in new[]
        {
            ServiceDependencyConfigurationDiagnostics.Assess(new(false, []), installed),
            ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(new ServiceDependencyConfigurationEntry("root", true, [], [], false)), installed),
            ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(new ServiceDependencyConfigurationEntry("root", true, [], [], false), new("left", false, [], [], false), new("right", true, [], [], false)), installed),
            ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(new ServiceDependencyConfigurationEntry("root", true, ["left", "LEFT"], [], false), new("left", true, [], [], false), new("right", true, [], [], false)), installed),
            ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(new ServiceDependencyConfigurationEntry("root", true, ["bad/name"], [], false), new("left", true, [], [], false), new("right", true, [], [], false)), installed),
            ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(new ServiceDependencyConfigurationEntry("root", true, ["left"], ["right"], false), new("left", true, [], [], false), new("right", true, [], [], false)), installed)
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Incomplete, missing, unreadable, duplicate or malformed dependency evidence remains unknown");
        try
        {
            ServiceDependencyConfigurationDiagnostics.Assess(Snapshot(
                new("root", true, ["left"], [], false), new("left", true, [], [], false), new("right", true, [], [], false)), installed,
                token: new CancellationToken(true));
            throw new Exception("Dependency configuration cancellation was ignored");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: service dependency identities, load-order group context, missing references, cycles and no configuration changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
