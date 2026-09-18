using KillerBench.Engine;

internal static class CryptographicServiceOperationalCorrelationChecks
{
    public static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear cryptographic evidence remains informational");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention,
            "Current service concern requests review");
        Require(Check(registration: CheckState.Attention).State == CheckState.Attention,
            "Registered DLL concern requests review");
        Require(Check(history: CheckState.Attention).State == CheckState.Attention,
            "Historical service failures request review without selecting repair");
        Require(Check(service: CheckState.Unknown, registration: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete cryptographic evidence takes precedence over attention");
        Require(CryptographicServiceOperationalCorrelation.Check(
            Info("Wrong service"), Registration(), History()).State == CheckState.Unknown,
            "Unexpected service identity fails closed");
        Require(CryptographicServiceOperationalCorrelation.Check(
            Service(), Info("Wrong registration"), History()).State == CheckState.Unknown,
            "Unexpected registration identity fails closed");
        Require(CryptographicServiceOperationalCorrelation.Check(
            Service(), Registration(), Info("Wrong history")).State == CheckState.Unknown,
            "Unexpected history identity fails closed");
        try
        {
            CryptographicServiceOperationalCorrelation.Check(Service(), Registration(), History(), new CancellationToken(true));
            throw new Exception("Canceled cryptographic correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(history: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize a service start", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not validate certificates, signatures, catalogs, trust chains", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not replace a DLL", StringComparison.Ordinal),
            "Cryptographic conclusion preserves repair and validation boundaries");
        Console.WriteLine("PASS: Cryptographic Services state, registered DLL and exact history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState service = CheckState.Information,
        CheckState registration = CheckState.Information, CheckState history = CheckState.Information) =>
        CryptographicServiceOperationalCorrelation.Check(Service(service), Registration(registration), History(history));

    private static WorkflowCheck Service(CheckState state = CheckState.Information) =>
        new("Cryptographic Services state", state, "fixture");
    private static WorkflowCheck Registration(CheckState state = CheckState.Information) =>
        new("Cryptographic Services registration", state, "fixture");
    private static WorkflowCheck History(CheckState state = CheckState.Information) =>
        new("Cryptographic Services failure history", state, "fixture");
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
