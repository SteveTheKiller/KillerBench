namespace KillerBench.Engine.Services;

public static class ServiceTransitionDiagnostics
{
    private static readonly HashSet<string> TransitionalStates = new(StringComparer.Ordinal)
    {
        "Start Pending", "Stop Pending", "Continue Pending", "Pause Pending"
    };

    public static async Task<WorkflowCheck> CheckAsync(WorkflowCheck first, Func<CancellationToken, Task<WorkflowCheck>>? collect = null,
        TimeSpan? observationInterval = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(first);
        token.ThrowIfCancellationRequested();
        var interval = observationInterval ?? TimeSpan.FromSeconds(3);
        if (interval < TimeSpan.FromMilliseconds(1) || interval > TimeSpan.FromSeconds(10))
            throw new ArgumentOutOfRangeException(nameof(observationInterval));
        if (!TrySnapshot(first, out var initial)) return Unknown(first.Evidence);
        if (!initial.Services.Any(service => TransitionalStates.Contains(service.State)))
            return Check(first, first, token);
        collect ??= ServiceInventoryDiagnostics.CheckAsync;
        try
        {
            await Task.Delay(interval, token).ConfigureAwait(false);
            var pending = collect(token);
            if (pending is null) return Unknown(first.Evidence);
            var second = await pending.WaitAsync(token).ConfigureAwait(false);
            return second is null ? Unknown(first.Evidence) : Check(first, second, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception or TimeoutException or InvalidOperationException or NotSupportedException)
        {
            return Unknown(first.Evidence);
        }
    }

    public static WorkflowCheck Check(WorkflowCheck first, WorkflowCheck second, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        token.ThrowIfCancellationRequested();
        if (!TrySnapshot(first, out var initial) || !TrySnapshot(second, out var final))
            return Unknown(second.Evidence ?? first.Evidence);
        var finalByName = final.Services.ToDictionary(service => service.Name, StringComparer.OrdinalIgnoreCase);
        var initialPending = initial.Services.Where(service => TransitionalStates.Contains(service.State)).ToArray();
        var persistent = initialPending.Where(service => finalByName.TryGetValue(service.Name, out var later) &&
            string.Equals(later.State, service.State, StringComparison.Ordinal)).OrderBy(service => service.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        string detail = $"Compared two complete installed-service snapshots. {initialPending.Length} service{(initialPending.Length == 1 ? " was" : "s were")} transitional initially; " +
            $"{persistent.Length} remained in the same exact transitional state.";
        if (persistent.Length > 0)
        {
            detail += " Persistent transitions: " + string.Join("; ", persistent.Take(20).Select(service => service.DisplayName + " [" + service.Name + "]: " + service.State)) +
                (persistent.Length > 20 ? "; additional identities remain in retained evidence." : ".");
            detail += " A bounded repeated observation does not prove a hang. Review the related feature and Service Control Manager history before choosing a focused recovery.";
        }
        else if (initialPending.Length > 0)
            detail += " Each initial transition changed state or identity before the second snapshot, so no persistent transition was established.";
        else
            detail += " No service was transitional in the initial snapshot.";
        detail += " No service was started, stopped, continued, paused, restarted or reconfigured.";
        return new("Persistent service transitions", persistent.Length > 0 ? CheckState.Attention : CheckState.Information,
            detail, Evidence: second.Evidence);
    }

    private static bool TrySnapshot(WorkflowCheck check, out ServiceInventorySnapshot snapshot)
    {
        snapshot = new(false, []);
        if (check.EffectiveState == CheckState.Unknown || check.Evidence is not { } evidence || evidence.ExitCode != 0 ||
            evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return false;
        try
        {
            snapshot = ServiceInventoryDiagnostics.Parse(evidence.Output);
            return snapshot.Complete;
        }
        catch (Exception error) when (error is System.Text.Json.JsonException or InvalidDataException or InvalidOperationException)
        {
            return false;
        }
    }

    private static WorkflowCheck Unknown(ToolResult? evidence) => new("Persistent service transitions", CheckState.Unknown,
        "Two complete installed-service snapshots are required to evaluate persistent transitional states. No service was started, stopped, continued, paused, restarted or reconfigured.", Evidence: evidence);
}
