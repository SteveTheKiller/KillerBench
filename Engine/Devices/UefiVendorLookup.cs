namespace KillerBench.Engine;

public enum UefiVendorRegistry
{
    Pnp,
    Acpi
}

public sealed record UefiVendorResult(string PackId, long Revision, UefiVendorRegistry Registry,
    string VendorId, KnowledgeEntry? Match, string Detail);

public static class UefiVendorLookup
{
    public static UefiVendorResult Find(KnowledgeCatalog catalog, UefiVendorRegistry registry, string vendorId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        string normalized = Normalize(registry, vendorId);
        string context = registry == UefiVendorRegistry.Pnp ? "pnp-vendor" : "acpi-vendor";
        var match = catalog.Find(context, normalized);
        string detail = match is null
            ? "No matching UEFI vendor ID assignment is present in this pack."
            : "Matched the exact UEFI vendor ID assignment present in this pack.";
        detail += " A registry assignment does not prove the manufacturer of a complete product, identify a compatible driver or authorize a download or installation.";
        return new(catalog.PackId, catalog.Revision, registry, normalized, match, detail);
    }

    public static string Normalize(UefiVendorRegistry registry, string? vendorId)
    {
        int length = registry == UefiVendorRegistry.Pnp ? 3 : 4;
        if (string.IsNullOrWhiteSpace(vendorId) || vendorId != vendorId.Trim() || vendorId.Length != length)
            throw new ArgumentException($"Enter one exact {registry.ToString().ToUpperInvariant()} vendor ID.", nameof(vendorId));
        string normalized = vendorId.ToUpperInvariant();
        bool valid = registry == UefiVendorRegistry.Pnp
            ? normalized.All(character => character is >= 'A' and <= 'Z')
            : normalized.All(character => character is >= 'A' and <= 'Z' or >= '0' and <= '9');
        if (!valid)
            throw new ArgumentException($"Enter one exact {registry.ToString().ToUpperInvariant()} vendor ID.", nameof(vendorId));
        return normalized;
    }
}
