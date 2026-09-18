using KillerBench.Engine;
using System.Text.Json;

internal static class EventLogCapacityChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(EventLogCapacitySnapshot value) => new(0, JsonSerializer.Serialize(value, Capture.JsonOptions), "", false);
        var circular = new EventLogCapacitySnapshot("System", true, "Circular", 950, 1000, 40, false);
        var circularCheck = EventLogCapacityDiagnostics.Interpret("System", Evidence(circular));
        Require(circularCheck.State == CheckState.Information && circularCheck.Detail.Contains("overwrite the oldest", StringComparison.Ordinal),
            "Near-full circular log lost overwrite context");
        var retained = EventLogCapacityDiagnostics.Interpret("System", Evidence(circular with { Mode = "Retain" }));
        Require(retained.State == CheckState.Attention && retained.Detail.Contains("stop accepting new records", StringComparison.Ordinal),
            "Near-full retained log did not request review");
        var disabled = EventLogCapacityDiagnostics.Interpret("System", Evidence(circular with { Enabled = false, FileSize = 100 }));
        Require(disabled.State == CheckState.Attention && disabled.Detail.Contains("disabled", StringComparison.Ordinal),
            "Disabled log did not request review");
        var autoBackup = EventLogCapacityDiagnostics.Interpret("System", Evidence(circular with { Mode = "AutoBackup" }));
        Require(autoBackup.State == CheckState.Information && autoBackup.Detail.Contains("rotate", StringComparison.Ordinal),
            "Automatic backup capacity context was lost");
        foreach (var invalid in new[]
        {
            Evidence(circular) with { ExitCode = 5 }, Evidence(circular) with { TimedOut = true }, Evidence(circular) with { Truncated = true },
            Evidence(circular) with { Error = "denied" }, Evidence(circular with { Channel = "Application" }),
            Evidence(circular with { Mode = "Unknown" }), Evidence(circular with { FileSize = -1 }), Evidence(circular with { MaximumSize = 0 }),
            Evidence(circular with { FileSize = 2001, MaximumSize = 1000 }), Evidence(circular with { RecordCount = null }),
            Evidence(circular with { Full = null }),
            new ToolResult(0, "{\"channel\":\"System\",\"channel\":\"System\",\"enabled\":true,\"mode\":\"Circular\",\"fileSize\":1,\"maximumSize\":2,\"recordCount\":1,\"full\":false}", "", false)
        }) Require(EventLogCapacityDiagnostics.Interpret("System", invalid).State == CheckState.Unknown,
            "Invalid event log capacity evidence was accepted");

        var visited = new List<string>();
        var checks = await EventLogCapacityDiagnostics.CheckAsync(collect: (channel, _) =>
        {
            visited.Add(channel);
            if (channel == "Application") throw new UnauthorizedAccessException("fixture");
            return Task.FromResult(Evidence(circular with { Channel = channel }));
        });
        Require(visited.SequenceEqual(EventLogCapacityDiagnostics.Channels) && checks.Count == 4 && checks[1].State == CheckState.Unknown,
            "Capacity collection did not isolate a failed channel");
        Console.WriteLine("PASS: event log size, record count, mode, saturation and partial collection remain bounded and read only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
