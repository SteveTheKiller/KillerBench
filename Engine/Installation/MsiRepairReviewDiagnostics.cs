namespace KillerBench.Engine.Installation;

public static class MsiRepairReviewDiagnostics
{
    public static WorkflowCheck Check(string productCode, WorkflowCheck product, WorkflowCheck activity,
        WorkflowCheck restart, bool isAdministrator, CancellationToken token = default)
    {
        string target = MsiProductDiagnostics.NormalizeProductCode(productCode);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(restart);
        token.ThrowIfCancellationRequested();

        const string boundary = " This review does not run Windows Installer, authorize package repair, restart Windows or establish application health.";
        if (!MsiProductDiagnostics.TryGetReportedState(target, product, out int productState))
            return Unknown("Exact product registration evidence is unavailable, incomplete or does not match the selected product." + boundary);
        if (!MsiRepair.HasCompleteEvidence(activity) || activity.Evidence is null)
            return Unknown("Current Windows Installer activity evidence is unavailable or incomplete." + boundary);
        if (!MsiRepair.HasCompleteEvidence(restart) || restart.Evidence is null)
            return Unknown("Current restart evidence is unavailable or incomplete." + boundary);

        WorkflowCheck observedActivity = InstallerActivityDiagnostics.Interpret(activity.Evidence);
        WorkflowCheck observedRestart = RestartDiagnostics.Interpret(restart.Evidence);
        if (observedActivity.State == CheckState.Unknown)
            return Unknown("Current Windows Installer activity evidence could not be interpreted completely." + boundary);
        if (observedRestart.State == CheckState.Unknown)
            return Unknown("Current restart evidence could not be interpreted completely." + boundary);
        bool repairCandidate = productState is 0 or -6;
        if (!repairCandidate)
            return new("Selected MSI repair review", CheckState.Information,
                $"Exact product: {target}. Windows Installer reported state {productState}, which is outside the guarded Broken or Bad configuration repair boundary. No repair is offered." + boundary);

        var blockers = new List<string>();
        if (observedActivity.State != CheckState.Information)
            blockers.Add("Windows Installer activity is present or could not be shown idle");
        if (observedRestart.State != CheckState.Clear)
            blockers.Add("restart state is pending or unclear");
        if (!isAdministrator)
            blockers.Add("administrator access is unavailable");
        if (blockers.Count > 0)
            return new("Selected MSI repair review", CheckState.Attention,
                $"Exact product: {target}. Windows Installer reported state {productState}, but guarded repair is currently blocked because {string.Join("; ", blockers)}. Recollect current evidence before considering a package action." + boundary);

        return new("Selected MSI repair review", CheckState.Attention,
            $"Exact product: {target}. Windows Installer reported state {productState}; activity is idle, restart evidence is clear and administrator access is available. A separate guarded repair can repeat these prerequisites, prepare durable evidence, request one bounded full-file repair and verify the same registration after explicit confirmation. Vendor custom actions may still change product settings or user data." + boundary);
    }

    private static WorkflowCheck Unknown(string detail) =>
        new("Selected MSI repair review", CheckState.Unknown, detail);
}
