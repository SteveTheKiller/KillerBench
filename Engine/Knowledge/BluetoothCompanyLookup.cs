using System.Globalization;

namespace KillerBench.Engine;

public sealed record BluetoothCompanyResult(string PackId, long Revision, string CompanyId,
    ushort NumericId, KnowledgeEntry? Match, string Detail);

public static class BluetoothCompanyLookup
{
    public static BluetoothCompanyResult Find(KnowledgeCatalog catalog, string companyId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        (string normalized, ushort numeric) = Normalize(companyId);
        var match = catalog.Find("bluetooth-company", normalized);
        string detail = match is null
            ? "No matching Bluetooth company identifier assignment is present in this pack."
            : "Matched the exact Bluetooth company identifier assignment present in this pack.";
        detail += " An assignment does not prove the manufacturer of a complete product, identify a compatible driver or authorize a download or installation.";
        return new(catalog.PackId, catalog.Revision, normalized, numeric, match, detail);
    }

    public static (string Normalized, ushort Numeric) Normalize(string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId) || companyId != companyId.Trim())
            throw new ArgumentException("Enter one decimal or 0x-prefixed Bluetooth company identifier from 0 through 65535.", nameof(companyId));
        bool hexadecimal = companyId.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        ReadOnlySpan<char> digits = hexadecimal ? companyId.AsSpan(2) : companyId.AsSpan();
        bool decimalDigits = true;
        foreach (char character in digits)
            if (character is < '0' or > '9') { decimalDigits = false; break; }
        bool valid = digits.Length > 0 && (hexadecimal
            ? digits.Length <= 4 && ushort.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _)
            : decimalDigits && ushort.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out _));
        if (!valid || !ushort.TryParse(digits, hexadecimal ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
                CultureInfo.InvariantCulture, out ushort numeric))
            throw new ArgumentException("Enter one decimal or 0x-prefixed Bluetooth company identifier from 0 through 65535.", nameof(companyId));
        return ("0x" + numeric.ToString("X4", CultureInfo.InvariantCulture), numeric);
    }
}
