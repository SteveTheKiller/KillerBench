using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class PlatformServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        int providerCalls = 0;
        string? capturedCommand = null;
        var displayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppReadiness"] = "App Readiness",
            ["StateRepository"] = "State Repository Service",
            ["ProfSvc"] = "User Profile Service",
            ["VaultSvc"] = "Credential Manager",
            ["ShellHWDetection"] = "Shell Hardware Detection",
            ["DeviceInstall"] = "Device Install Service",
            ["AppXSvc"] = "AppX Deployment Service",
            ["ClipSVC"] = "Client License Service",
            ["InstallService"] = "Microsoft Store Install Service",
            ["LicenseManager"] = "Windows License Manager Service",
            ["PushToInstall"] = "Windows PushToInstall Service",
            ["TokenBroker"] = "Web Account Manager",
            ["wlidsvc"] = "Microsoft Account Sign-in Assistant",
            ["defragsvc"] = "Optimize Drives",
            ["stisvc"] = "Windows Image Acquisition",
            ["fdPHost"] = "Function Discovery Provider Host",
            ["FDResPub"] = "Function Discovery Resource Publication",
            ["hidserv"] = "Human Interface Device Service",
            ["WPDBusEnum"] = "Portable Device Enumerator Service",
            ["FontCache"] = "Windows Font Cache Service",
            ["DeviceAssociationService"] = "Device Association Service",
            ["TextInputManagementService"] = "Text Input Management Service",
            ["DisplayEnhancementService"] = "Display Enhancement Service"
        };

        var checks = await PlatformServiceFailureEvents.CheckAllAsync(execute: (step, providerToken) =>
        {
            Require(providerToken.CanBeCanceled, "Platform service history provider must be bounded");
            Interlocked.Increment(ref providerCalls);
            capturedCommand = step.Arguments.Last();
            return Task.FromResult(new ToolResult(0,
                JsonSerializer.Serialize(new
                {
                    complete = true,
                    scanned = 2,
                    items = displayNames.Select(pair => new
                    {
                        target = pair.Key,
                        service = pair.Value,
                        available = pair.Key != "stisvc",
                        complete = pair.Key != "TextInputManagementService",
                        records = pair.Key == "DeviceInstall" ? new[] { Event(451, pair.Value) } : []
                    }).ToArray()
                }), "", false));
        });

        Require(checks.Count == displayNames.Count && checks.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() == displayNames.Count,
            "Platform service history must return every distinct configured check");
        Require(checks.Single(check => check.Name == "Device Install service failure history") is { State: CheckState.Attention, Repair: null },
            "Exact Device Install failure history must request review without selecting recovery");
        Require(checks.Single(check => check.Name == "Text Input Management service failure history") is { State: CheckState.Unknown, Repair: null },
            "A saturated exact service match set must remain incomplete");
        Require(checks.Single(check => check.Name == "Windows Image Acquisition service failure history") is { State: CheckState.Unknown, Repair: null },
            "One unavailable optional service must remain isolated from the shared snapshot");
        Require(checks.Where(check => check.Name is not ("Device Install service failure history" or "Text Input Management service failure history" or "Windows Image Acquisition service failure history"))
            .All(check => check is { State: CheckState.Information, Repair: null }),
            "Complete empty platform service histories must remain informational");
        Require(providerCalls == 1 && capturedCommand is not null &&
            displayNames.Keys.All(name => capturedCommand.Contains("'" + name + "'", StringComparison.Ordinal)) &&
            capturedCommand.Contains("Get-WinEvent", StringComparison.Ordinal) &&
            capturedCommand.Contains("Get-Service -Name $_", StringComparison.Ordinal) &&
            !capturedCommand.Contains("Start-Service", StringComparison.Ordinal) &&
            !capturedCommand.Contains("Restart-Service", StringComparison.Ordinal),
            "Platform service histories must share one bounded snapshot, use exact identities and remain read only");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await RequireCanceled(() => PlatformServiceFailureEvents.CheckAllAsync(canceled.Token,
            (_, _) => throw new Exception("Canceled platform service collection must not invoke its provider.")));
        try
        {
            await PlatformServiceFailureEvents.CheckAsync((PlatformServiceFailureArea)99, execute: (_, _) =>
                throw new Exception("Invalid platform service area must not invoke its provider."));
            throw new Exception("Invalid platform service area accepted.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        Console.WriteLine($"PASS: {displayNames.Count} exact platform service failure histories share one bounded snapshot and remain non-mutating; fixtures only.");
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
            throw new Exception("Canceled platform service history completed unexpectedly.");
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
