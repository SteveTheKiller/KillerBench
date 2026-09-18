namespace KillerBench.Engine;

public sealed record MacVendorResult(string PackId, long Revision, string Address, bool LocallyAdministered,
    bool Multicast, string? MatchedPrefix, KnowledgeEntry? Match, string Detail);

public static class MacVendorLookup
{
    public static MacVendorResult Find(KnowledgeCatalog catalog, string address)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        string normalized = Normalize(address);
        byte first = Convert.ToByte(normalized[..2], 16);
        bool multicast = (first & 1) != 0;
        bool local = (first & 2) != 0;
        if (multicast)
            return new(catalog.PackId, catalog.Revision, normalized, local, true, null, null,
                "Multicast addresses do not identify one device vendor. No registry match was applied.");
        if (local)
            return new(catalog.PackId, catalog.Revision, normalized, true, false, null, null,
                "The locally administered bit is set. The prefix may be randomized or assigned by local software, so no vendor claim was made.");
        foreach (int length in new[] { 9, 7, 6 })
        {
            string prefix = normalized[..length];
            if (catalog.Find("mac-vendor", prefix) is { } match)
                return new(catalog.PackId, catalog.Revision, normalized, false, false, prefix, match,
                    "Matched the longest available IEEE assignment prefix. Registry ownership does not prove the current device manufacturer.");
        }
        return new(catalog.PackId, catalog.Revision, normalized, false, false, null, null,
            "No matching 36-bit, 28-bit or 24-bit assignment is present in this pack.");
    }

    public static string Normalize(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || address != address.Trim())
            throw new ArgumentException("Enter one complete MAC address.", nameof(address));
        string hex;
        if (address.Length == 12 && address.All(Uri.IsHexDigit)) hex = address;
        else if (address.Length == 17 && (address[2] is ':' or '-') && address[5] == address[2] &&
            address[8] == address[2] && address[11] == address[2] && address[14] == address[2])
            hex = string.Concat(address.Where(character => character != address[2]));
        else if (address.Length == 14 && address[4] == '.' && address[9] == '.')
            hex = address.Replace(".", "", StringComparison.Ordinal);
        else throw new ArgumentException("Use 12 hexadecimal digits, six colon- or hyphen-separated octets, or three dot-separated groups.", nameof(address));
        if (hex.Length != 12 || !hex.All(Uri.IsHexDigit) || hex.All(character => character == '0') || hex.All(character => character is 'F' or 'f'))
            throw new ArgumentException("The MAC address is incomplete or reserved.", nameof(address));
        return hex.ToUpperInvariant();
    }
}
