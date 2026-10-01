using System.Text.Json;

namespace KillerBench.Engine.Storage;

public static class StorageVolumeCapacityCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck topology, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(topology);
        token.ThrowIfCancellationRequested();
        const string name = "Drive-letter volume capacity";
        const string scope = " The 10% threshold is a review aid, not a cleanup recommendation or a filesystem, media or application health verdict. Unlettered volumes are excluded from pressure classification. No file, volume, mount point or storage setting changed.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + scope, Evidence: topology.Evidence);
        if (topology.EffectiveState == CheckState.Unknown || topology.Evidence is null)
            return Unknown("Complete storage topology evidence is required before volume capacity can be evaluated.");
        try
        {
            using var document = JsonDocument.Parse(topology.Evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("volumes", out var volumes) || volumes.ValueKind != JsonValueKind.Array || volumes.GetArrayLength() > 128)
                return Unknown("Storage topology capacity coverage is incomplete.");
            var observed = new List<(string Letter, ulong Size, ulong Remaining, double Percent)>();
            var letters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int unlettered = 0;
            int unavailable = 0;
            foreach (var volume in volumes.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!volume.TryGetProperty("letter", out var letterValue) || letterValue.ValueKind != JsonValueKind.String || letterValue.GetString() is not { } letter ||
                    letter.Length > 1 || letter.Length == 1 && !char.IsAsciiLetter(letter[0]))
                    return Unknown("A volume identity is malformed.");
                if (letter.Length == 0) { unlettered++; continue; }
                letter = letter.ToUpperInvariant();
                if (!letters.Add(letter)) return Unknown("A drive-letter identity is duplicated.");
                if (!NullableUnsigned(volume, "size", out ulong? size) || !NullableUnsigned(volume, "remaining", out ulong? remaining) ||
                    size is null or 0 || remaining is null || remaining > size)
                {
                    unavailable++;
                    continue;
                }
                observed.Add((letter, size.Value, remaining!.Value, 100.0 * remaining.Value / size.Value));
            }
            var ordered = observed.OrderBy(item => item.Letter, StringComparer.OrdinalIgnoreCase).ToArray();
            var low = ordered.Where(item => item.Percent < 10).ToArray();
            string detail = ordered.Length == 0
                ? "No drive-letter volume had complete capacity evidence."
                : "Drive-letter capacity: " + string.Join("; ", ordered.Select(item =>
                    $"{item.Letter}: {item.Remaining / 1073741824.0:F1} GiB of {item.Size / 1073741824.0:F1} GiB available ({item.Percent:F1}%)")) + ".";
            if (low.Length > 0) detail += " Below 10%: " + string.Join(", ", low.Select(item => item.Letter + ":")) + ". Review usage and application requirements before selecting any cleanup.";
            if (unavailable > 0) detail += $" {unavailable} drive-letter volume{(unavailable == 1 ? " has" : "s have")} unavailable or inconsistent capacity evidence.";
            if (unlettered > 0) detail += $" {unlettered} unlettered volume{(unlettered == 1 ? " was" : "s were")} retained in topology but excluded from the capacity threshold.";
            return new(name, unavailable > 0 ? CheckState.Unknown : low.Length > 0 ? CheckState.Attention : CheckState.Information,
                detail + scope, Evidence: topology.Evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Unknown("Storage topology capacity evidence could not be interpreted.");
        }
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
}
