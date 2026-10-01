using System.Text.Json;
using System.Text.RegularExpressions;

namespace KillerBench.Engine.SystemHealth;

public static partial class WindowsRecoveryPartitionCorrelation
{
    private const int MaximumPartitions = 256;

    [GeneratedRegex(@"\\harddisk(?<disk>\d+)\\partition(?<partition>\d+)(?:\\|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LocationPattern();

    public static async Task<WorkflowCheck> CheckAsync(WorkflowCheck recovery, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(recovery);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Recovery partition correlation requires Windows.");
        if (!TryStatus(recovery, out var status) || status.State != WindowsRecoveryState.Enabled || !TryTarget(status.Location, out _, out _))
            return Interpret(recovery, null, token);

        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$rows=@(Get-Partition -ErrorAction Stop|Sort-Object DiskNumber,PartitionNumber|Select-Object -First 257|ForEach-Object{[pscustomobject]@{" +
            "disk=[int]$_.DiskNumber;number=[int]$_.PartitionNumber;type=[string]$_.Type;size=[uint64]$_.Size;hidden=[bool]$_.IsHidden;readOnly=[bool]$_.IsReadOnly}});" +
            "[pscustomobject]@{complete=($rows.Count-le 256);partitions=@($rows|Select-Object -First 256)}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(recovery, evidence, token);
    }

    public static WorkflowCheck Interpret(WorkflowCheck recovery, ToolResult? partitionEvidence,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(recovery);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Recovery partition correlation";
        const string boundary = " Partition presence does not verify recovery-image files, image integrity, BCD correctness, encryption access, bootability or repair success. No partition, volume, filesystem, recovery image, boot setting or Windows Recovery setting changed.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary,
            Evidence: partitionEvidence ?? recovery.Evidence);

        if (!TryStatus(recovery, out var status))
            return Unknown("Windows Recovery status evidence was missing, contradictory or malformed.");
        if (status.State == WindowsRecoveryState.Disabled)
            return new(name, CheckState.Information,
                "Windows Recovery is disabled, so no configured recovery partition was asserted as ready." + boundary,
                Evidence: recovery.Evidence);
        if (!TryTarget(status.Location, out int targetDisk, out int targetPartition))
            return Unknown("Enabled Windows Recovery did not provide a recognized local disk and partition location.");
        if (partitionEvidence is null || partitionEvidence.ExitCode != 0 || partitionEvidence.TimedOut ||
            partitionEvidence.Truncated || !string.IsNullOrWhiteSpace(partitionEvidence.Error))
            return Unknown("The current partition inventory was unavailable or incomplete.");

        try
        {
            using var document = JsonDocument.Parse(partitionEvidence.Output);
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) ||
                !root.TryGetProperty("partitions", out var partitions) || partitions.ValueKind != JsonValueKind.Array ||
                !complete || partitions.GetArrayLength() > MaximumPartitions)
                return Unknown("The current partition inventory had an unrecognized or incomplete shape.");

            var identities = new HashSet<string>(StringComparer.Ordinal);
            JsonElement? match = null;
            foreach (var partition in partitions.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!Unique(partition, 6) || !Integer(partition, "disk", 0, int.MaxValue, out int disk) ||
                    !Integer(partition, "number", 1, int.MaxValue, out int number) ||
                    !Text(partition, "type", 128, out _) || !Unsigned(partition, "size", out _) ||
                    !Boolean(partition, "hidden", out _) || !Boolean(partition, "readOnly", out _) ||
                    !identities.Add(disk + ":" + number))
                    return Unknown("The current partition inventory contained malformed or duplicate identities.");
                if (disk == targetDisk && number == targetPartition) match = partition;
            }

            if (match is null)
                return new(name, CheckState.Attention,
                    $"Windows Recovery reports disk {targetDisk}, partition {targetPartition}, but that exact partition was not present in the complete current inventory. Review the configured location before attempting recovery or enablement." + boundary,
                    Evidence: partitionEvidence);
            var selected = match.Value;
            _ = Text(selected, "type", 128, out string type);
            _ = Unsigned(selected, "size", out ulong size);
            _ = Boolean(selected, "hidden", out bool hidden);
            _ = Boolean(selected, "readOnly", out bool readOnly);
            return new(name, CheckState.Information,
                $"Windows Recovery reports disk {targetDisk}, partition {targetPartition}, and that exact partition is present in the current inventory. Reported type: {type}; size: {size} bytes; hidden: {hidden}; read-only: {readOnly}." + boundary,
                Evidence: partitionEvidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        { return Unknown("The current partition inventory could not be interpreted."); }
    }

    private static bool TryStatus(WorkflowCheck recovery, out WindowsRecoveryStatus status)
    {
        status = new(WindowsRecoveryState.Unknown, null, null);
        if (recovery.Name != "Windows Recovery Environment" || recovery.Evidence is null) return false;
        string first = recovery.Evidence.Output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', 2)[0];
        try { status = JsonSerializer.Deserialize<WindowsRecoveryStatus>(first) ?? status; }
        catch (JsonException) { return false; }
        return status.State switch
        {
            WindowsRecoveryState.Enabled => recovery.EffectiveState == CheckState.Clear && recovery.Repair is null,
            WindowsRecoveryState.Disabled => recovery.EffectiveState == CheckState.Repairable && recovery.Repair == RepairKind.WindowsRecoveryEnvironment,
            _ => false
        };
    }

    private static bool TryTarget(string? location, out int disk, out int partition)
    {
        disk = partition = -1;
        if (string.IsNullOrWhiteSpace(location) || location.Length > 4096 || location.Any(char.IsControl)) return false;
        var match = LocationPattern().Match(location);
        return match.Success && int.TryParse(match.Groups["disk"].Value, out disk) && disk >= 0 &&
            int.TryParse(match.Groups["partition"].Value, out partition) && partition >= 1;
    }

    private static bool Unique(JsonElement value, int count) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == count && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == count;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt32(out value) && value >= minimum && value <= maximum;
    }
    private static bool Unsigned(JsonElement owner, string name, out ulong value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt64(out value);
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String) return false;
        value = item.GetString() ?? "";
        return value.Length <= maximum && !value.Any(char.IsControl);
    }
}
