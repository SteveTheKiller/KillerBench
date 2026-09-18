using KillerBench.Engine;

internal static class SecurityCenterOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear.State == CheckState.Information && clear.Detail.Contains("Reported registrations: 2", StringComparison.Ordinal),
            "Complete Security Center evidence remains informational and retains the bounded registration count");
        Require(Check(registrations: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete security-product evidence takes precedence over current service attention");
        Require(Check(history: CheckState.Attention).State == CheckState.Information,
            "Historical Security Center failures alone remain informational");
        var attention = Check(service: CheckState.Attention);
        Require(attention.State == CheckState.Attention && attention.Repair is null &&
            attention.Detail.Contains("cannot authorize recovery or a security change", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not interpret raw provider state", StringComparison.Ordinal),
            "Current Security Center service concern requests review without inventing protection health");
        Require(SecurityCenterOperationalCorrelation.Check(Inputs().Append(Info("Registered antivirus products")).ToArray()).State == CheckState.Unknown,
            "Duplicate Security Center evidence identity fails closed");
        Require(SecurityCenterOperationalCorrelation.Check(Inputs().Where(check => check.Name != "Focused service readiness").ToArray()).State == CheckState.Unknown,
            "Missing Security Center evidence identity fails closed");
        try
        {
            SecurityCenterOperationalCorrelation.Check(Inputs(), new CancellationToken(true));
            throw new Exception("Canceled Security Center correlation completed");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: antivirus registrations, exact history and Security Center service readiness correlate without interpreting protection health.");
    }

    private static WorkflowCheck Check(CheckState registrations = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
        SecurityCenterOperationalCorrelation.Check(Inputs(registrations, history, service));

    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState registrations = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Registered antivirus products", registrations, "2 antivirus product registrations reported by Windows Security Center."),
        new("Security Center service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
