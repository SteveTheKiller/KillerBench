using KillerBench.Engine;

internal static class DeleteNotificationChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(string text) => new(0, text, "", false);
        var enabled = DeleteNotificationDiagnostics.Interpret(Evidence(
            "NTFS DisableDeleteNotify = 0  (Allows TRIM operations to be sent to the storage device)\r\nReFS DisableDeleteNotify = 0  (Disabled = 0)"));
        Require(enabled.Count == 2 && enabled.All(check => check.State == CheckState.Information) &&
            enabled.All(check => check.Repair is null), "Enabled NTFS and ReFS delete notifications remain descriptive and read only");

        var disabled = DeleteNotificationDiagnostics.Interpret(Evidence("NTFS DisableDeleteNotify = 1  (Disabled = 1)"));
        Require(disabled.Single().State == CheckState.Attention && disabled.Single().Detail.Contains("management source"),
            "Disabled delete notifications request review without selecting a change");

        foreach (var invalid in new[]
        {
            Evidence(""), Evidence("NTFS DisableDeleteNotify = 2"),
            Evidence("NTFS DisableDeleteNotify = 0  (unterminated"),
            Evidence("NTFS DisableDeleteNotify = 0\nNTFS DisableDeleteNotify = 1"),
            new ToolResult(1, "NTFS DisableDeleteNotify = 0", "denied", false),
            new ToolResult(0, "NTFS DisableDeleteNotify = 0", "", true)
        }) Require(DeleteNotificationDiagnostics.Interpret(invalid).Single().State == CheckState.Unknown,
            "Missing, unsupported, duplicate, failed and truncated evidence stays unknown");

        RepairStep? observed = null;
        var collected = await DeleteNotificationDiagnostics.CheckAsync(execute: (step, _) =>
        {
            observed = step;
            return Task.FromResult(Evidence("NTFS DisableDeleteNotify = 0"));
        });
        Require(observed is { Tool: "fsutil.exe", Verification: true } &&
            observed.Arguments.SequenceEqual(new[] { "behavior", "query", "DisableDeleteNotify" }) &&
            collected.Single().State == CheckState.Information,
            "Native plan is a bounded exact read-only delete-notification query");

        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DeleteNotificationDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("provider should not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents delete-notification collection");
        Console.WriteLine("PASS: NTFS and ReFS delete-notification states, incomplete evidence, read-only planning and cancellation; fixtures only.");
    }

    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
