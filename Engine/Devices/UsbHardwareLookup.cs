using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public sealed record UsbHardwareResult(string PackId, long Revision, string HardwareId, string VendorCode,
    string ProductCode, KnowledgeEntry? Vendor, KnowledgeEntry? Product, string Detail);

public static partial class UsbHardwareLookup
{
    [GeneratedRegex(@"^USB\\VID_(?<vendor>[0-9A-F]{4})&PID_(?<product>[0-9A-F]{4})(?:&[A-Z0-9_&.-]{1,480})?$", RegexOptions.CultureInvariant)]
    private static partial Regex HardwareIdPattern();

    public static UsbHardwareResult Find(KnowledgeCatalog catalog, string hardwareId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        string normalized = Normalize(hardwareId);
        var match = HardwareIdPattern().Match(normalized);
        string vendorCode = "VID_" + match.Groups["vendor"].Value;
        string productCode = vendorCode + "&PID_" + match.Groups["product"].Value;
        var vendor = catalog.Find("usb-vendor", vendorCode);
        var product = catalog.Find("usb-device", productCode);
        string detail = product is not null
            ? "Exact USB product and vendor assignments were found in the local pack. This does not establish compatible driver selection."
            : vendor is not null
                ? "The USB vendor assignment was found, but the product ID is absent from the local pack. This does not identify a product or compatible driver."
                : "The USB vendor and product IDs are absent from the local pack. No manufacturer or driver is inferred.";
        return new(catalog.PackId, catalog.Revision, normalized, vendorCode, productCode, vendor, product, detail);
    }

    public static string Normalize(string hardwareId)
    {
        if (string.IsNullOrWhiteSpace(hardwareId) || hardwareId.Length > 512 || hardwareId != hardwareId.Trim())
            throw new ArgumentException("USB hardware ID is empty, padded or too long.", nameof(hardwareId));
        string normalized = hardwareId.ToUpperInvariant();
        if (Count(normalized, "VID_") != 1 || Count(normalized, "PID_") != 1 || !HardwareIdPattern().IsMatch(normalized))
            throw new ArgumentException("USB hardware ID must contain one exact VID_hhhh and PID_hhhh pair.", nameof(hardwareId));
        return normalized;
    }

    private static int Count(string value, string part)
    {
        int count = 0, offset = 0;
        while ((offset = value.IndexOf(part, offset, StringComparison.Ordinal)) >= 0) { count++; offset += part.Length; }
        return count;
    }
}
