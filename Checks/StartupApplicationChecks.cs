using KillerBench.Engine;

internal static class StartupApplicationChecks
{
    public static void Run()
    {
        ToolResult Evidence(string complete = "true", string issues = "[]", string entries = "[]") =>
            new(0, $"{{\"complete\":{complete},\"issues\":{issues},\"entries\":{entries}}}", "", false);
        var populated = Evidence(entries: "[{\"source\":\"Machine64Run\",\"name\":\"Updater\",\"kind\":\"Registry\"},{\"source\":\"UserStartup\",\"name\":\"Notes.lnk\",\"kind\":\"File\"}]");
        var result = StartupApplicationDiagnostics.Interpret(populated);
        Require(result.State == CheckState.Information && result.Repair is null && result.Evidence == populated &&
            result.Detail.Contains("Reviewed 2 startup registrations", StringComparison.Ordinal) && result.Detail.Contains("Updater", StringComparison.Ordinal) &&
            result.Detail.Contains("Command lines and target paths were not collected", StringComparison.Ordinal),
            "Startup inventory reports bounded names and sources without commands or repairs");
        Require(StartupApplicationDiagnostics.Interpret(Evidence()).State == CheckState.Information,
            "Empty complete startup inventory remains informational");
        var partial = StartupApplicationDiagnostics.Interpret(Evidence("false", "[\"Machine32Run\"]"));
        Require(partial.State == CheckState.Unknown && partial.Detail.Contains("Machine32Run", StringComparison.Ordinal),
            "Unavailable startup sources retain explicit incomplete coverage");
        foreach (var invalid in new[]
        {
            populated with { ExitCode = 5 }, populated with { Error = "denied" }, populated with { TimedOut = true }, populated with { Truncated = true },
            Evidence(entries: "[{}]"), Evidence(entries: "[{\"source\":\"Other\",\"name\":\"App\",\"kind\":\"Registry\"}]"),
            Evidence(entries: "[{\"source\":\"UserRun\",\"name\":\" App\",\"kind\":\"Registry\"}]"),
            Evidence(entries: "[{\"source\":\"UserRun\",\"name\":\"App\",\"kind\":\"Command\"}]"),
            Evidence(entries: "[{\"source\":\"UserRun\",\"name\":\"App\",\"kind\":\"Registry\"},{\"source\":\"UserRun\",\"name\":\"app\",\"kind\":\"Registry\"}]"),
            Evidence("true", "[\"UserRun\"]")
        })
            Require(StartupApplicationDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Failed, malformed, contradictory and duplicate startup inventory remains unknown");
        var capped = Evidence(entries: "[" + string.Join(",", Enumerable.Range(0, 513).Select(index => $"{{\"source\":\"UserRun\",\"name\":\"App{index}\",\"kind\":\"Registry\"}}")) + "]");
        Require(StartupApplicationDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized startup inventory cannot claim complete coverage");
        ToolResult TaskEvidence(string complete = "true", string entries = "[]") =>
            new(0, $"{{\"complete\":{complete},\"entries\":{entries}}}", "", false);
        var tasks = TaskEvidence(entries: "[{\"path\":\"\\\\Vendor\\\\\",\"name\":\"Updater\",\"trigger\":\"Boot\",\"state\":\"Ready\"},{\"path\":\"\\\\\",\"name\":\"Welcome\",\"trigger\":\"Logon\",\"state\":\"Disabled\"}]");
        var taskResult = StartupScheduledTaskDiagnostics.Interpret(tasks);
        Require(taskResult.State == CheckState.Information && taskResult.Repair is null && taskResult.Evidence == tasks &&
            taskResult.Detail.Contains("Reviewed 2 boot or logon task triggers", StringComparison.Ordinal) &&
            taskResult.Detail.Contains("Actions, arguments, accounts and credentials were not collected", StringComparison.Ordinal),
            "Startup task inventory reports bounded identity and state without executable or account data");
        Require(StartupScheduledTaskDiagnostics.Interpret(TaskEvidence()).State == CheckState.Information,
            "Empty complete startup task inventory remains informational");
        foreach (var invalid in new[]
        {
            tasks with { ExitCode = 5 }, tasks with { Error = "denied" }, tasks with { TimedOut = true }, tasks with { Truncated = true },
            TaskEvidence("false"), TaskEvidence(entries: "[{}]"),
            TaskEvidence(entries: "[{\"path\":\"Vendor\",\"name\":\"Task\",\"trigger\":\"Boot\",\"state\":\"Ready\"}]"),
            TaskEvidence(entries: "[{\"path\":\"\\\\\",\"name\":\"Task\",\"trigger\":\"Daily\",\"state\":\"Ready\"}]"),
            TaskEvidence(entries: "[{\"path\":\"\\\\\",\"name\":\"Task\",\"trigger\":\"Boot\",\"state\":\"Other\"}]"),
            TaskEvidence(entries: "[{\"path\":\"\\\\\",\"name\":\"Task\",\"trigger\":\"Boot\",\"state\":\"Ready\"},{\"path\":\"\\\\\",\"name\":\"task\",\"trigger\":\"Boot\",\"state\":\"Ready\"}]")
        }) Require(StartupScheduledTaskDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed and duplicate startup task evidence remains unknown");
        var cappedTasks = TaskEvidence(entries: "[" + string.Join(",", Enumerable.Range(0, 513).Select(index =>
            $"{{\"path\":\"\\\\\",\"name\":\"Task{index}\",\"trigger\":\"Boot\",\"state\":\"Ready\"}}")) + "]");
        Require(StartupScheduledTaskDiagnostics.Interpret(cappedTasks).State == CheckState.Unknown,
            "Oversized startup task inventory cannot claim complete coverage");

        ToolResult ApprovalEvidence(string complete = "true", string issues = "[]", string entries = "[]") =>
            new(0, $"{{\"complete\":{complete},\"issues\":{issues},\"entries\":{entries}}}", "", false);
        var approvals = ApprovalEvidence(entries: "[{\"source\":\"Machine64Run\",\"name\":\"Updater\",\"state\":2,\"bytes\":12},{\"source\":\"UserStartupFolder\",\"name\":\"Notes.lnk\",\"state\":3,\"bytes\":12}]");
        var approvalResult = StartupApprovalDiagnostics.Interpret(approvals);
        Require(approvalResult.State == CheckState.Information && approvalResult.Repair is null && approvalResult.Evidence == approvals &&
            approvalResult.Detail.Contains("1 enabled and 1 disabled", StringComparison.Ordinal) &&
            approvalResult.Detail.Contains("Binary timestamps and command lines were not collected", StringComparison.Ordinal),
            "Startup approval inventory reports bounded state without timestamps, commands or changes");
        Require(StartupApprovalDiagnostics.Interpret(ApprovalEvidence()).State == CheckState.Information,
            "Empty complete approval inventory remains informational");
        foreach (var invalid in new[]
        {
            approvals with { ExitCode = 5 }, approvals with { Error = "denied" }, approvals with { TimedOut = true }, approvals with { Truncated = true },
            ApprovalEvidence("false", "[\"Machine64Run\"]"), ApprovalEvidence(entries: "[{}]"),
            ApprovalEvidence("false", "[\"Other\"]"), ApprovalEvidence("false", "[\"UserRun:\"]"),
            ApprovalEvidence("false", "[\" UserRun\"]"),
            ApprovalEvidence(entries: "[{\"source\":\"Other\",\"name\":\"App\",\"state\":2,\"bytes\":12}]"),
            ApprovalEvidence(entries: "[{\"source\":\"UserRun\",\"name\":\" App\",\"state\":2,\"bytes\":12}]"),
            ApprovalEvidence(entries: "[{\"source\":\"UserRun\",\"name\":\"App\",\"state\":4,\"bytes\":12}]"),
            ApprovalEvidence(entries: "[{\"source\":\"UserRun\",\"name\":\"App\",\"state\":2,\"bytes\":0}]"),
            ApprovalEvidence(entries: "[{\"source\":\"UserRun\",\"name\":\"App\",\"state\":2,\"bytes\":12},{\"source\":\"UserRun\",\"name\":\"app\",\"state\":3,\"bytes\":12}]")
        }) Require(StartupApprovalDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed, unknown-marker and duplicate approval evidence remains unknown");
        var cappedApprovals = ApprovalEvidence(entries: "[" + string.Join(",", Enumerable.Range(0, 513).Select(index =>
            $"{{\"source\":\"UserRun\",\"name\":\"App{index}\",\"state\":2,\"bytes\":12}}")) + "]");
        Require(StartupApprovalDiagnostics.Interpret(cappedApprovals).State == CheckState.Unknown,
            "Oversized approval inventory cannot claim complete coverage");
        var mixedOverflow = ApprovalEvidence(issues: "[" + string.Join(",", Enumerable.Range(0, 256).Select(index => $"\"UserRun:Issue{index}\"")) + "]",
            entries: "[" + string.Join(",", Enumerable.Range(0, 257).Select(index =>
                $"{{\"source\":\"UserRun\",\"name\":\"App{index}\",\"state\":2,\"bytes\":12}}")) + "]");
        Require(StartupApprovalDiagnostics.Interpret(mixedOverflow).State == CheckState.Unknown,
            "Combined approval entries and issues cannot exceed the evidence bound");
        Console.WriteLine("PASS: startup registration, approval and scheduled-task identity, privacy, bounds and incomplete evidence; no startup changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
