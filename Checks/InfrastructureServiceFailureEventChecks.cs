using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class InfrastructureServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        int providerCalls = 0;
        string? capturedCommand = null;
        var displayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["bthserv"] = "Bluetooth Support Service", ["FrameServer"] = "Windows Camera Frame Server",
            ["SCardSvr"] = "Smart Card", ["WbioSrvc"] = "Windows Biometric Service", ["LanmanServer"] = "Server",
            ["Schedule"] = "Task Scheduler", ["DPS"] = "Diagnostic Policy Service", ["WerSvc"] = "Windows Error Reporting Service",
            ["Wecsvc"] = "Windows Event Collector", ["RasMan"] = "Remote Access Connection Manager",
            ["TermService"] = "Remote Desktop Services", ["WinRM"] = "Windows Remote Management", ["wscsvc"] = "Security Center",
            ["Netman"] = "Network Connections", ["WinHttpAutoProxySvc"] = "WinHTTP Web Proxy Auto-Discovery Service",
            ["Wcmsvc"] = "Windows Connection Manager", ["NetSetupSvc"] = "Network Setup Service",
            ["EventSystem"] = "COM+ Event System", ["SENS"] = "System Event Notification Service", ["Power"] = "Power",
            ["PlugPlay"] = "Plug and Play", ["UserManager"] = "User Manager", ["gpsvc"] = "Group Policy Client",
            ["nsi"] = "Network Store Interface Service", ["RpcEptMapper"] = "RPC Endpoint Mapper",
            ["RpcSs"] = "Remote Procedure Call", ["DcomLaunch"] = "DCOM Server Process Launcher",
            ["BrokerInfrastructure"] = "Background Tasks Infrastructure Service", ["CoreMessagingRegistrar"] = "CoreMessaging"
        };

        var checks = await InfrastructureServiceFailureEvents.CheckAllAsync(execute: (step, providerToken) =>
        {
            Require(providerToken.CanBeCanceled, "Infrastructure service history provider must be bounded");
            Interlocked.Increment(ref providerCalls);
            capturedCommand = step.Arguments.Last();
            return Task.FromResult(new ToolResult(0, JsonSerializer.Serialize(new
            {
                complete = true,
                scanned = 2,
                items = displayNames.Select(pair => new
                {
                    target = pair.Key,
                    service = pair.Value,
                    available = pair.Key != "FrameServer",
                    complete = pair.Key != "SENS",
                    records = pair.Key == "Schedule" ? new[] { Event(7024, pair.Value) } : []
                }).ToArray()
            }), "", false));
        });

        Require(checks.Count == displayNames.Count && checks.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() == displayNames.Count,
            "Infrastructure service history must return 29 distinct checks");
        Require(checks.Single(check => check.Name == "Task Scheduler service failure history") is { State: CheckState.Attention, Repair: null },
            "Exact Task Scheduler failure history must request review without selecting recovery");
        Require(checks.Single(check => check.Name == "Camera Frame Server failure history") is { State: CheckState.Unknown, Repair: null } &&
            checks.Single(check => check.Name == "System Event Notification service failure history") is { State: CheckState.Unknown, Repair: null },
            "Unavailable and saturated infrastructure services must remain isolated and incomplete");
        Require(checks.Where(check => check.Name is not ("Task Scheduler service failure history" or "Camera Frame Server failure history" or "System Event Notification service failure history"))
            .All(check => check is { State: CheckState.Information, Repair: null }),
            "Complete empty infrastructure service histories must remain informational");
        Require(providerCalls == 1 && capturedCommand is not null &&
            displayNames.Keys.All(name => capturedCommand.Contains("'" + name + "'", StringComparison.Ordinal)) &&
            capturedCommand.Contains("Get-WinEvent", StringComparison.Ordinal) &&
            capturedCommand.Contains("Get-Service -Name $_", StringComparison.Ordinal) &&
            !capturedCommand.Contains("Start-Service", StringComparison.Ordinal) &&
            !capturedCommand.Contains("Restart-Service", StringComparison.Ordinal),
            "Infrastructure service histories must share one bounded snapshot, use exact identities and remain read only");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await RequireCanceled(() => InfrastructureServiceFailureEvents.CheckAllAsync(canceled.Token,
            (_, _) => throw new Exception("Canceled infrastructure service collection must not invoke its provider.")));
        try
        {
            await InfrastructureServiceFailureEvents.CheckManyAsync(
                [InfrastructureServiceFailureArea.Power, InfrastructureServiceFailureArea.Power], execute: (_, _) =>
                    throw new Exception("Duplicate infrastructure areas must not invoke their provider."));
            throw new Exception("Duplicate infrastructure service area accepted.");
        }
        catch (ArgumentException)
        {
        }
        Console.WriteLine("PASS: 29 exact infrastructure service failure histories share one bounded snapshot and remain non-mutating; fixtures only.");
    }

    private static string Event(long recordId, string service)
    {
        string time = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O");
        return $"<Event xmlns=\"http://schemas.microsoft.com/win/2004/08/events/event\"><System><Provider Name=\"Service Control Manager\"/><EventID>7031</EventID><Level>2</Level><TimeCreated SystemTime=\"{time}\"/><EventRecordID>{recordId}</EventRecordID></System><EventData><Data Name=\"param1\">{SecurityElement.Escape(service)}</Data><Data Name=\"param2\">1</Data></EventData></Event>";
    }

    private static async Task RequireCanceled(Func<Task<IReadOnlyList<WorkflowCheck>>> action)
    {
        try
        {
            await action();
            throw new Exception("Canceled infrastructure service history completed unexpectedly.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
