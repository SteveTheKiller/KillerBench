using KillerBench.Engine;

internal static class WmiRecoveryGateChecks
{
    internal static async Task RunAsync()
    {
        var inconsistent = WmiRecovery.Interpret(new ToolResult(1358, "WMI repository is inconsistent", "", false));
        var consistent = WmiRecovery.Interpret(new ToolResult(0, "WMI repository is consistent", "", false));
        Require(WmiRecovery.IsEligible(inconsistent) && !WmiRecovery.IsEligible(consistent),
            "Only exact freshly interpreted repository inconsistency authorizes salvage");
        Require(!WmiRecovery.IsEligible(inconsistent with
        {
            Evidence = new ToolResult(0, "WMI repository is consistent", "", false)
        }), "Caller labels cannot override contradictory WMI evidence");

        var eligibleHost = new Host(new ToolResult(1358, "WMI repository is inconsistent", "", false));
        var eligibleResult = await WmiRecovery.ExecuteAsync(WmiRecovery.Step(false), eligibleHost);
        Require(eligibleResult.ExitCode == 0 && eligibleHost.Verifications == 1 && eligibleHost.Salvages == 1,
            "Fresh exact inconsistency dispatches one bounded salvage command");

        var changedHost = new Host(new ToolResult(0, "WMI repository is consistent", "", false));
        try
        {
            await WmiRecovery.ExecuteAsync(WmiRecovery.Step(false), changedHost);
            throw new Exception("A consistent WMI repository reached salvage");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(skipped.Evidence.Output.Contains("consistent") && changedHost.Verifications == 1 && changedHost.Salvages == 0,
                "A state change to consistent skips WMI salvage with retained evidence");
        }

        var unavailableHost = new Host(new ToolResult(5, "", "provider failed", false));
        try
        {
            await WmiRecovery.ExecuteAsync(WmiRecovery.Step(false), unavailableHost);
            throw new Exception("Unavailable WMI evidence reached salvage");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(unavailableHost.Verifications == 1 && unavailableHost.Salvages == 0 &&
                error.Message.Contains("deferred", StringComparison.OrdinalIgnoreCase),
                "Unavailable fresh WMI evidence defers salvage without dispatch");
        }

        var verifyHost = new Host(new ToolResult(0, "WMI repository is consistent", "", false));
        await WmiRecovery.ExecuteAsync(WmiRecovery.Step(true), verifyHost);
        Require(verifyHost.Verifications == 1 && verifyHost.Salvages == 0,
            "WMI verification dispatches only the exact read-only command");
        try
        {
            await WmiRecovery.ExecuteAsync(new RepairStep(WmiRecovery.Tool, ["/resetrepository"], false), eligibleHost);
            throw new Exception("Unsupported WMI command was accepted");
        }
        catch (ArgumentException)
        {
            Require(eligibleHost.Salvages == 1, "Unsupported WMI command is rejected before provider dispatch");
        }

        Console.WriteLine("PASS: WMI salvage requires a fresh exact inconsistency recheck immediately before simulated dispatch.");
    }

    private sealed class Host(ToolResult verification) : IWmiRecoveryHost
    {
        public int Verifications { get; private set; }
        public int Salvages { get; private set; }

        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (step.Verification)
            {
                Verifications++;
                return Task.FromResult(verification);
            }
            Salvages++;
            return Task.FromResult(new ToolResult(0, "WMI repository has been salvaged", "", false));
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
