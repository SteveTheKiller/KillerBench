namespace KillerBench.Engine;

public static class InstallerPendingOperationCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck pendingOperations, WorkflowCheck installerActivity,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pendingOperations);
        ArgumentNullException.ThrowIfNull(installerActivity);
        const string scope = " This correlation does not identify which application owns an operation and does not authorize clearing a registration, ending a process, retrying an installation or restarting Windows. No file, process, package, registration or Installer setting changed.";
        if (pendingOperations.Name != "Pending file operations" || installerActivity.Name != "Installer activity" ||
            !Complete(pendingOperations) || !Complete(installerActivity) ||
            pendingOperations.State is CheckState.Unknown or CheckState.Repairable ||
            installerActivity.State is CheckState.Unknown or CheckState.Repairable)
            return new("Installer pending-operation correlation", CheckState.Unknown,
                "Complete pending-file and Installer-activity evidence was not available. Do not infer that a restart or retry is safe." + scope);

        if (pendingOperations.State == CheckState.Information)
            return new("Installer pending-operation correlation", CheckState.Information,
                "No pending file operation was registered, so no pending-operation action is recommended from this snapshot. Installer activity remains a separate observation." + scope);

        if (pendingOperations.State != CheckState.Attention)
            return new("Installer pending-operation correlation", CheckState.Unknown,
                "Pending file operation evidence had an unsupported state. Restart and retry guidance remains unavailable." + scope);

        if (installerActivity.State == CheckState.Attention)
            return new("Installer pending-operation correlation", CheckState.Attention,
                "Pending file operations coexist with an Installer process or transaction marker. Allow known deployment work to finish, then collect fresh Installer activity and pending-operation evidence before deciding whether to restart or retry." + scope);

        if (installerActivity.State == CheckState.Information)
            return new("Installer pending-operation correlation", CheckState.Attention,
                "Pending file operations remain, while this bounded snapshot observed no Installer process or transaction marker. A normal user-controlled restart followed by fresh collection is the next supported step before related installation or servicing work." + scope);

        return new("Installer pending-operation correlation", CheckState.Unknown,
            "Installer activity evidence had an unsupported state. Restart and retry guidance remains unavailable." + scope);
    }

    private static bool Complete(WorkflowCheck check) => check.Evidence is
    {
        ExitCode: 0,
        TimedOut: false,
        Truncated: false
    } evidence && string.IsNullOrWhiteSpace(evidence.Error);
}
