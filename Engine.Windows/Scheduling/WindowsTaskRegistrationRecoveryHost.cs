namespace KillerBench.Engine.Windows.Scheduling;

public sealed class WindowsTaskRegistrationRecoveryHost : ITaskRegistrationRecoveryHost
{
    public Task<ScheduledTaskDefinitionResult> ReadAsync(ScheduledTaskIdentity target, CancellationToken token) =>
        ScheduledTaskDefinition.ReadAsync(target, token);
}
