using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public sealed record SystemRestorePoint(uint Sequence, string Description, uint Type, DateTimeOffset CreatedUtc);
public sealed record SystemRestoreInventory(bool Complete, IReadOnlyList<SystemRestorePoint> Points);

public static class SystemRestoreDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$rows=@(Get-CimInstance -Namespace root/default -ClassName SystemRestore -OperationTimeoutSec 15 | Sort-Object CreationTime -Descending | Select-Object -First 257); " +
        "$points=@($rows | Select-Object -First 256 | ForEach-Object { $raw=$_.CreationTime; " +
        "$created=if($raw -is [datetime]){$raw.ToUniversalTime().ToString('o')}else{[System.Management.ManagementDateTimeConverter]::ToDateTime([string]$raw).ToUniversalTime().ToString('o')}; " +
        "[pscustomobject]@{sequence=[uint32]$_.SequenceNumber;description=[string]$_.Description;type=[uint32]$_.RestorePointType;created=$created} }); " +
        "[pscustomobject]@{complete=($rows.Count -le 256);points=$points} | ConvertTo-Json -Depth 3 -Compress";

    public static RepairStep Step() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("System Restore inspection requires Windows.");
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown(string detail = "System Restore point inventory was unavailable, incomplete or malformed. No restore point or protection change was requested.") =>
            new("System Restore points", CheckState.Unknown, detail, Evidence: result);
        var inventory = Read(result, nowUtc);
        if (inventory is null) return Unknown();
        if (!inventory.Complete) return Unknown("More than 256 System Restore points were reported. The bounded inventory is incomplete, so no recovery-readiness conclusion was made and nothing was changed.");
        try
        {
            var ordered = inventory.Points;
            string detail = ordered.Count == 0
                ? "Windows reported no available System Restore points. This can be intentional and does not establish whether protection is enabled, disabled or managed."
                : $"Windows reported {ordered.Count} available System Restore point{(ordered.Count == 1 ? "" : "s")}. Newest: {ordered[0].CreatedUtc:O}.";
            if (ordered.Count > 0)
            {
                detail += "\n" + string.Join("\n", ordered.Take(12).Select(point =>
                    $"{point.CreatedUtc:O} | RP{point.Sequence} | Type {point.Type} | {(string.IsNullOrWhiteSpace(point.Description) ? "No description" : point.Description)}"));
                if (ordered.Count > 12) detail += $"\n{ordered.Count - 12} additional restore points remain in raw evidence.";
            }
            detail += "\nInventory does not verify that a restore will succeed, that all volumes are protected or that a point is compatible with current security policy. No restore point was created, deleted or applied.";
            return new("System Restore points", CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException) { return Unknown(); }
    }

    public static SystemRestoreInventory? Read(ToolResult result, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return null;
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !Unique(root, "complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !Unique(root, "points", out var points) || points.ValueKind != JsonValueKind.Array || points.GetArrayLength() > 256)
                return null;
            var retained = new List<SystemRestorePoint>();
            var sequences = new HashSet<uint>();
            var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
            foreach (var point in points.EnumerateArray())
            {
                if (point.ValueKind != JsonValueKind.Object || point.EnumerateObject().Count() != 4 ||
                    !Unique(point, "sequence", out var sequence) || !sequence.TryGetUInt32(out uint number) || !sequences.Add(number) ||
                    !Unique(point, "description", out var description) || description.ValueKind != JsonValueKind.String ||
                    !Unique(point, "type", out var type) || !type.TryGetUInt32(out uint pointType) || pointType > 255 ||
                    !Unique(point, "created", out var created) || created.ValueKind != JsonValueKind.String ||
                    !DateTimeOffset.TryParseExact(created.GetString(), "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp) ||
                    timestamp.Offset != TimeSpan.Zero || timestamp > now.AddMinutes(5)) return null;
                string label = description.GetString() ?? "";
                if (label.Length > 1024 || label.Any(char.IsControl)) return null;
                retained.Add(new(number, label, pointType, timestamp));
            }
            return new(complete.GetBoolean(), [.. retained.OrderByDescending(point => point.CreatedUtc).ThenByDescending(point => point.Sequence)]);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException) { return null; }
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }
}
