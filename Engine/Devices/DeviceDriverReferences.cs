using System.Text.Json;

namespace KillerBench.Engine;

public sealed record DriverPlatformContext(string Architecture, string WindowsProduct, int Build)
{
    public static DriverPlatformContext? FromCurrentWindows()
    {
        var snapshot = UpdateApplicabilityDiagnostics.Read();
        if (!snapshot.Complete || !int.TryParse(snapshot.Build,
            System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int build))
            return null;
        string? architecture = snapshot.Architecture?.ToLowerInvariant() switch
        {
            "x86" => "x86",
            "x64" or "amd64" => "x64",
            "arm64" => "arm64",
            _ => null
        };
        string? product = snapshot.InstallationType switch
        {
            string value when value.StartsWith("Server", StringComparison.OrdinalIgnoreCase) => "server",
            string value when value.Equals("Client", StringComparison.OrdinalIgnoreCase) => "client",
            _ => null
        };
        return architecture is null || product is null || build <= 0 ? null : new(architecture, product, build);
    }
}

public static class DeviceDriverReferences
{
    public static WorkflowCheck Check(ToolResult evidence, KnowledgeCatalogCollection catalogs,
        DriverPlatformContext? platform, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        if (catalogs.Count == 0) throw new ArgumentException("At least one knowledge pack is required.", nameof(catalogs));
        WorkflowCheck Unknown(string detail) => new("Device driver references", CheckState.Unknown,
            detail + " No driver was downloaded, selected or installed.", Evidence: evidence);
        if (platform is null || platform.Build <= 0 || platform.Architecture is not ("x86" or "x64" or "arm64") ||
            platform.WindowsProduct is not ("client" or "server"))
            return Unknown("Exact Windows architecture, product type or build evidence is unavailable. Driver references were not evaluated.");
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Device evidence is unavailable or incomplete. Driver references were not evaluated.");
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            if (!json.RootElement.TryGetProperty("devices", out var devices) || devices.ValueKind != JsonValueKind.Array ||
                devices.GetArrayLength() > 256)
                return Unknown("Device evidence is malformed or exceeds the 256-device review bound.");
            int problemDevices = 0, referencedDevices = 0, totalReferences = 0;
            bool incomplete = false;
            var details = new List<string>();
            var deviceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in devices.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!TryDevice(device, deviceIds, out string deviceId, out string name, out bool present,
                    out uint code, out IReadOnlyList<string> hardwareIds))
                { incomplete = true; continue; }
                if (!present || code == 0) continue;
                problemDevices++;
                var matches = new Dictionary<string, KnowledgeDriverMatch>(StringComparer.Ordinal);
                var conflicts = new SortedSet<string>(StringComparer.Ordinal);
                foreach (string hardwareId in hardwareIds)
                {
                    var result = catalogs.FindDriverReferences(hardwareId, platform.Architecture,
                        platform.WindowsProduct, platform.Build, 64, token);
                    totalReferences += result.TotalMatches;
                    if (result.Truncated) incomplete = true;
                    foreach (string conflict in result.ConflictingIdentities) conflicts.Add(conflict);
                    foreach (var match in result.Matches)
                        matches.TryAdd(match.PackId + "\n" + match.Entry.Context + "\n" + match.Entry.Code, match);
                }
                if (conflicts.Count > 0)
                {
                    incomplete = true;
                    details.Add(name + " (Device Manager code " + code + "): conflicting reference identities " +
                        string.Join(", ", conflicts.Take(8)) + ".");
                    continue;
                }
                if (matches.Count == 0) continue;
                referencedDevices++;
                var selected = matches.Values.OrderBy(match => match.Entry.Driver!.Vendor, StringComparer.Ordinal)
                    .ThenBy(match => match.Entry.Driver!.DriverVersion, StringComparer.Ordinal)
                    .ThenBy(match => match.PackId, StringComparer.Ordinal).Take(8).ToArray();
                if (matches.Count > selected.Length) incomplete = true;
                details.Add(name + " (Device Manager code " + code + ", " + deviceId + "):");
                foreach (var match in selected)
                {
                    var driver = match.Entry.Driver!;
                    details.Add("  " + driver.Vendor + " " + driver.DriverVersion + " - " + driver.DownloadUrl +
                        " (pack " + match.PackId + " revision " + match.Revision + ")");
                }
            }
            if (problemDevices == 0)
                return new("Device driver references", incomplete ? CheckState.Unknown : CheckState.Information,
                    incomplete ? "Some device records were malformed. No valid present device reported a nonzero Device Manager code, but coverage is incomplete. No driver was downloaded, selected or installed."
                        : "No present device in the supplied evidence reported a nonzero Device Manager code, so no driver reference lookup was needed. This does not prove driver currency or device operation. No driver was downloaded, selected or installed.",
                    Evidence: evidence);
            string summary = referencedDevices == 0
                ? problemDevices + " problem device(s) had no exact declared driver reference for " + platform.Architecture +
                    " Windows " + platform.WindowsProduct + " build " + platform.Build + "."
                : referencedDevices + " of " + problemDevices + " problem device(s) had exact declared driver references for " +
                    platform.Architecture + " Windows " + platform.WindowsProduct + " build " + platform.Build + ".";
            if (details.Count > 0) summary += "\n" + string.Join("\n", details);
            summary += " References are candidates for review only. Exact device operation, package signature, installed-version ordering, vendor support and compatibility remain unverified. No driver was downloaded, selected or installed.";
            if (totalReferences > 512) incomplete = true;
            return new("Device driver references", incomplete ? CheckState.Unknown : CheckState.Attention, summary, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException)
        {
            return Unknown("Device evidence could not be correlated with driver references: " + error.Message);
        }
    }

    private static bool TryDevice(JsonElement device, HashSet<string> seen, out string deviceId,
        out string name, out bool present, out uint code, out IReadOnlyList<string> hardwareIds)
    {
        deviceId = name = string.Empty;
        present = false;
        code = 0;
        hardwareIds = [];
        if (device.ValueKind != JsonValueKind.Object ||
            !device.TryGetProperty("DeviceID", out var idValue) || idValue.ValueKind != JsonValueKind.String ||
            !device.TryGetProperty("Name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
            !device.TryGetProperty("Present", out var presentValue) || presentValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
            !device.TryGetProperty("ConfigManagerErrorCode", out var codeValue) || !codeValue.TryGetUInt32(out code) ||
            !device.TryGetProperty("HardwareID", out var hardwareValue) || hardwareValue.ValueKind != JsonValueKind.Array ||
            hardwareValue.GetArrayLength() is < 1 or > 64)
            return false;
        string? reportedId = idValue.GetString(), reportedName = nameValue.GetString();
        if (!DriverReference.ValidText(reportedId, 1024) || reportedId != reportedId!.Trim() || !seen.Add(reportedId) ||
            !DriverReference.ValidText(reportedName, 512) || reportedName != reportedName!.Trim())
            return false;
        deviceId = reportedId;
        name = reportedName;
        present = presentValue.GetBoolean();
        var ids = new List<string>();
        foreach (var value in hardwareValue.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String || !DriverReference.ValidText(value.GetString(), 512) ||
                value.GetString() != value.GetString()!.Trim()) return false;
            if (!ids.Contains(value.GetString()!, StringComparer.OrdinalIgnoreCase)) ids.Add(value.GetString()!);
        }
        hardwareIds = ids.AsReadOnly();
        return true;
    }
}
