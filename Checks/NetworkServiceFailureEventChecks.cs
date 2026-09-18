using System.Collections.Concurrent;
using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class NetworkServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        var commands = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        var displayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Dhcp"] = "DHCP Client",
            ["Dnscache"] = "DNS Client",
            ["LanmanWorkstation"] = "Workstation",
            ["NlaSvc"] = "Network Location Awareness",
            ["netprofm"] = "Network List Service",
            ["iphlpsvc"] = "IP Helper"
        };

        var checks = await NetworkServiceFailureEvents.CheckAsync(execute: (step, providerToken) =>
        {
            Require(providerToken.CanBeCanceled, "Every network service history provider must be bounded");
            string command = step.Arguments.Last();
            string target = displayNames.Keys.Single(name =>
                command.StartsWith("$target='" + name + "';", StringComparison.Ordinal));
            Require(commands.TryAdd(target, command), "Each network service identity must be collected once");
            string[] records = target == "Dnscache" ? [Event(87, displayNames[target])] : [];
            return Task.FromResult(new ToolResult(0,
                JsonSerializer.Serialize(new
                {
                    complete = true,
                    scanned = records.Length,
                    service = displayNames[target],
                    records
                }), "", false));
        });

        Require(checks.Count == displayNames.Count &&
            checks.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() == displayNames.Count,
            "Network service history must return six distinct checks");
        Require(checks.Single(check => check.Name == "DNS Client failure history") is
            { State: CheckState.Attention, Repair: null },
            "Exact DNS Client failure history must request review without selecting recovery");
        Require(checks.Where(check => check.Name != "DNS Client failure history")
            .All(check => check is { State: CheckState.Information, Repair: null }),
            "Complete empty network service histories must remain informational");
        Require(commands.Count == displayNames.Count &&
            commands.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(displayNames.Keys) &&
            commands.Values.All(command => command.Contains("Get-WinEvent", StringComparison.Ordinal) &&
                command.Contains("Get-Service -Name $target", StringComparison.Ordinal) &&
                !command.Contains("Start-Service", StringComparison.Ordinal) &&
                !command.Contains("Restart-Service", StringComparison.Ordinal)),
            "Network service histories must use exact identities and remain read only");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await RequireCanceled(() => NetworkServiceFailureEvents.CheckAsync(canceled.Token,
            (_, _) => throw new Exception("Canceled network service collection must not invoke its provider.")));
        Console.WriteLine("PASS: six exact network service failure histories, bounded collection and non-mutating results; fixtures only.");
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
            throw new Exception("Canceled network service history completed unexpectedly.");
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
