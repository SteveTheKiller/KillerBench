using System.Text.Json;

namespace KillerBench.Engine.Core.Symptoms;

/// <summary>Reads and writes symptom catalogs with the same strict JSON rules as recipe packs.</summary>
public static class SymptomCatalogJson
{
    public static string Serialize(SymptomCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return JsonSerializer.Serialize(catalog, RecipePackJson.Options);
    }

    public static SymptomCatalog Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > RecipePackJson.MaximumBytes) throw new InvalidDataException("Symptom catalog exceeds the " + RecipePackJson.MaximumBytes + " byte limit.");
        SymptomCatalog? catalog;
        try { catalog = JsonSerializer.Deserialize<SymptomCatalog>(json, RecipePackJson.Options); }
        catch (JsonException error) { throw new InvalidDataException("Symptom catalog JSON is invalid: " + error.Message, error); }
        return catalog ?? throw new InvalidDataException("Symptom catalog JSON is empty.");
    }

    public static async Task<SymptomCatalog> LoadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Symptom catalog not found.", path);
        if (info.Length > RecipePackJson.MaximumBytes) throw new InvalidDataException("Symptom catalog exceeds the " + RecipePackJson.MaximumBytes + " byte limit.");
        return Deserialize(await File.ReadAllTextAsync(path, token).ConfigureAwait(false));
    }
}
