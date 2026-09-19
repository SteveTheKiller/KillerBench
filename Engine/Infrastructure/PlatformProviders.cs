namespace KillerBench.Engine;

/// <summary>
/// Registry of platform-specific providers used by the platform-neutral core.
/// A provider assembly registers its implementations at host startup. Core code that needs a
/// native provider resolves it here and fails with a clear message when none is registered.
/// </summary>
public static class PlatformProviders
{
    private static Func<IProgress<string>?, IWorkflowHost>? _workflowHostFactory;
    private static Func<IServicingRepairCommandHost>? _servicingRepairCommandHostFactory;

    public static string? RegisteredPlatform { get; private set; }

    public static bool IsRegistered => RegisteredPlatform is not null;

    public static void Register(string platform,
        Func<IProgress<string>?, IWorkflowHost> workflowHostFactory,
        Func<IServicingRepairCommandHost> servicingRepairCommandHostFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        ArgumentNullException.ThrowIfNull(workflowHostFactory);
        ArgumentNullException.ThrowIfNull(servicingRepairCommandHostFactory);
        _workflowHostFactory = workflowHostFactory;
        _servicingRepairCommandHostFactory = servicingRepairCommandHostFactory;
        RegisteredPlatform = platform;
    }

    public static IWorkflowHost CreateWorkflowHost(IProgress<string>? progress = null) =>
        (_workflowHostFactory ?? throw Unregistered())(progress)
        ?? throw new InvalidOperationException("The platform workflow host factory returned no host.");

    public static IServicingRepairCommandHost CreateServicingRepairCommandHost() =>
        (_servicingRepairCommandHostFactory ?? throw Unregistered())()
        ?? throw new InvalidOperationException("The platform servicing command host factory returned no host.");

    private static PlatformNotSupportedException Unregistered() =>
        new("No platform provider is registered. Call the platform engine's Register method at startup, for example KillerBench.Engine.WindowsEngine.Register().");
}
