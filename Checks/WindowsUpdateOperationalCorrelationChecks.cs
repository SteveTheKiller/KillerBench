using KillerBench.Engine;

internal static class WindowsUpdateOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = WindowsUpdateOperationalCorrelation.Check(Evidence());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete") &&
            clear.Detail.Contains("no repair is selected"), "Complete update prerequisites produce a bounded informational result");

        var attention = WindowsUpdateOperationalCorrelation.Check(Evidence(CheckState.Information, CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("1 need attention") &&
            attention.Detail.Contains("before searching for updates"), "Update prerequisite attention remains review-only");

        var incomplete = WindowsUpdateOperationalCorrelation.Check(Evidence(CheckState.Unknown, CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete update prerequisite evidence takes precedence over attention");

        var missing = Evidence().Skip(1).ToArray();
        var duplicated = Evidence().Append(Evidence()[0]).ToArray();
        Require(WindowsUpdateOperationalCorrelation.Check(missing).State == CheckState.Unknown &&
            WindowsUpdateOperationalCorrelation.Check(duplicated).State == CheckState.Unknown,
            "Missing and duplicate update prerequisite identities fail closed");

        try
        {
            WindowsUpdateOperationalCorrelation.Check(Evidence(), new CancellationToken(true));
            throw new Exception("Windows Update operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: Windows Update operational correlation combines servicing, restart, capacity, transfer and network prerequisites without selecting repair.");
    }

    private static WorkflowCheck[] Evidence(CheckState chain = CheckState.Information, CheckState restart = CheckState.Information) =>
    [
        new("Windows Update servicing-chain correlation", chain, "fixture"),
        new("Restart", restart, "fixture"),
        new("Servicing transition readiness", CheckState.Information, "fixture"),
        new("Volume capacity", CheckState.Information, "fixture"),
        new("Windows reserved storage", CheckState.Information, "fixture"),
        new("BITS transfer jobs", CheckState.Information, "fixture"),
        new("Network operational correlation", CheckState.Information, "fixture")
    ];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
