namespace KillerBench.Engine;

/// <summary>
/// Registers the Windows providers with the platform-neutral core. Hosts call this once at startup
/// before running any workflow or repair.
/// </summary>
public static class WindowsEngine
{
    public const string Platform = "Windows";

    public static void Register() => PlatformProviders.Register(Platform,
        progress => new WindowsWorkflowHost { Progress = progress },
        () => new WindowsServicingRepairCommandHost());
}
