using KillerBench.Engine;

internal static class WindowsUpdateActivityChecks
{
    internal static void Run()
    {
        string Event(int id, string time, string title = "", string code = "", int level = 4, string extra = "") =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{WindowsUpdateEvents.Provider}'/><EventID>{id}</EventID><EventRecordID>{id + 100}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='{time}'/></System><EventData>" +
            (title.Length == 0 ? "" : $"<Data Name='updateTitle'>{title}</Data>") +
            (code.Length == 0 ? "" : $"<Data Name='errorCode'>{code}</Data>") + extra + "</EventData></Event>";

        WorkflowCheck History(params string[] events) => WindowsUpdateEvents.Interpret(
            new ToolResult(0, "<Events>" + string.Concat(events) + "</Events>", "", false));

        var completed = WindowsUpdateActivityDiagnostics.Interpret(History(
            Event(19, "2026-09-16T10:05:00Z", "Security update"),
            Event(43, "2026-09-16T10:00:00Z", "Security update")));
        Require(completed.State == CheckState.Information && completed.Detail.Contains("Installed | Security update | Event 19", StringComparison.Ordinal) &&
            !completed.Detail.Contains("no later matching", StringComparison.Ordinal),
            "Matching install completion produces a bounded latest transition without an active claim");

        var recovered = WindowsUpdateActivityDiagnostics.Interpret(History(Event(26, "2026-09-16T10:06:00Z")),
            new ToolResult(0, "<Events>" + Event(19, "2026-09-16T10:05:00Z", "Recovered update") +
                Event(20, "2026-09-16T10:00:00Z", "Recovered update", "0x80240034", 2) + "</Events>", "", false));
        Require(recovered.State == CheckState.Information && recovered.Detail.Contains("Installed | Recovered update", StringComparison.Ordinal),
            "An older failure does not color a later successful transition as current attention");

        var active = WindowsUpdateActivityDiagnostics.Interpret(History(
            Event(43, "2026-09-16T10:00:00Z", "Feature update", extra: "<Data Name='serviceUrl'>https://private.invalid/update</Data>")));
        Require(active.State == CheckState.Information && active.Detail.Contains("Installing | Feature update", StringComparison.Ordinal) &&
            active.Detail.Contains("no later matching completion or failure", StringComparison.Ordinal) &&
            !active.Detail.Contains("private.invalid", StringComparison.Ordinal),
            "Unmatched installation start remains qualified and excludes service URLs");

        var failed = WindowsUpdateActivityDiagnostics.Interpret(History(
            Event(20, "2026-09-16T10:00:00Z", "Driver update", "0x80240034", 2)));
        Require(failed.State == CheckState.Attention && failed.Detail.Contains("WU_E_DOWNLOAD_FAILED", StringComparison.Ordinal) &&
            failed.Detail.Contains("BITS", StringComparison.Ordinal),
            "Known failure codes receive the shared Windows Update diagnostic route");

        var unknown = WindowsUpdateActivityDiagnostics.Interpret(History(
            Event(20, "2026-09-16T10:00:00Z", "Unknown update", "0x81234567", 2)));
        Require(unknown.State == CheckState.Attention && unknown.Detail.Contains("No installed reference is available", StringComparison.Ordinal),
            "Unknown failure codes remain exact unresolved evidence");

        var none = WindowsUpdateActivityDiagnostics.Interpret(History(Event(26, "2026-09-16T10:00:00Z")));
        Require(none.State == CheckState.Information && none.Detail.Contains("No recognized", StringComparison.Ordinal),
            "Unrecognized informational events do not invent a transition");

        var malformed = WindowsUpdateActivityDiagnostics.Interpret(new("Recent Windows Update Client events", CheckState.Information, "fixture",
            Evidence: new(0, "<Events><Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System/></Event></Events>", "", false)));
        Require(malformed.State == CheckState.Unknown, "Malformed event identity fails closed");

        var splitSource = WindowsUpdateActivityDiagnostics.Interpret(History(Event(26, "2026-09-16T10:06:00Z")),
            new ToolResult(0, "<Events>" + Event(19, "2026-09-16T10:05:00Z", "System channel update") + "</Events>", "", false));
        Require(splitSource.Detail.Contains("Installed | System channel update | Event 19", StringComparison.Ordinal),
            "System-channel transitions are projected alongside Operational-channel history");

        var unavailable = WindowsUpdateActivityDiagnostics.Interpret(History(Event(26, "2026-09-16T10:06:00Z")),
            new ToolResult(1, "", "Access denied", false));
        Require(unavailable.State == CheckState.Unknown, "Unavailable System-channel transition evidence fails closed");
        try { WindowsUpdateActivityDiagnostics.Interpret(completed, new CancellationToken(true)); throw new Exception("Windows Update activity ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: PEEK live Windows Update transitions retain bounded titles, exact failures, qualified starts and private-field exclusions.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
