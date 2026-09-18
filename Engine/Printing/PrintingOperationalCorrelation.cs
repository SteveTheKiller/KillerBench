namespace KillerBench.Engine;

public static class PrintingOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck spooler, WorkflowCheck defaultPrinter,
        IReadOnlyList<WorkflowCheck> queueConditions, IReadOnlyList<WorkflowCheck> queueAttributes,
        IReadOnlyList<WorkflowCheck> jobs, WorkflowCheck spoolDirectory, WorkflowCheck environment, WorkflowCheck spoolerHistory,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(spooler);
        ArgumentNullException.ThrowIfNull(defaultPrinter);
        ArgumentNullException.ThrowIfNull(queueConditions);
        ArgumentNullException.ThrowIfNull(queueAttributes);
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(spoolDirectory);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(spoolerHistory);
        token.ThrowIfCancellationRequested();
        if (spooler.Name != RepairKind.PrintSpooler.ToString() || defaultPrinter.Name != "Default printer selection" ||
            spoolDirectory.Name != "Print spool directory" || environment.Name != "Printing environment correlation" ||
            spoolerHistory.Name != "Print Spooler failure history" ||
            !ValidGroup(queueConditions, 256) || !ValidGroup(queueAttributes, 256) || !ValidGroup(jobs, 256))
            return Unknown("The expected current Print Spooler, queue, job, spool-directory or exact service-history evidence was incomplete or inconsistent.");

        var current = new[] { spooler, defaultPrinter, spoolDirectory, environment }
            .Concat(queueConditions).Concat(queueAttributes).Concat(jobs).ToArray();
        int incomplete = current.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = current.Count(check => NeedsAttention(check.EffectiveState));
        bool historyIncomplete = spoolerHistory.EffectiveState == CheckState.Unknown;
        bool historyAttention = NeedsAttention(spoolerHistory.EffectiveState);
        string counts = $"Reviewed the current Print Spooler, default selection, {queueConditions.Count} queue-condition results, {queueAttributes.Count} queue-attribute results, {jobs.Count} visible job results, spool-directory state, printing environment and exact service history. Current evidence: {attention} need attention and {incomplete} incomplete. Service history: {(historyIncomplete ? "incomplete" : historyAttention ? "review" : "clear or informational")}.";
        const string boundary = " Queue, job and service observations are point-in-time evidence. Historical failures do not establish a current outage. This does not print a page or prove driver, port, application, network, device or physical-printer operation. No service, queue, job, document, driver, port, processor, monitor, default selection, directory, file or print setting changed, and no repair is selected.";
        if (incomplete > 0 || historyIncomplete)
            return new("Printing operational correlation", CheckState.Unknown,
                counts + " Printing operation cannot be fully correlated until every required observation is complete." + boundary);
        if (attention > 0)
            return new("Printing operational correlation", CheckState.Attention,
                counts + " Review the exact underlying current and historical results together before selecting a focused action." + boundary);
        if (historyAttention)
            return new("Printing operational correlation", CheckState.Information,
                counts + " Current evidence did not establish a printing problem. Review retained service history only when it matches the time and symptom." + boundary);
        return new("Printing operational correlation", CheckState.Clear,
            counts + " No issue was established within this bounded printing evidence." + boundary);
    }

    private static bool ValidGroup(IReadOnlyList<WorkflowCheck> checks, int maximum) =>
        checks.Count is > 0 && checks.Count <= maximum && checks.All(check => check is not null);

    private static bool NeedsAttention(CheckState state) => state is CheckState.Attention or CheckState.Repairable;

    private static WorkflowCheck Unknown(string detail) => new("Printing operational correlation", CheckState.Unknown,
        detail + " No repair is selected and no service, queue, job, document, driver, port, directory, file or print setting changed.");
}
