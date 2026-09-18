using KillerBench.Engine;

internal static class WindowsErrorReportingOperationalCorrelationChecks
{
    public static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Windows Error Reporting evidence remains informational");
        Require(Check(configuration: CheckState.Attention).State == CheckState.Attention, "Concerning crash configuration requests review");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current service concern requests review");
        Require(Check(history: CheckState.Attention).State == CheckState.Information, "Historical service failures alone remain informational");
        Require(Check(reports: CheckState.Attention).State == CheckState.Information, "Historical crash reports alone remain informational");
        Require(Check(files: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete dump evidence takes precedence over current attention");
        Require(WindowsErrorReportingOperationalCorrelation.Check(Inputs().Append(Info("Crash dump files")).ToArray()).State == CheckState.Unknown,
            "Duplicate evidence identity fails closed");
        Require(WindowsErrorReportingOperationalCorrelation.Check(Inputs().Where(check => check.Name != "Recent system crash reports").ToArray()).State == CheckState.Unknown,
            "Missing evidence identity fails closed");
        try
        {
            WindowsErrorReportingOperationalCorrelation.Check(Inputs(), new CancellationToken(true));
            throw new Exception("Canceled Windows Error Reporting correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(service: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize service recovery or report handling", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish report creation", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not start a service", StringComparison.Ordinal),
            "Windows Error Reporting conclusion preserves repair and outcome boundaries");
        Console.WriteLine("PASS: Windows Error Reporting crash evidence, exact history and service readiness correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState configuration = CheckState.Information,
        CheckState files = CheckState.Information, CheckState reports = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
        WindowsErrorReportingOperationalCorrelation.Check(Inputs(configuration, files, reports, history, service));
    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState configuration = CheckState.Information,
        CheckState files = CheckState.Information, CheckState reports = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Crash dump configuration", configuration, "fixture"),
        new("Crash dump files", files, "fixture"),
        new("Recent system crash reports", reports, "fixture"),
        new("Windows Error Reporting service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
