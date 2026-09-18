using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class FeatureServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        int providerCalls = 0;
        string? capturedCommand = null;
        var displayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["W32Time"] = "Windows Time",
            ["WSearch"] = "Windows Search",
            ["winmgmt"] = "Windows Management Instrumentation",
            ["EventLog"] = "Windows Event Log",
            ["AudioEndpointBuilder"] = "Windows Audio Endpoint Builder",
            ["Audiosrv"] = "Windows Audio"
        };

        var checks = await FeatureServiceFailureEvents.CheckAllAsync(execute: (step, providerToken) =>
        {
            Require(providerToken.CanBeCanceled, "Every feature service history provider must be bounded");
            Interlocked.Increment(ref providerCalls);
            capturedCommand = step.Arguments.Last();
            return Task.FromResult(new ToolResult(0,
                JsonSerializer.Serialize(new
                {
                    complete = true,
                    scanned = 1,
                    items = displayNames.Select(pair => new
                    {
                        target = pair.Key,
                        service = pair.Value,
                        available = true,
                        complete = true,
                        records = pair.Key == "WSearch" ? new[] { Event(127, pair.Value) } : []
                    }).ToArray()
                }), "", false));
        });

        Require(checks.Count == displayNames.Count &&
            checks.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() == displayNames.Count,
            "Feature service history must return six distinct checks");
        Require(checks.Single(check => check.Name == "Windows Search service failure history") is
            { State: CheckState.Attention, Repair: null },
            "Exact Windows Search failure history must request review without selecting recovery");
        Require(checks.Where(check => check.Name != "Windows Search service failure history")
            .All(check => check is { State: CheckState.Information, Repair: null }),
            "Complete empty feature service histories must remain informational");
        Require(providerCalls == 1 && capturedCommand is not null &&
            displayNames.Keys.All(name => capturedCommand.Contains("'" + name + "'", StringComparison.Ordinal)) &&
            capturedCommand.Contains("Get-WinEvent", StringComparison.Ordinal) &&
            capturedCommand.Contains("Get-Service -Name $_", StringComparison.Ordinal) &&
            !capturedCommand.Contains("Start-Service", StringComparison.Ordinal) &&
            !capturedCommand.Contains("Restart-Service", StringComparison.Ordinal),
            "Feature service histories must share one bounded snapshot, use exact identities and remain read only");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await RequireCanceled(() => FeatureServiceFailureEvents.CheckAllAsync(canceled.Token,
            (_, _) => throw new Exception("Canceled feature service collection must not invoke its provider.")));
        try
        {
            await FeatureServiceFailureEvents.CheckAsync((FeatureServiceFailureGroup)99, execute: (_, _) =>
                throw new Exception("Invalid feature group must not invoke its provider."));
            throw new Exception("Invalid feature service group accepted.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        Console.WriteLine("PASS: six exact feature service failure histories share one bounded snapshot and remain non-mutating; fixtures only.");
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
            throw new Exception("Canceled feature service history completed unexpectedly.");
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
