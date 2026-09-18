using KillerBench.Engine;

internal static class FocusedBackupOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = FocusedBackupOperationalCorrelation.Check(Evidence());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("15 exact") &&
            clear.Detail.Contains("restore readiness remain unverified"),
            "Complete focused backup evidence produces a bounded informational conclusion");

        var history = FocusedBackupOperationalCorrelation.Check(Evidence("Recent VSS events", CheckState.Attention));
        Require(history is { State: CheckState.Information, Repair: null } && history.Detail.Contains("Historical observations needing review: 1") &&
            history.Detail.Contains("Historical records alone"), "Historical backup evidence alone does not become a current fault");

        var attention = FocusedBackupOperationalCorrelation.Check(Evidence("VSS writers", CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("Current evidence: 1 need attention"),
            "Current focused backup attention remains review-only");

        var incomplete = FocusedBackupOperationalCorrelation.Check(Evidence("VSS shadow storage", CheckState.Unknown));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete focused backup evidence takes precedence");

        Require(FocusedBackupOperationalCorrelation.Check(Evidence().Skip(1).ToArray()).State == CheckState.Unknown &&
            FocusedBackupOperationalCorrelation.Check(Evidence().Append(Evidence()[0]).ToArray()).State == CheckState.Unknown,
            "Missing and duplicate focused backup identities fail closed");

        try { FocusedBackupOperationalCorrelation.Check(Evidence(), new CancellationToken(true)); throw new Exception("Backup correlation cancellation ignored"); }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: focused backup-service evidence separates current state from history without claiming backup success or selecting repair.");
    }

    private static WorkflowCheck[] Evidence(string? changed = null, CheckState state = CheckState.Information)
    {
        string[] names =
        [
            "Windows Backup history", "File History protection state", "Recent File History events",
            "Recent VSS events", "Recent volsnap events", "VSS writers", "VSS provider registrations",
            "Volume Shadow Copy failure history", "Software Shadow Copy Provider failure history",
            "Windows Backup service failure history", "Block Level Backup Engine failure history",
            "VSS shadow copies", "VSS shadow storage", "Administrator", "Restart"
        ];
        return names.Select(name => new WorkflowCheck(name, name == changed ? state : CheckState.Information, "fixture")).ToArray();
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
