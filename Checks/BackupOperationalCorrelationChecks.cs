using KillerBench.Engine;

internal static class BackupOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete") && clear.Detail.Contains("no repair is selected"),
            "Complete backup prerequisites produce a bounded informational result");

        var attention = Check(writers: CheckState.Attention, storage: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("2 need attention") &&
            attention.Detail.Contains("restore-test evidence"), "Backup prerequisite attention remains review-only");

        var incomplete = Check(events: CheckState.Unknown, writers: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete backup evidence takes precedence over attention");

        Require(Invoke(backupEvents: [Info("Recent VSS events")]).State == CheckState.Unknown &&
            Invoke(serviceHistory: [Info("Windows Backup service failure history"), Info("Windows Backup service failure history")]).State == CheckState.Unknown &&
            Invoke(storageReadiness: []).State == CheckState.Unknown && Invoke(restartState: [Info("Restart"), Info("Restart")]).State == CheckState.Unknown,
            "Missing and duplicate backup prerequisite identities fail closed");

        try
        {
            Invoke(token: new CancellationToken(true));
            throw new Exception("Backup operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: backup operational correlation combines current state, exact history, storage and restart evidence without starting a backup or selecting repair.");
    }

    private static WorkflowCheck Check(CheckState events = CheckState.Information, CheckState writers = CheckState.Information,
        CheckState storage = CheckState.Information) => Invoke(
            backupEvents: [new("Recent VSS events", events, "fixture"), Info("Recent volsnap events")],
            writerEvidence: [new("VSS writers", writers, "fixture")],
            storageReadiness: [new("Storage operational correlation", storage, "fixture")]);

    private static WorkflowCheck Invoke(IReadOnlyList<WorkflowCheck>? backupEvents = null,
        IReadOnlyList<WorkflowCheck>? writerEvidence = null,
        IReadOnlyList<WorkflowCheck>? serviceHistory = null, IReadOnlyList<WorkflowCheck>? storageReadiness = null,
        IReadOnlyList<WorkflowCheck>? restartState = null, CancellationToken token = default) =>
        BackupOperationalCorrelation.Check(Info("Windows Backup history"), Info("File History protection state"),
            Info("Recent File History events"), backupEvents ?? [Info("Recent VSS events"), Info("Recent volsnap events")],
            writerEvidence ?? [Info("VSS writers")], Info("VSS provider registrations"), serviceHistory ??
            [Info("Windows Backup service failure history"), Info("Block Level Backup Engine failure history")],
            Info("VSS shadow copies"), [Info("System Restore points")],
            storageReadiness ?? [Info("Storage operational correlation")], restartState ?? [Info("Restart")], token);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
