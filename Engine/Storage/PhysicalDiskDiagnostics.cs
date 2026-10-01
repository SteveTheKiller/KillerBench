using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine.Storage;

public static class PhysicalDiskDiagnostics
{
    private const string Boundary = "Serial numbers, firmware data and unique hardware identifiers are excluded. Provider state is not a surface test, SMART result, backup check or proof of physical health. No disk, pool, volume, file, driver or storage setting changed.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$items=@(Get-PhysicalDisk -ErrorAction Stop|Select-Object -First 65 " +
            "@{n='deviceId';e={[string]$_.DeviceId}},@{n='name';e={[string]$_.FriendlyName}}," +
            "@{n='health';e={[string]$_.HealthStatus}},@{n='operational';e={[string]::Join(',',@($_.OperationalStatus))}}," +
            "@{n='media';e={[string]$_.MediaType}},@{n='bus';e={[string]$_.BusType}}," +
            "@{n='size';e={[uint64]$_.Size}},@{n='usage';e={[string]$_.Usage}},@{n='readOnly';e={[bool]$_.IsReadOnly}});" +
            "[pscustomobject]@{physicalDisks=$items}|ConvertTo-Json -Depth 4 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(25), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        WorkflowCheck Unknown(string detail) => new("Physical disk inventory", CheckState.Unknown,
            detail + " " + Boundary, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Windows did not return a complete physical-disk inventory. Review the retained provider error.")];
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("physicalDisks", out var disks) || disks.ValueKind != JsonValueKind.Array ||
                disks.GetArrayLength() > 65)
                return [Unknown("Unrecognized physical-disk inventory envelope.")];
            if (disks.GetArrayLength() == 0)
                return [Unknown("Windows returned no physical disks. Storage provider coverage is incomplete.")];

            var checks = new List<WorkflowCheck>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var disk in disks.EnumerateArray().Take(64))
            {
                if (!TryRead(disk, out var value) || !identities.Add(value.DeviceId))
                    return [Unknown("A physical-disk record was malformed or duplicated.")];
                var state = Assess(value.Health, value.Operational);
                string size = (value.Size / 1073741824d).ToString("F1", CultureInfo.InvariantCulture);
                string condition = state switch
                {
                    CheckState.Attention => "Windows reports a warning, unhealthy or fault-class current state. Confirm backups and vendor diagnostics before repair writes.",
                    CheckState.Unknown => "Windows did not provide a complete recognized current state for this disk.",
                    _ => "Windows reports Healthy with an OK operational state."
                };
                checks.Add(new("Physical disk: " + value.DeviceId, state,
                    $"{value.Name}; {size} GiB; media {value.Media}; bus {value.Bus}; usage {value.Usage}; read-only {value.ReadOnly.ToString().ToLowerInvariant()}. " +
                    $"Health: {value.Health}; operational: {value.Operational}. {condition} {Boundary}", Evidence: result));
            }
            if (disks.GetArrayLength() > 64)
                checks.Add(Unknown("Only the first 64 physical disks were evaluated; additional disks remain unassessed."));
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return [Unknown("Physical-disk inventory evidence was malformed.")];
        }
    }

    private static CheckState Assess(string health, string operational)
    {
        if (health.Equals("Warning", StringComparison.OrdinalIgnoreCase) ||
            health.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase))
            return CheckState.Attention;
        if (!health.Equals("Healthy", StringComparison.OrdinalIgnoreCase))
            return CheckState.Unknown;
        var states = operational.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (states.Length == 0) return CheckState.Unknown;
        if (states.Any(value => value.Equals("Degraded", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Stressed", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Predictive Failure", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Lost Communication", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("No Contact", StringComparison.OrdinalIgnoreCase)))
            return CheckState.Attention;
        return states.All(value => value.Equals("OK", StringComparison.OrdinalIgnoreCase))
            ? CheckState.Information : CheckState.Unknown;
    }

    private static bool TryRead(JsonElement disk, out Disk value)
    {
        value = default!;
        if (disk.ValueKind != JsonValueKind.Object || disk.EnumerateObject().Count() != 9 ||
            !Text(disk, "deviceId", 128, out string id) || !Text(disk, "name", 256, out string name) ||
            !Text(disk, "health", 64, out string health) || !Text(disk, "operational", 256, out string operational) ||
            !Text(disk, "media", 64, out string media) || !Text(disk, "bus", 64, out string bus) ||
            !Text(disk, "usage", 64, out string usage) ||
            !disk.TryGetProperty("size", out var size) || size.ValueKind != JsonValueKind.Number ||
            !size.TryGetUInt64(out ulong bytes) || bytes == 0 ||
            !disk.TryGetProperty("readOnly", out var readOnly) ||
            readOnly.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return false;
        value = new(id, name, health, operational, media, bus, bytes, usage, readOnly.GetBoolean());
        return true;
    }

    private static bool Text(JsonElement item, string name, int maximum, out string value)
    {
        value = "";
        if (!item.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String ||
            !DriverReference.ValidText(property.GetString(), maximum))
            return false;
        value = property.GetString()!.Trim();
        return true;
    }

    private sealed record Disk(string DeviceId, string Name, string Health, string Operational,
        string Media, string Bus, ulong Size, string Usage, bool ReadOnly);
}
