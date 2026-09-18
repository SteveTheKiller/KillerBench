using KillerBench.Engine;

internal static class ServiceOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = ServiceOperationalCorrelation.Check(Structural(), Events());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete") &&
            clear.Detail.Contains("no repair is selected"), "Complete service evidence produces a bounded informational result");

        var attention = ServiceOperationalCorrelation.Check(Structural(CheckState.Information, CheckState.Attention), Events(CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("2 need attention") &&
            attention.Detail.Contains("Historical failures do not establish"), "Structural and historical service attention remains review-only");

        var incomplete = ServiceOperationalCorrelation.Check(Structural(CheckState.Unknown), Events(CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete service evidence takes precedence over attention");

        var wrongIdentity = Structural();
        wrongIdentity[0] = wrongIdentity[0] with { Name = "Other inventory" };
        Require(ServiceOperationalCorrelation.Check(wrongIdentity, Events()).State == CheckState.Unknown &&
            ServiceOperationalCorrelation.Check(Structural(), Events() with { Name = "Other events" }).State == CheckState.Unknown,
            "Unexpected structural and history identities fail closed");

        try
        {
            ServiceOperationalCorrelation.Check(Structural(), Events(), new CancellationToken(true));
            throw new Exception("Service operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: service operational correlation combines registration, configuration, current state and history without selecting recovery.");
    }

    private static WorkflowCheck[] Structural(CheckState inventory = CheckState.Information, CheckState startup = CheckState.Information) =>
    [
        new("Installed service inventory", inventory, "fixture"),
        new("Service startup configuration", startup, "fixture"),
        new("Service binary registrations", CheckState.Information, "fixture"),
        new("Service executable path quoting", CheckState.Information, "fixture"),
        new("Service logon configuration", CheckState.Information, "fixture"),
        new("Registered service DLLs", CheckState.Information, "fixture"),
        new("Service recovery policy", CheckState.Information, "fixture"),
        new("Service dependency configuration", CheckState.Information, "fixture"),
        new("Service triggers", CheckState.Information, "fixture"),
        new("Persistent service transitions", CheckState.Information, "fixture")
    ];

    private static WorkflowCheck Events(CheckState state = CheckState.Information) => new("Service failure correlation", state, "fixture");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
