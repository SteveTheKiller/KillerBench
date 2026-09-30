using System.IO;
using KillerBench.Engine;

internal static class MacePackageRemovalChecks
{
    private const string Identity = "Microsoft.OutlookForWindows_1.0.0.0_x64__8wekyb3d8bbwe";

    internal static async Task RunAsync(string scratch)
    {
        MaceRemovalReview eligible = CreateReview(packagePresent: true);
        MacePackageRemovalEligibility eligibility = MacePackageRemoval.Evaluate(eligible, Identity);
        Require(eligibility is { Eligible: true, Candidate.Target: MaceTargets.Outlook } &&
            eligibility.Candidate.Identity == Identity,
            "Exact current-account package and clear safeguards establish bounded eligibility");
        Require(!MacePackageRemoval.Evaluate(CreateReview(packagePresent: true, active: true), Identity).Eligible &&
            !MacePackageRemoval.Evaluate(CreateReview(packagePresent: true, managed: true), Identity).Eligible &&
            !MacePackageRemoval.Evaluate(CreateReview(packagePresent: true, complete: false), Identity).Eligible,
            "Active applications, managed configuration and incomplete evidence fail closed");
        Require(!MacePackageRemoval.Evaluate(eligible with
            {
                Options = eligible.Options with { PreserveApplicationData = false }
            }, Identity).Eligible &&
            !MacePackageRemoval.Evaluate(eligible, "MSTeams_1.0.0.0_x64__8wekyb3d8bbwe").Eligible,
            "Data-removal intent and absent exact identity cannot widen eligibility");

        string root = Path.Combine(scratch, "mace-package-removal");
        Directory.CreateDirectory(root);
        var previewHost = new FakeHost([eligible]);
        MacePackageRemovalResult preview = await MacePackageRemoval.RunAsync(eligible.Options, Identity,
            false, Path.Combine(root, "preview"), previewHost);
        Require(preview.Outcome == MacePackageRemovalOutcome.Preview && previewHost.RemoveCalls == 0,
            "Preview performs no removal request");
        var wrongSelectionHost = new FakeHost([eligible with
        {
            Options = eligible.Options with { PreserveCachedCredentials = false }
        }]);
        MacePackageRemovalResult wrongSelection = await MacePackageRemoval.RunAsync(eligible.Options, Identity,
            false, Path.Combine(root, "wrong-selection"), wrongSelectionHost);
        Require(wrongSelection.Outcome == MacePackageRemovalOutcome.Blocked && wrongSelectionHost.RemoveCalls == 0,
            "Evidence returned for a different selection cannot authorize a request");

        var verifiedHost = new FakeHost([eligible, eligible, eligible, CreateReview(packagePresent: false)]);
        string verifiedRoot = Path.Combine(root, "verified");
        MacePackageRemovalResult verified = await MacePackageRemoval.RunAsync(eligible.Options, Identity,
            true, verifiedRoot, verifiedHost);
        Require(verified.Outcome == MacePackageRemovalOutcome.Verified && verifiedHost.RemoveCalls == 1 &&
            !File.Exists(Path.Combine(verifiedRoot, MacePackageRemoval.PendingFileName)) &&
            Directory.GetFiles(verifiedRoot, "*.prepared.json").Length == 1,
            "Execution repeats exact eligibility, records preparation, dispatches once and verifies absence");

        var changedHost = new FakeHost([eligible, eligible, CreateReview(packagePresent: true, active: true)]);
        string changedRoot = Path.Combine(root, "changed");
        MacePackageRemovalResult changed = await MacePackageRemoval.RunAsync(eligible.Options, Identity,
            true, changedRoot, changedHost);
        Require(changed.Outcome == MacePackageRemovalOutcome.Blocked && changedHost.RemoveCalls == 0 &&
            !File.Exists(Path.Combine(changedRoot, MacePackageRemoval.PendingFileName)),
            "Changed final evidence closes preparation without dispatch");

        var uncertainHost = new FakeHost([eligible, eligible, eligible, CreateReview(packagePresent: true, complete: false)]);
        string uncertainRoot = Path.Combine(root, "uncertain");
        MacePackageRemovalResult uncertain = await MacePackageRemoval.RunAsync(eligible.Options, Identity,
            true, uncertainRoot, uncertainHost);
        Require(uncertain.Outcome == MacePackageRemovalOutcome.Uncertain && uncertainHost.RemoveCalls == 1 &&
            File.Exists(Path.Combine(uncertainRoot, MacePackageRemoval.PendingFileName)),
            "Incomplete post-dispatch evidence preserves the no-repeat guard");
        var reconcileHost = new FakeHost([CreateReview(packagePresent: false)]);
        MacePackageRemovalReconciliationResult reconciled = await MacePackageRemoval.ReconcileAsync(
            uncertainRoot, reconcileHost);
        Require(reconciled.Outcome == MacePackageRemovalReconciliationOutcome.Verified &&
            reconcileHost.RemoveCalls == 0 && !File.Exists(Path.Combine(uncertainRoot, MacePackageRemoval.PendingFileName)),
            "Reconciliation verifies fresh absence and never repeats removal");

        var notAppliedHost = new FakeHost([eligible, eligible, eligible, eligible]);
        MacePackageRemovalResult notApplied = await MacePackageRemoval.RunAsync(eligible.Options, Identity,
            true, Path.Combine(root, "not-applied"), notAppliedHost);
        Require(notApplied.Outcome == MacePackageRemovalOutcome.NotApplied && notAppliedHost.RemoveCalls == 1,
            "A package that remains registered is reported honestly without retry");

        var canceled = new CancellationToken(true);
        try
        {
            await MacePackageRemoval.RunAsync(eligible.Options, Identity, false,
                Path.Combine(root, "canceled"), new FakeHost([eligible]), token: canceled);
            throw new Exception("Canceled app removal package review completed");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: Microsoft app removal exact current-account package eligibility, durable preparation, final evidence, verification and no-repeat reconciliation; simulated provider only.");
    }

    internal static MaceRemovalReview CreateReview(bool packagePresent, bool active = false,
        bool managed = false, bool complete = true)
    {
        var evidence = new ToolResult(0, "{}", "", false);
        var options = new MaceReviewOptions(MaceTargets.Outlook);
        var machine = new InstalledApplicationInventory([], complete, evidence);
        var account = new InstalledApplicationInventory([], complete, evidence)
            { Scope = InstalledApplicationScope.CurrentAccount };
        var packages = new AppPackageInventoryResult(AppPackageScope.CurrentAccount,
            packagePresent ? [new AppPackage("Microsoft.OutlookForWindows", Identity,
                "Microsoft.OutlookForWindows_8wekyb3d8bbwe", false, false) { DevelopmentMode = true }] : [], complete, evidence);
        var processes = new MaceProcessEvidence(0, active ? 1 : 0, 0, 0, 0, complete, "fixture");
        var policy = MaceManagedPolicyReview.Analyze(MaceScope.RunningAccount, MaceTargets.Outlook,
            [new(MaceTargets.Outlook, "Outlook policy", managed, false)], complete);
        return MacePlanning.Review(options, machine, account, packages,
            clickToRun: new([], complete), processes: processes, managedPolicy: policy);
    }

    private sealed class FakeHost(IReadOnlyList<MaceRemovalReview> reviews) : IMacePackageRemovalHost
    {
        private int index;
        public int RemoveCalls { get; private set; }

        public Task<MaceRemovalReview> ReviewAsync(MaceReviewOptions options, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (index >= reviews.Count) throw new InvalidOperationException("No review fixture remains.");
            return Task.FromResult(reviews[index++]);
        }

        public Task<ToolResult> RemoveCurrentAccountPackageAsync(string packageFullName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Require(packageFullName == Identity, "Only the exact selected package reaches the provider");
            RemoveCalls++;
            return Task.FromResult(new ToolResult(0, "", "", false));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
