using System.Text.Json;
using KillerBench.Engine;

internal static class ServiceTriggerChecks
{
    internal static async Task RunAsync()
    {
        var installed = new ServiceInventorySnapshot(true,
            [new("Alpha", "Alpha service", "Stopped", "Manual", false, "Own Process"),
             new("Beta", "Beta service", "Running", "Auto", false, "Own Process")]);
        var inventoryEvidence = new ToolResult(0, JsonSerializer.Serialize(new
        {
            complete = true,
            services = installed.Services.Select(service => new
            {
                name = service.Name, displayName = service.DisplayName, state = service.State, startMode = service.StartMode,
                delayedAutoStart = service.DelayedAutoStart, serviceType = service.ServiceType
            })
        }), "", false);
        var inventory = new WorkflowCheck("Inventory", CheckState.Information, "fixture", Evidence: inventoryEvidence);
        var snapshot = new ServiceTriggerSnapshot(true,
            [new("Alpha", true, [new("NetworkEndpoint", "Start", 1), new("IpAddress", "Stop", 0)]), new("Beta", true, [])]);
        var check = ServiceTriggerDiagnostics.Assess(snapshot, installed);
        Require(check is { State: CheckState.Information, Repair: null } &&
            check.Detail.Contains("1 have triggers", StringComparison.Ordinal) && check.Detail.Contains("NetworkEndpoint 1", StringComparison.Ordinal) &&
            !check.Detail.Contains("\\\\pipe\\secret", StringComparison.OrdinalIgnoreCase),
            "Service trigger summary retains bounded types and actions without trigger payload");

        foreach (var invalid in new[]
        {
            snapshot with { Complete = false },
            snapshot with { Services = [snapshot.Services[0]] },
            snapshot with { Services = [snapshot.Services[0], snapshot.Services[0]] },
            snapshot with { Services = [snapshot.Services[0] with { Readable = false }, snapshot.Services[1]] },
            snapshot with { Services = [snapshot.Services[0] with { Name = "Foreign" }, snapshot.Services[1]] },
            snapshot with { Services = [snapshot.Services[0] with { Triggers = [new("Future", "Start", 0)] }, snapshot.Services[1]] },
            snapshot with { Services = [snapshot.Services[0] with { Triggers = [new("Custom", "Pause", 0)] }, snapshot.Services[1]] },
            snapshot with { Services = [snapshot.Services[0] with { Triggers = [new("Custom", "Start", 65)] }, snapshot.Services[1]] }
        }) Require(ServiceTriggerDiagnostics.Assess(invalid, installed).State == CheckState.Unknown,
            "Incomplete, unreadable, foreign, duplicate and unsupported trigger evidence remains unknown");

        IReadOnlyList<string>? requested = null;
        var collected = await ServiceTriggerDiagnostics.CheckAsync(inventory, collect: (names, _) =>
        {
            requested = names;
            return Task.FromResult(snapshot);
        });
        Require(collected.State == CheckState.Information && requested is not null && requested.SequenceEqual(["Alpha", "Beta"]),
            "Trigger collection is bound to the complete installed-service inventory");
        Require((await ServiceTriggerDiagnostics.CheckAsync(new("bad", CheckState.Unknown, "fixture"), collect: (_, _) => throw new Exception())).State == CheckState.Unknown,
            "Incomplete service inventory prevents trigger collection");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await ServiceTriggerDiagnostics.CheckAsync(inventory, (_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents trigger collection");
        Console.WriteLine("PASS: installed service trigger types, actions, payload privacy, bounds and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
