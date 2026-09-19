using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Network adapter recipes that go beyond service state.</summary>
public static class WindowsNetworkPack
{
    public const string PackId = "windows.network";

    public static RecipePack Build() => new(RecipePack.CurrentSchemaVersion, PackId, 1, WindowsPrimitiveLibrary.Platform, "Windows network adapters",
    [
        new RecipeDefinition("network.disabled-adapters", 1, "Disabled physical network adapters", "Network",
            "Administrative state of physical adapters only; link, driver and connectivity are not tested",
            new ProbeSpec("netadapter.disabled-physical", new Dictionary<string, string>(), new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear),
                new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "At least one physical adapter is disabled. Enable it from Network Connections if it is the intended connection; a driver problem code shows in the Device Problems workflow instead." }
            ])) { Name = "Physical adapters" })
        {
            Symptoms = ["network.adapter-missing"],
            Limitations = "Enabling a specific adapter needs its exact name and is a separate explicit option once per-recipe options exist."
        },
        new RecipeDefinition("network.wifi-power-saving", 1, "Wi-Fi adapter power saving", "Network",
            "The Wi-Fi adapter's power management setting; roaming, driver and access point behavior are untested",
            new ProbeSpec("netadapter.power-saving", new Dictionary<string, string> { ["name"] = "Wi-Fi" }, new ExpectSpec(
            [
                new ExpectClause("equals", "Disabled", ProbeVerdict.Clear),
                new ExpectClause("equals", "Enabled", ProbeVerdict.Repairable),
                new ExpectClause("equals", "Unsupported", ProbeVerdict.Information),
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Wi-Fi power saving" })
        {
            Symptoms = ["network.wifi-drops"],
            Action = new ActionSpec("netadapter.power-saving-disable", new Dictionary<string, string> { ["name"] = "Wi-Fi" })
                { Timeout = TimeSpan.FromSeconds(30), Effects = "Sets AllowComputerToTurnOffDevice to Disabled on the adapter named Wi-Fi. Battery life on laptops may drop slightly." },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "Only an adapter named exactly Wi-Fi is covered; renamed adapters report Unknown."
        }
    ]);
}
