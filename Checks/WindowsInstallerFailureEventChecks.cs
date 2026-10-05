using System.IO;
using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsInstallerFailureEventChecks
{
    internal static async Task RunAsync()
    {
        var now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        string Event(long recordId = 42, int eventId = 7031, string level = "2", string service = "Windows Installer", DateTimeOffset? time = null) =>
            $"<Event xmlns=\"http://schemas.microsoft.com/win/2004/08/events/event\"><System><Provider Name=\"Service Control Manager\"/><EventID>{eventId}</EventID><Level>{level}</Level><TimeCreated SystemTime=\"{(time ?? now.AddHours(-1)):O}\"/><EventRecordID>{recordId}</EventRecordID></System><EventData><Data Name=\"param1\">{SecurityElement.Escape(service)}</Data><Data Name=\"param2\">1603</Data></EventData></Event>";
        ToolResult Evidence(string[] records, bool complete = true, int scanned = 4, string service = "Windows Installer") =>
            new(0, JsonSerializer.Serialize(new { complete, scanned, service, records }), "", false);

        var empty = Evidence([]);
        Require(WindowsInstallerFailureEvents.Interpret(empty, now) is { State: CheckState.Information, Repair: null },
            "Complete empty Windows Installer service failure history remains informational");
        var failures = Evidence([Event(), Event(43, 7034, service: "msiserver", time: now.AddHours(-2))]);
        var attention = WindowsInstallerFailureEvents.Interpret(failures, now);
        Require(attention is { State: CheckState.Attention, Repair: null } && ReferenceEquals(attention.Evidence, failures) &&
            attention.Detail.Contains("event 7031", StringComparison.Ordinal) && attention.Detail.Contains("event 7034", StringComparison.Ordinal) &&
            attention.Detail.Contains("do not establish a current outage", StringComparison.Ordinal) &&
            attention.Detail.Contains("do not", StringComparison.Ordinal),
            "Exact internal and display-name Installer failures request review without authorizing recovery");
        Require(WindowsInstallerFailureEvents.Interpret(Evidence([Event()], false, 2000), now).State == CheckState.Unknown,
            "Capped Installer service history cannot produce a current verdict");
        Require(WindowsInstallerFailureEvents.Interpret(Evidence([Event(service: "Programme d’installation Windows")], service: "Programme d’installation Windows"), now).State == CheckState.Attention,
            "Localized Installer display identity remains exact historical evidence");

        foreach (var invalid in new[]
        {
            Evidence([Event(service: "Other Service")]), Evidence([Event(level: "3")]), Evidence([Event(recordId: 0)]),
            Evidence([Event(time: now.AddDays(-9))]), Evidence([Event(), Event()]),
            Evidence([Event().Replace("Service Control Manager", "Other Provider", StringComparison.Ordinal)]),
            Evidence([], scanned: 2001), Evidence([], service: " Windows Installer"), new ToolResult(0, "{}", "", false),
            new ToolResult(0, "not json", "", false), empty with { ExitCode = 5 }, empty with { TimedOut = true },
            empty with { Truncated = true }, empty with { Error = "denied" }
        })
            Require(WindowsInstallerFailureEvents.Interpret(invalid, now) is { State: CheckState.Unknown, Repair: null },
                "Malformed, duplicate, stale, unrelated, failed and incomplete Installer event evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await WindowsInstallerFailureEvents.CheckAsync(execute: (step, providerToken) =>
        {
            planned = step;
            bounded = providerToken.CanBeCanceled;
            return Task.FromResult(empty);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().StartsWith("$target='msiserver';", StringComparison.Ordinal) &&
            planned.Arguments.Contains("-Command") && planned.Arguments.Last().Contains("Get-WinEvent", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Get-Service -Name $target", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Start-Service", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Restart-Service", StringComparison.Ordinal),
            "Native plan binds the exact local Installer identity without service mutation");
        var failure = await WindowsInstallerFailureEvents.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"));
        Require(failure is { State: CheckState.Unknown, Evidence.ExitCode: -1 }, "Provider failure retains unknown Installer evidence");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await WindowsInstallerFailureEvents.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents Installer event collection");
        Console.WriteLine("PASS: exact Windows Installer service failure history, grouping, bounds and non-mutating collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
