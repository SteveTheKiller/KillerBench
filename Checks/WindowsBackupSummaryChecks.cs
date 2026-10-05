using KillerBench.Engine;

internal static class WindowsBackupSummaryChecks
{
    internal static async Task RunAsync()
    {
        var now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        const string unavailable = "{\"providerAvailable\":false,\"complete\":true,\"hasSummary\":false,\"lastBackupUtc\":null,\"lastSuccessUtc\":null,\"nextBackupUtc\":null,\"lastResult\":null}";
        var missing = WindowsBackupSummary.Interpret(Result(unavailable), now);
        Require(missing.State == CheckState.Information && missing.Repair is null && missing.Detail.Contains("not installed"),
            "Missing optional provider was treated as a backup failure");

        const string empty = "{\"providerAvailable\":true,\"complete\":true,\"hasSummary\":false,\"lastBackupUtc\":null,\"lastSuccessUtc\":null,\"nextBackupUtc\":null,\"lastResult\":null}";
        Require(WindowsBackupSummary.Interpret(Result(empty), now).State == CheckState.Information,
            "Empty provider summary claimed backup health or failure");

        const string success = "{\"providerAvailable\":true,\"complete\":true,\"hasSummary\":true,\"lastBackupUtc\":\"2026-09-15T10:00:00.0000000+00:00\",\"lastSuccessUtc\":\"2026-09-15T10:00:00.0000000+00:00\",\"nextBackupUtc\":\"2026-09-16T10:00:00.0000000+00:00\",\"lastResult\":0}";
        var completed = WindowsBackupSummary.Interpret(Result(success), now);
        Require(completed.State == CheckState.Information && completed.Evidence?.Output == success &&
            !completed.Detail.Contains(@"\\backup-host\", StringComparison.OrdinalIgnoreCase) && !completed.Detail.Contains("BackupTarget", StringComparison.Ordinal),
            "Successful summary lost evidence or exposed destination data");

        const string failed = "{\"providerAvailable\":true,\"complete\":true,\"hasSummary\":true,\"lastBackupUtc\":\"2026-09-15T11:00:00.0000000+00:00\",\"lastSuccessUtc\":\"2026-09-14T11:00:00.0000000+00:00\",\"nextBackupUtc\":null,\"lastResult\":2155348001}";
        var attention = WindowsBackupSummary.Interpret(Result(failed), now);
        Require(attention.State == CheckState.Attention && attention.Repair is null && attention.Detail.Contains("0x80780021") && attention.Detail.Contains("does not identify a cause"),
            "Nonzero result did not remain a conservative review finding");

        foreach (string invalid in new[]
        {
            success.Replace("\"complete\":true", "\"complete\":false"),
            success.Replace("\"lastResult\":0", "\"lastResult\":null"),
            success.Replace("2026-09-15T10:00:00.0000000+00:00", "2026-09-16T10:00:00.0000000+00:00"),
            success.Replace("\"lastSuccessUtc\":\"2026-09-15T10:00:00.0000000+00:00\"", "\"lastSuccessUtc\":null"),
            unavailable.Replace("\"hasSummary\":false", "\"hasSummary\":true"),
            success.Replace("\"lastResult\":0", "\"lastResult\":\"0\""),
            success[..^1] + ",\"extra\":true}"
        })
            Require(WindowsBackupSummary.Interpret(Result(invalid), now).State == CheckState.Unknown,
                "Malformed or contradictory summary was accepted");
        foreach (var invalid in new[] { Result(success) with { TimedOut = true }, Result(success) with { Truncated = true }, Result(success) with { Error = "Denied" }, Result(success) with { ExitCode = 1 } })
            Require(WindowsBackupSummary.Interpret(invalid, now).State == CheckState.Unknown,
                "Incomplete command evidence was accepted");

        RepairStep? requested = null;
        var injected = await WindowsBackupSummary.CheckAsync(execute: (step, _) =>
        {
            requested = step;
            return Task.FromResult(Result(success));
        });
        Require(injected.State == CheckState.Information && requested?.Tool == PrintRecovery.Tool && requested.Verification,
            "Collector did not use one bounded read-only provider command");
        try
        {
            await WindowsBackupSummary.CheckAsync((_, _) => Task.FromResult(Result(success)), token: new CancellationToken(true));
            throw new Exception("Windows Backup summary ignored cancellation");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Windows Backup provider availability, last-result classification, privacy, malformed evidence and bounded substituted collection; no backup changed.");
    }

    private static ToolResult Result(string output) => new(0, output, "", false);
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
