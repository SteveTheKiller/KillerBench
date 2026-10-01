using KillerBench.Engine;

internal static class DiagnosticReadinessSummaryChecks
{
    internal static void Run()
    {
        var clear = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.NetworkPathReadiness,
            [new("Adapter", CheckState.Clear, "fixture"), new("Route", CheckState.Information, "fixture")]);
        Require(clear.State == CheckState.Clear && clear.Repair is null && clear.Detail.Contains("0 need attention, 0 incomplete and 2 clear or informational") &&
            clear.Detail.Contains("gateway, DNS configuration, DNS cache, DNS Client event, Winsock, TCP setting", StringComparison.Ordinal) &&
            clear.Detail.Contains("operational correlation evidence", StringComparison.Ordinal) &&
            clear.Detail.Contains("does not establish overall machine or feature health"),
            "Network readiness declares gateway, Winsock and operational correlation evidence without a repair or broad health claim");

        var storage = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.StorageReadiness,
            [new("Topology", CheckState.Information, "fixture"), new("Jobs", CheckState.Clear, "fixture")]);
        Require(storage.State == CheckState.Clear && storage.Repair is null &&
            storage.Detail.Contains("quota, reliability, Storage Spaces, current job, dirty-bit, exact service history, event evidence and operational correlation", StringComparison.Ordinal),
            "Storage readiness exposes quota, current jobs, service history, event evidence and operational correlation without selecting a repair");

        var attention = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.PrintingReadiness,
            [new("Queue", CheckState.Attention, "fixture"), new("Port", CheckState.Clear, "fixture")]);
        Require(attention.State == CheckState.Attention && attention.Repair is null && attention.Detail.Contains("1 need attention"),
            "An aggregate problem produces an attention verdict without selecting a repair");
        Require(attention.Detail.Contains("current Print Spooler state and operational correlation", StringComparison.Ordinal),
            "Printing readiness declares its current service and operational correlation boundary");

        var serviceReadiness = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.ServiceReadiness,
            [new("Inventory", CheckState.Information, "fixture"), new("Triggers", CheckState.Information, "fixture")]);
        Require(serviceReadiness.State == CheckState.Clear && serviceReadiness.Repair is null && serviceReadiness.Detail.Contains("startup and dependency configuration, executable presence and path quoting, logon class, registered DLL presence, recovery policy, trigger", StringComparison.Ordinal),
            "Service readiness exposes startup and dependency configuration, executable presence, path quoting, logon class, registered DLL and trigger evidence without selecting a repair");
        Require(serviceReadiness.Detail.Contains("Service Control Manager evidence and operational correlation", StringComparison.Ordinal),
            "Service readiness declares current, historical and operational correlation evidence");

        var update = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.WindowsUpdateReadiness,
            [new("Update source", CheckState.Clear, "fixture"), new("Network path", CheckState.Information, "fixture")]);
        Require(update.State == CheckState.Clear && update.Name == "Windows Update readiness verdict" &&
            update.Detail.Contains("update context, exact servicing-chain state, registered service DLL, exact service failure history, restart, servicing, component repair source, capacity, reserved storage, transfer, network and operational correlation evidence"),
            "Windows Update readiness exposes servicing-chain, capacity, transfer, network and operational correlation evidence in its bounded scope");

        var backup = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.BackupReadiness,
            [new("VSS writers", CheckState.Clear, "fixture"), new("Storage", CheckState.Attention, "fixture")]);
        Require(backup.State == CheckState.Attention && backup.Name == "Backup readiness verdict" && backup.Repair is null &&
            backup.Detail.Contains("Windows Backup summary, File History state and events, VSS history, writer, provider, current shadow-copy, exact service history, restore-point, storage, restart and operational correlation evidence"),
            "Backup readiness exposes its bounded evidence scope without claiming backup success or selecting a repair");

        var stability = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.SystemStabilityReadiness,
            [new("Vitals", CheckState.Clear, "fixture"), new("Crash history", CheckState.Unknown, "fixture")]);
        Require(stability.State == CheckState.Unknown && stability.Name == "System stability readiness verdict" &&
            stability.Repair is null && stability.Detail.Contains("current vital, device, reliability, hardware-error, crash, shutdown and operational correlation evidence"),
            "System stability readiness preserves incomplete evidence without claiming machine health or selecting a repair");

        var security = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.SecurityReadiness,
            [new("Security hardware", CheckState.Clear, "fixture"), new("Threat history", CheckState.Information, "fixture")]);
        Require(security.State == CheckState.Clear && security.Name == "Security readiness verdict" &&
            security.Repair is null && security.Detail.Contains("security hardware, certificate, licensing, protection-product, Defender, restart and operational correlation evidence"),
            "Security readiness exposes its bounded evidence scope without claiming overall protection or selecting a repair");

        var applications = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.ApplicationDeploymentReadiness,
            [new("Registered packages", CheckState.Clear, "fixture"), new("Application history", CheckState.Attention, "fixture")]);
        Require(applications.State == CheckState.Attention && applications.Name == "Application deployment readiness verdict" &&
            applications.Repair is null && applications.Detail.Contains("registered and provisioned packaged-app, machine and current-account Win32 registration, Installer, AppX, Store, application-failure, restart and operational correlation evidence"),
            "Application deployment readiness exposes its bounded evidence scope without claiming application operation or selecting a repair");

        var services = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.ServiceReadiness,
            [new("Inventory", CheckState.Information, "fixture"), new("Transitions", CheckState.Attention, "fixture")]);
        Require(services.State == CheckState.Attention && services.Name == "Service readiness verdict" && services.Repair is null &&
            services.Detail.Contains("installed service, startup and dependency configuration, executable presence and path quoting, logon class, registered DLL presence, recovery policy, trigger, repeated transition state, focused current state, Service Control Manager evidence and operational correlation"),
            "Service readiness exposes startup and dependency configuration, executable presence, path quoting, logon class, registered DLL presence, recovery policy, trigger and repeated transition evidence without selecting a repair");

        var incomplete = DiagnosticReadinessSummary.Check(CommonDiagnosticScope.InstallationReadiness,
            [new("Installer", CheckState.Clear, "fixture", Evidence: new(5, "failed", "", false)),
                new("Store", CheckState.Attention, "fixture")]);
        Require(incomplete.State == CheckState.Unknown && incomplete.Repair is null && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete effective evidence takes precedence over an attention finding");
        Require(incomplete.Detail.Contains("current service state, engine, service registration", StringComparison.Ordinal) &&
            incomplete.Detail.Contains("operational correlation", StringComparison.Ordinal),
            "Installation readiness declares its current service, registration, exact history and operational correlation boundary");

        foreach (var invalid in new IReadOnlyList<WorkflowCheck>[] { [],
            [.. Enumerable.Range(0, 513).Select(index => new WorkflowCheck("Fixture " + index, CheckState.Clear, "fixture"))] })
        {
            try { DiagnosticReadinessSummary.Check(CommonDiagnosticScope.ServiceReadiness, invalid); throw new Exception("Invalid readiness input accepted"); }
            catch (ArgumentException) { }
        }
        try
        {
            DiagnosticReadinessSummary.Check(CommonDiagnosticScope.Vitals, [new("Fixture", CheckState.Clear, "fixture")]);
            throw new Exception("Unsupported aggregate scope accepted");
        }
        catch (ArgumentOutOfRangeException) { }
        try
        {
            DiagnosticReadinessSummary.Check(CommonDiagnosticScope.StorageReadiness,
                [new("Fixture", CheckState.Clear, "fixture")], new CancellationToken(true));
            throw new Exception("Readiness cancellation ignored");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: aggregate readiness verdicts prioritize incomplete evidence, retain bounded counts and never select repairs.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
