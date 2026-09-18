using System.Globalization;
using System.Text;
using System.Text.Json;
using KillerBench.Engine;

internal sealed record BluetoothCompanyIdentifierImport(KnowledgePack Pack, byte[] Source);

internal static class BluetoothCompanyIdentifierImporter
{
    internal static async Task<BluetoothCompanyIdentifierImport> BuildAsync(string path, long revision,
        DateOnly publishedOn, string license, CancellationToken token)
    {
        if (revision < 1 || publishedOn == default || string.IsNullOrWhiteSpace(license) ||
            license.Length > 2000 || license.Any(char.IsControl))
            throw new ArgumentException("Bluetooth company identifier import metadata is incomplete or unsupported.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is 0 or > 8_000_000)
            throw new InvalidDataException("Bluetooth company identifier source exceeds the supported size limit.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException error)
        { throw new InvalidDataException("Bluetooth company identifier source is not valid UTF-8.", error); }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length == 0 || lines[0].TrimStart('\uFEFF') != "company_identifiers:")
            throw new InvalidDataException("Bluetooth company identifier YAML lacks the expected root.");
        const string sourceUrl = "https://www.bluetooth.com/specifications/assigned-numbers/";
        var entries = new List<KnowledgeEntry>();
        var codes = new HashSet<string>(StringComparer.Ordinal);
        string? pendingCode = null;
        for (int index = 1; index < lines.Length; index++)
        {
            token.ThrowIfCancellationRequested();
            string line = lines[index];
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (pendingCode is null)
            {
                const string prefix = "  - value: 0x";
                if (!line.StartsWith(prefix, StringComparison.Ordinal) || line.Length != prefix.Length + 4 ||
                    !ushort.TryParse(line.AsSpan(prefix.Length), NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture, out ushort value))
                    throw new InvalidDataException("Bluetooth company identifier YAML contains an unsupported value record.");
                pendingCode = "0x" + value.ToString("X4", CultureInfo.InvariantCulture);
                if (!codes.Add(pendingCode))
                    throw new InvalidDataException("Bluetooth company identifier YAML contains a duplicate identifier.");
                continue;
            }
            const string namePrefix = "    name: ";
            if (!line.StartsWith(namePrefix, StringComparison.Ordinal))
                throw new InvalidDataException("Bluetooth company identifier YAML contains an incomplete identifier record.");
            string name = ParseName(line[namePrefix.Length..]);
            if (name.Length is 0 or > 512 || name != name.Trim() || name.Any(char.IsControl) || !name.Any(char.IsLetterOrDigit))
                throw new InvalidDataException("Bluetooth company identifier YAML contains an invalid company name.");
            entries.Add(new("bluetooth-company", pendingCode, name,
                "Bluetooth company identifier assignment from the reviewed Bluetooth SIG Assigned Numbers snapshot.",
                sourceUrl, publishedOn,
                "Bluetooth company identifiers. The assignment holder does not establish the current device manufacturer or select a driver."));
            pendingCode = null;
        }
        if (pendingCode is not null)
            throw new InvalidDataException("Bluetooth company identifier YAML ends with an incomplete record.");
        if (entries.Count is 0 or > 65_536)
            throw new InvalidDataException("Bluetooth company identifier entry count is unsupported.");
        var pack = new KnowledgePack(1, "bluetooth-company-identifiers", revision, publishedOn, license,
            entries.OrderBy(entry => entry.Code, StringComparer.Ordinal).ToArray());
        KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack), token);
        return new(pack, bytes);
    }

    private static string ParseName(string scalar)
    {
        if (scalar.Length >= 2 && scalar[0] == '\'' && scalar[^1] == '\'')
            return scalar[1..^1].Replace("''", "'", StringComparison.Ordinal);
        if (scalar.Length >= 2 && scalar[0] == '"' && scalar[^1] == '"')
        {
            try { return JsonSerializer.Deserialize<string>(scalar) ?? string.Empty; }
            catch (JsonException error)
            { throw new InvalidDataException("Bluetooth company identifier YAML contains an invalid quoted company name.", error); }
        }
        throw new InvalidDataException("Bluetooth company identifier YAML contains an unsupported company-name scalar.");
    }
}
