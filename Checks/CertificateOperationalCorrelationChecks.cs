using KillerBench.Engine;

internal static class CertificateOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Information, Repair: null }, "Complete certificate evidence remains informational");
        Require(Invoke(expiration: new("Personal certificate expiration", CheckState.Attention, "fixture")).State == CheckState.Attention,
            "Certificate validity concerns produce attention");
        Require(Invoke(chain: new("Personal certificate chain trust", CheckState.Attention, "fixture")).State == CheckState.Attention,
            "Local chain concerns produce attention");
        Require(Invoke(chain: new("Personal certificate chain trust", CheckState.Unknown, "fixture")).State == CheckState.Unknown,
            "Incomplete evidence takes precedence");
        Require(Invoke(expiration: Info("Unexpected")).State == CheckState.Unknown, "Unexpected identity fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Certificate correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: certificate operational correlation combines bounded validity and local chain evidence without selecting repair.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? expiration = null, WorkflowCheck? chain = null,
        CancellationToken token = default) => CertificateOperationalCorrelation.Check(
            expiration ?? Info("Personal certificate expiration"), chain ?? Info("Personal certificate chain trust"), token);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
