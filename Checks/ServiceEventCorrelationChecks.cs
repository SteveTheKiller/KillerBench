using KillerBench.Engine;

internal static class ServiceEventCorrelationChecks
{
    public static void Run()
    {
        const string template = "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Service Control Manager'/><EventID>7000</EventID><EventRecordID>1</EventRecordID><Level>2</Level><TimeCreated SystemTime='2026-09-14T12:00:00Z'/></System><EventData><Data Name='param1'>{0}</Data></EventData></Event>";
        const string catalogJson = "{\"complete\":true,\"services\":[{\"name\":\"Spooler\",\"displayName\":\"Print Spooler\",\"state\":\"Stopped\",\"startMode\":\"Auto\",\"delayedAutoStart\":false,\"serviceType\":\"Own Process\"},{\"name\":\"wuauserv\",\"displayName\":\"Windows Update\",\"state\":\"Running\",\"startMode\":\"Manual\",\"delayedAutoStart\":false,\"serviceType\":\"Share Process\"},{\"name\":\"VendorSvc\",\"displayName\":\"Vendor Agent\",\"state\":\"Stopped\",\"startMode\":\"Manual\",\"delayedAutoStart\":false,\"serviceType\":\"Own Process\"}]}";
        WorkflowCheck Inventory(string json = catalogJson, CheckState state = CheckState.Information) => new("Installed service inventory", state, "fixture", Evidence: new(0, json, "", false));
        WorkflowCheck Current(RepairKind kind, CheckState state) => new(kind.ToString(), state, "fixture", Evidence:
            new(0, "SERVICE_NAME: " + ServiceRecovery.ServiceName(kind) + "\r\n        STATE              : " + (state == CheckState.Clear ? "4  RUNNING" : "1  STOPPED"), "", false));
        WorkflowCheck History(string xml, CheckState state = CheckState.Information) => new("Recent service failures", state, "fixture", Evidence: new(0, xml, "", false));
        string Event(string service, int record = 1) => string.Format(template, service).Replace("<EventRecordID>1", "<EventRecordID>" + record);
        var current = new[] { Current(RepairKind.UpdateService, CheckState.Clear), Current(RepairKind.PrintSpooler, CheckState.Attention) };

        var matched = ServiceEventCorrelation.Check(Inventory(), current, History("<Events>" + Event("Windows Update") + Event("Spooler", 2) + Event("Print Spooler", 3) + Event("Vendor Agent", 4) + "</Events>"));
        Require(matched.State == CheckState.Attention && matched.Repair is null &&
            matched.Detail.Contains("Windows Update [wuauserv]: 1 historical record, current state Clear", StringComparison.Ordinal) &&
            matched.Detail.Contains("Print Spooler [Spooler]: 2 historical records, current state Attention", StringComparison.Ordinal) &&
            matched.Detail.Contains("Vendor Agent [VendorSvc]: 1 historical record, current state Stopped, Manual", StringComparison.Ordinal),
            "Exact service names and unique display names join history to current installed state without selecting repair");
        string timeout = Event("30000").Replace("<Data Name='param1'>30000</Data>", "<Data Name='param1'>30000</Data><Data Name='param2'>Windows Update</Data>");
        Require(ServiceEventCorrelation.Check(Inventory(), current, History("<Events>" + timeout + "</Events>")).Detail.Contains("Windows Update [wuauserv]", StringComparison.Ordinal),
            "Service identity can be resolved from a later structured event field");
        string collision = catalogJson.Replace("\"Vendor Agent\"", "\"Windows Update\"");
        var ambiguous = ServiceEventCorrelation.Check(Inventory(collision), current, History("<Events>" + Event("Windows Update") + "</Events>"));
        Require(ambiguous.State == CheckState.Information && ambiguous.Detail.Contains("display-name collisions remain unresolved", StringComparison.OrdinalIgnoreCase) && ambiguous.Repair is null,
            "Duplicate display names remain unresolved instead of being guessed");
        var unknownCurrent = new[] { Current(RepairKind.UpdateService, CheckState.Unknown) };
        Require(ServiceEventCorrelation.Check(Inventory(), unknownCurrent, History("<Events>" + Event("wuauserv") + "</Events>")).State == CheckState.Unknown,
            "A matched event with unconfirmed focused current state keeps the correlation unknown");
        foreach (var invalid in new[]
        {
            ServiceEventCorrelation.Check(Inventory(state: CheckState.Unknown), current, History("<Events/>")),
            ServiceEventCorrelation.Check(Inventory(), current, History("<Events/>", CheckState.Unknown)),
            ServiceEventCorrelation.Check(Inventory(), [current[0], current[0]], History("<Events/>")),
            ServiceEventCorrelation.Check(Inventory("{}"), current, History("<Events/>")),
            ServiceEventCorrelation.Check(Inventory(), current, History("broken")),
            ServiceEventCorrelation.Check(Inventory(), current, History("<Events>" + Event("wuauserv").Replace("Service Control Manager", "Other") + "</Events>"))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Incomplete, malformed and ambiguous service evidence cannot produce correlation");
        try { ServiceEventCorrelation.Check(Inventory(), current, History("<Events/>"), new CancellationToken(true)); throw new Exception("Correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: installed service-name and display-name event correlation, collisions and incomplete evidence; no service changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
