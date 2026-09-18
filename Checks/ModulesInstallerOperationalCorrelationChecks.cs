using KillerBench.Engine;

internal static class ModulesInstallerOperationalCorrelationChecks
{
    public static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Modules Installer evidence remains informational");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention,
            "Current Modules Installer service concern requests review");
        Require(Check(packages: CheckState.Attention).State == CheckState.Attention,
            "Pending servicing packages request review");
        Require(Check(history: CheckState.Attention).State == CheckState.Attention,
            "Historical Modules Installer failures request review without selecting repair");
        Require(Check(service: CheckState.Unknown, packages: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete Modules Installer evidence takes precedence over attention");
        Require(ModulesInstallerOperationalCorrelation.Check(
            Info("Wrong service"), Packages(), History()).State == CheckState.Unknown,
            "Unexpected service identity fails closed");
        Require(ModulesInstallerOperationalCorrelation.Check(
            Service(), Info("Wrong packages"), History()).State == CheckState.Unknown,
            "Unexpected package identity fails closed");
        Require(ModulesInstallerOperationalCorrelation.Check(
            Service(), Packages(), Info("Wrong history")).State == CheckState.Unknown,
            "Unexpected history identity fails closed");
        try
        {
            ModulesInstallerOperationalCorrelation.Check(Service(), Packages(), History(), new CancellationToken(true));
            throw new Exception("Canceled Modules Installer correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(packages: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize a service start or package operation", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish component-store health", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not install, remove, clean up or roll back a package", StringComparison.Ordinal),
            "Modules Installer conclusion preserves repair and servicing boundaries");
        Console.WriteLine("PASS: Windows Modules Installer state, package transitions and exact history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState service = CheckState.Information,
        CheckState packages = CheckState.Information, CheckState history = CheckState.Information) =>
        ModulesInstallerOperationalCorrelation.Check(Service(service), Packages(packages), History(history));

    private static WorkflowCheck Service(CheckState state = CheckState.Information) =>
        new("Windows Modules Installer state", state, "fixture");
    private static WorkflowCheck Packages(CheckState state = CheckState.Information) =>
        new("Servicing packages", state, "fixture");
    private static WorkflowCheck History(CheckState state = CheckState.Information) =>
        new("Windows Modules Installer failure history", state, "fixture");
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
