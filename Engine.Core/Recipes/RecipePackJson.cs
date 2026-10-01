using System.Text.Json;
using System.Text.Json.Serialization;

namespace KillerBench.Engine.Core.Recipes;

/// <summary>Reads and writes recipe packs as UTF-8 JSON. Loading is bounded and strict: unknown properties, duplicate properties and oversized documents are rejected.</summary>
public static class RecipePackJson
{
    public const int MaximumBytes = 8 * 1024 * 1024;

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(RecipePack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        return JsonSerializer.Serialize(pack, Options);
    }

    public static RecipePack Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > MaximumBytes) throw new InvalidDataException("Recipe pack exceeds the " + MaximumBytes + " byte limit.");
        RecipePack? pack;
        try { pack = JsonSerializer.Deserialize<RecipePack>(json, Options); }
        catch (JsonException error) { throw new InvalidDataException("Recipe pack JSON is invalid: " + error.Message, error); }
        return pack ?? throw new InvalidDataException("Recipe pack JSON is empty.");
    }

    public static async Task<RecipePack> LoadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Recipe pack not found.", path);
        if (info.Length > MaximumBytes) throw new InvalidDataException("Recipe pack exceeds the " + MaximumBytes + " byte limit.");
        string json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
        return Deserialize(json);
    }
}
