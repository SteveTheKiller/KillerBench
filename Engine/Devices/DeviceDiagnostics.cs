using System.Text.Json;

namespace KillerBench.Engine;

public enum DeviceCategory { Audio, Network, Storage, Printing, Display, Input, Portable, Bluetooth, Camera, SmartCard, Biometric, Problems }

public static class DeviceDiagnostics
{
    public static Task<IReadOnlyList<WorkflowCheck>> CheckAsync(DeviceCategory category, CancellationToken token = default) =>
        CheckAsync(category, null, token);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(DeviceCategory category,
        KnowledgeCatalogCollection? knowledgeCatalogs, CancellationToken token = default)
    {
        string filter = category switch { DeviceCategory.Audio => "PNPClass='AudioEndpoint' OR PNPClass='MEDIA'", DeviceCategory.Network => "PNPClass='Net'", DeviceCategory.Storage => "PNPClass='DiskDrive' OR PNPClass='HDC' OR PNPClass='SCSIAdapter'", DeviceCategory.Printing => "PNPClass='Printer' OR PNPClass='PrintQueue'", DeviceCategory.Display => "PNPClass='Display' OR PNPClass='Monitor'", DeviceCategory.Input => "PNPClass='Keyboard' OR PNPClass='Mouse' OR PNPClass='HIDClass'", DeviceCategory.Portable => "PNPClass='WPD'", DeviceCategory.Bluetooth => "PNPClass='Bluetooth'", DeviceCategory.Camera => "PNPClass='Camera' OR PNPClass='Image'", DeviceCategory.SmartCard => "PNPClass='SmartCardReader'", DeviceCategory.Biometric => "PNPClass='Biometric'", DeviceCategory.Problems => "ConfigManagerErrorCode <> 0", _ => throw new ArgumentOutOfRangeException(nameof(category)) };
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$devices=@(Get-CimInstance -ClassName Win32_PnPEntity -Filter \"" + filter + "\" -OperationTimeoutSec 15 | " +
            "Select-Object -First 257 DeviceID,Name,PNPClass,Present,ConfigManagerErrorCode,HardwareID); " +
            "$drivers=$null; $driverError=$null; try { $drivers=@(Get-CimInstance -ClassName Win32_PnPSignedDriver -OperationTimeoutSec 15 | Where-Object { $_.DeviceID -in $devices.DeviceID } | Select-Object DeviceID,DriverVersion,DriverProviderName) } catch { $driverError=$_.Exception.Message } " +
            "[pscustomobject]@{devices=$devices; drivers=$drivers; driverError=$driverError} | ConvertTo-Json -Depth 3 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        var checks = Interpret(evidence, category).ToList();
        if (knowledgeCatalogs is { Count: > 0 })
            checks.Add(DeviceDriverReferences.Check(evidence, knowledgeCatalogs,
                DriverPlatformContext.FromCurrentWindows(), token));
        return checks.AsReadOnly();
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result, DeviceCategory category)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        string label = category.ToString();
        WorkflowCheck Unknown() => new(label + " devices", CheckState.Unknown, "Device data is unavailable or incomplete. No device settings changed.", Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return [Unknown()];
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var devices = json.RootElement.GetProperty("devices");
            if (devices.ValueKind != JsonValueKind.Array) return [Unknown()];
            if (devices.GetArrayLength() == 0 && category == DeviceCategory.Problems)
                return [new("Device problems", CheckState.Information, "No nonzero configuration error codes were returned by the Plug and Play provider. Undetected devices, missing provider data and physical operation are not verified.", Evidence: result)];
            if (devices.GetArrayLength() == 0 && category == DeviceCategory.Printing)
                return [new("Printing devices", CheckState.Information, "No Printer or PrintQueue Plug and Play devices were reported. This does not establish whether remote or other print queues exist. Queue checks remain separate.", Evidence: result)];
            if (devices.GetArrayLength() == 0 && category == DeviceCategory.Portable)
                return [new("Portable devices", CheckState.Information, "No WPD Plug and Play devices were reported. This is normal when no portable device is connected and does not verify device discovery, file access or synchronization.", Evidence: result)];
            if (devices.GetArrayLength() == 0) return [new(label + " devices", CheckState.Attention, "No devices were reported in this category. Check device connection, drivers and remote-session settings.", Evidence: result)];
            var checks = new List<WorkflowCheck>();
            if (devices.GetArrayLength() > 256)
                checks.Add(new(label + " device coverage", CheckState.Unknown,
                    "Only the first 256 reported devices were evaluated. Additional devices remain unassessed; this is partial coverage.", Evidence: result));
            bool driverCollectionFailed = json.RootElement.TryGetProperty("driverError", out var driverError) && driverError.ValueKind != JsonValueKind.Null;
            if (driverCollectionFailed)
            {
                string reason = driverError.ValueKind == JsonValueKind.String ? driverError.GetString() ?? "" : "Invalid provider error data.";
                reason = new string(reason.Take(512).Select(c => char.IsControl(c) ? ' ' : c).ToArray());
                checks.Add(new(label + " driver collection", CheckState.Unknown,
                    "Driver inventory collection failed. " + reason + " Device configuration observations remain separate; no driver change is authorized.", Evidence: result));
            }
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in devices.EnumerateArray().Take(256))
            {
                try
                {
                string? id = device.GetProperty("DeviceID").GetString();
                string? name = device.TryGetProperty("Name", out var reportedName) ? reportedName.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) name = "Unnamed device";
                string? deviceClass = device.TryGetProperty("PNPClass", out var reportedClass) ? reportedClass.GetString() : null;
                if (id is null || !DriverReference.ValidText(id, 1024) || !DriverReference.ValidText(name, 512) ||
                    !seen.Add(id) || (category != DeviceCategory.Problems && !(category switch
                    {
                        DeviceCategory.Audio => new[] { "AudioEndpoint", "MEDIA" },
                        DeviceCategory.Network => new[] { "Net" },
                        DeviceCategory.Printing => new[] { "Printer", "PrintQueue" },
                        DeviceCategory.Display => new[] { "Display", "Monitor" },
                        DeviceCategory.Input => new[] { "Keyboard", "Mouse", "HIDClass" },
                        DeviceCategory.Portable => new[] { "WPD" },
                        DeviceCategory.Bluetooth => new[] { "Bluetooth" },
                        DeviceCategory.Camera => new[] { "Camera", "Image" },
                        DeviceCategory.SmartCard => new[] { "SmartCardReader" },
                        DeviceCategory.Biometric => new[] { "Biometric" },
                        _ => new[] { "DiskDrive", "HDC", "SCSIAdapter" }
                    }).Contains(deviceClass, StringComparer.OrdinalIgnoreCase)))
                { checks.Add(Unknown()); continue; }
                bool present = device.GetProperty("Present").GetBoolean();
                uint code = device.GetProperty("ConfigManagerErrorCode").GetUInt32();
                if (category == DeviceCategory.Problems && code == 0) { checks.Add(Unknown()); continue; }
                var problem = DeviceProblemCodeCatalog.Find(code);
                string detail = !present ? "Device is not currently present. Check the intended physical or remote-session connection." :
                    problem.Summary + " " + problem.NextAction;
                checks.Add(new(label + " device: " + name, !present ? CheckState.Information : code == 0 ? CheckState.Clear : CheckState.Attention,
                    $"Device Manager code {code} ({problem.Symbol}). {detail} Device: {id}", Evidence: result));
                if (device.TryGetProperty("HardwareID", out var hardware))
                {
                    string hardwareDetail = "Hardware IDs unavailable or incomplete. No driver match inferred.";
                    if (hardware.ValueKind == JsonValueKind.Array && hardware.GetArrayLength() is > 0 and <= 64)
                    {
                        var ids = hardware.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : null).ToArray();
                        if (ids.All(value => DriverReference.ValidText(value, 512) && value == value!.Trim()))
                            hardwareDetail = "Hardware IDs: " + string.Join("; ", ids.Distinct(StringComparer.OrdinalIgnoreCase)) +
                                ". Inventory only; driver package compatibility has not been verified.";
                    }
                    checks.Add(new(label + " hardware: " + name, CheckState.Information, hardwareDetail, Evidence: result));
                }
                if (json.RootElement.TryGetProperty("drivers", out var drivers))
                {
                    string driverDetail = "Driver inventory unavailable or ambiguous for this exact device ID.";
                    bool driverMatched = false;
                    if (!driverCollectionFailed && drivers.ValueKind == JsonValueKind.Array && drivers.GetArrayLength() <= 512)
                    {
                        var matches = drivers.EnumerateArray().Where(driver => driver.ValueKind == JsonValueKind.Object &&
                            driver.TryGetProperty("DeviceID", out var driverId) && driverId.ValueKind == JsonValueKind.String &&
                            string.Equals(driverId.GetString(), id, StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (matches.Length == 1 && matches[0].TryGetProperty("DriverVersion", out var version) &&
                            version.ValueKind == JsonValueKind.String && DriverReference.ValidText(version.GetString(), 128) &&
                            matches[0].TryGetProperty("DriverProviderName", out var provider) && provider.ValueKind == JsonValueKind.String &&
                            DriverReference.ValidText(provider.GetString(), 256))
                        {
                            driverMatched = true;
                            driverDetail = $"Version: {version.GetString()}; provider: {provider.GetString()}. Device: {id}. Inventory only; package trust and update availability were not evaluated.";
                        }
                    }
                    checks.Add(new(label + " driver: " + name, driverMatched ? CheckState.Information : CheckState.Unknown, driverDetail, Evidence: result));
                }
                }
                catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException)
                {
                    checks.Add(Unknown());
                }
            }
            return checks;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return [Unknown()]; }
    }
}
