using System.Collections.Concurrent;

namespace KillerBench.Engine.Infrastructure;

/// <summary>
/// Registry of platform-specific providers used by the platform-neutral core.
/// A provider assembly registers its implementations at host startup. Core code that needs a
/// native provider resolves it here and fails with a clear message when none is registered.
/// </summary>
public static class PlatformProviders
{
    private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();
    private static Func<IProgress<string>?, IWorkflowHost>? _workflowHostFactory;

    public static string? RegisteredPlatform { get; private set; }

    public static bool IsRegistered => RegisteredPlatform is not null;

    public static void RegisterPlatform(string platform, Func<IProgress<string>?, IWorkflowHost> workflowHostFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        ArgumentNullException.ThrowIfNull(workflowHostFactory);
        _workflowHostFactory = workflowHostFactory;
        RegisteredPlatform = platform;
    }

    /// <summary>Registers the factory for one provider interface. A later registration replaces an earlier one.</summary>
    public static void Register<TProvider>(Func<TProvider> factory) where TProvider : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Factories[typeof(TProvider)] = () => factory();
    }

    public static bool IsAvailable<TProvider>() where TProvider : class => Factories.ContainsKey(typeof(TProvider));

    public static TProvider Create<TProvider>() where TProvider : class
    {
        if (!Factories.TryGetValue(typeof(TProvider), out var factory)) throw Unregistered(typeof(TProvider).Name);
        return factory() as TProvider
            ?? throw new InvalidOperationException("The platform provider factory for " + typeof(TProvider).Name + " returned no provider.");
    }

    public static IWorkflowHost CreateWorkflowHost(IProgress<string>? progress = null) =>
        (_workflowHostFactory ?? throw Unregistered(nameof(IWorkflowHost)))(progress)
        ?? throw new InvalidOperationException("The platform workflow host factory returned no host.");

    public static IServicingRepairCommandHost CreateServicingRepairCommandHost() => Create<IServicingRepairCommandHost>();

    private static PlatformNotSupportedException Unregistered(string provider) =>
        new("No platform provider is registered for " + provider + ". Call the platform engine's Register method at startup, for example KillerBench.Engine.Windows.WindowsEngine.Register().");
}
