using KillerBench.Engine;

internal static class RemoteDesktopOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Remote Desktop evidence remains informational");
        Require(Check(nla: CheckState.Attention).State == CheckState.Attention, "Current NLA concern requests Remote Desktop review");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current Remote Desktop service concern requests review");
        Require(Check(connection: CheckState.Attention, session: CheckState.Attention, history: CheckState.Attention).State == CheckState.Information,
            "Historical Remote Desktop records alone remain informational");
        Require(Check(session: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete Remote Desktop history takes precedence over current attention");
        var incompleteConfiguration = Inputs().Where(check => check.Name is not
            ("Remote Desktop enablement" or "Remote Desktop Network Level Authentication" or "Remote Desktop listener port")).ToList();
        incompleteConfiguration.Add(new("Remote Desktop configuration", CheckState.Unknown, "fixture"));
        Require(RemoteDesktopOperationalCorrelation.Check(incompleteConfiguration).State == CheckState.Unknown,
            "A single incomplete Remote Desktop configuration result fails closed without requiring fabricated subresults");
        Require(RemoteDesktopOperationalCorrelation.Check(Inputs().Append(Info("Recent Remote Desktop session events")).ToArray()).State == CheckState.Unknown,
            "Duplicate Remote Desktop evidence identity fails closed");
        Require(RemoteDesktopOperationalCorrelation.Check(Inputs().Where(check => check.Name != "Remote Desktop listener port").ToArray()).State == CheckState.Unknown,
            "Missing Remote Desktop configuration identity fails closed");
        try
        {
            RemoteDesktopOperationalCorrelation.Check(Inputs(), new CancellationToken(true));
            throw new Exception("Canceled Remote Desktop correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(nla: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize service recovery or configuration changes", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish effective policy", StringComparison.Ordinal),
            "Remote Desktop conclusion preserves configuration and repair boundaries");
        Console.WriteLine("PASS: Remote Desktop configuration, history and current service readiness correlate without selecting changes.");
    }

    private static WorkflowCheck Check(CheckState nla = CheckState.Information, CheckState connection = CheckState.Information,
        CheckState session = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) =>
        RemoteDesktopOperationalCorrelation.Check(Inputs(nla, connection, session, history, service));

    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState nla = CheckState.Information,
        CheckState connection = CheckState.Information, CheckState session = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        Info("Remote Desktop enablement"),
        new("Remote Desktop Network Level Authentication", nla, "fixture"),
        Info("Remote Desktop listener port"),
        new("Recent Remote Desktop connection events", connection, "fixture"),
        new("Recent Remote Desktop session events", session, "fixture"),
        new("Remote Desktop Services failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
