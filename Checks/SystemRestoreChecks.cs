using KillerBench.Engine;

internal static class SystemRestoreChecks
{
    internal static void Run()
    {
        var now = DateTimeOffset.Parse("2026-09-14T12:00:00.0000000+00:00");
        static string Point(uint sequence, string description, uint type, string created) =>
            $"{{\"sequence\":{sequence},\"description\":\"{description}\",\"type\":{type},\"created\":\"{created}\"}}";
        var evidence = new ToolResult(0,
            "{\"complete\":true,\"points\":[" + Point(7, "Before update", 0, "2026-09-13T10:00:00.0000000+00:00") + "," +
            Point(9, "Driver install", 10, "2026-09-14T10:00:00.0000000+00:00") + "]}", "", false);
        var result = SystemRestoreDiagnostics.Interpret(evidence, now);
        Require(result.State == CheckState.Information && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("2 available") && result.Detail.IndexOf("RP9", StringComparison.Ordinal) < result.Detail.IndexOf("RP7", StringComparison.Ordinal) &&
            result.Detail.Contains("No restore point was created, deleted or applied"),
            "Restore point inventory retains bounded newest-first evidence without offering mutation");
        var empty = SystemRestoreDiagnostics.Interpret(evidence with { Output = "{\"complete\":true,\"points\":[]}" }, now);
        Require(empty.State == CheckState.Information && empty.Detail.Contains("does not establish whether protection is enabled"),
            "An empty inventory does not invent protection state or failure");
        foreach (var invalid in new[]
        {
            evidence with { ExitCode = 1 }, evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Error = "Access denied" }, evidence with { Output = "{\"complete\":false,\"points\":[]}" },
            evidence with { Output = "{\"complete\":true,\"points\":[" + Point(1, "A", 0, "2026-09-14T10:00:00.0000000+00:00") + "," + Point(1, "B", 0, "2026-09-14T11:00:00.0000000+00:00") + "]}" },
            evidence with { Output = "{\"complete\":true,\"points\":[" + Point(1, "Future", 0, "2026-09-15T10:00:00.0000000+00:00") + "]}" },
            evidence with { Output = "{}" }
        })
        {
            var unknown = SystemRestoreDiagnostics.Interpret(invalid, now);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == invalid,
                "Incomplete or contradictory restore point evidence remains unknown");
        }
        Require(SystemRestoreDiagnostics.Step().Verification, "System Restore collection step is read-only");
        Console.WriteLine("PASS: System Restore inventory bounds, chronology, empty state and incomplete evidence; no restore points changed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
