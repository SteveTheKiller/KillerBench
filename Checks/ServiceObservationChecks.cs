using KillerBench.Engine;

internal static class ServiceObservationChecks
{
    internal static async Task RunAsync()
    {
        static ToolResult State(int state, string service = "Spooler") => new(0, $"SERVICE_NAME: {service}\nSTATE : {state}", "", false);
        string[] labels = ["STOPPED", "START_PENDING", "STOP_PENDING", "RUNNING", "CONTINUE_PENDING", "PAUSE_PENDING", "PAUSED"];
        for (int state = 1; state <= labels.Length; state++)
        {
            if (ServiceRecovery.State(State(state) with { Output = $"SERVICE_NAME: Spooler\nSTATE : {state} {labels[state - 1]}" }) != state)
                throw new Exception("Recognized service state label was rejected.");
        }
        foreach (string invalid in new[] { "STATE : 4 STOPPED", "STATE : 1 RUNNING", "STATE : 4 UNKNOWN", "STATE : 4 RUNNING trailing" })
            if (ServiceRecovery.State(new(0, invalid, "", false)) is not null)
                throw new Exception("Contradictory or malformed service state label was accepted.");
        foreach (var (states, success) in new (ToolResult[] States, bool Success)[] {
            ([State(4), State(4)], true),
            ([State(2), State(4), State(4)], true),
            ([State(4), State(1)], false),
            ([State(4), State(4, "Other")], false),
            ([State(4), State(4) with { Truncated = true }], false),
            ([State(7)], false) })
        {
            int calls = 0;
            var observed = await ServiceRecovery.ObserveRunningAsync("Spooler", _ => Task.FromResult(states[calls++]));
            if ((observed.ExitCode == 0) != success || calls != states.Length)
                throw new Exception("Service observation accepted unstable or incomplete evidence.");
        }
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var starting = State(2);
        var expired = await ServiceRecovery.ObserveRunningAsync("Spooler", _ => Task.FromResult(starting), observationTimeout: TimeSpan.FromMilliseconds(30));
        if (!expired.TimedOut || expired.ExitCode == 0 || expired.Output != starting.Output || !expired.Error.Contains("current state is unverified"))
            throw new Exception("Observation timeout lost the last service evidence or claimed verification.");
        foreach (var invalid in new[] { TimeSpan.Zero, TimeSpan.FromSeconds(21) })
        {
            try
            {
                await ServiceRecovery.ObserveRunningAsync("Spooler", _ => throw new Exception("Invalid deadline reached query"), observationTimeout: invalid);
                throw new Exception("Invalid observation deadline accepted.");
            }
            catch (ArgumentOutOfRangeException) { }
        }
        try
        {
            await ServiceRecovery.ObserveRunningAsync("Spooler", _ => new TaskCompletionSource<ToolResult>().Task, token: stop.Token);
            throw new Exception("Uncooperative service query ignored cancellation.");
        }
        catch (OperationCanceledException) { }
        foreach (string failure in new[] { "null-task", "null-result", "sync-fault", "async-fault" })
        {
            int calls = 0;
            var observed = await ServiceRecovery.ObserveRunningAsync("Spooler", _ =>
            {
                calls++;
                if (calls == 1) return Task.FromResult(State(4));
                return failure switch
                {
                    "null-task" => null!,
                    "null-result" => Task.FromResult<ToolResult>(null!),
                    "sync-fault" => throw new InvalidOperationException("Fixture provider detail"),
                    _ => Task.FromException<ToolResult>(new InvalidOperationException("Fixture provider detail"))
                };
            });
            if (observed.ExitCode == 0 || observed.Output != State(4).Output || calls != 2 ||
                !observed.Error.Contains("provider failed") || observed.Error.Contains("Fixture provider detail"))
                throw new Exception("Failed service provider lost prior evidence or exposed an exception as verification.");
        }
        Console.WriteLine("PASS: service verification requires coherent state labels and two Running observations; stops, identity changes, provider failures, truncation and cancellation remain unverified (substituted queries).");
    }
}
