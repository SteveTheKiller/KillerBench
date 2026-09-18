using KillerBench.Engine;

internal static class SetupApiDriverInstallChecks
{
    internal static void Run()
    {
        string success = Section(@"USB\VID_1234&PID_5678\ONE", "SUCCESS", "0x00000000", null);
        string failed = Section(@"PCI\VEN_1234&DEV_5678\TWO", "FAILURE", "0x80070005", "!!!  ndv: Driver package installation failed.");
        var evidence = new ToolResult(0, success + failed, "", false);
        var findings = SetupApiDriverInstall.Interpret(evidence);
        Require(findings.Count == 1 && findings[0] is { State: CheckState.Attention, Repair: null } && findings[0].Evidence == evidence &&
            findings[0].Name.Contains(@"PCI\VEN_1234&DEV_5678\TWO") && findings[0].Detail.Contains("0X80070005") &&
            findings[0].Detail.Contains("Driver package installation failed"),
            "Failed device section retains hardware identity, exit code and marked evidence without selecting a driver repair");
        var clean = SetupApiDriverInstall.Interpret(new(0, success, "", false));
        Require(clean.Single() is { State: CheckState.Information, Repair: null } && clean[0].Detail.Contains("1 complete"),
            "Complete successful retained sections remain informational rather than a device-health verdict");
        Require(!Workflows.SupportsRepair(WorkflowKind.DriverInstallHistory) && Workflows.Scopes(WorkflowKind.DriverInstallHistory).Length == 0 &&
            RecipeCatalog.ForWorkflow(WorkflowKind.DriverInstallHistory).Count == 0,
            "Driver installation history remains read-only through workflow and recipe APIs");

        var failedSection = new DriverInstallSection(@"PCI\VEN_1234&DEV_5678\TWO", "0X80070005", 1, 5, ["fixture"]);
        string currentProblem = System.Text.Json.JsonSerializer.Serialize(new { complete = true, devices = new[]
        {
            new { DeviceID = @"PCI\VEN_1234&DEV_5678\TWO", Name = "Fixture adapter", Present = true, ConfigManagerErrorCode = 28u },
            new { DeviceID = @"USB\VID_1111&PID_2222\OTHER", Name = "Other device", Present = true, ConfigManagerErrorCode = 0u }
        } });
        var correlated = DriverInstallCorrelation.Interpret([failedSection], new(0, currentProblem, "", false)).Single();
        Require(correlated is { State: CheckState.Attention, Repair: null } && correlated.Name.Contains("Fixture adapter") &&
            correlated.Detail.Contains("Device Manager code: 28") && correlated.Detail.Contains(failedSection.Identity),
            "Exact current device problems are correlated with retained failed-install identities without authorizing repair");
        string currentClear = currentProblem.Replace("ConfigManagerErrorCode\":28", "ConfigManagerErrorCode\":0");
        Require(DriverInstallCorrelation.Interpret([failedSection], new(0, currentClear, "", false)).Single().State == CheckState.Information,
            "An exact current device with code zero keeps the old installation failure historical");
        string absent = System.Text.Json.JsonSerializer.Serialize(new { complete = true, devices = Array.Empty<object>() });
        Require(DriverInstallCorrelation.Interpret([failedSection], new(0, absent, "", false)).Single() is { State: CheckState.Information, Repair: null },
            "An unmatched target is reported without treating historical failure as current damage");
        string partialDevices = currentProblem.Replace("\"complete\":true", "\"complete\":false");
        Require(DriverInstallCorrelation.Interpret([new("ROOT\\MISSING", "unreported", 1, 2, [])], new(0, partialDevices, "", false)).All(check => check.State == CheckState.Unknown),
            "Unmatched targets remain unknown when current inventory is capped");
        foreach (var invalidCurrent in new[]
        {
            new ToolResult(5, currentProblem, "", false), new ToolResult(0, currentProblem, "", true),
            new ToolResult(0, currentProblem, "fixture error", false), new ToolResult(0, "broken", "", false),
            new ToolResult(0, currentProblem.Replace("\"DeviceID\":", "\"DeviceID\":\"duplicate\",\"DeviceID\":"), "", false),
            new ToolResult(0, currentProblem.Replace("\"Present\":true", "\"Present\":null"), "", false)
        })
            Require(DriverInstallCorrelation.Interpret([failedSection], invalidCurrent).Single() is { State: CheckState.Unknown, Repair: null },
                "Failed, duplicate or malformed current-device evidence cannot establish correlation");
        try { DriverInstallCorrelation.Interpret([failedSection], new(0, currentProblem, "", false), new CancellationToken(true)); throw new Exception("Driver correlation cancellation ignored"); }
        catch (OperationCanceledException) { }

        var partial = SetupApiDriverInstall.Interpret(new(0, ">>>  [Device Install (Hardware initiated) - ROOT\\PARTIAL]", "", false));
        Require(partial.Any(check => check.State == CheckState.Unknown) && partial.All(check => check.Repair is null),
            "An incomplete retained section reports unknown coverage");
        var many = SetupApiDriverInstall.Interpret(new(0, string.Concat(Enumerable.Range(0, 33).Select(index =>
            Section("ROOT\\FIXTURE\\" + index, "FAILURE", "0x80070002", "!!!  sto: Fixture failure."))), "", false));
        Require(many.Count(check => check.State == CheckState.Attention) == 32 && many.Any(check => check.State == CheckState.Unknown),
            "Displayed failure sections are bounded while omitted coverage remains explicit");
        foreach (var invalid in new[] { evidence with { ExitCode = 5 }, evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Error = "fixture error" }, evidence with { Output = new string('x', TextEvidence.MaximumCharacters + 1) } })
            Require(SetupApiDriverInstall.Interpret(invalid).Single() is { State: CheckState.Unknown, Repair: null },
                "Failed or oversized SetupAPI evidence cannot authorize repair or claim complete history");
        try { SetupApiDriverInstall.Interpret(evidence, new CancellationToken(true)); throw new Exception("Driver history cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: bounded SetupAPI driver history, exact current-device correlation and read-only workflow behavior.");
    }

    private static string Section(string identity, string outcome, string code, string? error) =>
        ">>>  [Device Install (Hardware initiated) - " + identity + "]\n" +
        ">>>  Section start 2026/09/14 10:00:00.000\n" + (error is null ? "" : error + "\n") +
        "<<<  Section end 2026/09/14 10:00:01.000\n<<<  [Exit status: " + outcome + "(" + code + ")]\n";

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
