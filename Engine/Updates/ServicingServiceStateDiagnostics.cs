using Microsoft.Win32;

namespace KillerBench.Engine.Updates;

public sealed record ServicingServiceDefinition(string Name, string DisplayName);

public static class ServicingServiceStateDiagnostics
{
    public static IReadOnlyList<ServicingServiceDefinition> Services { get; } = Array.AsReadOnly<ServicingServiceDefinition>(
    [
        new ServicingServiceDefinition("wuauserv", "Windows Update"),
        new ServicingServiceDefinition("BITS", "Background Intelligent Transfer Service"),
        new ServicingServiceDefinition("CryptSvc", "Cryptographic Services"),
        new ServicingServiceDefinition("TrustedInstaller", "Windows Modules Installer"),
        new ServicingServiceDefinition("WaaSMedicSvc", "Windows Update Medic Service"),
        new ServicingServiceDefinition("UsoSvc", "Update Orchestrator Service")
    ]);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<ServicingServiceDefinition, CancellationToken, Task<(ToolResult Result, int? Startup)>>? inspect = null)
        => await CheckSelectedAsync(Services.Select(service => service.Name), token, inspect).ConfigureAwait(false);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckSelectedAsync(IEnumerable<string> serviceNames,
        CancellationToken token = default,
        Func<ServicingServiceDefinition, CancellationToken, Task<(ToolResult Result, int? Startup)>>? inspect = null)
    {
        ArgumentNullException.ThrowIfNull(serviceNames);
        inspect ??= InspectAsync;
        string[] requested = [.. serviceNames];
        if (requested.Length is < 1 or > 6 || requested.Any(string.IsNullOrWhiteSpace) ||
            requested.Distinct(StringComparer.OrdinalIgnoreCase).Count() != requested.Length)
            throw new ArgumentException("Select one through six unique servicing service names.", nameof(serviceNames));
        var selected = Services.Where(service => requested.Contains(service.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (selected.Length != requested.Length)
            throw new ArgumentException("The selection contains an unsupported servicing service name.", nameof(serviceNames));
        var checks = new List<WorkflowCheck>(selected.Length);
        foreach (var service in selected)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var (result, startup) = await (inspect(service, token) ?? throw new InvalidOperationException("The service-state provider returned no task."))
                    .WaitAsync(TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
                checks.Add(Interpret(service, result, startup));
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                checks.Add(Unknown(service, null, "Service-state collection exceeded its time limit."));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
                System.Security.SecurityException or System.ComponentModel.Win32Exception or TimeoutException)
            {
                checks.Add(Unknown(service, null, "Service-state collection was unavailable."));
            }
        }
        return checks.AsReadOnly();
    }

    public static WorkflowCheck Interpret(ServicingServiceDefinition service, ToolResult result, int? startup)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(result);
        if (!Services.Contains(service) || !ServiceRecovery.MatchesService(result, service.Name) || startup is not (0 or 1 or 2 or 3 or 4))
            return Unknown(service, result, "Exact service identity, state or startup evidence is incomplete or unrecognized.");
        int? state = ServiceRecovery.State(result);
        if (state is null) return Unknown(service, result, "The current service state could not be recognized.");
        string startupLabel = startup switch { 0 => "boot", 1 => "system", 2 => "automatic", 3 => "demand-start", 4 => "disabled", _ => "unknown" };
        return state switch
        {
            4 => new(service.DisplayName + " state", CheckState.Information,
                $"The exact service is running with {startupLabel} startup. Running state does not prove update, transfer or servicing success. No service or startup setting changed.", Evidence: result),
            1 when startup == 3 => new(service.DisplayName + " state", CheckState.Information,
                "The exact demand-start service is stopped. This can be normal while idle. No start was requested and no startup setting changed.", Evidence: result),
            1 when startup == 4 => new(service.DisplayName + " state", CheckState.Attention,
                "The exact service is disabled and stopped. Review policy and component ownership. No start was requested and no startup setting changed.", Evidence: result),
            1 => new(service.DisplayName + " state", CheckState.Attention,
                $"The exact {startupLabel} service is stopped. This read-only result does not authorize a start or configuration change.", Evidence: result),
            _ => new(service.DisplayName + " state", CheckState.Attention,
                "The exact service is transitioning or paused. Recheck after Windows settles. No service or startup setting changed.", Evidence: result)
        };
    }

    private static WorkflowCheck Unknown(ServicingServiceDefinition service, ToolResult? evidence, string reason) =>
        new(service.DisplayName + " state", CheckState.Unknown,
            reason + " No service, update, transfer, cache, source, policy or startup setting changed.", Evidence: evidence);

    private static async Task<(ToolResult Result, int? Startup)> InspectAsync(ServicingServiceDefinition service, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + service.Name);
        int? startup = key?.GetValue("Start", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as int?;
        var result = await BoundedTool.RunAsync(new("sc.exe", ["query", service.Name], true),
            TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
        return (result, startup);
    }
}
