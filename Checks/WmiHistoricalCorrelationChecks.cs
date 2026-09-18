using KillerBench.Engine;

internal static class WmiHistoricalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            clear.Detail.Contains("independently verified by focused checks", StringComparison.Ordinal),
            "Complete clear WMI histories remain informational without claiming current operation");

        var attention = Check(activity: CheckState.Attention, history: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("fresh repository, provider and service evidence", StringComparison.Ordinal),
            "WMI activity and service failures request focused review without selecting repair");

        var incomplete = Check(activity: CheckState.Unknown, history: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete WMI activity evidence takes precedence over historical attention");

        Require(WmiHistoricalCorrelation.Check(Info("Wrong activity"), Info("WMI service failure history")).State == CheckState.Unknown,
            "Unexpected WMI history identities fail closed");

        try
        {
            WmiHistoricalCorrelation.Check(Info("Recent WMI activity events"), Info("WMI service failure history"),
                new CancellationToken(true));
            throw new Exception("WMI historical correlation ignored cancellation");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: WMI activity and exact service-history correlation is deterministic, privacy-bounded and read-only.");
    }

    private static WorkflowCheck Check(CheckState activity = CheckState.Information,
        CheckState history = CheckState.Information) => WmiHistoricalCorrelation.Check(
            new("Recent WMI activity events", activity, "fixture"),
            new("WMI service failure history", history, "fixture"));

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
