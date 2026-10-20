namespace KillerBench.Engine.Windows.Validation;

public sealed record NativeServiceSnapshot(int? State, int? Start, bool Delayed, string Text);

/// <summary>The operating-system boundary used by service fault-injection validation.</summary>
public interface INativeScenarioService
{
    Task<NativeServiceSnapshot> SnapshotAsync(CancellationToken token);
    Task<bool> StopAsync(CancellationToken token);
    Task<bool> SetStartAsync(string start, CancellationToken token);
    Task<bool> StartAsync(CancellationToken token);
}

/// <summary>Tracks a service scenario's restoration responsibility independently of injection success.</summary>
public sealed class NativeServiceScenario(INativeScenarioService service, NativeServiceSnapshot before)
{
    private bool responsible;
    private readonly List<string> stages = [];
    public IReadOnlyList<string> Stages => stages.AsReadOnly();
    public bool SupportedBaseline => before.State is 1 or 4 && before.Start is 2 or 3 or 4 &&
        !(before.State == 4 && before.Start == 4) && (!before.Delayed || before.Start == 2);

    public async Task<bool> StopAsync(CancellationToken token)
    {
        BeginMutation("Stop");
        try
        {
            bool stopped = await service.StopAsync(token).ConfigureAwait(false);
            stages.Add("Stop completed: " + stopped);
            return stopped;
        }
        catch (Exception exception)
        {
            stages.Add("Stop interrupted: " + exception.GetType().Name);
            throw;
        }
    }

    public async Task<bool> DisableAsync(CancellationToken token)
    {
        BeginMutation("Disable");
        try
        {
            bool disabled = await service.SetStartAsync("disabled", token).ConfigureAwait(false);
            stages.Add("Disable completed: " + disabled);
            return disabled;
        }
        catch (Exception exception)
        {
            stages.Add("Disable interrupted: " + exception.GetType().Name);
            throw;
        }
    }

    private void BeginMutation(string stage)
    {
        if (!SupportedBaseline) throw new InvalidOperationException("The service baseline cannot be safely restored.");
        responsible = true;
        stages.Add(stage + " requested; restoration required.");
    }

    public async Task<bool> RestoreAsync()
    {
        if (!responsible) return true;
        string? start = before.Start switch { 2 => before.Delayed ? "delayed-auto" : "auto", 3 => "demand", 4 => "disabled", _ => null };
        bool ok = await AttemptAsync("Restore configuration", () => service.SetStartAsync(start!, CancellationToken.None)).ConfigureAwait(false);
        ok &= await AttemptAsync("Restore runtime state", () => before.State == 4 ? service.StartAsync(CancellationToken.None) : service.StopAsync(CancellationToken.None)).ConfigureAwait(false);
        try
        {
            var after = await service.SnapshotAsync(CancellationToken.None).ConfigureAwait(false);
            bool verified = after.State == before.State && after.Start == before.Start && after.Delayed == before.Delayed;
            stages.Add("Restoration verified: " + verified + "; " + after.Text);
            return ok && verified;
        }
        catch (Exception exception)
        {
            stages.Add("Restoration verification failed: " + exception.GetType().Name);
            return false;
        }
    }

    private async Task<bool> AttemptAsync(string stage, Func<Task<bool>> action)
    {
        stages.Add(stage + " requested.");
        try
        {
            bool ok = await action().ConfigureAwait(false);
            stages.Add(stage + " completed: " + ok);
            return ok;
        }
        catch (Exception exception)
        {
            stages.Add(stage + " failed: " + exception.GetType().Name);
            return false;
        }
    }
}
