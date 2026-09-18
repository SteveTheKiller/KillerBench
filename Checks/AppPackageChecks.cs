using KillerBench.Engine;

internal static class AppPackageChecks
{
    internal static void Run()
    {
        const string row = "{\"name\":\"Fixture\",\"fullName\":\"Fixture_1_x64__test\",\"familyName\":\"Fixture_test\",\"framework\":false,\"nonRemovable\":true,\"developmentMode\":true,\"dependencies\":[\"Framework_1_x64__test\"]}";
        var raw = new ToolResult(0, "{\"complete\":true,\"packages\":[" + row + "]}", "", false);
        var result = AppPackageInventory.Interpret(AppPackageScope.AllUsers, raw);
        var diagnostic = AppPackageDiagnostics.Describe(result);
        foreach (var (status, expected) in new[] { ("OK", CheckState.Information), ("LicenseIssue", CheckState.Attention),
            ("Modified, Disabled", CheckState.Attention), ("Tampered", CheckState.Attention), ("FutureFlag", CheckState.Unknown), ("", CheckState.Unknown) })
        {
            var evidence = raw with { Output = raw.Output.Replace("\"name\":", "\"status\":" + System.Text.Json.JsonSerializer.Serialize(status) + ",\"name\":") };
            var checks = AppPackageDiagnostics.Describe(AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, evidence));
            if (checks[1].State != expected || checks[1].Repair is not null || checks[0].Evidence != evidence)
                throw new Exception("Package status classification lost uncertainty, evidence or repair isolation.");
            var summary = AppPackageDiagnostics.Summarize(checks);
            if (summary.State != expected || summary.Repair is not null || summary.Evidence != evidence ||
                !summary.Detail.Contains("1 registered packaged-app status rows", StringComparison.Ordinal))
                throw new Exception("Package summary lost status precedence, evidence or bounded row count.");
        }
        foreach (var (status, expectedText) in new[] { ("OK", "remain untested"), ("LicenseIssue", "entitlement"),
            ("Modified", "payload changes"), ("Tampered", "Preserve deployment evidence"),
            ("Disabled", "administrator restrictions"), ("FutureFlag", "not fully recognized"),
            ("LicenseIssue, FutureFlag", "not fully recognized"), ("Modified, Disabled", "administrator restrictions") })
        {
            var evidence = raw with { Output = raw.Output.Replace("\"name\":", "\"status\":" + System.Text.Json.JsonSerializer.Serialize(status) + ",\"name\":") };
            var finding = AppPackageDiagnostics.Describe(AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, evidence))[1];
            if (!finding.Detail.Contains(expectedText, StringComparison.Ordinal) || finding.Repair is not null)
                throw new Exception("Package follow-up lost status-specific guidance or exposed a repair.");
        }
        if (diagnostic[1].State != CheckState.Unknown) throw new Exception("Missing package status was treated as healthy.");
        var many = new List<WorkflowCheck> { diagnostic[0] };
        many.AddRange(Enumerable.Range(0, 600).Select(index => new WorkflowCheck("Package " + index, CheckState.Information, "fixture")));
        var manySummary = AppPackageDiagnostics.Summarize(many);
        if (manySummary.State != CheckState.Information || manySummary.Evidence != raw ||
            !manySummary.Detail.Contains("600 registered packaged-app status rows", StringComparison.Ordinal))
            throw new Exception("Large packaged-app inventory did not collapse to one bounded readiness result.");
        if (AppPackageDiagnostics.Summarize([]).State != CheckState.Unknown ||
            AppPackageDiagnostics.Summarize([new("Unexpected", CheckState.Information, "fixture")]).State != CheckState.Unknown)
            throw new Exception("Invalid packaged-app summary input claimed complete readiness evidence.");
        if (diagnostic.Count != 2 || diagnostic[0].State != CheckState.Information || diagnostic[0].Evidence != raw ||
            diagnostic[1].Name != "Fixture_1_x64__test" || diagnostic.Any(check => check.Repair is not null) ||
            Workflows.SupportsRepair(WorkflowKind.AppPackages) || Workflows.Scopes(WorkflowKind.AppPackages).Length != 0 ||
            WorkflowCatalog.Find("app-packages")?.Kind != WorkflowKind.AppPackages)
            throw new Exception("Package workflow lost inventory identity or exposed a repair.");
        var failedDiagnostic = AppPackageDiagnostics.Describe(result with { Evidence = raw with { TimedOut = true } });
        if (failedDiagnostic.Count != 1 || failedDiagnostic[0].State != CheckState.Unknown)
            throw new Exception("Failed package inventory claimed successful workflow coverage.");
        if (!result.Complete || result.Scope != AppPackageScope.AllUsers || !result.Packages.Single().NonRemovable ||
            !result.Packages.Single().DevelopmentMode || result.Evidence != raw)
            throw new Exception("Package inventory lost scope, protection flags or evidence.");
        if (result.Packages.Single().Dependencies?.Single() != "Framework_1_x64__test") throw new Exception("Package dependency identity was lost.");
        var dependencyReview = AppPackageReview.Review(result, "Framework_1_x64__test");
        if (dependencyReview.RemovalSupported || dependencyReview.Selected is not null || dependencyReview.DependentPackages.Single() != "Fixture_1_x64__test")
            throw new Exception("Package review lost a dependent or authorized removal of an absent target.");
        var protectedReview = AppPackageReview.Review(result, "Fixture_1_x64__test");
        var graphRows = new[]
        {
            new { name = "A", fullName = "A_1", familyName = "A", framework = true, nonRemovable = false, developmentMode = false, dependencies = new[] { "C_1" } },
            new { name = "B", fullName = "B_1", familyName = "B", framework = false, nonRemovable = false, developmentMode = false, dependencies = new[] { "a_1" } },
            new { name = "C", fullName = "C_1", familyName = "C", framework = false, nonRemovable = false, developmentMode = false, dependencies = new[] { "B_1" } },
            new { name = "D", fullName = "D_1", familyName = "D", framework = false, nonRemovable = false, developmentMode = false, dependencies = Array.Empty<string>() }
        };
        var graph = AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, raw with
        { Output = System.Text.Json.JsonSerializer.Serialize(new { complete = true, packages = graphRows }) });
        var graphReview = AppPackageReview.Review(graph, "A_1");
        if (!graph.Complete || !graphReview.DependentPackages.SequenceEqual(new[] { "B_1", "C_1" }) ||
            graphReview.RemovalSupported || !graphReview.ReviewItems.Any(item => item.Contains("framework")))
            throw new Exception("Cyclic package graph lost indirect dependents, isolation or framework warning.");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try { AppPackageReview.Review(graph, "A_1", canceled.Token); throw new Exception("Canceled package review continued."); }
        catch (OperationCanceledException) { }
        if (!protectedReview.ReviewItems.Any(item => item.Contains("non-removable"))) throw new Exception("Protected package warning missing.");
        var incompleteReview = AppPackageReview.Review(result with { Complete = true, Evidence = raw with { TimedOut = true } }, "Fixture_1_x64__test");
        if (incompleteReview.Selected is not null || !incompleteReview.ReviewItems.Any(item => item.Contains("incomplete")))
            throw new Exception("Modified inventory summary bypassed retained evidence.");
        foreach (string dependencies in new[] { "null", "[null]", "[\"Fixture_1_x64__test\"]", "[\"same\",\"SAME\"]" })
        {
            var incomplete = AppPackageInventory.Interpret(AppPackageScope.CurrentAccount,
                raw with { Output = raw.Output.Replace("[\"Framework_1_x64__test\"]", dependencies) });
            if (incomplete.Complete || incomplete.Packages.Count != 1 || incomplete.Packages[0].Dependencies is not null)
                throw new Exception("Unknown dependency evidence must preserve identity without complete coverage.");
        }
        foreach (var invalid in new[] { raw with { Error = "Denied" }, raw with { TimedOut = true }, raw with { Truncated = true },
            raw with { Output = "{}" }, raw with { Output = raw.Output.Replace("false", "null") },
            raw with { Output = raw.Output.Replace("[" + row + "]", "[" + row + "," + row + "]") },
            raw with { Output = raw.Output.Replace("\"complete\":true", "\"complete\":false,\"complete\":true") } })
            if (AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, invalid).Complete) throw new Exception("Incomplete package inventory claimed complete coverage.");
        string conflicting = row.Replace("\"nonRemovable\":true", "\"nonRemovable\":false")
            .Replace("Fixture_1_x64__test", "FIXTURE_1_X64__TEST");
        foreach (string variant in new[] { conflicting,
            conflicting.Replace("\"nonRemovable\":false", "\"nonRemovable\":null"),
            conflicting.Replace("\"name\":\"Fixture\"", "\"name\":42"),
            conflicting.Replace("\"familyName\":\"Fixture_test\",", "") })
        foreach (string duplicateRows in new[] { row + "," + variant, variant + "," + row })
        {
            var duplicateEvidence = raw with { Output = "{\"complete\":true,\"packages\":[" + duplicateRows + "]}" };
            var duplicate = AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, duplicateEvidence);
            if (duplicate.Complete || duplicate.Packages.Count != 0 || duplicate.Evidence != duplicateEvidence ||
                AppPackageReview.Review(duplicate, "Fixture_1_x64__test").Selected is not null ||
                AppPackageDiagnostics.Describe(duplicate).Count != 1)
                throw new Exception("Conflicting package identity supplied a selected record or diagnostic status.");
        }
        if (!AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, raw with { Output = "{\"complete\":true,\"packages\":[]}" }).Complete)
            throw new Exception("Empty scoped inventory was rejected.");
        Console.WriteLine("PASS: package inventory scope, identity, protection flags and incomplete evidence (fixtures only; no removal).");
    }
}
