using KillerBench.Engine;

internal static class PrintingOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = PrintingOperationalCorrelation.Check(Spooler(), DefaultPrinter(), Conditions(), Attributes(),
            Jobs(), SpoolDirectory(), Environment(), History());
        Require(clear is { State: CheckState.Clear, Repair: null } &&
            clear.Detail.Contains("current Print Spooler", StringComparison.Ordinal) &&
            clear.Detail.Contains("No issue was established within this bounded printing evidence", StringComparison.Ordinal),
            "Complete current and historical printing evidence produces a bounded clear correlation without selecting repair");

        var attention = PrintingOperationalCorrelation.Check(Spooler(CheckState.Attention), DefaultPrinter(),
            Conditions(CheckState.Attention), Attributes(), Jobs(CheckState.Attention), SpoolDirectory(), Environment(),
            History(CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("Current evidence: 3 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("Service history: review", StringComparison.Ordinal) &&
            attention.Detail.Contains("before selecting a focused action", StringComparison.Ordinal),
            "Spooler, queue, job and historical findings produce one review outcome without choosing an action");

        var incomplete = PrintingOperationalCorrelation.Check(Spooler(), DefaultPrinter(), Conditions(), Attributes(),
            Jobs(), SpoolDirectory(CheckState.Unknown), Environment(), History(CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("cannot be fully correlated", StringComparison.Ordinal),
            "Incomplete current printing evidence takes precedence over historical attention");

        var malformed = PrintingOperationalCorrelation.Check(Spooler(), DefaultPrinter(), [], Attributes(), Jobs(),
            SpoolDirectory(), Environment(), History());
        var wrongIdentity = PrintingOperationalCorrelation.Check(Spooler() with { Name = "Other service" },
            DefaultPrinter(), Conditions(), Attributes(), Jobs(), SpoolDirectory(), Environment(), History());
        Require(malformed.State == CheckState.Unknown && wrongIdentity.State == CheckState.Unknown &&
            malformed.Repair is null && wrongIdentity.Repair is null,
            "Missing result groups and mismatched service identities fail closed without selecting repair");

        var historical = PrintingOperationalCorrelation.Check(Spooler(), DefaultPrinter(), Conditions(), Attributes(), Jobs(),
            SpoolDirectory(), Environment(), History(CheckState.Attention));
        Require(historical is { State: CheckState.Information, Repair: null } &&
            historical.Detail.Contains("Current evidence did not establish a printing problem", StringComparison.Ordinal),
            "Historical service attention alone does not establish a current printing failure");

        try
        {
            PrintingOperationalCorrelation.Check(Spooler(), DefaultPrinter(), Conditions(), Attributes(), Jobs(),
                SpoolDirectory(), Environment(), History(), new CancellationToken(true));
            throw new InvalidOperationException("Canceled printing correlation completed unexpectedly");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: current Print Spooler, queue, job, spool-directory and exact history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Spooler(CheckState state = CheckState.Clear) =>
        new(RepairKind.PrintSpooler.ToString(), state, "fixture");

    private static WorkflowCheck DefaultPrinter(CheckState state = CheckState.Information) =>
        new("Default printer selection", state, "fixture");

    private static WorkflowCheck[] Conditions(CheckState state = CheckState.Information) =>
        [new("Printer condition: Fixture queue", state, "fixture")];

    private static WorkflowCheck[] Attributes(CheckState state = CheckState.Information) =>
        [new("Printer queue attributes: Fixture queue", state, "fixture")];

    private static WorkflowCheck[] Jobs(CheckState state = CheckState.Information) =>
        [new("Print jobs", state, "fixture")];

    private static WorkflowCheck SpoolDirectory(CheckState state = CheckState.Information) =>
        new("Print spool directory", state, "fixture");

    private static WorkflowCheck Environment(CheckState state = CheckState.Information) =>
        new("Printing environment correlation", state, "fixture");

    private static WorkflowCheck History(CheckState state = CheckState.Information) =>
        new("Print Spooler failure history", state, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
