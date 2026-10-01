namespace KillerBench.Engine.Services;

public enum FeatureServiceFailureGroup
{
    Time,
    Search,
    Wmi,
    EventLog,
    Audio
}

public static class FeatureServiceFailureEvents
{
    private static readonly Dictionary<FeatureServiceFailureGroup, ExactServiceFailureEventDefinition[]> Definitions =
        new Dictionary<FeatureServiceFailureGroup, ExactServiceFailureEventDefinition[]>
        {
            [FeatureServiceFailureGroup.Time] =
            [
                Definition("W32Time", "Windows Time", "Windows Time service failure history",
                    "current time synchronization, source reachability or clock accuracy")
            ],
            [FeatureServiceFailureGroup.Search] =
            [
                Definition("WSearch", "Windows Search", "Windows Search service failure history",
                    "current indexing progress, catalog health or query results")
            ],
            [FeatureServiceFailureGroup.Wmi] =
            [
                Definition("winmgmt", "Windows Management Instrumentation", "WMI service failure history",
                    "current provider availability, repository consistency or query success")
            ],
            [FeatureServiceFailureGroup.EventLog] =
            [
                Definition("EventLog", "Windows Event Log", "Windows Event Log service failure history",
                    "current channel availability, retention state or event completeness")
            ],
            [FeatureServiceFailureGroup.Audio] =
            [
                Definition("AudioEndpointBuilder", "Windows Audio Endpoint Builder", "Audio Endpoint Builder failure history",
                    "current endpoint discovery, device availability or audio playback"),
                Definition("Audiosrv", "Windows Audio", "Windows Audio service failure history",
                    "current endpoint state, stream availability or audio playback")
            ]
        };

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(FeatureServiceFailureGroup group,
        CancellationToken token = default, Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        if (!Enum.IsDefined(group)) throw new ArgumentOutOfRangeException(nameof(group));
        token.ThrowIfCancellationRequested();
        return await ExactServiceFailureEvents.CheckManyAsync(Definitions[group], token, execute).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAllAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        return await ExactServiceFailureEvents.CheckManyAsync(
            [.. Enum.GetValues<FeatureServiceFailureGroup>().SelectMany(group => Definitions[group])], token, execute)
            .ConfigureAwait(false);
    }

    private static ExactServiceFailureEventDefinition Definition(string service, string display, string check, string excludedClaim) =>
        new(service, display, check,
            $" Records are historical observations tied to the exact local {display} identity. They do not establish a current outage, prove {excludedClaim} or authorize a service start, restart, startup change, component reset, policy change or repair.");
}
