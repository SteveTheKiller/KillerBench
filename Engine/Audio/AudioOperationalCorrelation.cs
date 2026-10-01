namespace KillerBench.Engine.Audio;

public static class AudioOperationalCorrelation
{
    private static readonly string[] HistoryNames =
        ["Audio Endpoint Builder failure history", "Windows Audio service failure history"];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> devices,
        IReadOnlyList<WorkflowCheck> endpoints, IReadOnlyList<WorkflowCheck> serviceHistory,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();

        const string name = "Audio operational correlation";
        const string boundary = " This bounded correlation does not open an audio stream or prove playback, recording, application permissions, routing, live signal, per-application volume, Bluetooth transport or physical device operation. No endpoint, device, driver, service, format, effect, volume, mute, policy or audio setting changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (!ValidGroup(devices, "Audio ", 1_024) || !ValidGroup(endpoints, "Default audio ", 16) ||
            !ExactNames(serviceHistory, HistoryNames))
            return Unknown("Audio correlation inputs were missing, duplicated, excessive or carried an unexpected identity.");

        var all = devices.Concat(endpoints).Concat(serviceHistory).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {devices.Count} audio device and driver results, {endpoints.Count} selected default-endpoint results and two exact audio service histories. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete device, endpoint or service-history evidence before drawing a combined audio-readiness conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the exact device, endpoint and historical results together before selecting a focused action." + boundary);
        return new(name, CheckState.Information,
            detail + " No correlated observation requested review. Successful playback and recording remain unverified." + boundary);
    }

    private static bool ValidGroup(IReadOnlyList<WorkflowCheck> checks, string prefix, int maximum) =>
        checks.Count is > 0 && checks.Count <= maximum && checks.All(item => item is not null &&
            item.Name.StartsWith(prefix, StringComparison.Ordinal));

    private static bool ExactNames(IReadOnlyList<WorkflowCheck> checks, string[] names) =>
        checks.Count == names.Length && checks.Select(item => item.Name).OrderBy(value => value, StringComparer.Ordinal)
            .SequenceEqual(names.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
}
