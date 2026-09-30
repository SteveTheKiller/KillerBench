using KillerBench.Engine;

internal static class DebloatPlanningChecks
{
    internal static void Run()
    {
        var options = new DebloatOptions(DebloatAreas.RecallAndAi | DebloatAreas.TelemetryAndReporting |
            DebloatAreas.SearchAndSuggestions | DebloatAreas.ConsumerContent | DebloatAreas.TaskbarAndStart |
            DebloatAreas.Notifications | DebloatAreas.Edge | DebloatAreas.OfficeLanguages |
            DebloatAreas.HighPerformancePower | DebloatAreas.SleepAndDisplayTimers | DebloatAreas.Hibernation |
            DebloatAreas.HpSoftware | DebloatAreas.VendorData, DebloatUserScope.AllProfilesAndDefault,
            RetainedOfficeCulture: "en-us", PowerRequest: new(0, 60, 60, 15));
        var review = DebloatPlanning.ReviewAsync(options, environmentReader: new FixtureEnvironmentReader(
            new("HP Inc.", true, true, true, "Fixture management evidence.")),
            registryReader: target => new(target, "Recorded", "0", "DWord"),
            serviceReader: (target, token) => Task.FromResult(new ToolResult(0,
                "SERVICE_NAME: " + target.Name + "\nSTATE : 4 RUNNING", "", false)),
            powerReader: new FixturePowerReader(new("381b4222-f694-41f0-9685-ff5bb260df2e",
                1800, 900, 600, 300, true, true, true, "Fixture power evidence.")),
            powerPlanReader: new FixturePowerPlanReader(new([
                new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High performance", false)],
                new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High performance", false), true,
                new(0, "fixture", "", false), "Fixture power plan evidence.")),
            specificRegistryReader: target => new(target, "Already proposed", target.ProposedValue, "DWord"),
            inventoryReader: token => Task.FromResult(new DebloatInventoryEvidence([], [
                new("Registry64", "O365BusinessRetail", "16.0", "x64", "fr-fr")], [
                new("Registry64", "O365BusinessRetail", "16.0", "x64", "fr-fr")], true,
                "Fixture exact inventory.")),
            recallReader: new FixtureRecallReader(new("Recall", "Enabled", true, true,
                new(0, "fixture", "", false), "Fixture Recall evidence.")),
            taskReader: token => Task.FromResult(new DebloatTaskEvidence([
                new(DebloatAreas.TelemetryAndReporting, new(new(@"\Microsoft\Windows\Customer Experience Improvement Program\", "Consolidator"), "Ready"),
                    @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator")],
                true, new(0, "fixture", "", false), "Fixture task evidence.")),
            oemServiceReader: token => Task.FromResult(new DebloatOemServiceEvidence([
                new("HPTouchpointAnalyticsService", "HP Touchpoint Analytics", "Running", "Auto", false, "Own Process")],
                true, new(0, "fixture", "", false), "Fixture OEM service evidence.")),
            vendorDataReader: token => new DebloatVendorDataEvidence([
                new(new("Fixture vendor path", @"C:\Fixture", @"C:\Fixture"), "Absent", null, null, "Fixture absent.")],
                true, "Fixture vendor data evidence."))
            .GetAwaiter().GetResult();
        Require(review.Ready && !review.ExecutionSupported && review.SelectedAreas.Count == 13,
            "Selected declutter areas produce a review without execution");
        Require(review.CurrentValues.Count > 10 && review.CurrentValues.All(value => value.State == "Recorded") &&
            review.Checks.Any(check => check.Name == "Current registry state" && check.Detail.Contains("No offline hive was loaded")),
            "Selected policy areas collect bounded current state and preserve all-profile limits");
        Require(review.SpecificValues.Count > 20 && review.SpecificValues.All(value => value.State == "Already proposed") &&
            review.Checks.Single(check => check.Name == "Declutter-specific registry state").Detail.Contains("No offline hive was loaded"),
            "Declutter-specific registry targets retain proposed values and all-profile limits");
        try
        {
            _ = DebloatRegistryInspection.Inspect(new(DebloatAreas.RecallAndAi, DebloatUserScope.RunningAccount),
                _ => new(DebloatRegistryInspection.Targets[1], "Recorded", 0, "DWord"));
            throw new Exception("Mismatched declutter registry evidence target accepted");
        }
        catch (InvalidOperationException) { }
        Require(review.CurrentServices.Count == 4 && review.CurrentServices.All(service => service.State == "Running") &&
            review.Checks.Any(check => check.Name == "Current service state" && check.Detail.Contains("No service change is authorized")),
            "Selected telemetry services retain exact identity and read-only state");
        Require(review.Power is { Complete: true, SleepAcSeconds: 1800, HibernationEnabled: true } &&
            review.Checks.Single(check => check.Name == "Current power state").Detail.Contains("Zero timeout means Never"),
            "Selected power areas retain exact timeout and hibernation evidence");
        Require(review.Options.PowerRequest == new DebloatPowerRequest(0, 60, 60, 15) &&
            review.Checks.Single(check => check.Name == "Requested power timers").Detail.Contains("Sleep AC/DC: 0/60") &&
            review.Format().Contains("display AC/DC 60/15"),
            "Requested AC and battery timers remain explicit and unapplied");
        Require(review.PowerPlans is { Complete: true, HighPerformance.Active: false } &&
            review.Checks.Single(check => check.Name == "High performance power plan").Repair is null,
            "Available inactive High performance plan remains read only");
        var powerPlans = WindowsDebloatPowerPlanReader.Interpret(new(0,
            "{\"complete\":true,\"plans\":[{\"id\":\"381b4222-f694-41f0-9685-ff5bb260df2e\",\"name\":\"Balanced\",\"active\":true},{\"id\":\"8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c\",\"name\":\"High performance\",\"active\":false}]}", "", false));
        Require(powerPlans.Complete && powerPlans.HighPerformance is { Active: false } && powerPlans.Detail.Contains("available but inactive"),
            "Power plan inventory identifies exact High performance availability");
        Require(!WindowsDebloatPowerPlanReader.Interpret(new(0,
            "{\"complete\":true,\"plans\":[{\"id\":\"381b4222-f694-41f0-9685-ff5bb260df2e\",\"name\":\"Balanced\",\"active\":false}]}", "", false)).Complete,
            "Power plan inventory without one active plan remains unknown");
        Require(review.Inventory is { Complete: true } && review.Inventory.OfficeProducts.Single().Culture == "fr-fr" &&
            review.Inventory.OfficeLanguageCandidates.Single().Culture == "fr-fr" &&
            review.Format().Contains("Retained Office culture: en-us") && review.Format().Contains("Execution available: No"),
            "Office inventory retains exact culture without selecting a removal");
        Require(review.Recall is { Complete: true, Present: true, State: "Enabled" } &&
            review.Checks.Single(check => check.Name == "Recall optional feature").Repair is null,
            "Exact Recall feature state remains read only");
        Require(review.Tasks is { Complete: true } && review.Tasks.Matches.Single().Task.Identity.Name == "Consolidator" &&
            review.Checks.Single(check => check.Name == "Selected scheduled tasks").Repair is null,
            "Exact telemetry task identity remains read only");
        Require(review.OemServices is { Complete: true } && review.OemServices.Matches.Single().Name == "HPTouchpointAnalyticsService" &&
            review.Checks.Single(check => check.Name == "Selected OEM services").Repair is null,
            "Exact OEM service identity remains read only");
        Require(review.VendorData is { Complete: true } && review.VendorData.Paths.Single().State == "Absent" &&
            review.Checks.Single(check => check.Name == "Vendor data paths").Repair is null,
            "Vendor path inspection remains read only");
        var vendorReview = DebloatVendorDataReview.Review(target => new(target,
            target.Label.StartsWith("McAfee", StringComparison.Ordinal) ? "Present" : "Absent",
            target.Label.StartsWith("McAfee", StringComparison.Ordinal) ? true : null, false,
            "Fixture metadata only."));
        Require(vendorReview.Complete && vendorReview.Paths.Count == 3 && vendorReview.Paths.Count(value => value.State == "Present") == 1 &&
            vendorReview.Detail.Contains("No directory was traversed"),
            "Vendor review inspects only three exact machine paths without traversal");
        try
        {
            _ = DebloatVendorDataReview.Review(target => new(new("Changed", target.Path, target.SourcePattern), "Absent", null, null, "Fixture"));
            throw new Exception("Mismatched vendor path target accepted");
        }
        catch (InvalidOperationException) { }
        const string serviceJson = "{\"complete\":true,\"services\":[{\"name\":\"DellOptimizer\",\"displayName\":\"Dell Optimizer\",\"state\":\"Stopped\",\"startMode\":\"Disabled\",\"delayedAutoStart\":false,\"serviceType\":\"Own Process\"},{\"name\":\"SimilarDellOptimizer\",\"displayName\":\"Similar\",\"state\":\"Running\",\"startMode\":\"Auto\",\"delayedAutoStart\":false,\"serviceType\":\"Own Process\"}]}";
        var oemServiceReview = DebloatOemServiceReview.Review(
            new(DebloatAreas.DellSoftware, DebloatUserScope.RunningAccount),
            new("Dell Inc.", false, false, true, "Fixture."), new(0, serviceJson, "", false));
        Require(oemServiceReview.Matches.Count == 1 && oemServiceReview.Matches[0].Name == "DellOptimizer" &&
            oemServiceReview.Detail.Contains("does not establish safe removal"),
            "OEM service review accepts only exact source names and does not infer removal safety");
        var wrongManufacturerServices = DebloatOemServiceReview.Review(
            new(DebloatAreas.DellSoftware, DebloatUserScope.RunningAccount),
            new("HP Inc.", false, false, true, "Fixture."), new(0, serviceJson, "", false));
        Require(wrongManufacturerServices.Matches.Count == 0,
            "Mismatched manufacturer prevents OEM service correlation");
        var taskInventory = new ScheduledTaskInventoryResult([
            new(new(@"\Microsoft\Windows\Application Experience\", "Microsoft Compatibility Appraiser"), "Ready"),
            new(new(@"\Microsoft\Windows\Application Experience\", "Similar Name"), "Ready")],
            false, new(0, "fixture", "", false));
        var taskReview = DebloatTaskReview.Review(new(DebloatAreas.TelemetryAndReporting,
            DebloatUserScope.RunningAccount), new("HP Inc.", false, false, true, "Fixture."), taskInventory);
        Require(taskReview.Matches.Count == 1 && !taskReview.Complete && taskReview.Detail.Contains("No task was"),
            "Task review accepts only exact catalog identities and preserves incomplete coverage");
        var oemTasks = DebloatTaskReview.Review(new(DebloatAreas.HpSoftware, DebloatUserScope.RunningAccount),
            new("HP Inc.", false, false, true, "Fixture."), new([
                new(new(@"\HP\Support\", "Check"), "Ready"),
                new(new(@"\Other\", "HP Check"), "Ready")], true, new(0, "fixture", "", false)));
        Require(oemTasks.Matches.Count == 1 && oemTasks.Matches[0].Task.Identity.FullName == @"\HP\Support\Check" &&
            oemTasks.Matches[0].SourcePattern == @"\HP*",
            "OEM task path patterns retain exact discovered identity and source provenance");
        var mismatchedOemTasks = DebloatTaskReview.Review(new(DebloatAreas.HpSoftware, DebloatUserScope.RunningAccount),
            new("Dell Inc.", false, false, true, "Fixture."), new([
                new(new(@"\HP\", "Check"), "Ready")], true, new(0, "fixture", "", false)));
        Require(mismatchedOemTasks.Matches.Count == 0,
            "Mismatched manufacturer prevents OEM task correlation");
        var pendingRecall = WindowsDebloatRecallReader.Interpret(new(0,
            "{\"present\":true,\"name\":\"Recall\",\"state\":\"DisablePending\"}", "", false));
        Require(pendingRecall.Complete && pendingRecall.State == "DisablePending",
            "Pending Recall state remains explicit");
        Require(!WindowsDebloatRecallReader.Interpret(new(0,
            "{\"present\":false,\"name\":\"Recall\",\"state\":\"Enabled\"}", "", false)).Complete &&
            !WindowsDebloatRecallReader.Interpret(new(5, "", "denied", false)).Complete,
            "Contradictory and failed Recall evidence remains unknown");
        var correlated = DebloatInventoryReview.Review(
            new(DebloatAreas.HpSoftware | DebloatAreas.ThirdPartyApps | DebloatAreas.OfficeLanguages, DebloatUserScope.RunningAccount,
                RetainedOfficeCulture: "fr-fr"),
            new("HP Inc.", false, false, true, "Fixture."),
            new([new("Registry64", "hp-key", "HP Wolf Security", "1", "HP Inc.")], true, new(0, "", "", false)),
            new(AppPackageScope.CurrentAccount, [new("SpotifyAB.SpotifyMusic", "spotify-full", "spotify-family", false, false) { Dependencies = [] }], true, new(0, "", "", false)),
            new([], true, new(0, "", "", false)), new([new("Registry64", "O365BusinessRetail", "16", "x64", "en-us")], true));
        Require(correlated.Candidates.Count == 2 && correlated.OfficeProducts.Count == 1 && !correlated.Candidates.Any(candidate => candidate.Protected) &&
            correlated.Candidates.Single(candidate => candidate.Area == DebloatAreas.ThirdPartyApps).SourcePattern == "*Spotify*" &&
            correlated.OfficeLanguageCandidates.Single().Culture == "en-us",
            "Exact OEM and bounded third-party identities correlate without authorizing removal");
        var sourceCatalog = DebloatInventoryReview.Review(
            new(DebloatAreas.DellSoftware | DebloatAreas.ThirdPartyApps, DebloatUserScope.RunningAccount),
            new("Dell Inc.", false, false, true, "Fixture."),
            new([new("Registry64", "dell", "Dell Optimizer Service", "2", "Fixture Publisher")], true, new(0, "", "", false)),
            new(AppPackageScope.CurrentAccount, [
                new("Publisher.NetflixClient", "netflix-full", "netflix-family", false, false) { Dependencies = [] },
                new("Publisher.Unrelated", "other-full", "other-family", false, false) { Dependencies = [] }], true, new(0, "", "", false)),
            new([], true, new(0, "", "", false)), new([], true));
        Require(sourceCatalog.Candidates.Count == 2 && sourceCatalog.Candidates.Any(candidate => candidate.SourcePattern == "Dell Optimizer*") &&
            sourceCatalog.Candidates.Any(candidate => candidate.SourcePattern == "*Netflix*") &&
            sourceCatalog.Detail.Contains("do not establish removal eligibility"),
            "Source wildcard catalog retains exact identities and never becomes removal eligibility");
        var incompletePower = DebloatPlanning.ReviewAsync(
            new(DebloatAreas.Hibernation, DebloatUserScope.RunningAccount),
            environmentReader: new FixtureEnvironmentReader(new("Fixture OEM", false, false, true, "Fixture evidence.")),
            powerReader: new FixturePowerReader(new(null, null, null, null, null, null, null, false, "Fixture unavailable.")))
            .GetAwaiter().GetResult();
        Require(incompletePower.Checks.Single(check => check.Name == "Current power state").State == CheckState.Unknown,
            "Incomplete power evidence cannot become a complete review");
        Require(review.Environment.Manufacturer == "HP Inc." &&
            review.Checks.Any(check => check.Name == "HP software" && check.Detail.Contains("matches this OEM family")) &&
            review.Checks.Any(check => check.Name == "Vendor data" && check.Detail.Contains("browser data remain protected")),
            "OEM eligibility and user-data protection remain explicit");
        var mismatch = DebloatPlanning.ReviewAsync(new(DebloatAreas.DellSoftware, DebloatUserScope.RunningAccount),
            environmentReader: new FixtureEnvironmentReader(new("HP Inc.", false, false, true, "Fixture evidence.")))
            .GetAwaiter().GetResult();
        Require(mismatch.Checks.Single(check => check.Name == "Dell software").State == CheckState.Attention &&
            mismatch.Checks.Single(check => check.Name == "Dell software").Detail.Contains("not applicable"),
            "Mismatched OEM evidence prevents applicability");
        var unavailable = DebloatPlanning.ReviewAsync(new(DebloatAreas.LenovoSoftware, DebloatUserScope.RunningAccount),
            environmentReader: new FixtureEnvironmentReader(new(null, false, false, false, "Fixture unavailable.")))
            .GetAwaiter().GetResult();
        Require(unavailable.Checks.Single(check => check.Name == "Lenovo software").State == CheckState.Unknown,
            "Unavailable manufacturer evidence cannot establish OEM applicability");
        var unavailableService = DebloatPlanning.ReviewAsync(
            new(DebloatAreas.TelemetryAndReporting, DebloatUserScope.RunningAccount),
            environmentReader: new FixtureEnvironmentReader(new("Fixture OEM", false, false, true, "Fixture evidence.")),
            registryReader: target => new(target, "Not configured", null, null),
            serviceReader: (target, token) => Task.FromResult(new ToolResult(5, "", "Access denied", false)))
            .GetAwaiter().GetResult();
        Require(unavailableService.CurrentServices.All(service => service.State == "Unavailable") &&
            unavailableService.Checks.Single(check => check.Name == "Current service state").State == CheckState.Unknown,
            "Unavailable service evidence cannot produce a complete current-state review");
        Require(review.Format().Contains("Preserve managed settings: Yes") &&
            review.Format().Contains("No optional feature, policy, profile, service, task, package, power setting, file or folder was changed."),
            "Formatted review states preservation and execution boundaries");
        Reject(new(DebloatAreas.None, DebloatUserScope.RunningAccount));
        Reject(new(DebloatAreas.Edge, DebloatUserScope.RunningAccount, false, true));
        Reject(new(DebloatAreas.Edge, DebloatUserScope.RunningAccount, true, false));
        Reject(new(DebloatAreas.Edge, DebloatUserScope.RunningAccount, RetainedOfficeCulture: "en-us"));
        Reject(new(DebloatAreas.OfficeLanguages, DebloatUserScope.RunningAccount, RetainedOfficeCulture: "english"));
        Reject(new(DebloatAreas.SleepAndDisplayTimers, DebloatUserScope.RunningAccount));
        Reject(new(DebloatAreas.Edge, DebloatUserScope.RunningAccount, PowerRequest: new(0, 60, 60, 15)));
        Reject(new(DebloatAreas.SleepAndDisplayTimers, DebloatUserScope.RunningAccount, PowerRequest: new(0, 60, 60, 1441)));
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        try
        {
            _ = DebloatPlanning.ReviewAsync(new(DebloatAreas.Edge, DebloatUserScope.RunningAccount), token: stop.Token)
                .GetAwaiter().GetResult();
            throw new Exception("Canceled declutter review continued");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: declutter area selection, preservation requirements and consequence review; no changes available.");
    }

    private static void Reject(DebloatOptions options)
    {
        try { options.Validate(); throw new Exception("Invalid declutter options accepted"); }
        catch (ArgumentException) { }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    private sealed class FixtureEnvironmentReader(DebloatEnvironmentEvidence evidence) : IDebloatEnvironmentReader
    {
        public Task<DebloatEnvironmentEvidence> ReadAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(evidence);
        }
    }

    private sealed class FixturePowerReader(DebloatPowerEvidence evidence) : IDebloatPowerReader
    {
        public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(evidence);
        }
    }

    private sealed class FixtureRecallReader(DebloatRecallEvidence evidence) : IDebloatRecallReader
    {
        public Task<DebloatRecallEvidence> ReadAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(evidence);
        }
    }

    private sealed class FixturePowerPlanReader(DebloatPowerPlanEvidence evidence) : IDebloatPowerPlanReader
    {
        public Task<DebloatPowerPlanEvidence> ReadAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(evidence);
        }
    }
}
