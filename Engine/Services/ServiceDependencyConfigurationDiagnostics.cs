using System.Security;
using System.Text.Json;
using Microsoft.Win32;

namespace KillerBench.Engine;

public sealed record ServiceDependencyConfigurationEntry(string Name, bool Readable,
    IReadOnlyList<string> ServiceDependencies, IReadOnlyList<string> AbsentDependencies, bool HasGroupDependencies);
public sealed record ServiceDependencyConfigurationSnapshot(bool Complete,
    IReadOnlyList<ServiceDependencyConfigurationEntry> Services);

public static class ServiceDependencyConfigurationDiagnostics
{
    private const int MaximumServices = 1024;
    private const int MaximumDependenciesPerService = 64;
    private const string Scope = " Load-order group membership is counted but not expanded. Dependency configuration does not prove that a service can start or that its dependent feature works. No service, dependency, load-order group, startup setting, executable, account, permission, policy or registry value changed.";

    public static async Task<WorkflowCheck> CheckAsync(WorkflowCheck inventory, CancellationToken token = default,
        Func<IReadOnlyList<string>, CancellationToken, Task<ServiceDependencyConfigurationSnapshot>>? collect = null)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        token.ThrowIfCancellationRequested();
        if (!TryInventory(inventory, out var installed)) return Unknown(inventory.Evidence);
        collect ??= CollectAsync;
        try
        {
            var pending = collect(installed.Services.Select(service => service.Name).ToArray(), token);
            if (pending is null) return Unknown(inventory.Evidence);
            var snapshot = await pending.WaitAsync(token).ConfigureAwait(false);
            if (snapshot is null) return Unknown(inventory.Evidence);
            token.ThrowIfCancellationRequested();
            var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
            return Assess(snapshot, installed, evidence, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException or
            InvalidOperationException or NotSupportedException or TimeoutException)
        {
            return Unknown(inventory.Evidence);
        }
    }

