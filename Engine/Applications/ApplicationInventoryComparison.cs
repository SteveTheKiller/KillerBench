namespace KillerBench.Engine.Applications;

public enum ApplicationRegistrationChange { Added, Removed, VersionChanged, MetadataChanged }
public sealed record ApplicationRegistrationDifference(ApplicationRegistrationChange Change, InstalledApplication? Before, InstalledApplication? After);
public sealed record ApplicationInventoryComparisonResult(bool Comparable, InstalledApplicationScope Scope,
    IReadOnlyList<ApplicationRegistrationDifference> Differences, string Detail);

public static class ApplicationInventoryComparison
{
    public static ApplicationInventoryComparisonResult Compare(InstalledApplicationInventory before, InstalledApplicationInventory after,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        token.ThrowIfCancellationRequested();
        var first = InstalledApplications.Interpret(before.Evidence);
        var second = InstalledApplications.Interpret(after.Evidence);
        if (!first.Complete || !second.Complete || first.Scope != second.Scope)
            return new(false, first.Scope, [], "Complete inventories from the same scope are required. Missing registrations cannot establish removals when coverage is incomplete.");
        var old = first.Applications.ToDictionary(app => app.View + "\\" + app.Key, StringComparer.OrdinalIgnoreCase);
        var current = second.Applications.ToDictionary(app => app.View + "\\" + app.Key, StringComparer.OrdinalIgnoreCase);
        var changes = new List<ApplicationRegistrationDifference>();
        foreach (string key in old.Keys.Union(current.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            old.TryGetValue(key, out var previous);
            current.TryGetValue(key, out var next);
            if (previous is null) changes.Add(new(ApplicationRegistrationChange.Added, null, next));
            else if (next is null) changes.Add(new(ApplicationRegistrationChange.Removed, previous, null));
            else if (!string.Equals(previous.Version, next.Version, StringComparison.Ordinal)) changes.Add(new(ApplicationRegistrationChange.VersionChanged, previous, next));
            else if (!string.Equals(previous.Name, next.Name, StringComparison.Ordinal) || !string.Equals(previous.Publisher, next.Publisher, StringComparison.Ordinal) ||
                previous.HasUninstallCommand != next.HasUninstallCommand || previous.HasInstallLocation != next.HasInstallLocation)
                changes.Add(new(ApplicationRegistrationChange.MetadataChanged, previous, next));
        }
        return new(true, first.Scope, changes.AsReadOnly(), "Structural registry differences only. Comparable means complete matching registry scopes, not verified machine or account identity. The caller must establish that snapshots belong to the same machine and account before treating differences as changes over time. Added or removed registrations do not prove successful installation or uninstallation. No system changes performed.");
    }
}
