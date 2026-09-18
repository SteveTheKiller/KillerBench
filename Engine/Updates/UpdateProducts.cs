using System.Text.Json;

namespace KillerBench.Engine;

public sealed record UpdateProduct(string Id, string Name);

internal static class UpdateProducts
{
    internal static IReadOnlyList<UpdateProduct>? Read(JsonElement update)
    {
        if (!update.TryGetProperty("products", out var products) || products.ValueKind != JsonValueKind.Array || products.GetArrayLength() > 64) return null;
        var result = new List<UpdateProduct>();
        var identities = new HashSet<Guid>();
        foreach (var product in products.EnumerateArray())
        {
            if (product.ValueKind != JsonValueKind.Object || !product.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
                !product.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String) return null;
            var entry = new UpdateProduct(id.GetString()!, name.GetString()!);
            if (!Valid(entry) || !identities.Add(Guid.Parse(entry.Id))) return null;
            result.Add(entry with { Id = Guid.Parse(entry.Id).ToString("D") });
        }
        return result.AsReadOnly();
    }

    internal static IReadOnlyList<UpdateProduct> Choices(IReadOnlyList<AvailableUpdate> updates) => Array.AsReadOnly(updates
        .SelectMany(update => update.Products ?? []).Where(Valid)
        .GroupBy(product => Guid.Parse(product.Id))
        .Select(group => new UpdateProduct(group.Key.ToString("D"), group.Select(product => product.Name).Order(StringComparer.Ordinal).First()))
        .OrderBy(product => product.Name, StringComparer.Ordinal).ThenBy(product => product.Id, StringComparer.Ordinal).ToArray());

    private static bool Valid(UpdateProduct product) => Guid.TryParse(product.Id, out var id) && id != Guid.Empty &&
        !string.IsNullOrWhiteSpace(product.Name) && product.Name.Length <= 256 && !product.Name.Any(char.IsControl);
}
