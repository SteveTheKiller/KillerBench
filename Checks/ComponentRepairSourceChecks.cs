using KillerBench.Engine;

internal static class ComponentRepairSourceChecks
{
    internal static void Run()
    {
        var unconfigured = ComponentRepairSourceDiagnostics.Assess(new(true));
        Require(unconfigured is { State: CheckState.Information, Repair: null } &&
            unconfigured.Detail.Contains("No restrictive") && unconfigured.Evidence is not null,
            "Unconfigured component repair source remains informational");

        var local = ComponentRepairSourceDiagnostics.Assess(new(true,
            LocalSourcePath: @"Wim:\\server.example.test\images\install.wim:3", UseWindowsUpdate: 2));
        Require(local.State == CheckState.Information && local.Detail.Contains("alternate source") && local.Repair is null,
            "Alternate source satisfies the no-Windows-Update policy prerequisite without proving reachability");

        var unavailable = ComponentRepairSourceDiagnostics.Assess(new(true, UseWindowsUpdate: 2));
        Require(unavailable.State == CheckState.Attention && unavailable.Detail.Contains("no alternate repair source") &&
            unavailable.Repair is null, "Blocked Windows Update without an alternate source defers component repair");

        var direct = ComponentRepairSourceDiagnostics.Assess(new(true, RepairContentServerSource: 2));
        Require(direct.State == CheckState.Information && direct.Detail.Contains("directs repair content to Windows Update"),
            "Direct Windows Update source remains informational without a reachability claim");

        var conflict = ComponentRepairSourceDiagnostics.Assess(new(true, UseWindowsUpdate: 2,
            RepairContentServerSource: 2));
        Require(conflict.State == CheckState.Attention && conflict.Detail.Contains("both forbids") &&
            conflict.Detail.Contains("directs"), "Conflicting managed source flags request policy review");

        var corruption = new WorkflowCheck("Component store", CheckState.Repairable,
            "The component store is repairable.", RepairKind.ComponentStore,
            new ToolResult(0, "The component store is repairable.", "", false));
        var gated = ComponentRepairSourceDiagnostics.ApplyPrerequisite(corruption, unavailable);
        Require(gated.State == CheckState.Attention && gated.Repair is null &&
            ReferenceEquals(gated.Evidence, corruption.Evidence) && gated.Detail.Contains("repairable") &&
            gated.Detail.Contains("no alternate repair source"),
            "Confirmed corruption retains DISM evidence but loses repair eligibility when its source is blocked");
        Require(ComponentRepairSourceDiagnostics.ApplyPrerequisite(corruption, local).Repair == RepairKind.ComponentStore,
            "An allowed repair source preserves confirmed component-store eligibility");
        var clear = corruption with { State = CheckState.Clear, Repair = null };
        Require(ReferenceEquals(ComponentRepairSourceDiagnostics.ApplyPrerequisite(clear, unavailable), clear),
            "Source policy does not alter a clear component-store result");

        foreach (var malformed in new[]
        {
            new ComponentRepairSourceSnapshot(false),
            new ComponentRepairSourceSnapshot(true, UseWindowsUpdate: 1),
            new ComponentRepairSourceSnapshot(true, LocalSourcePath: 42),
            new ComponentRepairSourceSnapshot(true, LocalSourcePath: new string('x', 4097)),
            new ComponentRepairSourceSnapshot(true, RepairContentServerSource: 3),
            new ComponentRepairSourceSnapshot(true, CollectionIssue: "Access unavailable")
        })
        {
            var check = ComponentRepairSourceDiagnostics.Assess(malformed);
            Require(check is { State: CheckState.Unknown, Repair: null } && check.Evidence is not null,
                "Incomplete or malformed component repair source remains unknown with evidence");
        }
        Console.WriteLine("PASS: component repair source policy, conflicts, malformed evidence and read-only outcomes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
