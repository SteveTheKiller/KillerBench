using KillerBench.Engine;

internal static class ProfileOperationalCorrelationChecks
{
    internal static void Run()
    {
        const string sid = "S-1-5-21-1-2-3-1001";
        var candidate = new ProfileChoice(new(sid, @"C:\Users\Fixture", false, false,
            DateTimeOffset.Parse("2025-01-01T00:00:00+00:00"), false), true, "fixture");
        var notRequested = ProfileDependencyAssessment.Check([candidate], null);
        Require(notRequested.State == CheckState.Information && notRequested.Detail.Contains("not requested", StringComparison.Ordinal),
            "An omitted optional dependency scope remains explicit");
        var dependency = ProfileDependencyAssessment.Check([candidate],
            new([new("Service", "FixtureService", sid)], true));
        Require(dependency.State == CheckState.Attention && dependency.Detail.Contains("FixtureService", StringComparison.Ordinal),
            "Exact candidate account dependencies request review");
        Require(ProfileDependencyAssessment.Check([candidate], new([], false)).State == CheckState.Unknown,
            "Incomplete dependency evidence fails closed");

        Require(Invoke() is { State: CheckState.Information, Repair: null }, "Complete profile review remains informational");
        Require(Invoke(registrations: Check("Profile registrations", CheckState.Attention)).State == CheckState.Attention,
            "ProfileList concerns request review");
        Require(Invoke(dependencies: Check("Profile account dependencies", CheckState.Attention)).State == CheckState.Attention,
            "Account dependencies request review");
        Require(Invoke(inventory: Check("Profile inventory", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete inventory takes precedence");
        Require(Invoke(dependencies: Check("Unexpected", CheckState.Information)).State == CheckState.Unknown,
            "Unexpected source identity fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Profile correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: profile review correlation separates inventory, registrations and optional dependencies without authorizing removal.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? inventory = null, WorkflowCheck? registrations = null,
        WorkflowCheck? dependencies = null, CancellationToken token = default) => ProfileOperationalCorrelation.Check(
            inventory ?? Check("Profile inventory", CheckState.Information),
            registrations ?? Check("Profile registrations", CheckState.Information),
            dependencies ?? Check("Profile account dependencies", CheckState.Information), token);

    private static WorkflowCheck Check(string name, CheckState state) => new(name, state, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
