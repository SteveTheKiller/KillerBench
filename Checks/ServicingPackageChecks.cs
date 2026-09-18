using KillerBench.Engine;

internal static class ServicingPackageChecks
{
    public static void Run()
    {
        ToolResult Evidence(string complete = "true", string packages = "[]") =>
            new(0, $"{{\"complete\":{complete},\"packages\":{packages}}}", "", false);
        var stable = Evidence(packages: "[{\"identity\":\"Package_for_KB000000~31bf~amd64~~1.0.0.0\",\"state\":\"Installed\",\"releaseType\":\"SecurityUpdate\"},{\"identity\":\"Foundation-Package~31bf~amd64~~10.0.0.0\",\"state\":\"Superseded\",\"releaseType\":\"Foundation\"}]");
        var stableResult = ServicingPackageDiagnostics.Interpret(stable);
        Require(stableResult.State == CheckState.Information && stableResult.Repair is null && stableResult.Evidence == stable &&
            stableResult.Detail.Contains("Installed: 1", StringComparison.Ordinal) && stableResult.Detail.Contains("Superseded: 1", StringComparison.Ordinal),
            "Stable servicing package states remain informational and non-mutating");
        var pending = ServicingPackageDiagnostics.Interpret(Evidence(packages: "[{\"identity\":\"Pending-Package~31bf~amd64~~1.0.0.0\",\"state\":\"InstallPending\",\"releaseType\":\"Update\"},{\"identity\":\"Partial-Package~31bf~amd64~~1.0.0.0\",\"state\":\"PartiallyInstalled\",\"releaseType\":\"Update\"}]"));
        Require(pending.State == CheckState.Attention && pending.Repair is null && pending.Detail.Contains("Pending or partial packages", StringComparison.Ordinal),
            "Pending and partially installed packages request review without selecting a package action");
        Require(ServicingPackageDiagnostics.Interpret(Evidence()).State == CheckState.Information,
            "An empty complete package inventory does not invent corruption");
        foreach (var invalid in new[]
        {
            stable with { ExitCode = 5 }, stable with { Error = "denied" }, stable with { TimedOut = true }, stable with { Truncated = true },
            Evidence("false"), Evidence(packages: "[{}]"),
            Evidence(packages: "[{\"identity\":\" Package\",\"state\":\"Installed\",\"releaseType\":\"Update\"}]"),
            Evidence(packages: "[{\"identity\":\"Package\",\"state\":\"FutureState\",\"releaseType\":\"Update\"}]"),
            Evidence(packages: "[{\"identity\":\"Package\",\"state\":\"Installed\",\"releaseType\":\"Update\"},{\"identity\":\"package\",\"state\":\"Installed\",\"releaseType\":\"Update\"}]")
        }) Require(ServicingPackageDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed, future and duplicate package evidence remains unknown");
        var capped = Evidence(packages: "[" + string.Join(",", Enumerable.Range(0, 4097).Select(index =>
            $"{{\"identity\":\"Package{index}\",\"state\":\"Installed\",\"releaseType\":\"Update\"}}")) + "]");
        Require(ServicingPackageDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized servicing package evidence cannot claim complete coverage");
        Console.WriteLine("PASS: bounded servicing package states, pending review and incomplete evidence; no package changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
