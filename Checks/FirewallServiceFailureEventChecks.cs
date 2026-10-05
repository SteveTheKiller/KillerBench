using System.Collections.Concurrent;
using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class FirewallServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        var commands = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        var displayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["BFE"] = "Base Filtering Engine",
            ["MpsSvc"] = "Windows Defender Firewall"
        };

        var checks = await FirewallServiceFailureEvents.CheckAsync(execute: (step, providerToken) =>
        {
            Require(providerToken.CanBeCanceled, "Every firewall service history provider must be bounded");
            string command = step.Arguments.Last();
            string target = displayNames.Keys.Single(name =>
                command.StartsWith("$target='" + name + "';", StringComparison.Ordinal));
            Require(commands.TryAdd(target, command), "Each firewall service identity must be collected once");
            string[] records = target == "BFE" ? [Event(73, displayNames[target])] : [];
            return Task.FromResult(new ToolResult(0,
                JsonSerializer.Serialize(new
                {
                    complete = true,
                    scanned = records.Length,
                    service = displayNames[target],
                    records
                }), "", false));
        });

        Require(checks.Count == 2 && checks.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() == 2,
            "Firewall service history must return two distinct checks");
        Require(checks.Single(check => check.Name == "Base Filtering Engine failure history") is
            { State: CheckState.Attention, Repair: null },
            "Exact BFE failure history must request review without selecting recovery");
        Require(checks.Single(check => check.Name == "Windows Defender Firewall service failure history") is
            { State: CheckState.Information, Repair: null },
            "Complete empty MpsSvc failure history must remain informational");
        Require(commands.Count == displayNames.Count &&
            commands.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(displayNames.Keys) &&
            commands.Values.All(command => command.Contains("Get-WinEvent", StringComparison.Ordinal) &&
                command.Contains("Get-Service -Name $target", StringComparison.Ordinal) &&
                !command.Contains("Start-Service", StringComparison.Ordinal) &&
                !command.Contains("Restart-Service", StringComparison.Ordinal)),
            "Firewall service histories must use exact identities and remain read only");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await RequireCanceled(() => FirewallServiceFailureEvents.CheckAsync((_, _) => throw new Exception("Canceled firewall service collection must not invoke its provider."),
            token: canceled.Token));
        Console.WriteLine("PASS: exact BFE and Windows Defender Firewall service histories, bounded collection and non-mutating results; fixtures only.");
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
            throw new Exception("Canceled firewall service history completed unexpectedly.");
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
