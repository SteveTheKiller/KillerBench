namespace KillerBench.Engine;

public sealed record AppPackageReviewResult(AppPackageScope Scope, string SelectedFullName, AppPackage? Selected,
    IReadOnlyList<string> DependentPackages, IReadOnlyList<string> ReviewItems)
{
    public bool RemovalSupported => false;
}

public static class AppPackageReview
{
    public static AppPackageReviewResult Review(AppPackageInventoryResult inventory, string fullName, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 512 || fullName != fullName.Trim() || fullName.Any(char.IsControl))
            throw new ArgumentException("Select an exact package full name.", nameof(fullName));
        token.ThrowIfCancellationRequested();
        // Reinterpret retained evidence so modified summary flags cannot establish coverage.
        var observed = AppPackageInventory.Interpret(inventory.Scope, inventory.Evidence);
        var selected = observed.Packages.SingleOrDefault(package => string.Equals(package.FullName, fullName, StringComparison.OrdinalIgnoreCase));
        var items = new List<string>();
        if (!observed.Complete) items.Add("Package or dependency coverage is incomplete. Recollect before planning changes.");
        if (selected is null) items.Add("The exact selected package was not found in retained inventory.");
        if (selected?.Framework == true) items.Add("The selected package is a framework. Other applications may require it.");
        if (selected?.NonRemovable == true) items.Add("Windows reports this package as non-removable.");
        if (selected is not null) items.Add("Windows package status: " + (selected.Status ?? "unavailable") + ". Retained provider status does not verify application operation or authorize repair.");
        var dependents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reverse = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in observed.Packages)
        {
            token.ThrowIfCancellationRequested();
            foreach (string required in package.Dependencies ?? [])
            {
                if (!reverse.TryGetValue(required, out var consumers)) reverse.Add(required, consumers = []);
                consumers.Add(package.FullName);
            }
        }
        var pending = new Queue<string>();
        pending.Enqueue(fullName);
        while (pending.TryDequeue(out string? dependency))
        {
            token.ThrowIfCancellationRequested();
            if (!reverse.TryGetValue(dependency, out var consumers)) continue;
            foreach (string consumer in consumers)
                if (!string.Equals(consumer, fullName, StringComparison.OrdinalIgnoreCase) && dependents.Add(consumer))
                    pending.Enqueue(consumer);
        }
        if (dependents.Count > 0) items.Add("Other inventoried packages depend directly or indirectly on this target. Review their requirements before removal planning.");
        items.Add("Scope covers registered packages only. Provisioning, managed deployment, application data and unreported dependencies remain unverified. No removal is available or authorized by this review.");
        return new(observed.Scope, fullName, selected, Array.AsReadOnly(dependents.Order(StringComparer.Ordinal).ToArray()), items.AsReadOnly());
    }
}
