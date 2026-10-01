using System.Text.RegularExpressions;

namespace KillerBench.Engine.Devices;

public sealed record PciHardwareResult(string PackId, long Revision, string HardwareId, string VendorCode,
    string DeviceCode, KnowledgeEntry? Vendor, KnowledgeEntry? Device, string Detail);

public static partial class PciHardwareLookup
{
    [GeneratedRegex(@"^PCI\\VEN_(?<vendor>[0-9A-F]{4})&DEV_(?<device>[0-9A-F]{4})(?:&[A-Z0-9_&.-]{1,480})?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HardwareIdPattern();

    [GeneratedRegex("VEN_", RegexOptions.CultureInvariant)]
    private static partial Regex VendorMarkerPattern();

    [GeneratedRegex("DEV_", RegexOptions.CultureInvariant)]
    private static partial Regex DeviceMarkerPattern();

    public static PciHardwareResult Find(KnowledgeCatalog catalog, string hardwareId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        string normalized = Normalize(hardwareId);
        var match = HardwareIdPattern().Match(normalized);
        string vendorCode = "VEN_" + match.Groups["vendor"].Value;
        string deviceCode = vendorCode + "&DEV_" + match.Groups["device"].Value;
        var vendor = catalog.Find("pci-vendor", vendorCode);
        var device = catalog.Find("pci-device", deviceCode);
        string detail = device is not null
            ? "Matched the exact PCI vendor and device assignment present in this pack."
            : vendor is not null
                ? "Matched the PCI vendor assignment, but this pack has no exact device assignment."
                : "No matching PCI vendor or device assignment is present in this pack.";
        detail += " Registry assignments do not prove the manufacturer of the complete product, identify a compatible driver or authorize a download or installation.";
        return new(catalog.PackId, catalog.Revision, normalized, vendorCode, deviceCode, vendor, device, detail);
    }

    public static string Normalize(string? hardwareId)
    {
        if (string.IsNullOrWhiteSpace(hardwareId) || hardwareId != hardwareId.Trim() || hardwareId.Length > 512)
            throw new ArgumentException("Enter one complete PCI hardware ID.", nameof(hardwareId));
        string normalized = hardwareId.ToUpperInvariant();
        if (!HardwareIdPattern().IsMatch(normalized) ||
            VendorMarkerPattern().Count(normalized) != 1 ||
            DeviceMarkerPattern().Count(normalized) != 1)
            throw new ArgumentException("Use a PCI hardware ID beginning with PCI\\VEN_hhhh&DEV_hhhh.", nameof(hardwareId));
        return normalized;
    }
}
