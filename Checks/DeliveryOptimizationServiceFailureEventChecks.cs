using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class DeliveryOptimizationServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        var now = new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);
        string Event(long recordId = 42, string service = "Delivery Optimization") =>
            $"<Event xmlns=\"http://schemas.microsoft.com/win/2004/08/events/event\"><System><Provider Name=\"Service Control Manager\"/><EventID>7031</EventID><Level>2</Level><TimeCreated SystemTime=\"{now.AddHours(-1):O}\"/><EventRecordID>{recordId}</EventRecordID></System><EventData><Data Name=\"param1\">{SecurityElement.Escape(service)}</Data><Data Name=\"param2\">1</Data></EventData></Event>";
        ToolResult Evidence(string[] records, bool complete = true, string service = "Delivery Optimization") =>
            new(0, JsonSerializer.Serialize(new { complete, scanned = records.Length, service, records }), "", false);

        Require(DeliveryOptimizationServiceFailureEvents.Interpret(Evidence([]), now) is
            { Name: "Delivery Optimization failure history", State: CheckState.Information, Repair: null },
            "Complete empty Delivery Optimization history remains informational");
        var failure = DeliveryOptimizationServiceFailureEvents.Interpret(Evidence([Event()]), now);
        Require(failure is { State: CheckState.Attention, Repair: null } &&
            failure.Detail.Contains("do not establish a current outage", StringComparison.Ordinal) &&
            failure.Detail.Contains("authorize a service continue", StringComparison.Ordinal),
            "Exact Delivery Optimization history requests review without authorizing continuation");
        Require(DeliveryOptimizationServiceFailureEvents.Interpret(Evidence([Event()], false), now).State == CheckState.Unknown &&
            DeliveryOptimizationServiceFailureEvents.Interpret(Evidence([Event(service: "Other")]), now).State == CheckState.Unknown,
            "Incomplete and unrelated Delivery Optimization history remains unknown");

        RepairStep? planned = null;
        var collected = await DeliveryOptimizationServiceFailureEvents.CheckAsync(execute: (step, token) =>
        {
            token.ThrowIfCancellationRequested();
            planned = step;
            return Task.FromResult(Evidence([]));
        });
        Require(collected.State == CheckState.Information && planned is { Verification: true } &&
            planned.Arguments.Last().StartsWith("$target='DoSvc';", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Get-WinEvent", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Continue-Service", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Start-Service", StringComparison.Ordinal),
            "Delivery Optimization history plan is exact, bounded and non-mutating");
        Console.WriteLine("PASS: exact Delivery Optimization failure history is bounded, non-mutating and cannot authorize continuation; fixtures only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
