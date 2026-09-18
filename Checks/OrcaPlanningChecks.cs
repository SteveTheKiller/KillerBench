using KillerBench.Engine;

internal static class OrcaPlanningChecks
{
    internal static void Run()
    {
        var options = new OrcaReviewOptions(OrcaOutlookMode.Both, OrcaScope.RunningAccount,
            OrcaActions.ClearCaches | OrcaActions.DeleteOstFiles | OrcaActions.RemoveAddIns);
        var evidence = new OrcaEvidenceSnapshot([
            new("New Outlook registration", "RunningAccount", true, 1, "Exact package found", true),
            new("Classic Outlook registration", "Machine", true, 1, "Application path found", true),
            new("Classic Outlook OST files", "RunningAccount", true, 2, "Cache files found", true),
            new("Classic Outlook user add-ins", "RunningAccount", false, 0, "No entries found", true)],
            true, "Mailbox contents were not inspected.");
        var review = OrcaPlanning.Review(options, evidence, new(0, 0, true, "Counts only."));
        Require(review.Applicable && review.Complete && !review.ExecutionSupported,
            "Exact Outlook evidence produces a complete review without execution");
        Require(review.Checks.Any(check => check.Name == "Classic Outlook OST files" && check.State == CheckState.Information),
            "Selected Classic Outlook evidence is retained");
        Require(review.Format().Contains("Execution available: No") && review.Checks.All(check => check.Repair is null),
            "ORCA review never offers a repair");
        Require(review.ResetReadiness is { ApplicationsClosed: true, BackupRequired: true, Complete: true } &&
            review.Checks.Any(check => check.Name == "ORCA backup prerequisite" && check.State == CheckState.Attention),
            "Destructive ORCA selections require closed applications and a verified backup before future execution");

        var active = OrcaPlanning.Review(options, evidence, new(1, 2, true, "Counts only."));
        Require(active.ResetReadiness is { ApplicationsClosed: false } &&
            active.Checks.Any(check => check.Name == "Outlook process readiness" && check.State == CheckState.Attention &&
                check.Detail.Contains("3 selected Outlook processes")),
            "Active selected Outlook editions block reset readiness without retaining process identities");

        var processEvidence = OrcaProcessInventory.Read(OrcaOutlookMode.Both, name => name == "olk" ? 1 : 2);
        Require(processEvidence.Complete && processEvidence.NewOutlookProcesses == 1 &&
            processEvidence.ClassicOutlookProcesses == 2 && processEvidence.Limitation.Contains("Process IDs"),
            "ORCA process inventory retains bounded counts and an explicit privacy boundary");
        Require(!OrcaProcessInventory.Read(OrcaOutlookMode.New, _ => 129).Complete &&
            !OrcaProcessInventory.Read(OrcaOutlookMode.Classic, _ => throw new InvalidOperationException()).Complete,
            "Oversized and failed process evidence remains incomplete");
        using (var stopped = new CancellationTokenSource())
        {
            stopped.Cancel();
            try
            {
                OrcaProcessInventory.Read(OrcaOutlookMode.Both, _ => throw new Exception("Canceled provider ran"), stopped.Token);
                throw new Exception("Canceled ORCA process inventory continued.");
            }
            catch (OperationCanceledException) { }
        }

        var incomplete = OrcaPlanning.Review(options, evidence with
        {
            Complete = false,
            Items = [new("Classic Outlook stored credentials", "RunningAccount", null, 0, "Not enumerated", false)]
        });
        Require(!incomplete.Complete && !incomplete.Applicable &&
            incomplete.Checks.Any(check => check.State == CheckState.Unknown),
            "Unknown sensitive evidence cannot establish absence or complete coverage");

        var classic = new OrcaReviewOptions(OrcaOutlookMode.Classic, OrcaScope.RunningAccount,
            OrcaActions.ClearCaches);
        var filtered = OrcaPlanning.Review(classic, evidence);
        Require(filtered.Checks.All(check => !check.Name.StartsWith("New Outlook", StringComparison.Ordinal)),
            "Classic review excludes New Outlook evidence rows");
        var cacheOnly = OrcaPlanning.Review(new(OrcaOutlookMode.Both, OrcaScope.RunningAccount, OrcaActions.ClearCaches),
            evidence, new(0, 0, true, "Counts only."));
        Require(cacheOnly.ExecutionSupported && cacheOnly.Format().Contains("Execution available: Cache retention only") &&
            cacheOnly.Checks.Any(check => check.Name == "Requested ORCA actions" && check.Detail.Contains("guarded reset action")) &&
            cacheOnly.Checks.Any(check => check.Name == "ORCA backup prerequisite" && check.State == CheckState.Information),
            "Complete cache-only review exposes the guarded retention action without widening other ORCA execution");
        var activeCache = OrcaPlanning.Review(new(OrcaOutlookMode.Both, OrcaScope.RunningAccount, OrcaActions.ClearCaches),
            evidence, new(1, 0, true, "Counts only."));
        Require(!activeCache.ExecutionSupported, "Active Outlook blocks cache-only execution");
        ExpectInvalid(new(OrcaOutlookMode.New, OrcaScope.RunningAccount, OrcaActions.DeleteOstFiles));
        ExpectInvalid(new(OrcaOutlookMode.Classic, OrcaScope.RunningAccount, OrcaActions.ReinstallNewOutlook));
        ExpectInvalid(new(OrcaOutlookMode.Both, OrcaScope.RunningAccount, OrcaActions.None));
        Console.WriteLine("PASS: ORCA edition, scope, action, process privacy, backup readiness and guarded cache-only execution boundary.");
    }

    private static void ExpectInvalid(OrcaReviewOptions options)
    {
        try { options.Validate(); throw new Exception("Invalid ORCA options were accepted."); }
        catch (ArgumentException) { }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
