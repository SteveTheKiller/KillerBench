using KillerBench.Engine;

internal static class WindowsUpdateMedicOperationalCorrelationChecks
{
    public static void Run()
    {
        Require(Check().State == CheckState.Information,
            "Complete clear Windows Update Medic evidence remains informational");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention,
            "Current Windows Update Medic service concern requests review");
        Require(Check(registration: CheckState.Attention).State == CheckState.Attention,
            "Windows Update Medic registration concern requests review");
        Require(Check(activity: CheckState.Attention).State == CheckState.Attention,
            "Windows Update activity concern requests review");
        Require(Check(history: CheckState.Attention).State == CheckState.Attention,
            "Historical Windows Update Medic failures request review");
        Require(Check(registration: CheckState.Unknown, activity: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete Windows Update Medic evidence takes precedence over attention");
        Require(WindowsUpdateMedicOperationalCorrelation.Check(
            Info("Wrong service"), Registration(), Activity(), History()).State == CheckState.Unknown,
            "Unexpected service identity fails closed");
        Require(WindowsUpdateMedicOperationalCorrelation.Check(
            Service(), Info("Wrong registration"), Activity(), History()).State == CheckState.Unknown,
            "Unexpected registration identity fails closed");
        Require(WindowsUpdateMedicOperationalCorrelation.Check(
            Service(), Registration(), Info("Wrong activity"), History()).State == CheckState.Unknown,
            "Unexpected activity identity fails closed");
        Require(WindowsUpdateMedicOperationalCorrelation.Check(
            Service(), Registration(), Activity(), Info("Wrong history")).State == CheckState.Unknown,
            "Unexpected history identity fails closed");
        try
        {
            WindowsUpdateMedicOperationalCorrelation.Check(Service(), Registration(), Activity(), History(), new CancellationToken(true));
            throw new Exception("Canceled Windows Update Medic correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(activity: CheckState.Attention);
        Require(attention.Repair is null &&
            attention.Detail.Contains("cannot authorize a service start or component action", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish remediation success", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not start the service", StringComparison.Ordinal),
            "Windows Update Medic conclusion preserves repair and outcome boundaries");
        Console.WriteLine("PASS: Windows Update Medic state, registration, live activity and history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState service = CheckState.Information,
        CheckState registration = CheckState.Information, CheckState activity = CheckState.Information,
        CheckState history = CheckState.Information) =>
        WindowsUpdateMedicOperationalCorrelation.Check(Service(service), Registration(registration), Activity(activity), History(history));

    private static WorkflowCheck Service(CheckState state = CheckState.Information) =>
        new("Windows Update Medic Service state", state, "fixture");
    private static WorkflowCheck Registration(CheckState state = CheckState.Information) =>
        new("Windows Update Medic registration", state, "fixture");
    private static WorkflowCheck Activity(CheckState state = CheckState.Information) =>
        new("Windows Update live activity", state, "fixture");
    private static WorkflowCheck History(CheckState state = CheckState.Information) =>
        new("Windows Update Medic failure history", state, "fixture");
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
