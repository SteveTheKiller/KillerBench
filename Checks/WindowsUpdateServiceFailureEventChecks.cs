using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsUpdateServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        string Event(long recordId = 42, int eventId = 7031, string level = "2", string service = "Windows Update",
            DateTimeOffset? time = null) =>
            $"<Event xmlns=\"http://schemas.microsoft.com/win/2004/08/events/event\"><System><Provider Name=\"Service Control Manager\"/><EventID>{eventId}</EventID><Level>{level}</Level><TimeCreated SystemTime=\"{(time ?? now.AddHours(-1)):O}\"/><EventRecordID>{recordId}</EventRecordID></System><EventData><Data Name=\"param1\">{SecurityElement.Escape(service)}</Data><Data Name=\"param2\">1</Data></EventData></Event>";
        ToolResult Evidence(string[] records, bool complete = true, int scanned = 4, string service = "Windows Update") =>
            new(0, JsonSerializer.Serialize(new { complete, scanned, service, records }), "", false);

        Require(WindowsUpdateServiceFailureEvents.Interpret(Evidence([]), now) is { State: CheckState.Information, Repair: null },
            "Complete empty Windows Update service failure history remains informational");
        var failures = Evidence([Event(), Event(43, 7034, time: now.AddHours(-2))]);
        var attention = WindowsUpdateServiceFailureEvents.Interpret(failures, now);
        Require(attention is { State: CheckState.Attention, Repair: null } && ReferenceEquals(attention.Evidence, failures) &&
            attention.Detail.Contains("event 7031", StringComparison.Ordinal) &&
            attention.Detail.Contains("do not establish a current outage", StringComparison.Ordinal),
            "Exact retained Windows Update service failures request review without authorizing recovery");
        Require(WindowsUpdateServiceFailureEvents.Interpret(Evidence([Event()], false, 2000), now).State == CheckState.Unknown,
            "Capped Windows Update service history cannot produce a current verdict");
        Require(WindowsUpdateServiceFailureEvents.Interpret(
            Evidence([Event(service: "Windows Update localisé")], service: "Windows Update localisé"), now).State == CheckState.Attention,
            "Localized Windows Update service display identity remains exact historical evidence");
        foreach (var invalid in new[]
        {
            Evidence([Event(service: "Other Service")]), Evidence([Event(level: "3")]), Evidence([Event(recordId: 0)]),
            Evidence([Event(time: now.AddDays(-9))]), Evidence([Event(), Event()]),
            Evidence([Event().Replace("Service Control Manager", "Other Provider", StringComparison.Ordinal)]),
            Evidence([], scanned: 2001), Evidence([], service: " Windows Update"), new ToolResult(0, "{}", "", false),
            failures with { ExitCode = 5 }, failures with { TimedOut = true }, failures with { Truncated = true }, failures with { Error = "denied" }
        })
            Require(WindowsUpdateServiceFailureEvents.Interpret(invalid, now) is { State: CheckState.Unknown, Repair: null },
                "Malformed, duplicate, stale, unrelated, failed and incomplete Windows Update service evidence remains unknown");

        RepairStep? planned = null;
        var collected = await WindowsUpdateServiceFailureEvents.CheckAsync(execute: (step, providerToken) =>
        {
            planned = step;
            Require(providerToken.CanBeCanceled, "Windows Update service event provider must be bounded");
            return Task.FromResult(Evidence([]));
        });
        Require(collected.State == CheckState.Information && planned is { Verification: true } &&
            planned.Arguments.Last().StartsWith("$target='wuauserv';", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Get-WinEvent", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Start-Service", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Restart-Service", StringComparison.Ordinal),
            "Native Windows Update service event plan is exact, bounded and non-mutating");
        Console.WriteLine("PASS: exact Windows Update service failure history, localization, grouping, bounds and non-mutating collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
