using System.IO;
using KillerBench.Engine;

internal static class FocusedWindowsUpdateOperationalCorrelationChecks
{
    public static void Run(string scratch)
    {
        var clear = FocusedWindowsUpdateOperationalCorrelation.Check(Evidence());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete") &&
            clear.Detail.Contains("does not test external update-source or Internet reachability"),
            "Complete focused local Windows Update evidence produces one bounded informational conclusion");

        var attention = FocusedWindowsUpdateOperationalCorrelation.Check(Evidence(restart: CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("1 need attention") &&
            attention.Detail.Contains("before searching for updates"),
            "Current focused update attention remains review-only");

        var incomplete = FocusedWindowsUpdateOperationalCorrelation.Check(Evidence(reserved: CheckState.Unknown, restart: CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete focused update evidence takes precedence over attention");

        var missing = Evidence().Skip(1).ToArray();
        var duplicated = Evidence().Append(Evidence()[0]).ToArray();
        Require(FocusedWindowsUpdateOperationalCorrelation.Check(missing).State == CheckState.Unknown &&
            FocusedWindowsUpdateOperationalCorrelation.Check(duplicated).State == CheckState.Unknown,
            "Missing and duplicate focused update identities fail closed");

        try
        {
            FocusedWindowsUpdateOperationalCorrelation.Check(Evidence(), new CancellationToken(true));
            throw new Exception("Focused Windows Update correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }

        string folder = Path.Combine(scratch, "focused-windows-update-correlation");
        var workflow = Workflows.RunAsync(WorkflowKind.WindowsUpdate, false, folder,
            host: new WorkflowHost()).GetAwaiter().GetResult();
        Require(workflow.Checks.Single(check => check.Name == "Windows Update focused conclusion") is
            { State: CheckState.Information, Repair: null } && workflow.Recipes.Count == RecipeCatalog.ForWorkflow(WorkflowKind.WindowsUpdate).Count &&
            workflow.Repairs.Count == 0,
            "Focused Windows Update appends exactly one local operational conclusion during diagnosis");

        var blocked = Workflows.RunAsync(WorkflowKind.WindowsUpdate, false, folder,
            host: new WorkflowHost(blocked: true)).GetAwaiter().GetResult();
        Require(blocked.Checks.Single(check => check.Name == "Windows Update focused conclusion") is
            { State: CheckState.Attention, Repair: null } && blocked.Recipes.Count == 0 && blocked.Repairs.Count == 0,
            "A blocked focused update recipe pass retains one prerequisite conclusion without repair");

        Console.WriteLine("PASS: focused Windows Update produces one bounded local conclusion without claiming external source or Internet reachability.");
    }

    private static WorkflowCheck[] Evidence(CheckState reserved = CheckState.Information, CheckState restart = CheckState.Clear) =>
    [
        new("Windows Update servicing-chain correlation", CheckState.Clear, "fixture"),
        new("Windows Update engine files", CheckState.Information, "fixture"),
        new("Restart", restart, "fixture"),
        new("Servicing transition readiness", CheckState.Information, "fixture"),
        new("System disk", CheckState.Clear, "fixture"),
        new("Windows reserved storage", reserved, "fixture"),
        new("BITS transfer jobs", CheckState.Information, "fixture"),
        new("Update policy", CheckState.Information, "fixture"),
        new("Component repair source", CheckState.Information, "fixture")
    ];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class WorkflowHost(bool blocked = false) : IWorkflowHost
    {
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token)
        {
            var evidence = Evidence();
            if (blocked) evidence[2] = new("Restart", CheckState.Attention, "fixture");
            return Task.FromResult<IReadOnlyList<WorkflowCheck>>(evidence);
        }

        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Clear, "fixture"));

        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Read-only focused Windows Update workflow dispatched a repair");
    }
}
