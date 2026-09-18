using KillerBench.Engine;

internal static class InstallerActivityChecks
{
    internal static void Run()
    {
        var idle = InstallerActivityDiagnostics.Interpret(Result("{\"complete\":true,\"marker\":false,\"processes\":[]}"));
        Require(idle is { State: CheckState.Information, Repair: null, Evidence: not null } && idle.Detail.Contains("single snapshot"),
            "Idle Installer snapshot remains informational without a health claim");
        var marker = InstallerActivityDiagnostics.Interpret(Result("{\"complete\":true,\"marker\":true,\"processes\":[]}"));
        Require(marker is { State: CheckState.Attention, Repair: null } && marker.Detail.Contains("InProgress marker"),
            "Transaction marker requests review without changing the marker");
        string processJson = "{\"complete\":true,\"marker\":false,\"processes\":[{\"id\":42,\"parent\":7,\"created\":\"2026-09-13T12:00:00.0000000Z\"}]}";
        var active = InstallerActivityDiagnostics.Interpret(Result(processJson));
        Require(active is { State: CheckState.Attention, Repair: null } && active.Detail.Contains("PID 42") &&
            active.Detail.Contains("command lines were not collected", StringComparison.OrdinalIgnoreCase),
            "Observed Installer process requests review with bounded key-free evidence");
        foreach (string invalid in new[]
        {
            "{}", "{\"complete\":false,\"marker\":false,\"processes\":[]}",
            "{\"complete\":true,\"marker\":false,\"processes\":[{\"id\":0,\"parent\":7,\"created\":\"2026-09-13T12:00:00Z\"}]}",
            "{\"complete\":true,\"marker\":false,\"processes\":[{\"id\":42,\"parent\":7,\"created\":\"bad\"}]}",
            "{\"complete\":true,\"complete\":true,\"marker\":false,\"processes\":[]}",
            "{\"complete\":true,\"marker\":false,\"processes\":[{\"id\":42,\"parent\":7,\"created\":\"2026-09-13T12:00:00Z\"},{\"id\":42,\"parent\":8,\"created\":\"2026-09-13T12:00:01Z\"}]}"
        })
            Require(InstallerActivityDiagnostics.Interpret(Result(invalid)) is { State: CheckState.Unknown, Repair: null },
                "Incomplete, duplicate or malformed Installer activity evidence remains unknown");
        foreach (var failure in new[] { Result("{}").WithExit(5), Result("{}") with { TimedOut = true }, new ToolResult(0, "{}", "provider error", false), new ToolResult(0, "{}", "", true) })
            Require(InstallerActivityDiagnostics.Interpret(failure).State == CheckState.Unknown, "Failed Installer activity collection cannot pass");
        string script = string.Join(" ", InstallerActivityDiagnostics.Step().Arguments);
        Require(script.Contains("Name='msiexec.exe'", StringComparison.Ordinal) && !script.Contains("CommandLine", StringComparison.OrdinalIgnoreCase),
            "Installer activity query selects only msiexec.exe and excludes process command lines");

        var noProcess = InstallerDiagnostics.Assess(new(null, false, true) { ProcessInventoryCollected = true, ActiveInstallerProcesses = 0 });
        Require(noProcess.State == CheckState.Clear, "Zero-process readiness snapshot can pass existing prerequisite checks");
        foreach (int? count in new int?[] { 1, null, -1, 129 })
        {
            var readiness = InstallerDiagnostics.Assess(new(null, false, true) { ProcessInventoryCollected = true, ActiveInstallerProcesses = count });
            Require(readiness.State == (count == 1 ? CheckState.Attention : CheckState.Unknown) && readiness.Repair is null,
                "Installer recovery readiness fails closed for active or invalid process inventory");
        }
        Console.WriteLine("PASS: Installer process correlation, transaction marker handling, privacy and recovery gating.");
    }

    private static ToolResult Result(string json) => new(0, json, "", false);
    private static ToolResult WithExit(this ToolResult result, int exitCode) => result with { ExitCode = exitCode };
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
