using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Shared helpers for Windows service primitives. The service name parameter must name a service the Engine's service catalog supports.</summary>
public static class WindowsServicePrimitives
{
    public const string NamePattern = "[A-Za-z0-9_.-]{1,256}";

    public static readonly PrimitiveParameter NameParameter = new("name", true, "Exact Windows service name from the supported service catalog") { Pattern = NamePattern };

    /// <summary>Resolves the catalog repair kind for a supported service name. Unknown names return null; the primitives then report unavailable evidence rather than inventing a check.</summary>
    public static RepairKind? ResolveKind(string? service)
    {
        if (string.IsNullOrWhiteSpace(service)) return null;
        var matches = Enum.GetValues<RepairKind>()
            .Where(kind => string.Equals(ServiceRecovery.ServiceName(kind), service, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    /// <summary>Focused workflows may start these stopped demand-start services; the generic recipe path treats a stopped demand-start service as informational.</summary>
    public static bool IsFocusedDemandStart(RepairKind kind) =>
        kind is RepairKind.WindowsTime or RepairKind.WinHttpAutoProxyService or RepairKind.WindowsUpdateMedicService;

    /// <summary>Maps an Engine service check to the primitive's observed value. The value is the effective check state name, so packs match on Clear, Repairable, Attention, Information or Unknown.</summary>
    public static PrimitiveResult ToPrimitiveResult(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var evidence = check.Evidence;
        bool complete = evidence is not null && evidence.ExitCode == 0 && !evidence.TimedOut && !evidence.Truncated && string.IsNullOrWhiteSpace(evidence.Error);
        string raw = check.Detail + (evidence is null ? "" : "\n" + evidence.Output + (string.IsNullOrWhiteSpace(evidence.Error) ? "" : "\n" + evidence.Error));
        if (check.EffectiveState == CheckState.Unknown && !complete)
            return new PrimitiveResult(false, null, raw, check.Detail) { TimedOut = evidence?.TimedOut ?? false, Truncated = evidence?.Truncated ?? false };
        return new PrimitiveResult(true, check.EffectiveState.ToString(), raw) { Detail = check.Detail };
    }

    internal static string Name(IReadOnlyDictionary<string, string> parameters) =>
        parameters.TryGetValue("name", out string? name) && !string.IsNullOrWhiteSpace(name) ? name : "";
}

/// <summary>Observes one supported service through the Engine's full service check: identity, startup type, exit code, dependency graph and delayed-start window.</summary>
public sealed class WindowsServiceCheckProbe : IProbePrimitive
{
    public string Name => "service.check";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsServicePrimitives.NameParameter];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        string service = WindowsServicePrimitives.Name(parameters);
        if (WindowsServicePrimitives.ResolveKind(service) is not { } kind)
            return PrimitiveResult.Unavailable("The service '" + service + "' is not in the supported service catalog.");
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Service checks require Windows.");
        try
        {
            var check = await ServiceRecovery.CheckAsync(kind, token, WindowsServicePrimitives.IsFocusedDemandStart(kind)).ConfigureAwait(false);
            return WindowsServicePrimitives.ToPrimitiveResult(check);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return PrimitiveResult.Unavailable("Service evidence could not be collected: " + error.Message);
        }
    }
}

/// <summary>Observes whether one service identifies itself and stays Running across two observations. Value is Running or NotRunning.</summary>
public sealed class WindowsServiceRunningProbe : IProbePrimitive
{
    public string Name => "service.running";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsServicePrimitives.NameParameter];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        string service = WindowsServicePrimitives.Name(parameters);
        if (WindowsServicePrimitives.ResolveKind(service) is null)
            return PrimitiveResult.Unavailable("The service '" + service + "' is not in the supported service catalog.");
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Service checks require Windows.");
        var result = await ServiceRecovery.ObserveRunningAsync(service,
            cancellationToken => NativeTool.RunAsync("sc.exe", ["query", service], token: cancellationToken), token).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "Service observation timed out.") { TimedOut = true };
        if (result.Truncated) return new PrimitiveResult(false, null, raw, "Service observation output was truncated.") { Truncated = true };
        if (result.ExitCode == 0 && ServiceRecovery.MatchesService(result, service) && ServiceRecovery.State(result) == 4)
            return new PrimitiveResult(true, "Running", raw);
        if (ServiceRecovery.MatchesService(result with { ExitCode = 0, Error = "" }, service) && ServiceRecovery.State(result with { ExitCode = 0, Error = "" }) is { } state)
            return new PrimitiveResult(true, state == 4 ? "NotStable" : "NotRunning", raw);
        return PrimitiveResult.Unavailable(string.IsNullOrWhiteSpace(result.Error) ? "Service state could not be recognized." : result.Error.Trim());
    }
}

/// <summary>Starts one supported service through the Engine's guarded start path, which rechecks eligibility and executable registration immediately before dispatch.</summary>
public sealed class WindowsServiceStartAction : IActionPrimitive
{
    public string Name => "service.start";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsServicePrimitives.NameParameter];
    public RecipeRisk Risk => RecipeRisk.Reversible;
    public bool RequiresElevation => true;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        string service = WindowsServicePrimitives.Name(parameters);
        if (WindowsServicePrimitives.ResolveKind(service) is null)
            return new ActionResult(false, false, "", "The service '" + service + "' is not in the supported service catalog.");
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Service start requires Windows.");
        try
        {
            var result = await ServiceRecovery.ExecuteAsync(new RepairStep("sc.exe", ["start", service], false), progress, token).ConfigureAwait(false);
            string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
            if (result.TimedOut) return new ActionResult(true, false, raw, "Service start timed out; state is unverified.") { TimedOut = true };
            return new ActionResult(true, result.ExitCode == 0, raw, result.ExitCode == 0 ? null : "sc.exe start exited with " + result.ExitCode + ".");
        }
        catch (RepairStepSkippedException skipped) { return new ActionResult(false, true, skipped.Evidence.Output, skipped.Message); }
        catch (RepairDispatchDeferredException deferred) { return new ActionResult(false, false, "", deferred.Message); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return new ActionResult(false, false, "", "Service start could not be dispatched: " + error.Message);
        }
    }
}
