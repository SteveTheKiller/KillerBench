using KillerBench.Engine;

internal static class FileHistoryStatusChecks
{
    internal static async Task RunAsync()
    {
        const string unavailable = "{\"apiAvailable\":false,\"complete\":true,\"loadResult\":null,\"queryResult\":null,\"state\":null}";
        var absent = FileHistoryStatus.Interpret(Result(unavailable));
        Require(absent.State == CheckState.Information && absent.Repair is null && absent.Detail.Contains("not registered"),
            "Missing File History interface was treated as a protection failure");

        var expected = new Dictionary<uint, CheckState>
        {
            [0x00] = CheckState.Information,
            [0x01] = CheckState.Information,
            [0x02] = CheckState.Information,
            [0x03] = CheckState.Attention,
            [0x04] = CheckState.Information,
            [0x05] = CheckState.Information,
            [0x0D] = CheckState.Attention,
            [0x0E] = CheckState.Attention,
            [0x0F] = CheckState.Attention,
            [0x10] = CheckState.Attention,
            [0x11] = CheckState.Attention,
            [0x12] = CheckState.Attention,
            [0x13] = CheckState.Attention,
            [0x14] = CheckState.Attention,
            [0x15] = CheckState.Attention,
            [0x16] = CheckState.Attention,
            [0xF0] = CheckState.Attention,
            [0xFF] = CheckState.Clear,
            [0x810] = CheckState.Attention,
            [0x110] = CheckState.Attention,
            [0x1FF] = CheckState.Clear
        };
        foreach (var pair in expected)
        {
            var check = FileHistoryStatus.Interpret(Result(Success(pair.Key)));
            Require(check.State == pair.Value && check.Repair is null && check.Evidence?.Output == Success(pair.Key),
                "File History state " + pair.Key.ToString("X") + " was classified incorrectly or authorized repair");
        }
        var running = FileHistoryStatus.Interpret(Result(Success(0x1FF)));
        Require(running.Detail.Contains("currently reported as running") &&
            !running.Detail.Contains(@"C:\", StringComparison.OrdinalIgnoreCase) &&
            !running.Detail.Contains(@"\\", StringComparison.OrdinalIgnoreCase),
            "Running File History state lost current-cycle context or exposed excluded provider data");

        const string loadFailure = "{\"apiAvailable\":true,\"complete\":false,\"loadResult\":2147942402,\"queryResult\":null,\"state\":null}";
        const string queryFailure = "{\"apiAvailable\":true,\"complete\":false,\"loadResult\":0,\"queryResult\":2147500037,\"state\":null}";
        Require(FileHistoryStatus.Interpret(Result(loadFailure)).State == CheckState.Unknown &&
            FileHistoryStatus.Interpret(Result(loadFailure)).Detail.Contains("0x80070002") &&
            FileHistoryStatus.Interpret(Result(queryFailure)).State == CheckState.Unknown &&
            FileHistoryStatus.Interpret(Result(queryFailure)).Detail.Contains("0x80004005"),
            "File History provider failures lost their bounded HRESULT evidence");

        foreach (string invalid in new[]
        {
            Success(0x01).Replace("\"complete\":true", "\"complete\":false"),
            Success(0x01).Replace("\"loadResult\":0", "\"loadResult\":1"),
            Success(0x01).Replace("\"queryResult\":0", "\"queryResult\":null"),
            Success(0x01).Replace("\"state\":1", "\"state\":257"),
            Success(0x01).Replace("\"state\":1", "\"state\":42"),
            unavailable.Replace("\"loadResult\":null", "\"loadResult\":0"),
            unavailable[..^1] + ",\"target\":\"private\"}"
        })
            Require(FileHistoryStatus.Interpret(Result(invalid)).State == CheckState.Unknown,
                "Malformed, contradictory or unknown File History state was accepted");
        foreach (var invalid in new[] { Result(Success(0xFF)) with { TimedOut = true }, Result(Success(0xFF)) with { Truncated = true }, Result(Success(0xFF)) with { Error = "Denied" }, Result(Success(0xFF)) with { ExitCode = 1 } })
            Require(FileHistoryStatus.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete File History command evidence was accepted");

        RepairStep? requested = null;
        var injected = await FileHistoryStatus.CheckAsync(execute: (step, _) =>
        {
            requested = step;
            return Task.FromResult(Result(Success(0x01)));
        });
        Require(injected.State == CheckState.Information && requested?.Tool == PrintRecovery.Tool && requested.Verification &&
            requested.Arguments.Any(argument => argument.Contains("QueryProtectionStatus", StringComparison.Ordinal)) &&
            requested.Arguments.All(argument => !argument.Contains("manager.GetDefaultTarget", StringComparison.Ordinal) && !argument.Contains("manager.GetIncludeExcludeRules", StringComparison.Ordinal)),
            "Collector did not use one bounded read-only protection-state query");
        try
        {
            await FileHistoryStatus.CheckAsync((_, _) => Task.FromResult(Result(Success(0x01))), token: new CancellationToken(true));
            throw new Exception("File History status review ignored cancellation");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: File History current-account protection states, provider failures, privacy, malformed evidence and bounded substituted collection; no backup changed.");
    }

    private static string Success(uint state) => "{\"apiAvailable\":true,\"complete\":true,\"loadResult\":0,\"queryResult\":0,\"state\":" + state + "}";
    private static ToolResult Result(string output) => new(0, output, "", false);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
