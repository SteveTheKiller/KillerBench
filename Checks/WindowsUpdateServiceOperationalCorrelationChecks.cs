using KillerBench.Engine;

internal static class WindowsUpdateServiceOperationalCorrelationChecks
{
    public static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Windows Update Service evidence remains informational");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current Windows Update Service concern requests review");
        Require(Check(registration: CheckState.Attention).State == CheckState.Attention, "Windows Update Service registration concern requests review");
        Require(Check(activity: CheckState.Attention).State == CheckState.Attention, "Windows Update activity concern requests review");
        Require(Check(history: CheckState.Attention).State == CheckState.Attention, "Historical Windows Update Service failures request review");
        Require(Check(service: CheckState.Unknown, history: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete Windows Update Service evidence takes precedence over attention");
        Require(WindowsUpdateServiceOperationalCorrelation.Check(
            Info("Wrong service"), Registration(), Activity(), History()).State == CheckState.Unknown,
            "Unexpected service identity fails closed");
        Require(WindowsUpdateServiceOperationalCorrelation.Check(
            Service(), Info("Wrong registration"), Activity(), History()).State == CheckState.Unknown,
            "Unexpected registration identity fails closed");
        Require(WindowsUpdateServiceOperationalCorrelation.Check(
            Service(), Registration(), Info("Wrong activity"), History()).State == CheckState.Unknown,
            "Unexpected activity identity fails closed");
        Require(WindowsUpdateServiceOperationalCorrelation.Check(
            Service(), Registration(), Activity(), Info("Wrong history")).State == CheckState.Unknown,
            "Unexpected history identity fails closed");
        try
        {
            WindowsUpdateServiceOperationalCorrelation.Check(Service(), Registration(), Activity(), History(), new CancellationToken(true));
            throw new Exception("Canceled Windows Update Service correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(activity: CheckState.Attention);
        Require(attention.Repair is null &&
            attention.Detail.Contains("cannot authorize a service start or update operation", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish source reachability", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not start the service", StringComparison.Ordinal),
            "Windows Update Service conclusion preserves repair and outcome boundaries");
        Console.WriteLine("PASS: Windows Update Service state, registration, live activity and history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState service = CheckState.Information,
        CheckState registration = CheckState.Information, CheckState activity = CheckState.Information,
        CheckState history = CheckState.Information) =>
        WindowsUpdateServiceOperationalCorrelation.Check(Service(service), Registration(registration), Activity(activity), History(history));
    private static WorkflowCheck Service(CheckState state = CheckState.Information) => new("Windows Update state", state, "fixture");
    private static WorkflowCheck Registration(CheckState state = CheckState.Information) => new("Windows Update Service registration", state, "fixture");
    private static WorkflowCheck Activity(CheckState state = CheckState.Information) => new("Windows Update live activity", state, "fixture");
    private static WorkflowCheck History(CheckState state = CheckState.Information) => new("Windows Update service failure history", state, "fixture");
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
