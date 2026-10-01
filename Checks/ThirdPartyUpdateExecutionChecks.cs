using System.IO;
using KillerBench.Engine;

internal static class ThirdPartyUpdateExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var selection = new ThirdPartyUpdateSelection("Vendor.Tool", "winget", "2.0");
        var candidate = new ThirdPartyUpdateCandidate("Vendor.Tool", "Vendor Tool", "1.0", "winget", ["2.0", "1.5"]);
        var discovery = new ThirdPartyUpdateResult(true, "1.11.0", true, 1, [candidate], "fixture");
        var plan = ThirdPartyUpdatePlanning.Create(discovery, selection);
        Require(plan.Eligible && plan.Blockers.Count == 0 && plan.Format().Contains("PREVIEW ONLY", StringComparison.Ordinal), "Exact discovered third-party version is eligible for preview");
        Require(!ThirdPartyUpdatePlanning.Create(discovery with { Complete = false }, selection).Eligible, "Incomplete discovery blocks third-party update");
        Require(!ThirdPartyUpdatePlanning.Create(discovery, selection with { TargetVersion = "3.0" }).Eligible, "Unoffered version blocks third-party update");
        Require(!ThirdPartyUpdatePlanning.Create(discovery, selection with { Source = "store" }).Eligible, "Different source blocks third-party update");
        foreach (var invalid in new[] { selection with { Id = "" }, selection with { Source = " winget" }, selection with { TargetVersion = "2.0\n" } })
            try { ThirdPartyUpdatePlanning.Create(discovery, invalid); throw new Exception("Invalid third-party selection accepted"); } catch (ArgumentException) { }

        var step = WindowsThirdPartyUpdateExecutionHost.UpgradeStep(selection);
        Require(step.Tool == "winget.exe" && !step.Verification &&
            step.Arguments.SequenceEqual(["upgrade", "--id", "Vendor.Tool", "--exact", "--source", "winget", "--version", "2.0", "--silent", "--disable-interactivity", "--accept-source-agreements", "--accept-package-agreements"]) &&
            !step.Arguments.Contains("--all") && !step.Arguments.Contains("--force") && !step.Arguments.Contains("--allow-reboot"),
            "Native third-party update targets one exact version without broad or bypass options");
        Require(WindowsThirdPartyUpdateExecutionHost.InterpretInstalledVersion(new(0, "{\"count\":1,\"version\":\"2.0\"}", "", false)) == "2.0", "Exact installed version evidence is accepted");
        foreach (var evidence in new[]
        {
            new ToolResult(0, "{\"count\":0,\"version\":\"\"}", "", false),
            new ToolResult(0, "{\"count\":1,\"count\":1,\"version\":\"2.0\"}", "", false),
            new ToolResult(0, "{\"count\":1,\"version\":\"2.0\",\"extra\":true}", "", false),
            new ToolResult(1, "{\"count\":1,\"version\":\"2.0\"}", "", false),
            new ToolResult(0, "{\"count\":1,\"version\":\"2.0\"}", "warning", false),
            new ToolResult(0, "{\"count\":1,\"version\":\"2.0\"}", "", true)
        }) Require(WindowsThirdPartyUpdateExecutionHost.InterpretInstalledVersion(evidence) is null, "Incomplete or ambiguous installed-version evidence is rejected");

        string successFolder = Path.Combine(scratch, "third-party-update-success");
        var success = new Fixture(discovery, "2.0");
        var result = await ThirdPartyUpdateExecution.RunAsync(selection, true, successFolder, success);
        Require(result is { Status: "Completed", Outcome.Verified: true } && result.GetExitCode() == 0 && success.Discoveries == 2 && success.Upgrades == 1 &&
            !File.Exists(Path.Combine(successFolder, "third-party-update.pending.json")), "Execution repeats discovery, dispatches once and clears pending evidence only after exact verification");
        Require(File.ReadAllText(result.JournalPath).Contains("FinalEligibility", StringComparison.Ordinal) && File.ReadAllText(result.JournalPath).Contains("Verification", StringComparison.Ordinal),
            "Execution journal retains final gate and verification");

        string changedFolder = Path.Combine(scratch, "third-party-update-changed");
        var changed = new Fixture(discovery, "2.0") { FinalDiscovery = discovery with { Candidates = [] } };
        result = await ThirdPartyUpdateExecution.RunAsync(selection, true, changedFolder, changed);
        Require(result.Status == "Blocked" && changed.Upgrades == 0 && !File.Exists(Path.Combine(changedFolder, "third-party-update.pending.json")) &&
            Directory.GetFiles(changedFolder, "*.prepared.json").Length == 1, "Changed final eligibility blocks dispatch and archives prepared evidence");

        string pendingFolder = Path.Combine(scratch, "third-party-update-pending");
        var unverified = new Fixture(discovery, "1.0");
        result = await ThirdPartyUpdateExecution.RunAsync(selection, true, pendingFolder, unverified);
        Require(result.Status == "Needs review" && result.Outcome?.Verified == false && File.Exists(Path.Combine(pendingFolder, "third-party-update.pending.json")),
            "Unverified installation retains the no-repeat record");
        try { await ThirdPartyUpdateExecution.RunAsync(selection, true, pendingFolder, unverified); throw new Exception("Pending update allowed a retry"); }
        catch (InvalidOperationException) { }
        var stillPending = await ThirdPartyUpdateExecution.ReconcileAsync(pendingFolder, unverified);
        Require(stillPending.Outcome == ThirdPartyUpdateRecoveryOutcome.StillPending && unverified.Upgrades == 1, "Recovery never repeats an unverified update");
        unverified.InstalledVersion = "2.0";
        var recovered = await ThirdPartyUpdateExecution.ReconcileAsync(pendingFolder, unverified);
        Require(recovered.Outcome == ThirdPartyUpdateRecoveryOutcome.Verified && !File.Exists(recovered.PendingPath) && unverified.Upgrades == 1,
            "Fresh exact-version evidence archives pending update without another provider request");
        Require((await ThirdPartyUpdateExecution.ReconcileAsync(pendingFolder, unverified)).Outcome == ThirdPartyUpdateRecoveryOutcome.NoPendingRecord, "Cleared update has no pending recovery work");

        string invalidFolder = Path.Combine(scratch, "third-party-update-invalid");
        Directory.CreateDirectory(invalidFolder);
        await File.WriteAllTextAsync(Path.Combine(invalidFolder, "third-party-update.pending.json"),
            "{\"Schema\":1,\"Schema\":1,\"Machine\":\"" + Environment.MachineName + "\",\"Journal\":\"C:\\\\fixture.jsonl\",\"Selection\":{\"Id\":\"Vendor.Tool\",\"Source\":\"winget\",\"TargetVersion\":\"2.0\"},\"PreparedUtc\":\"2026-09-16T00:00:00Z\"}");
        Require((await ThirdPartyUpdateExecution.ReconcileAsync(invalidFolder, unverified)).Outcome == ThirdPartyUpdateRecoveryOutcome.InvalidRecord,
            "Duplicate pending fields cannot authorize recovery");
        Console.WriteLine("PASS: exact third-party application planning, bounded update dispatch, final eligibility, version verification and no-repeat recovery; simulated provider only.");
    }

    private sealed class Fixture(ThirdPartyUpdateResult discovery, string? installedVersion) : IThirdPartyUpdateExecutionHost
    {
        internal int Discoveries;
        internal int Upgrades;
        internal ThirdPartyUpdateResult? FinalDiscovery;
        internal string? InstalledVersion = installedVersion;
        public Task<ThirdPartyUpdateResult> DiscoverAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Discoveries++;
            return Task.FromResult(Discoveries == 2 && FinalDiscovery is not null ? FinalDiscovery : discovery);
        }
        public Task<ToolResult> UpgradeAsync(ThirdPartyUpdateSelection selection, CancellationToken token)
        { token.ThrowIfCancellationRequested(); Upgrades++; return Task.FromResult(new ToolResult(0, "updated", "", false)); }
        public Task<string?> ReadInstalledVersionAsync(string id, string source, CancellationToken token)
        { token.ThrowIfCancellationRequested(); return Task.FromResult(InstalledVersion); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
