using KillerBench.Engine;

internal static class DefenderChecks
{
    internal const string Fresh = "{\"mode\":\"Normal\",\"service\":true,\"antivirus\":true,\"realtime\":true,\"stale\":false,\"age\":0,\"version\":\"1.2.3.4\"}";
    private static readonly string Stale = Fresh.Replace("\"stale\":false", "\"stale\":true");

    internal static async Task RunAsync()
    {
        var fresh = new ToolResult(0, Fresh, "", false);
        foreach (bool staleEvidence in new[] { false, true })
        {
            var incomplete = fresh with { Output = staleEvidence ? Stale : Fresh,
                Error = "Status provider reported an error" };
            var finding = DefenderRecovery.Interpret(incomplete);
            Require(finding.State == CheckState.Unknown && finding.Repair is null && finding.Evidence == incomplete,
                "Mixed error output cannot authorize refresh or establish fresh signatures");
            var verification = RecipeChecks.Success(RepairKind.DefenderSignatures);
            verification = verification with { Steps = verification.Steps.Select(step => step.Step.Verification ? step with { Result = incomplete } : step).ToArray() };
            Require(!Repairs.IsVerified(verification), "Incomplete Defender verification cannot report a successful repair");
        }
        Require(DefenderRecovery.Interpret(fresh).State == CheckState.Clear, "Fresh status has limited clear scope");
        Require(DefenderRecovery.Interpret(fresh with { Output = Stale }).Repair == RepairKind.DefenderSignatures, "Only reported stale signatures choose refresh");
        foreach (string output in new[] { Fresh.Replace("Normal", "Passive"), Fresh.Replace("\"service\":true", "\"service\":false"), Fresh.Replace("\"realtime\":true", "\"realtime\":false") })
            Require(DefenderRecovery.Interpret(fresh with { Output = output }).State == CheckState.Attention && DefenderRecovery.Interpret(fresh with { Output = output }).Repair is null, "Inactive protection is preserved");
        foreach (var output in new[] { "{}", "not JSON", Fresh.Replace("\"age\":0", "\"age\":4294967295"), Fresh.Replace("\"stale\":false", "\"stale\":null") })
            Require(DefenderRecovery.Interpret(fresh with { Output = output }).State == CheckState.Unknown, "Malformed status cannot authorize repair");
        Require(DefenderRecovery.Interpret(fresh with { Truncated = true }).State == CheckState.Unknown && DefenderRecovery.Interpret(fresh with { TimedOut = true }).State == CheckState.Unknown, "Incomplete evidence fails closed");
        var eligibleHost = new Host(new(0, Stale, "", false), Mutation(true));
        var eligibleResult = await DefenderRecovery.ExecuteAsync(DefenderRecovery.Step(false), eligibleHost);
        Require(eligibleResult.ExitCode == 0 && eligibleHost.Inspections == 1 && eligibleHost.Refreshes == 1,
            "Fresh exact stale-signature evidence dispatches one bounded refresh");

        var currentHost = new Host(fresh, Mutation(false));
        try
        {
            await DefenderRecovery.ExecuteAsync(DefenderRecovery.Step(false), currentHost);
            throw new Exception("Current signatures reached the refresh command");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(skipped.Evidence.Output == Fresh && currentHost.Inspections == 1 && currentHost.Refreshes == 0,
                "A state change to current signatures skips refresh with retained evidence");
        }

        var inactive = fresh with { Output = Fresh.Replace("Normal", "Passive") };
        var inactiveHost = new Host(inactive, Mutation(false));
        try
        {
            await DefenderRecovery.ExecuteAsync(DefenderRecovery.Step(false), inactiveHost);
            throw new Exception("Inactive Defender protection reached the refresh command");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(error.Message.Contains("fresh exact stale-signature evidence") && inactiveHost.Inspections == 1 && inactiveHost.Refreshes == 0,
                "Changed or inactive Defender protection defers refresh before dispatch");
        }

        var raceHost = new Host(new(0, Stale, "", false), Mutation(false));
        try
        {
            await DefenderRecovery.ExecuteAsync(DefenderRecovery.Step(false), raceHost);
            throw new Exception("Final-boundary current signatures returned fabricated refresh evidence");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(skipped.Evidence.Output.Contains("\"commandDispatched\":false") && raceHost.Inspections == 1 && raceHost.Refreshes == 1,
                "The native refresh boundary reports that no signature command ran when freshness changed after inspection");
        }

        string setup = "function Get-MpComputerStatus { [pscustomobject]@{AMRunningMode='Normal'; AMServiceEnabled=$true; AntivirusEnabled=$true; RealTimeProtectionEnabled=$true; DefenderSignaturesOutOfDate=$true; AntivirusSignatureAge=0; AntivirusSignatureVersion='1.2.3.4'} }; function Update-MpSignature { throw 'FIXTURE_UPDATE_SELECTED' }; ";
        var step = DefenderRecovery.Step(false);
        var args = step.Arguments.ToArray();
        args[^1] = setup + args[^1];
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var native = await new WindowsDefenderRecoveryHost().RunAsync(step with { Arguments = args }, null, stop.Token);
        Require(native.ExitCode != 0 && native.Error.Contains("FIXTURE_UPDATE_SELECTED"),
            "Native command uses the substituted signature provider when its final evidence is stale");

        Console.WriteLine("PASS: Defender refresh requires fresh active stale-signature evidence, reports last-moment skips and uses substituted providers only.");
    }

    private static ToolResult Mutation(bool dispatched) => new(0,
        Stale.TrimEnd('}') + ",\"commandDispatched\":" + dispatched.ToString().ToLowerInvariant() + "}", "", false);

    private sealed class Host(ToolResult inspection, ToolResult mutation) : IDefenderRecoveryHost
    {
        internal int Inspections { get; private set; }
        internal int Refreshes { get; private set; }

        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (step.Verification)
            {
                Inspections++;
                return Task.FromResult(inspection);
            }
            Refreshes++;
            return Task.FromResult(mutation);
        }
    }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
