using KillerBench.Engine;

internal static class PrintingEnvironmentCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete") &&
            clear.Detail.Contains("no repair is selected"), "Complete printing environment evidence produces a bounded informational result");

        var attention = Check(driverFiles: Group("Printer driver files: Fixture", CheckState.Attention),
            reachability: Group("Printer endpoint reachability: 192.0.2.10:9100", CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("2 need attention"),
            "Driver-file and configured-endpoint findings produce review without selecting repair");

        var incomplete = Check(rendering: Fixed("Print rendering and isolation policy", CheckState.Unknown),
            ports: Group("Printer port: Fixture", CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete printing environment evidence takes precedence over attention");

        Require(Check(drivers: []).State == CheckState.Unknown,
            "Missing printing environment groups fail closed");
        Require(Check(endpoints: Group("Other endpoint")).State == CheckState.Unknown,
            "Unexpected printing environment identities fail closed");

        try
        {
            PrintingEnvironmentCorrelation.Check(Fixed("Point and Print policy"), Fixed("Print rendering and isolation policy"),
                Group("Print queues"), Group("Printer driver files"), Fixed("Printer driver models"), Group("Printer ports"),
                Group("Printer TCP/IP endpoints"), Group("Printer endpoint reachability"), Fixed("Print processors and monitors"),
                Group("Print extension files"), new CancellationToken(true));
            throw new InvalidOperationException("Canceled printing environment correlation completed unexpectedly");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: printing environment correlation combines policy, drivers, files, ports, endpoints and extensions without selecting repair.");
    }

    private static WorkflowCheck Check(WorkflowCheck? pointAndPrint = null, WorkflowCheck? rendering = null,
        IReadOnlyList<WorkflowCheck>? drivers = null, IReadOnlyList<WorkflowCheck>? driverFiles = null,
        WorkflowCheck? driverModels = null, IReadOnlyList<WorkflowCheck>? ports = null,
        IReadOnlyList<WorkflowCheck>? endpoints = null, IReadOnlyList<WorkflowCheck>? reachability = null,
        WorkflowCheck? extensions = null, IReadOnlyList<WorkflowCheck>? extensionFiles = null) =>
        PrintingEnvironmentCorrelation.Check(pointAndPrint ?? Fixed("Point and Print policy"),
            rendering ?? Fixed("Print rendering and isolation policy"), drivers ?? Group("Print queues"),
            driverFiles ?? Group("Printer driver files"), driverModels ?? Fixed("Printer driver models"),
            ports ?? Group("Printer ports"), endpoints ?? Group("Printer TCP/IP endpoints"),
            reachability ?? Group("Printer endpoint reachability"), extensions ?? Fixed("Print processors and monitors"),
            extensionFiles ?? Group("Print extension files"));

    private static WorkflowCheck Fixed(string name, CheckState state = CheckState.Information) => new(name, state, "fixture");
    private static WorkflowCheck[] Group(string name, CheckState state = CheckState.Information) => [new(name, state, "fixture")];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
