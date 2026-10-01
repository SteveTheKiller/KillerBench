using System.Text.Json;

namespace KillerBench.Engine.BitLocker;

public sealed record BitLockerProtector(Guid Id, string Type);
public sealed record BitLockerProtectorInventory(string Volume, IReadOnlyList<BitLockerProtector> Protectors, bool Complete);

public static class BitLockerProtectors
{
    public static RepairStep QueryStep(string volume)
    {
        new BitLockerOptions(volume).Validate();
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "$ErrorActionPreference='Stop'; $b=Get-BitLockerVolume -MountPoint '" + volume.ToUpperInvariant() + "' -ErrorAction Stop; " +
            "[pscustomobject]@{volume=$b.MountPoint; protectors=@($b.KeyProtector | ForEach-Object {[pscustomobject]@{id=$_.KeyProtectorId; type=[string]$_.KeyProtectorType}})} | ConvertTo-Json -Depth 3 -Compress"], true);
    }

    public static async Task<BitLockerProtectorInventory> InspectAsync(string volume, CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(volume), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false), volume);

    public static BitLockerProtectorInventory Interpret(ToolResult result, string volume)
    {
        new BitLockerOptions(volume).Validate();
        BitLockerProtectorInventory Unknown() => new(volume, [], false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            if (!UniqueProperties(root)) return Unknown();
            if (!string.Equals(root.GetProperty("volume").GetString(), volume, StringComparison.OrdinalIgnoreCase)) return Unknown();
            var items = root.GetProperty("protectors");
            if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > 64) return Unknown();
            var protectors = new List<BitLockerProtector>();
            foreach (var item in items.EnumerateArray())
            {
                if (!UniqueProperties(item)) return Unknown();
                if (!Guid.TryParse(item.GetProperty("id").GetString(), out var id) || id == Guid.Empty || protectors.Any(p => p.Id == id)) return Unknown();
                string? type = item.GetProperty("type").GetString();
                if (string.IsNullOrWhiteSpace(type) || type.Length > 64 || type.Any(char.IsControl)) return Unknown();
                protectors.Add(new(id, type));
            }
            return new(volume, protectors.AsReadOnly(), true);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException) { return Unknown(); }
    }

    private static bool UniqueProperties(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return element.EnumerateObject().All(property => names.Add(property.Name));
    }
}
