using KillerBench.Engine;

internal static class AudioOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            clear.Detail.Contains("Successful playback and recording remain unverified", StringComparison.Ordinal),
            "Complete audio evidence produces a bounded informational result without claiming stream operation");

        var attention = Check(device: CheckState.Attention, history: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("before selecting a focused action", StringComparison.Ordinal),
            "Current audio and historical attention produces review without selecting repair");

        var incomplete = Check(endpoint: CheckState.Unknown, history: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete endpoint evidence takes precedence over historical attention");

        Require(Invoke(devices: []).State == CheckState.Unknown &&
            Invoke(endpoints: [Info("Unexpected endpoint")]).State == CheckState.Unknown &&
            Invoke(history: [Info("Windows Audio service failure history"), Info("Windows Audio service failure history")]).State == CheckState.Unknown,
            "Missing, unexpected and duplicate audio identities fail closed");

        try
        {
            Invoke(token: new CancellationToken(true));
            throw new Exception("Audio operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: audio device, default-endpoint and exact service-history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState device = CheckState.Clear,
        CheckState endpoint = CheckState.Information, CheckState history = CheckState.Information) => Invoke(
            devices: [new("Audio device: Fixture", device, "fixture")],
            endpoints: [new("Default audio output", endpoint, "fixture")],
            history: [new("Audio Endpoint Builder failure history", history, "fixture"),
                Info("Windows Audio service failure history")]);

    private static WorkflowCheck Invoke(IReadOnlyList<WorkflowCheck>? devices = null,
        IReadOnlyList<WorkflowCheck>? endpoints = null, IReadOnlyList<WorkflowCheck>? history = null,
        CancellationToken token = default) => AudioOperationalCorrelation.Check(
            devices ?? [Info("Audio device: Fixture")], endpoints ?? [Info("Default audio output")],
            history ?? [Info("Audio Endpoint Builder failure history"), Info("Windows Audio service failure history")], token);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
