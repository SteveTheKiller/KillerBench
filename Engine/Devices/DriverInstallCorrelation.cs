using System.Text.Json;

namespace KillerBench.Engine.Devices;

public static class DriverInstallCorrelation
{
    public static IReadOnlyList<WorkflowCheck> Interpret(IReadOnlyList<DriverInstallSection> failures, ToolResult evidence,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(failures);
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        WorkflowCheck Unknown(string detail) => new("Current device correlation", CheckState.Unknown,
            detail + " Historical SetupAPI findings remain available, but current device state is unverified. No device or driver changed.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("Current Plug and Play inventory could not be read completely.")];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (!Unique(root, 2) || !root.TryGetProperty("complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("devices", out var devices) || devices.ValueKind != JsonValueKind.Array || devices.GetArrayLength() > 1024)
                return [Unknown("Current Plug and Play inventory had an unrecognized shape.")];
            bool complete = completeValue.GetBoolean();
            var current = new Dictionary<string, (string Name, bool Present, uint Code)>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in devices.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!Unique(device, 4) || !Text(device, "DeviceID", 1024, false, out string id) ||
                    !Text(device, "Name", 512, true, out string name) ||
                    !device.TryGetProperty("Present", out var present) || present.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !device.TryGetProperty("ConfigManagerErrorCode", out var code) || code.ValueKind != JsonValueKind.Number || !code.TryGetUInt32(out uint errorCode) ||
                    !current.TryAdd(id, (name, present.GetBoolean(), errorCode)))
                    return [Unknown("Current Plug and Play inventory contained malformed or duplicate device identities.")];
            }
            var results = new List<WorkflowCheck>();
            foreach (var failure in failures.Take(32))
            {
                token.ThrowIfCancellationRequested();
                if (failure is null || !DriverReference.ValidText(failure.Identity, 512))
                    return [Unknown("A retained SetupAPI target identity was invalid.")];
                if (!current.TryGetValue(failure.Identity, out var device))
                {
                    results.Add(new("Historical driver target not currently reported: " + failure.Identity,
                        complete ? CheckState.Information : CheckState.Unknown,
                        "No exact current Plug and Play identity matched the retained failed installation section. The device may be absent, replaced, hidden from this inventory or outside incomplete coverage. No driver action selected.", Evidence: evidence));
                    continue;
                }
                string name = string.IsNullOrWhiteSpace(device.Name) ? failure.Identity : device.Name;
                results.Add(new("Current state after driver failure: " + name,
                    device.Present && device.Code != 0 ? CheckState.Attention : CheckState.Information,
                    $"Exact device: {failure.Identity}. Present: {device.Present}. Device Manager code: {device.Code}. " +
                    (device.Present && device.Code != 0
                        ? "The same device still reports a configuration problem. Use its current code, hardware identity and approved vendor package for the next diagnostic step. "
                        : "The historical installation failure is not accompanied by a current nonzero configuration code for this exact identity. This does not verify feature operation. ") +
                    "No driver was installed, removed, rolled back or updated.", Evidence: evidence));
            }
            if (!complete) results.Add(Unknown("Only the first 1,024 current devices were evaluated; unmatched historical identities remain uncertain."));
            return results.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        { return [Unknown("Current Plug and Play evidence could not be interpreted.")]; }
    }

    private static bool Text(JsonElement row, string name, int maximum, bool allowEmpty, out string value)
    {
        value = "";
        if (!row.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) return false;
        value = item.ValueKind == JsonValueKind.Null ? "" : item.GetString() ?? "";
        return value.Length <= maximum && (allowEmpty || value.Length > 0) && !value.Any(char.IsControl);
    }

    private static bool Unique(JsonElement value, int count) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == count && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == count;
}
