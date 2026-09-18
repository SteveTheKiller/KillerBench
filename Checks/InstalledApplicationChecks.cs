using KillerBench.Engine;

internal static class InstalledApplicationChecks
{
    internal static void Run()
    {
        const string row = "{\"view\":\"Registry64\",\"key\":\"fixture\",\"name\":\"Fixture\",\"version\":null,\"publisher\":null}";
        var raw = new ToolResult(0, "{\"complete\":true,\"applications\":[" + row + "]}", "", false);
        var result = InstalledApplications.Interpret(raw);
        var empty = InstalledApplications.Interpret(raw with { Output = "{\"complete\":true,\"applications\":[]}" });
        var addition = ApplicationInventoryComparison.Compare(empty, result);
        var removal = ApplicationInventoryComparison.Compare(result, empty);
        var version = ApplicationInventoryComparison.Compare(result, InstalledApplications.Interpret(raw with { Output = raw.Output.Replace("\"version\":null", "\"version\":\"2.0\"") }));
        var metadata = ApplicationInventoryComparison.Compare(result, InstalledApplications.Interpret(raw with { Output = raw.Output.Replace("\"publisher\":null", "\"publisher\":\"Vendor\"") }));
        if (!addition.Comparable || addition.Differences.Single().Change != ApplicationRegistrationChange.Added ||
            removal.Differences.Single().Change != ApplicationRegistrationChange.Removed || version.Differences.Single().Change != ApplicationRegistrationChange.VersionChanged ||
            metadata.Differences.Single().Change != ApplicationRegistrationChange.MetadataChanged || ApplicationInventoryComparison.Compare(result, result).Differences.Count != 0)
            throw new Exception("Application registration comparison lost a change or invented one.");
        if (ApplicationInventoryComparison.Compare(result, result with { Evidence = raw with { TimedOut = true } }).Comparable)
            throw new Exception("Incomplete inventory established a comparison.");
        var diagnostic = InstalledApplications.Describe(result);
        var user = InstalledApplications.Interpret(raw with { Output = raw.Output.Replace("\"complete\":", "\"scope\":\"CurrentAccount\",\"complete\":") });
        if (ApplicationInventoryComparison.Compare(result, user).Comparable) throw new Exception("Different registry scopes were compared.");
        if (!user.Complete || user.Scope != InstalledApplicationScope.CurrentAccount || !InstalledApplications.Describe(user)[1].Detail.Contains("HKCU/"))
            throw new Exception("Current-account registry identity was relabeled as machine-wide.");
        var unavailableUser = InstalledApplications.Describe(user with { Evidence = raw with { TimedOut = true } });
        if (unavailableUser[0].State != CheckState.Unknown || !unavailableUser[0].Detail.Contains("CurrentAccount"))
            throw new Exception("Failed current-account collection lost requested scope.");
        if (diagnostic.Count != 2 || diagnostic[0].Evidence != raw || diagnostic.Any(check => check.State != CheckState.Information || check.Repair is not null) ||
            Workflows.SupportsRepair(WorkflowKind.InstalledApps) || Workflows.Scopes(WorkflowKind.InstalledApps).Length != 0 ||
            WorkflowCatalog.Find("installed-apps")?.Kind != WorkflowKind.InstalledApps)
            throw new Exception("Installed application workflow lost evidence or exposed a repair.");
        var failed = InstalledApplications.Describe(result with { Evidence = raw with { TimedOut = true } });
        if (failed.Count != 1 || failed[0].State != CheckState.Unknown) throw new Exception("Failed application inventory claimed coverage.");
        if (!result.Complete || result.Applications.Single().Key != "fixture" || result.Evidence != raw)
            throw new Exception("Installed application evidence lost identity.");
        var views = InstalledApplications.Interpret(raw with { Output = raw.Output.Replace("[" + row + "]", "[" + row + "," + row.Replace("Registry64", "Registry32") + "]") });
        if (!views.Complete || views.Applications.Count != 2) throw new Exception("Distinct registry views were collapsed.");
        foreach (var invalid in new[] { raw with { Error = "Denied" }, raw with { TimedOut = true }, raw with { Truncated = true },
            raw with { Output = "{}" }, raw with { Output = raw.Output.Replace("Registry64", "Unknown") },
            raw with { Output = raw.Output.Replace("[" + row + "]", "[" + row + "," + row + "]") },
            raw with { Output = raw.Output.Replace("\"complete\":true", "\"complete\":true,\"complete\":true") } })
            if (InstalledApplications.Interpret(invalid).Complete) throw new Exception("Incomplete installed applications claimed complete coverage.");
        Console.WriteLine("PASS: machine application identity, registry-view separation and incomplete evidence; fixtures only.");
    }
}