    public static WorkflowCheck Assess(ServiceDependencyConfigurationSnapshot snapshot,
        ServiceInventorySnapshot installed, ToolResult? evidence = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(installed);
        token.ThrowIfCancellationRequested();
        if (!installed.Complete || !snapshot.Complete || installed.Services.Count is 0 or > MaximumServices ||
            snapshot.Services.Count != installed.Services.Count) return Unknown(evidence);
        var installedNames = installed.Services.Select(service => service.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var graph = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        int directEdges = 0;
        int groupConfigured = 0;
        int selfDependencies = 0;
        int missingDependencies = 0;
        foreach (var service in snapshot.Services)
        {
            token.ThrowIfCancellationRequested();
            if (!ValidName(service.Name) || !seen.Add(service.Name) || !installedNames.Contains(service.Name) ||
                !service.Readable || service.ServiceDependencies is null || service.AbsentDependencies is null ||
                service.ServiceDependencies.Count > MaximumDependenciesPerService ||
                service.AbsentDependencies.Count > MaximumDependenciesPerService)
                return Unknown(evidence);
            var dependencies = service.ServiceDependencies.ToArray();
            var absent = service.AbsentDependencies.ToArray();
            if (dependencies.Any(dependency => !ValidName(dependency)) ||
                dependencies.Distinct(StringComparer.OrdinalIgnoreCase).Count() != dependencies.Length ||
                absent.Any(dependency => !ValidName(dependency)) ||
                absent.Distinct(StringComparer.OrdinalIgnoreCase).Count() != absent.Length ||
                absent.Any(dependency => !dependencies.Contains(dependency, StringComparer.OrdinalIgnoreCase)))
                return Unknown(evidence);
            directEdges = checked(directEdges + dependencies.Length);
            if (service.HasGroupDependencies) groupConfigured++;
            selfDependencies += dependencies.Count(dependency => dependency.Equals(service.Name, StringComparison.OrdinalIgnoreCase));
            missingDependencies += absent.Length;
            graph.Add(service.Name, dependencies.Where(installedNames.Contains).ToArray());
        }
        if (seen.Count != installedNames.Count) return Unknown(evidence);
        int cycleNodes = CountCycleNodes(graph, token);
        string summary = $"Reviewed dependency configuration for {snapshot.Services.Count} installed services: {directEdges} direct service dependency references, {groupConfigured} service{(groupConfigured == 1 ? " has" : "s have")} load-order group dependencies, {missingDependencies} references to absent service identities, {selfDependencies} self-dependencies, and {cycleNodes} services participating in a direct dependency cycle.";
        if (missingDependencies > 0 || selfDependencies > 0 || cycleNodes > 0)
            return new("Service dependency configuration", CheckState.Attention,
                summary + " Review the owning Windows component, driver or approved application package before changing dependency configuration." + Scope,
                Evidence: evidence);
        return new("Service dependency configuration", CheckState.Information,
            summary + " No missing identity, self-dependency or direct service dependency cycle was established within this bounded snapshot." + Scope,
            Evidence: evidence);
    }

    private static int CountCycleNodes(IReadOnlyDictionary<string, string[]> graph, CancellationToken token)
    {
        var state = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<string>();
        var cycleNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Visit(string name)
        {
            token.ThrowIfCancellationRequested();
            state[name] = 1;
            stack.Add(name);
            foreach (string dependency in graph[name])
            {
                token.ThrowIfCancellationRequested();
                if (!state.TryGetValue(dependency, out byte dependencyState)) Visit(dependency);
                else if (dependencyState == 1)
                {
                    int start = stack.FindLastIndex(item => item.Equals(dependency, StringComparison.OrdinalIgnoreCase));
                    for (int index = Math.Max(0, start); index < stack.Count; index++) cycleNodes.Add(stack[index]);
                }
            }
            stack.RemoveAt(stack.Count - 1);
            state[name] = 2;
        }
        foreach (string name in graph.Keys)
            if (!state.ContainsKey(name)) Visit(name);
        return cycleNodes.Count;
    }

    private static Task<ServiceDependencyConfigurationSnapshot> CollectAsync(IReadOnlyList<string> names, CancellationToken token) =>
        Task.Run(() => Collect(names, token), token);

    private static ServiceDependencyConfigurationSnapshot Collect(IReadOnlyList<string> names, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Service dependency configuration requires Windows.");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var services = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", writable: false);
        if (services is null) throw new IOException("The installed service registry was unavailable.");
        string[] registryNames = services.GetSubKeyNames();
        if (registryNames.Length > 4096) return new(false, []);
        var registered = registryNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = new List<ServiceDependencyConfigurationEntry>(names.Count);
        bool complete = true;
        foreach (string name in names)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var key = services.OpenSubKey(name, writable: false);
                object? direct = key?.GetValue("DependOnService", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                object? groups = key?.GetValue("DependOnGroup", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                bool readable = key is not null && (direct is null or string[]) && (groups is null or string[]);
                string[] dependencies = direct as string[] ?? [];
                string[] absent = dependencies.Where(dependency => !registered.Contains(dependency)).ToArray();
                bool hasGroups = groups is string[] groupNames && groupNames.Length > 0;
                entries.Add(new(name, readable, dependencies, absent, hasGroups));
                complete &= readable;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
            {
                entries.Add(new(name, false, [], [], false));
                complete = false;
            }
        }
        return new(complete, entries.AsReadOnly());
    }

    private static bool TryInventory(WorkflowCheck check, out ServiceInventorySnapshot snapshot)
    {
        snapshot = new(false, []);
        if (check.EffectiveState == CheckState.Unknown || check.Evidence is not { } evidence || evidence.ExitCode != 0 ||
            evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return false;
        try
        {
            snapshot = ServiceInventoryDiagnostics.Parse(evidence.Output);
            return snapshot.Complete;
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool ValidName(string value) => value.Length is > 0 and <= 256 && value == value.Trim() &&
        !value.Any(character => char.IsControl(character) || character is '\\' or '/');

    private static WorkflowCheck Unknown(ToolResult? evidence) => new("Service dependency configuration", CheckState.Unknown,
        "Complete installed-service and dependency evidence is required." + Scope, Evidence: evidence);
}
