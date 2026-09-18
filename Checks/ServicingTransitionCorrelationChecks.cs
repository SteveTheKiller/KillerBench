using KillerBench.Engine;

internal static class ServicingTransitionCorrelationChecks
{
    public static void Run()
    {
        const string stablePackages = "{\"complete\":true,\"packages\":[{\"identity\":\"A\",\"state\":\"Installed\",\"releaseType\":\"Update\"}]}";
        const string pendingPackages = "{\"complete\":true,\"packages\":[{\"identity\":\"A\",\"state\":\"InstallPending\",\"releaseType\":\"Update\"},{\"identity\":\"B\",\"state\":\"PartiallyInstalled\",\"releaseType\":\"Update\"}]}";
        const string stableFeatures = "{\"complete\":true,\"features\":[{\"name\":\"Containers\",\"state\":\"Disabled\"}]}";
        const string pendingFeatures = "{\"complete\":true,\"features\":[{\"name\":\"Containers\",\"state\":\"EnablePending\"}]}";
        const string clearRestart = "{\"complete\":true,\"componentServicing\":false,\"windowsUpdate\":false,\"pendingFileRenames\":false,\"computerRename\":false,\"updateExecutable\":false}";
        const string pendingRestart = "{\"complete\":true,\"componentServicing\":true,\"windowsUpdate\":true,\"pendingFileRenames\":false,\"computerRename\":false,\"updateExecutable\":false}";
        WorkflowCheck Packages(string json = stablePackages, CheckState state = CheckState.Information) => new("Servicing packages", state, "fixture", Evidence: new(0, json, "", false));
        WorkflowCheck Features(string json = stableFeatures, CheckState state = CheckState.Information) => new("Optional Windows features", state, "fixture", Evidence: new(0, json, "", false));
        WorkflowCheck Restart(string json = clearRestart, CheckState state = CheckState.Clear) => new("Restart", state, "fixture", Evidence: new(0, json, "", false));

        var stable = ServicingTransitionCorrelation.Check(Packages(), Features(), Restart());
        Require(stable.State == CheckState.Information && stable.Repair is null && stable.Detail.Contains("No pending package", StringComparison.Ordinal),
            "Stable servicing state remains informational and non-mutating");
        var aligned = ServicingTransitionCorrelation.Check(Packages(pendingPackages, CheckState.Attention), Features(pendingFeatures, CheckState.Attention), Restart(pendingRestart, CheckState.Attention));
        Require(aligned.State == CheckState.Attention && aligned.Repair is null && aligned.Detail.Contains("2 packages and 1 optional feature", StringComparison.Ordinal) &&
            aligned.Detail.Contains("component servicing, Windows Update", StringComparison.Ordinal), "Pending transitions correlate with supported restart sources without scheduling restart");
        var noIndicator = ServicingTransitionCorrelation.Check(Packages(pendingPackages, CheckState.Attention), Features(), Restart());
        Require(noIndicator.State == CheckState.Attention && noIndicator.Repair is null && noIndicator.Detail.Contains("no supported restart indicator", StringComparison.Ordinal),
            "Pending transition without recognized restart evidence requests recollection rather than repair");
        var unrelatedRestart = ServicingTransitionCorrelation.Check(Packages(), Features(), Restart(pendingRestart, CheckState.Attention));
        Require(unrelatedRestart.State == CheckState.Information && unrelatedRestart.Detail.Contains("may belong to other", StringComparison.Ordinal),
            "Restart evidence without pending package or feature state remains separate");
        foreach (var invalid in new[]
        {
            ServicingTransitionCorrelation.Check(Packages(state: CheckState.Unknown), Features(), Restart()),
            ServicingTransitionCorrelation.Check(Packages(), Features(state: CheckState.Unknown), Restart()),
            ServicingTransitionCorrelation.Check(Packages(), Features(), Restart(state: CheckState.Unknown)),
            ServicingTransitionCorrelation.Check(Packages("{}"), Features(), Restart()),
            ServicingTransitionCorrelation.Check(Packages(), Features("{}"), Restart()),
            ServicingTransitionCorrelation.Check(Packages(), Features(), Restart("{}"))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Incomplete servicing or restart evidence cannot produce transition readiness");
        try { ServicingTransitionCorrelation.Check(Packages(), Features(), Restart(), new CancellationToken(true)); throw new Exception("Servicing correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: package, optional-feature and restart correlation, incomplete evidence and cancellation; no servicing changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
