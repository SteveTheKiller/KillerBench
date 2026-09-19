using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>The whitelisted primitives Windows packs may reference.</summary>
public static class WindowsPrimitiveLibrary
{
    public const string Platform = "Windows";

    public static PrimitiveLibrary Create() => new PrimitiveLibrary(Platform)
        .AddProbe(new WindowsServiceCheckProbe())
        .AddProbe(new WindowsServiceRunningProbe())
        .AddProbe(new WindowsRegistryValueProbe())
        .AddProbe(new WindowsRegistrySubkeysProbe())
        .AddProbe(new WindowsBcdSafebootProbe())
        .AddProbe(new WindowsProcessRunningProbe())
        .AddAction(new WindowsServiceStartAction())
        .AddAction(new WindowsRegistrySetDwordAction())
        .AddAction(new WindowsBcdSafebootClearAction())
        .AddAction(new WindowsExplorerStartAction());
}
