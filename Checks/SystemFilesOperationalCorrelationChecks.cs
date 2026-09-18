using System.IO;
using KillerBench.Engine;

internal static class SystemFilesOperationalCorrelationChecks
{
    public static void Run(string scratch)
    {
        var clear = Result(RecipeOutcome.Clear);
        var historical = SystemFilesOperationalCorrelation.Check(clear, Evidence(cbs: CheckState.Attention));
        Require(historical is { State: CheckState.Information, Repair: null } && historical.Detail.Contains("Historical findings: 1") &&
            historical.Detail.Contains("historical context"),
            "Historical CBS findings alone do not become current protected-file corruption");

        var available = SystemFilesOperationalCorrelation.Check(Result(RecipeOutcome.RepairAvailable), Evidence());
        Require(available is { State: CheckState.Attention, Repair: null } && available.Detail.Contains("Review the current protected-file result"),
            "A current protected-file issue remains attention without selecting another repair");

        var repaired = SystemFilesOperationalCorrelation.Check(Result(RecipeOutcome.Repaired), Evidence(cbs: CheckState.Attention));
        Require(repaired is { State: CheckState.Information, Repair: null } && repaired.Detail.Contains("verified completion"),
            "A verified protected-file repair is reflected without turning retained history into failure");

        Require(SystemFilesOperationalCorrelation.Check(Result(RecipeOutcome.Blocked, includeCheck: false), Evidence()).State == CheckState.Unknown &&
            SystemFilesOperationalCorrelation.Check(clear, Evidence(reserved: CheckState.Unknown)).State == CheckState.Unknown &&
            SystemFilesOperationalCorrelation.Check(clear, Evidence(dism: CheckState.Unknown)).State == CheckState.Unknown,
            "Blocked current inspection and incomplete prerequisite or history evidence fail closed");

        var duplicate = Evidence().Append(new("Restart", CheckState.Clear, "fixture")).ToArray();
        Require(SystemFilesOperationalCorrelation.Check(clear with { RecipeId = "other.recipe" }, Evidence()).State == CheckState.Unknown &&
            SystemFilesOperationalCorrelation.Check(clear with { Check = new("Other", CheckState.Clear, "fixture") }, Evidence()).State == CheckState.Unknown &&
            SystemFilesOperationalCorrelation.Check(clear, duplicate).State == CheckState.Unknown,
            "Unexpected recipe, result and evidence identities fail closed");

        try
        {
            SystemFilesOperationalCorrelation.Check(clear, Evidence(), new CancellationToken(true));
            throw new Exception("System-files correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }

        string folder = Path.Combine(scratch, "system-files-correlation");
        var workflow = Workflows.RunAsync(WorkflowKind.SystemFiles, false, folder,
            host: new WorkflowHost()).GetAwaiter().GetResult();
        Require(workflow.Checks.Single(check => check.Name == "System files operational conclusion") is
            { State: CheckState.Information, Repair: null } && workflow.Recipes.Count == 1 && workflow.Repairs.Count == 0,
            "System Files appends exactly one complete operational conclusion during diagnosis");

        var blocked = Workflows.RunAsync(WorkflowKind.SystemFiles, false, folder,
            host: new WorkflowHost(blocked: true)).GetAwaiter().GetResult();
        Require(blocked.Checks.Single(check => check.Name == "System files operational conclusion") is
            { State: CheckState.Unknown, Repair: null } && blocked.Recipes.Count == 0 && blocked.Repairs.Count == 0,
            "A blocked System Files recipe pass still emits one explicit incomplete conclusion without repair");

        Console.WriteLine("PASS: System Files produces one bounded current conclusion from recipe, prerequisite, and CBS/DISM evidence.");
    }

    private static RecipeResult Result(RecipeOutcome outcome, bool includeCheck = true) =>
        new("windows.system-files", 1, outcome,
            includeCheck ? new(RepairKind.SystemFiles.ToString(), outcome == RecipeOutcome.RepairAvailable ? CheckState.Repairable : CheckState.Clear,
                "fixture", outcome == RecipeOutcome.RepairAvailable ? RepairKind.SystemFiles : null) : null,
            null, "fixture");

    private static WorkflowCheck[] Evidence(CheckState reserved = CheckState.Information,
        CheckState cbs = CheckState.Information, CheckState dism = CheckState.Information) =>
    [
        new("Administrator", CheckState.Clear, "fixture"),
        new("Restart", CheckState.Clear, "fixture"),
        new("System disk", CheckState.Clear, "fixture"),
        new("Servicing transition readiness", CheckState.Information, "fixture"),
        new("Windows reserved storage", reserved, "fixture"),
        new("CBS servicing log", cbs, "fixture"),
        new("DISM servicing log", dism, "fixture"),
        new("Servicing log coverage", CheckState.Information, "fixture")
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
            if (blocked) evidence[1] = new("Restart", CheckState.Attention, "fixture");
            return Task.FromResult<IReadOnlyList<WorkflowCheck>>(evidence);
        }

        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Clear, "fixture"));

        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Read-only System Files workflow dispatched a repair");
    }
}
