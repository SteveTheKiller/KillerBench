using KillerBench.Engine;

internal static class OptionalFeatureChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string complete = "true", string features = "[]") =>
            new(0, $"{{\"complete\":{complete},\"features\":{features}}}", "", false);
        var stable = Evidence(features: "[{\"name\":\"Containers\",\"state\":\"Enabled\"},{\"name\":\"TelnetClient\",\"state\":\"Disabled\"},{\"name\":\"LegacyPayload\",\"state\":\"Removed\"}]");
        var stableResult = OptionalFeatureDiagnostics.Interpret(stable);
        Require(stableResult.State == CheckState.Information && stableResult.Repair is null && stableResult.Evidence == stable &&
            stableResult.Detail.Contains("Disabled: 1", StringComparison.Ordinal) && stableResult.Detail.Contains("Enabled: 1", StringComparison.Ordinal) &&
            stableResult.Detail.Contains("Removed: 1", StringComparison.Ordinal),
            "Stable optional feature states remain informational and non-mutating");
        var pending = OptionalFeatureDiagnostics.Interpret(Evidence(features: "[{\"name\":\"HypervisorPlatform\",\"state\":\"EnablePending\"},{\"name\":\"LegacyFeature\",\"state\":\"DisablePending\"}]"));
        Require(pending.State == CheckState.Attention && pending.Repair is null && pending.Detail.Contains("Pending feature changes", StringComparison.Ordinal),
            "Pending optional feature changes request review without selecting a feature action");
        Require(OptionalFeatureDiagnostics.Interpret(Evidence()).State == CheckState.Information,
            "An empty complete optional feature inventory does not invent a fault");
        foreach (var invalid in new[]
        {
            stable with { ExitCode = 5 }, stable with { Error = "denied" }, stable with { TimedOut = true }, stable with { Truncated = true },
            Evidence("false"), Evidence(features: "[{}]"),
            Evidence(features: "[{\"name\":\" Feature\",\"state\":\"Enabled\"}]"),
            Evidence(features: "[{\"name\":\"Feature\",\"state\":\"FutureState\"}]"),
            Evidence(features: "[{\"name\":\"Feature\",\"state\":\"Enabled\"},{\"name\":\"feature\",\"state\":\"Disabled\"}]")
        }) Require(OptionalFeatureDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed, future and duplicate optional feature evidence remains unknown");
        var capped = Evidence(features: "[" + string.Join(",", Enumerable.Range(0, 1025).Select(index =>
            $"{{\"name\":\"Feature{index}\",\"state\":\"Disabled\"}}")) + "]");
        Require(OptionalFeatureDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized optional feature evidence cannot claim complete coverage");
        Console.WriteLine("PASS: bounded optional feature states, pending review and incomplete evidence; no feature changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
