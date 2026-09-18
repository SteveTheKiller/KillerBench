using KillerBench.Engine;

internal static class FileSharingOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            clear.Detail.Contains("client access and data transfer remain unverified", StringComparison.Ordinal),
            "Complete file-sharing evidence remains informational without claiming client access");

        var attention = Check(server: CheckState.Attention, alignment: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("before selecting a focused service or firewall action", StringComparison.Ordinal),
            "SMB and firewall attention requests exact review without selecting repair");

        var incomplete = Check(share: CheckState.Unknown, alignment: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete share evidence takes precedence over firewall attention");

        Require(FileSharingOperationalCorrelation.Check(Info("Wrong server"), Shares(), Alignment()).State == CheckState.Unknown,
            "Unexpected SMB server identity fails closed");
        Require(FileSharingOperationalCorrelation.Check(Server(), [], Alignment()).State == CheckState.Unknown,
            "Missing share coverage fails closed");
        Require(FileSharingOperationalCorrelation.Check(Server(), Shares("Wrong share"), Alignment()).State == CheckState.Unknown,
            "Unexpected share identity fails closed");

        try
        {
            FileSharingOperationalCorrelation.Check(Server(), Shares(), Alignment(), new CancellationToken(true));
            throw new Exception("File sharing operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: SMB configuration, bounded shares and firewall alignment correlate deterministically without selecting repair.");
    }

    private static WorkflowCheck Check(CheckState server = CheckState.Information,
        CheckState share = CheckState.Information, CheckState alignment = CheckState.Information) =>
        FileSharingOperationalCorrelation.Check(Server(server), Shares(state: share), Alignment(alignment));

    private static WorkflowCheck Server(CheckState state = CheckState.Information) =>
        new("SMB server configuration", state, "fixture");
    private static WorkflowCheck[] Shares(string name = "SMB shares", CheckState state = CheckState.Information) =>
        [new(name, state, "fixture")];
    private static WorkflowCheck Alignment(CheckState state = CheckState.Information) =>
        new("Active network firewall alignment", state, "fixture");
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
