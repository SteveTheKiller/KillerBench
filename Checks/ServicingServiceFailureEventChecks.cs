using System.Collections.Concurrent;
using System.Security;
using System.Text.Json;
using KillerBench.Engine;

internal static class ServicingServiceFailureEventChecks
{
    internal static async Task RunAsync()
    {
        var commands = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        var displayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["wuauserv"] = "Windows Update",
            ["BITS"] = "Background Intelligent Transfer Service",
            ["CryptSvc"] = "Cryptographic Services",
            ["TrustedInstaller"] = "Windows Modules Installer",
            ["WaaSMedicSvc"] = "Windows Update Medic Service",
            ["UsoSvc"] = "Update Orchestrator Service"
        };

        var checks = await ServicingServiceFailureEvents.CheckAsync(execute: (step, providerToken) =>
        {
            Require(providerToken.CanBeCanceled, "Every servicing service history provider must be bounded");
            string command = step.Arguments.Last();
            string target = displayNames.Keys.Single(name => command.StartsWith("$target='" + name + "';", StringComparison.Ordinal));
            Require(commands.TryAdd(target, command), "Each servicing service identity must be collected once");
            string[] records = target == "BITS" ? [Event(91, displayNames[target])] : [];
            return Task.FromResult(new ToolResult(0,
                JsonSerializer.Serialize(new { complete = true, scanned = records.Length, service = displayNames[target], records }), "", false));
        });

        Require(checks.Count == 6 && checks.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() == 6,
            "Servicing service history must return six distinct checks");
        Require(checks.Single(check => check.Name == "BITS service failure history") is { State: CheckState.Attention, Repair: null },
            "Exact BITS failure history must request review without selecting a repair");
        Require(checks.Where(check => check.Name != "BITS service failure history")
            .All(check => check is { State: CheckState.Information, Repair: null }),
            "Complete empty servicing service histories must remain informational");
        Require(commands.Count == displayNames.Count && commands.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(displayNames.Keys) &&
            commands.Values.All(command => command.Contains("Get-WinEvent", StringComparison.Ordinal) &&
                command.Contains("Get-Service", StringComparison.Ordinal) &&
                !command.Contains("Start-Service", StringComparison.Ordinal) &&
                !command.Contains("Restart-Service", StringComparison.Ordinal)),
            "Servicing service history plans must use exact identities and remain read-only");

        string? medicCommand = null;
        var medic = await ServicingServiceFailureEvents.CheckMedicAsync(execute: (step, providerToken) =>
        {
            Require(providerToken.CanBeCanceled, "Focused Medic history provider must be bounded");
            medicCommand = step.Arguments.Last();
            return Task.FromResult(new ToolResult(0,
                JsonSerializer.Serialize(new { complete = true, scanned = 1, service = "Windows Update Medic Service", records = new[] { Event(92, "Windows Update Medic Service") } }), "", false));
        });
        Require(medic is { Name: "Windows Update Medic failure history", State: CheckState.Attention, Repair: null } &&
            medicCommand is not null && medicCommand.StartsWith("$target='WaaSMedicSvc';", StringComparison.Ordinal) &&
            medicCommand.Contains("Get-WinEvent", StringComparison.Ordinal) && !medicCommand.Contains("Start-Service", StringComparison.Ordinal),
            "Focused Medic history uses the exact read-only service identity and never selects repair");

        foreach (var focused in new[]
        {
            (Kind: WorkflowKind.WindowsUpdateService, Service: "wuauserv", Name: "Windows Update service failure history"),
            (Kind: WorkflowKind.BitsService, Service: "BITS", Name: "BITS service failure history"),
            (Kind: WorkflowKind.CryptographicService, Service: "CryptSvc", Name: "Cryptographic Services failure history"),
            (Kind: WorkflowKind.ModulesInstallerService, Service: "TrustedInstaller", Name: "Windows Modules Installer failure history"),
            (Kind: WorkflowKind.WindowsUpdateMedicService, Service: "WaaSMedicSvc", Name: "Windows Update Medic failure history"),
            (Kind: WorkflowKind.UpdateOrchestratorService, Service: "UsoSvc", Name: "Update Orchestrator failure history")
        })
        {
            string? focusedCommand = null;
            var focusedCheck = await ServicingServiceFailureEvents.CheckFocusedAsync(focused.Kind, execute: (step, _) =>
            {
                focusedCommand = step.Arguments.Last();
                return Task.FromResult(new ToolResult(0,
                    JsonSerializer.Serialize(new { complete = true, scanned = 0, service = displayNames[focused.Service], records = Array.Empty<string>() }), "", false));
            });
            Require(focusedCheck is { State: CheckState.Information, Repair: null } && focusedCheck.Name == focused.Name &&
                focusedCommand is not null && focusedCommand.StartsWith("$target='" + focused.Service + "';", StringComparison.Ordinal),
                "Focused servicing history selects only its exact service identity: " + focused.Kind);
        }
        try
        {
            await ServicingServiceFailureEvents.CheckFocusedAsync(WorkflowKind.Audio, execute: (_, _) =>
                throw new Exception("Unsupported focused history invoked a provider."));
            throw new Exception("Unsupported focused servicing workflow was accepted.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await RequireCanceled(() => ServicingServiceFailureEvents.CheckAsync(canceled.Token,
            (_, _) => throw new Exception("A canceled servicing collection must not invoke its provider.")));
        Console.WriteLine("PASS: six exact servicing service failure histories, parallel bounded collection and non-mutating results; fixtures only.");
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
            throw new Exception("Canceled servicing history collection completed unexpectedly.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
