using KillerBench.Engine;

internal static class ServiceRecoveryPolicyChecks
{
    internal static void Run()
    {
        var inventoryCheck = InventoryCheck("alpha", "beta", "gamma", "delta");
        var inventory = ServiceInventoryDiagnostics.Parse(inventoryCheck.Evidence!.Output);
        var snapshot = new ServiceRecoveryPolicySnapshot(true,
        [
            Entry("alpha", new ServiceRecoveryAction("Restart", 60_000)),
            Entry("beta", new ServiceRecoveryAction("Reboot", 120_000)),
            Entry("gamma", new ServiceRecoveryAction("RunCommand", 5_000)) with { AppliesToNonCrashFailures = true },
            Entry("delta")
        ]);
        var evidence = new ToolResult(0, "fixture", "", false);
        var result = ServiceRecoveryPolicyDiagnostics.Assess(snapshot, inventory, evidence);
        Require(result.State == CheckState.Information && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("1 configure service restart", StringComparison.Ordinal) &&
            result.Detail.Contains("1 configure computer restart", StringComparison.Ordinal) &&
            result.Detail.Contains("1 configure a recovery command", StringComparison.Ordinal) &&
            result.Detail.Contains("1 apply actions to non-crash failures", StringComparison.Ordinal) &&
            result.Detail.Contains("1 have no effective action", StringComparison.Ordinal),
            "Complete service recovery policy is summarized without judging configured actions");

        var collected = ServiceRecoveryPolicyDiagnostics.CheckAsync(inventoryCheck, collect: (_, _) => Task.FromResult(snapshot)).GetAwaiter().GetResult();
        Require(collected.State == CheckState.Information && collected.Evidence is not null &&
            !collected.Evidence.Output.Contains("commandLine", StringComparison.OrdinalIgnoreCase) &&
            !collected.Evidence.Output.Contains("rebootMessage", StringComparison.OrdinalIgnoreCase),
            "Retained recovery evidence excludes command and reboot-message fields");

        foreach (var invalid in new[]
        {
            snapshot with { Complete = false },
            snapshot with { Services = [.. snapshot.Services.Take(3)] },
            snapshot with { Services = [snapshot.Services[0], snapshot.Services[0], snapshot.Services[2], snapshot.Services[3]] },
            snapshot with { Services = [snapshot.Services[0] with { Name = "foreign" }, snapshot.Services[1], snapshot.Services[2], snapshot.Services[3]] },
            snapshot with { Services = [snapshot.Services[0] with { Readable = false }, snapshot.Services[1], snapshot.Services[2], snapshot.Services[3]] },
            snapshot with { Services = [snapshot.Services[0] with { Actions = [new("Unsupported", 0)] }, snapshot.Services[1], snapshot.Services[2], snapshot.Services[3]] },
            snapshot with { Services = [snapshot.Services[0] with { Actions = [.. Enumerable.Repeat(new ServiceRecoveryAction("None", 0), 17)] }, snapshot.Services[1], snapshot.Services[2], snapshot.Services[3]] }
        })
            Require(ServiceRecoveryPolicyDiagnostics.Assess(invalid, inventory).State == CheckState.Unknown,
                "Incomplete, mismatched, unreadable and malformed recovery evidence remains unknown");

        var unavailable = inventoryCheck with { State = CheckState.Unknown };
        int calls = 0;
        var blocked = ServiceRecoveryPolicyDiagnostics.CheckAsync(unavailable, collect: (_, _) =>
        {
            calls++;
            return Task.FromResult(snapshot);
        }).GetAwaiter().GetResult();
        Require(blocked.State == CheckState.Unknown && calls == 0, "Incomplete inventory prevents recovery-policy collection");
        var failed = ServiceRecoveryPolicyDiagnostics.CheckAsync(inventoryCheck,
            collect: (_, _) => throw new UnauthorizedAccessException("fixture")).GetAwaiter().GetResult();
        Require(failed.State == CheckState.Unknown && failed.Repair is null, "Collector failure remains isolated and unknown");
        Require(ServiceRecoveryPolicyDiagnostics.CheckAsync(inventoryCheck,
            collect: (_, _) => (Task<ServiceRecoveryPolicySnapshot>)null!).GetAwaiter().GetResult().State == CheckState.Unknown &&
            ServiceRecoveryPolicyDiagnostics.CheckAsync(inventoryCheck,
            collect: (_, _) => Task.FromResult<ServiceRecoveryPolicySnapshot>(null!)).GetAwaiter().GetResult().State == CheckState.Unknown,
            "Missing collector task and result remain unknown");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try
        {
            ServiceRecoveryPolicyDiagnostics.CheckAsync(inventoryCheck, canceled.Token, (_, _) => Task.FromResult(snapshot)).GetAwaiter().GetResult();
            throw new Exception("Recovery-policy cancellation ignored");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: bounded service recovery policy types, privacy exclusions and incomplete evidence; no service changes.");
    }

    private static ServiceRecoveryPolicyEntry Entry(string name, params ServiceRecoveryAction[] actions) =>
        new(name, true, 86_400, false, actions);

    private static WorkflowCheck InventoryCheck(params string[] names)
    {
        var services = names.Select(name => new
        {
            name,
            displayName = name.ToUpperInvariant(),
            state = "Running",
            startMode = "Auto",
            delayedAutoStart = false,
            serviceType = "Own Process"
        });
        var evidence = new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(new { complete = true, services }), "", false);
        return ServiceInventoryDiagnostics.Interpret(evidence);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
