using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>The built-in Windows recipe packs, in load order.</summary>
public static class WindowsPacks
{
    public static IReadOnlyList<RecipePack> All() => [WindowsServicePack.Build(), WindowsSettingsPack.Build(), WindowsSystemPack.Build(), WindowsNetworkPack.Build(), WindowsMaintenancePack.Build()];

    public static IReadOnlySet<string> RecipeIds() =>
        All().SelectMany(pack => pack.Recipes).Select(recipe => recipe.Id).ToHashSet(StringComparer.Ordinal);
}
