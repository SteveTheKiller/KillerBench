using System.Text.Json;

namespace KillerBench.Engine;

public static class StorageTopologyDiagnostics
{
    private const int MaximumDisks = 64;
    private const int MaximumPartitions = 256;
    private const int MaximumVolumes = 128;
    private static readonly HashSet<string> DiskStyles = new(["RAW", "MBR", "GPT", "Unknown"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> HealthStates = new(["Healthy", "Warning", "Unhealthy", "Unknown"], StringComparer.OrdinalIgnoreCase);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$disks=@(Get-Disk -ErrorAction Stop|Sort-Object Number|Select-Object -First 65|ForEach-Object{[pscustomobject]@{" +
            "number=[int]$_.Number;name=[string]$_.FriendlyName;style=[string]$_.PartitionStyle;health=[string]$_.HealthStatus;" +
            "operational=@($_.OperationalStatus|ForEach-Object{[string]$_});boot=[bool]$_.IsBoot;system=[bool]$_.IsSystem;readOnly=[bool]$_.IsReadOnly;offline=[bool]$_.IsOffline;size=[uint64]$_.Size}});" +
            "$partitions=@(Get-Partition -ErrorAction Stop|Sort-Object DiskNumber,PartitionNumber|Select-Object -First 257|ForEach-Object{[pscustomobject]@{" +
            "disk=[int]$_.DiskNumber;number=[int]$_.PartitionNumber;letter=$(if([int][char]$_.DriveLetter-eq 0){''}else{[string]$_.DriveLetter});type=[string]$_.Type;size=[uint64]$_.Size;boot=[bool]$_.IsBoot;system=[bool]$_.IsSystem;hidden=[bool]$_.IsHidden;readOnly=[bool]$_.IsReadOnly}});" +
            "$volumes=@(Get-Volume -ErrorAction Stop|Sort-Object DriveLetter|Select-Object -First 129|ForEach-Object{[pscustomobject]@{" +
            "letter=[string]$_.DriveLetter;fileSystem=[string]$_.FileSystem;health=[string]$_.HealthStatus;operational=@($_.OperationalStatus|ForEach-Object{[string]$_});" +
            "size=$(if($null-eq$_.Size){$null}else{[uint64]$_.Size});remaining=$(if($null-eq$_.SizeRemaining){$null}else{[uint64]$_.SizeRemaining})}});" +
            "[pscustomobject]@{complete=($disks.Count-le 64-and$partitions.Count-le 256-and$volumes.Count-le 128);disks=$disks;partitions=$partitions;volumes=$volumes}|ConvertTo-Json -Depth 6 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "Storage topology";
        const string scope = " Disk numbers and drive letters are point-in-time identities. No disk, partition, volume, mount point, filesystem, data or storage setting changed.";
        WorkflowCheck Unknown(string detail = "Storage topology evidence is unavailable or incomplete.") =>
            new(title, CheckState.Unknown, detail + scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (!Unique(root) || !Boolean(root, "complete", out bool complete) ||
                !Array(root, "disks", out var disks) || !Array(root, "partitions", out var partitions) || !Array(root, "volumes", out var volumes)) return Unknown();
            if (!complete || disks.GetArrayLength() > MaximumDisks || partitions.GetArrayLength() > MaximumPartitions || volumes.GetArrayLength() > MaximumVolumes)
                return Unknown("Storage topology exceeded its evidence bound.");

            var diskNumbers = new HashSet<int>();
            var findings = new List<string>();
            foreach (var disk in disks.EnumerateArray())
            {
                if (!Unique(disk) || !Integer(disk, "number", 0, int.MaxValue, out int number) || !diskNumbers.Add(number) ||
                    Text(disk, "name", 256, allowEmpty: true) is null || Text(disk, "style", 32) is not { } style || !DiskStyles.Contains(style) ||
                    Text(disk, "health", 32) is not { } health || !HealthStates.Contains(health) || !TextArray(disk, "operational", 8, 64) ||
                    !Boolean(disk, "boot", out _) || !Boolean(disk, "system", out _) || !Boolean(disk, "readOnly", out bool readOnly) ||
                    !Boolean(disk, "offline", out bool offline) || !Unsigned(disk, "size", out _)) return Unknown();
                if (health is "Warning" or "Unhealthy") findings.Add("Disk " + number + " health: " + health);
                if (style.Equals("RAW", StringComparison.OrdinalIgnoreCase)) findings.Add("Disk " + number + " has RAW partition style");
                if (offline) findings.Add("Disk " + number + " is offline");
                if (readOnly) findings.Add("Disk " + number + " is read-only");
            }

            var partitionKeys = new HashSet<string>(StringComparer.Ordinal);
            var letters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var partition in partitions.EnumerateArray())
            {
                if (!Unique(partition) || !Integer(partition, "disk", 0, int.MaxValue, out int disk) || !diskNumbers.Contains(disk) ||
                    !Integer(partition, "number", 1, int.MaxValue, out int number) || !partitionKeys.Add(disk + ":" + number) ||
                    Text(partition, "letter", 1, allowEmpty: true) is not { } letter || (letter.Length == 1 && !char.IsAsciiLetter(letter[0])) ||
                    Text(partition, "type", 256, allowEmpty: true) is null || !Unsigned(partition, "size", out _) ||
                    !Boolean(partition, "boot", out _) || !Boolean(partition, "system", out _) || !Boolean(partition, "hidden", out _) || !Boolean(partition, "readOnly", out _)) return Unknown();
                if (letter.Length == 1 && !letters.TryAdd(letter, disk)) return Unknown();
            }

            var volumeLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var volume in volumes.EnumerateArray())
            {
                if (!Unique(volume) || Text(volume, "letter", 1, allowEmpty: true) is not { } letter ||
                    (letter.Length == 1 && (!char.IsAsciiLetter(letter[0]) || !volumeLetters.Add(letter))) ||
                    Text(volume, "fileSystem", 64, allowEmpty: true) is null || Text(volume, "health", 32) is not { } health || !HealthStates.Contains(health) ||
                    !TextArray(volume, "operational", 8, 64) || !NullableUnsigned(volume, "size", out ulong? size) ||
                    !NullableUnsigned(volume, "remaining", out ulong? remaining) || (size.HasValue && remaining.HasValue && remaining > size)) return Unknown();
                if (health is "Warning" or "Unhealthy") findings.Add("Volume " + (letter.Length == 0 ? "without drive letter" : letter + ":") + " health: " + health);
            }
            int mapped = volumeLetters.Count(letter => letters.ContainsKey(letter));
            string detail = $"Reviewed {disks.GetArrayLength()} disk{(disks.GetArrayLength() == 1 ? "" : "s")}, {partitions.GetArrayLength()} partition{(partitions.GetArrayLength() == 1 ? "" : "s")} and {volumes.GetArrayLength()} volume{(volumes.GetArrayLength() == 1 ? "" : "s")}. {mapped} drive-letter volume{(mapped == 1 ? "" : "s")} mapped to a reported disk and partition.";
            if (findings.Count > 0) detail += " Review observations: " + string.Join("; ", findings.Take(20)) + (findings.Count > 20 ? "; additional observations remain in evidence." : ".");
            detail += " Topology does not prove physical health, filesystem integrity, mount accessibility, data integrity or backup recoverability." + scope;
            return new(title, findings.Count > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }

    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
    private static bool Array(JsonElement owner, string name, out JsonElement value) => owner.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Array;
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
        return owner.TryGetProperty(name, out var item) && item.TryGetInt32(out value) && value >= minimum && value <= maximum;
    }
    private static bool Unsigned(JsonElement owner, string name, out ulong value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.TryGetUInt64(out value);
    }
    private static bool NullableUnsigned(JsonElement owner, string name, out ulong? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (!item.TryGetUInt64(out ulong parsed)) return false;
        value = parsed;
        return true;
    }
    private static string? Text(JsonElement owner, string name, int maximum, bool allowEmpty = false) => owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
        item.GetString() is { } text && text == text.Trim() && ((allowEmpty && text.Length == 0) || DriverReference.ValidText(text, maximum)) ? text : null;
    private static bool TextArray(JsonElement owner, string name, int maximumItems, int maximumText)
    {
        if (!owner.TryGetProperty(name, out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > maximumItems) return false;
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || text != text.Trim() ||
                !DriverReference.ValidText(text, maximumText) || !values.Add(text)) return false;
        }
        return true;
    }
}
