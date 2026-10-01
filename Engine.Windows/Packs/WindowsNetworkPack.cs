using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Packs;

/// <summary>Network adapter recipes that go beyond service state.</summary>
public static class WindowsNetworkPack
{
    public const string PackId = "windows.network";

    private static readonly RecipeOption AdapterOption = new("adapter", "Exact adapter name as Network Connections shows it") { Pattern = WindowsNetAdapterPrimitives.AdapterNamePattern };
    private static readonly Dictionary<string, string> AdapterTarget = new() { ["name"] = "${adapter}" };

    public static RecipePack Build() => new(RecipePack.CurrentSchemaVersion, PackId, 1, WindowsPrimitiveLibrary.Platform, "Windows network adapters",
    [
        new RecipeDefinition("network.disabled-adapters", 1, "Disabled physical network adapters", "Network",
            "Administrative state of physical adapters only; link, driver and connectivity are not tested",
            new ProbeSpec("netadapter.disabled-physical", new Dictionary<string, string>(), new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear),
                new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "At least one physical adapter is disabled. The evidence names it; the adapter enable recipe can turn it back on with the adapter option, or enable it from Network Connections. A driver problem code shows in the Device Problems workflow instead." }
            ])) { Name = "Physical adapters" })
        {
            Symptoms = ["network.adapter-missing"],
            Limitations = "A disabled adapter can be intentional, so the enable step is a separate explicit recipe."
        },
        new RecipeDefinition("network.adapter-enable", 1, "Enable a disabled network adapter", "Network",
            "The named physical adapter reports an enabled administrative state; link and connectivity are not tested",
            new ProbeSpec("netadapter.admin-status", AdapterTarget, new ExpectSpec(
            [
                new ExpectClause("equals", "Up", ProbeVerdict.Clear),
                new ExpectClause("equals", "Down", ProbeVerdict.Repairable),
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Adapter status" })
        {
            Symptoms = ["network.adapter-missing"],
            Options = [AdapterOption],
            Action = new ActionSpec("netadapter.enable", AdapterTarget) { Timeout = TimeSpan.FromSeconds(30), Effects = "Runs Enable-NetAdapter for the named physical adapter. An adapter that is already enabled is left alone." },
            Verify = new ProbeSpec("netadapter.admin-status", AdapterTarget, ExpectSpec.ClearWhenEquals("Up", ProbeVerdict.Attention)) { Name = "Adapter status" },
            Risk = RecipeRisk.Reversible,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "Only the administrative state is verified; link, driver and connectivity are not."
        },
        new RecipeDefinition("network.wifi-power-saving", 1, "Wireless adapter power saving", "Network",
            "The adapter's power management setting; roaming, driver and access point behavior are untested",
            new ProbeSpec("netadapter.power-saving", AdapterTarget, new ExpectSpec(
            [
                new ExpectClause("equals", "Disabled", ProbeVerdict.Clear),
                new ExpectClause("equals", "Enabled", ProbeVerdict.Repairable),
                new ExpectClause("equals", "Unsupported", ProbeVerdict.Information),
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Adapter power saving" })
        {
            Symptoms = ["network.wifi-drops"],
            Options = [AdapterOption with { Default = "Wi-Fi", Required = false }],
            Action = new ActionSpec("netadapter.power-saving-disable", AdapterTarget)
                { Timeout = TimeSpan.FromSeconds(30), Effects = "Sets AllowComputerToTurnOffDevice to Disabled on the named adapter. Battery life on laptops may drop slightly." },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "The adapter option defaults to Wi-Fi; a renamed adapter needs its exact name."
        },
        new RecipeDefinition("network.winhttp-proxy-reset", 1, "Reset the WinHTTP proxy", "Network",
            "The machine-wide WinHTTP proxy reads back as direct access; browser, per-user and PAC settings are untested",
            new ProbeSpec("winhttp.proxy", new Dictionary<string, string>(), new ExpectSpec(
            [
                new ExpectClause("equals", "Direct", ProbeVerdict.Clear),
                new ExpectClause("equals", "Proxy", ProbeVerdict.Repairable) { Detail = "A machine-wide WinHTTP proxy is set. Managed networks sometimes set one on purpose, so confirm it is wrong before resetting. The evidence names the server so it can be restored with netsh winhttp set proxy." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "WinHTTP proxy" })
        {
            Symptoms = ["network.proxy-wrong"],
            Action = new ActionSpec("winhttp.proxy-reset", new Dictionary<string, string>())
                { Timeout = TimeSpan.FromSeconds(30), Effects = "Runs netsh winhttp reset proxy so Windows services use direct access. Browser and per-user proxy settings are left alone." },
            Verify = new ProbeSpec("winhttp.proxy", new Dictionary<string, string>(), ExpectSpec.ClearWhenEquals("Direct", ProbeVerdict.Attention)) { Name = "WinHTTP proxy" },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "The prior proxy server is journaled in the detect evidence; there is no automatic rollback."
        }
    ]);
}
