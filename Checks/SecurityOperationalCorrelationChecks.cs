using KillerBench.Engine;

internal static class SecurityOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Invoke();
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete"), "Clear security evidence stays bounded");
        var attention = Invoke(security: [new("TPM", CheckState.Attention, "fixture")]);
        Require(attention is { State: CheckState.Attention, Repair: null }, "Security attention remains review-only");
        var incomplete = Invoke(threats: Info("Microsoft Defender threat history") with { State = CheckState.Unknown });
        Require(incomplete.State == CheckState.Unknown, "Incomplete security evidence takes precedence");
        Require(Invoke(certificates: [Info("Personal certificate expiration")]).State == CheckState.Unknown &&
            Invoke(restart: [Info("Restart"), Info("Restart")]).State == CheckState.Unknown, "Missing and duplicate security identities fail closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Security cancellation ignored"); } catch (OperationCanceledException) { }
        Console.WriteLine("PASS: security operational correlation combines configuration, product, Defender and restart evidence without selecting repair.");
    }
    private static WorkflowCheck Invoke(IReadOnlyList<WorkflowCheck>? security = null, IReadOnlyList<WorkflowCheck>? certificates = null,
        WorkflowCheck? threats = null, IReadOnlyList<WorkflowCheck>? restart = null, CancellationToken token = default) =>
        SecurityOperationalCorrelation.Check(security ?? [Info("TPM")], certificates ??
            [Info("Personal certificate expiration"), Info("Personal certificate chain trust")], [Info("Windows activation")],
            [Info("Registered antivirus products")], threats ?? Info("Microsoft Defender threat history"),
            Info("Recent Microsoft Defender events"), restart ?? [Info("Restart")], token);
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
