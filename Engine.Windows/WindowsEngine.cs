namespace KillerBench.Engine.Windows;

/// <summary>
/// Registers the Windows providers with the platform-neutral core. Hosts call this once at startup
/// before running any workflow or repair.
/// </summary>
public static class WindowsEngine
{
    public const string Platform = "Windows";

    public static void Register()
    {
        PlatformProviders.RegisterPlatform(Platform, progress => new WindowsWorkflowHost { Progress = progress });
        PlatformProviders.Register<IServicingRepairCommandHost>(() => new WindowsServicingRepairCommandHost());
        PlatformProviders.Register<ITaskRegistrationRecoveryHost>(() => new WindowsTaskRegistrationRecoveryHost());
        PlatformProviders.Register<IRepairHostFactory>(() => new WindowsRepairHostFactory());
    }

    private sealed class WindowsRepairHostFactory : IRepairHostFactory
    {
        public IRepairHost Create(RepairKind kind, IReadOnlyList<string>? selectedPrinterQueues, CancellationToken token) =>
            new WindowsRepairHost(kind, selectedPrinterQueues, token);
    }
}
