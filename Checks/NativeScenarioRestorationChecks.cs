using System.IO;
using KillerBench.Engine.Windows.Validation;

internal static class NativeScenarioRestorationChecks
{
    internal static async Task RunAsync()
    {
        var service = new Service { FailDisable = true };
        var scenario = new NativeServiceScenario(service, await service.SnapshotAsync(default));
        if (!await scenario.StopAsync(default) || await scenario.DisableAsync(default)) throw new Exception("Partial injection fixture did not stop then fail disable.");
        if (!await scenario.RestoreAsync() || service.State != 4 || service.Start != 3)
            throw new Exception("Partial injection left the original running service stopped.");

        foreach (string interruption in new[] { "cancel", "timeout", "exception" })
        {
            using var cancellation = new CancellationTokenSource();
            service = new Service { StopInterruption = interruption, Cancellation = cancellation };
            scenario = new NativeServiceScenario(service, await service.SnapshotAsync(default));
            try { await scenario.StopAsync(cancellation.Token); }
            catch (OperationCanceledException) when (interruption == "cancel") { }
            catch (IOException) when (interruption == "exception") { }
            if (!await scenario.RestoreAsync() || service.State != 4 || service.CanceledRestoration)
                throw new Exception("Post-stop " + interruption + " did not restore independently of scenario cancellation.");
            if (!scenario.Stages.Any(stage => stage.Contains("restoration required", StringComparison.Ordinal)))
                throw new Exception("The interrupted mutating request was not retained in stage evidence.");
        }

        foreach (int originalStart in new[] { 2, 3, 4 })
        {
            service = new Service { State = 1, Start = originalStart, Delayed = originalStart == 2 };
            scenario = new NativeServiceScenario(service, await service.SnapshotAsync(default));
            await scenario.StopAsync(default);
            await scenario.DisableAsync(default);
            // A substituted successful recipe starts a previously stopped, enabled service.
            service.State = 4;
            if (!await scenario.RestoreAsync() || service.State != 1 || service.Start != originalStart || service.Delayed != (originalStart == 2))
                throw new Exception("The original stopped service runtime/configuration baseline was not restored.");
        }

        service = new Service();
        scenario = new NativeServiceScenario(service, await service.SnapshotAsync(default));
        await scenario.StopAsync(default);
        service.IgnoreStart = true;
        if (await scenario.RestoreAsync()) throw new Exception("A successful start command hid failed runtime restoration.");

        service = new Service();
        scenario = new NativeServiceScenario(service, await service.SnapshotAsync(default));
        await scenario.StopAsync(default);
        await scenario.DisableAsync(default);
        service.FailConfiguration = true;
        if (await scenario.RestoreAsync() || service.State != 4)
            throw new Exception("Failed configuration restoration hid failure or prevented independent runtime restoration.");

        foreach (var baseline in new[] { (State: (int?)2, Start: (int?)3), (State: (int?)null, Start: (int?)3), (State: (int?)4, Start: (int?)4), (State: (int?)1, Start: (int?)0), (State: (int?)1, Start: (int?)null) })
        {
            service = new Service();
            scenario = new NativeServiceScenario(service, new NativeServiceSnapshot(baseline.State, baseline.Start, false, "unsupported"));
            try { await scenario.StopAsync(default); throw new Exception("An unsupported baseline accepted injection."); }
            catch (InvalidOperationException) { }
            if (service.Mutations != 0) throw new Exception("Unsupported baseline was rejected after mutation.");
        }

        scenario = new NativeServiceScenario(service, new NativeServiceSnapshot(1, 3, true, "retained delayed flag on demand start"));
        if (scenario.SupportedBaseline) throw new Exception("A delayed flag outside automatic startup was accepted without a restoration boundary for that combination.");
    }

    private sealed class Service : INativeScenarioService
    {
        public int State = 4;
        public int Start = 3;
        public bool FailDisable;
        public bool Delayed;
        public string? StopInterruption;
        public CancellationTokenSource? Cancellation;
        public bool IgnoreStart;
        public bool FailConfiguration;
        public bool CanceledRestoration;
        public int Mutations;
        public Task<NativeServiceSnapshot> SnapshotAsync(CancellationToken token) => Task.FromResult(new NativeServiceSnapshot(State, Start, Delayed, "substituted service"));
        public Task<bool> StopAsync(CancellationToken token)
        {
            Mutations++;
            State = 1;
            string? interruption = StopInterruption;
            StopInterruption = null;
            if (interruption == "cancel")
            {
                Cancellation!.Cancel();
                token.ThrowIfCancellationRequested();
            }
            if (interruption == "exception") throw new IOException("Substituted native stop failure after dispatch.");
            return Task.FromResult(interruption != "timeout");
        }
        public Task<bool> SetStartAsync(string start, CancellationToken token)
        {
            Mutations++;
            CanceledRestoration |= token.IsCancellationRequested;
            if (FailConfiguration) throw new IOException("Substituted configuration failure.");
            if (start == "disabled" && FailDisable) return Task.FromResult(false);
            Start = start switch { "disabled" => 4, "demand" => 3, _ => 2 };
            Delayed = start == "delayed-auto";
            return Task.FromResult(true);
        }
        public Task<bool> StartAsync(CancellationToken token)
        {
            Mutations++;
            CanceledRestoration |= token.IsCancellationRequested;
            if (!IgnoreStart) State = 4;
            return Task.FromResult(true);
        }
    }
}
