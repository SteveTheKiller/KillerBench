using System.Reflection;
using KillerBench.Engine;

internal static class DhcpChecks
{
    private static readonly int[] SingleTarget = [1];

    internal static async Task RunAsync()
    {
        Require(!DhcpRecovery.HasUsableAddress(["169.254.10.1", "::1", "0.0.0.0", "127.0.0.1", "224.0.0.1"]), "Unusable addresses rejected");
        Require(DhcpRecovery.HasUsableAddress(["192.0.2.10"]), "Usable IPv4 recognized");
        var success = RecipeChecks.Success(RepairKind.DhcpLease);
        Require(Repairs.IsVerified(success), "Matching renewed adapter verifies");
        static ToolResult Detailed(string captured, string expires, int reason, bool targeted = true, bool server = true) => new(0,
            "{\"schema\":2,\"capturedUtc\":\"" + captured + "\",\"targets\":" + (targeted ? "[1]" : "[]") +
            ",\"reasons\":" + (targeted ? "[{\"id\":1,\"reason\":" + reason + "}]" : "[]") +
            ",\"adapters\":[{\"id\":1,\"settingId\":\"11111111-1111-1111-1111-111111111111\",\"addresses\":[\"192.0.2.10\"],\"hasServer\":" + server.ToString().ToLowerInvariant() +
            ",\"leaseObtainedUtc\":\"2026-09-16T10:00:00.0000000+00:00\",\"leaseExpiresUtc\":\"" + expires + "\"}]}", "", false);
        var expiredBefore = Detailed("2026-09-16T12:00:00.0000000+00:00", "2026-09-16T11:00:00.0000000+00:00", 2);
        var renewedAfter = Detailed("2026-09-16T12:01:00.0000000+00:00", "2026-09-16T13:00:00.0000000+00:00", 2, targeted: false);
        var expiredRepair = new RepairResult("Synthetic", RepairKind.DhcpLease, DateTimeOffset.UtcNow, "Verification reported", "synthetic",
            [new(DhcpRecovery.Step(false), expiredBefore), new(DhcpRecovery.Step(true), renewedAfter)]);
        Require(DhcpRecovery.Interpret(expiredBefore) is { State: CheckState.Repairable, Repair: RepairKind.DhcpLease } && Repairs.IsVerified(expiredRepair),
            "Expired coherent lease is repairable and verifies only with a later valid lease on the same adapter");
        var staleSteps = expiredRepair.Steps.ToArray();
        staleSteps[1] = staleSteps[1] with { Result = Detailed("2026-09-16T12:01:00.0000000+00:00", "2026-09-16T11:00:00.0000000+00:00", 2, targeted: false) };
        Require(!Repairs.IsVerified(expiredRepair with { Steps = staleSteps }), "An expired post-repair lease cannot verify renewal");
        var serverlessSteps = expiredRepair.Steps.ToArray();
        serverlessSteps[1] = serverlessSteps[1] with { Result = Detailed("2026-09-16T12:01:00.0000000+00:00", "2026-09-16T13:00:00.0000000+00:00", 2, targeted: false, server: false) };
        Require(!Repairs.IsVerified(expiredRepair with { Steps = serverlessSteps }), "Missing DHCP server evidence cannot verify expired-lease renewal");
        foreach (string identity in new[] { "22222222-2222-2222-2222-222222222222", "00000000-0000-0000-0000-000000000000", "invalid" })
        {
            var changed = success.Steps.ToArray();
            changed[1] = changed[1] with { Result = changed[1].Result with { Output = changed[1].Result.Output.Replace("11111111-1111-1111-1111-111111111111", identity) } };
            Require(!Repairs.IsVerified(success with { Steps = changed }), "Reused index or missing stable identity cannot verify DHCP renewal");
        }
        foreach (string invalid in new[] {
            "{\"targets\":[],\"targets\":[1],\"adapters\":[]}",
            "{\"targets\":[],\"adapters\":[],\"adapters\":[{\"id\":1,\"addresses\":[]}]}",
            "{\"targets\":[],\"adapters\":[{\"id\":1,\"id\":2,\"addresses\":[]}]}",
            "{\"targets\":[],\"adapters\":[{\"id\":1,\"addresses\":[],\"addresses\":[\"192.0.2.1\"]}]}",
            "{\"targets\":[2147483648],\"adapters\":[]}",
            "{\"targets\":[],\"adapters\":[{\"id\":2147483648,\"addresses\":[]}]}", "null" })
        {
            var evidence = new ToolResult(0, invalid, "", false);
            Require(DhcpRecovery.Interpret(evidence) is { State: CheckState.Unknown, Repair: null },
                "Ambiguous or overflowing DHCP evidence cannot authorize renewal");
            foreach (int step in new[] { 0, 1 })
            {
                var changed = success.Steps.ToArray();
                changed[step] = changed[step] with { Result = evidence };
                Require(!Repairs.IsVerified(success with { Steps = changed }), "Ambiguous DHCP evidence cannot verify either repair step");
            }
        }
        var disappeared = success with { Steps = [success.Steps[0], success.Steps[1] with { Result = new(0, "{\"targets\":[],\"adapters\":[{\"id\":2,\"addresses\":[\"192.0.2.10\"]}]}", "", false) }] };
        Require(!Repairs.IsVerified(disappeared), "Different healthy adapter cannot verify missing target");
        Require(DhcpRecovery.Read(new(0, "{\"targets\":[],\"adapters\":[{\"id\":1,\"addresses\":[\"invalid\"]}]}", "", false)) is null, "Malformed addresses cannot verify");

        // Both CIM entry points are replaced in the child process. No machine state is queried or changed.
        const string fixture = """
            $global:lease='169.254.10.1';
            function Get-CimInstance($ClassName,$OperationTimeoutSec) {
                if($ClassName -eq 'Win32_NetworkAdapter') {
                    1..4 | ForEach-Object { [pscustomobject]@{Index=$_;PhysicalAdapter=($_ -ne 3);NetConnectionStatus=$(if($_ -eq 4){7}else{2})} }
                } elseif($ClassName -eq 'Win32_NetworkAdapterConfiguration') {
                    1..4 | ForEach-Object { [pscustomobject]@{Index=$_;DHCPEnabled=($_ -ne 2);SettingID=('11111111-1111-1111-1111-11111111111'+$_);IPEnabled=$true;IPAddress=@($global:lease)} }
                } else { throw 'Unexpected query' }
            }
            function Invoke-CimMethod($InputObject,$MethodName,$OperationTimeoutSec) {
                if($InputObject.Index -ne 1 -or $MethodName -ne 'RenewDHCPLease') { throw 'Wrong renewal target' }
                $global:lease='192.0.2.10'; [pscustomobject]@{ReturnValue=0}
            }
            """;
        var method = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.Infrastructure.NativeTool")!.GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        async Task<ToolResult> RunFixture(string providers, bool readOnly = false)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var args = DhcpRecovery.Step(readOnly).Arguments.ToArray();
            args[^1] = providers + "\n" + args[^1];
            return await ((Task<ToolResult>)method.Invoke(null, [PrintRecovery.Tool, args, false, null, timeout.Token])!);
        }
        var result = await RunFixture(fixture);
        var swapped = await RunFixture(fixture.Replace("('11111111-1111-1111-1111-11111111111'+$_)",
            "$(if($global:lease -eq '192.0.2.10'){'22222222-2222-2222-2222-222222222222'}else{'11111111-1111-1111-1111-11111111111'+$_})"));
        Require(swapped.ExitCode != 0 && swapped.Error.Contains("Adapter identity changed during renewal"), "Native script rejects a reused index after dispatch");
        var diagnostic = await RunFixture(fixture.Replace("$global:lease='192.0.2.10'; [pscustomobject]@{ReturnValue=0}", "throw 'MUTATION_CALLED'"), true);
        Require(DhcpRecovery.Read(diagnostic) is { Targets.Length: 1 } diagnosticSnapshot &&
            diagnosticSnapshot.Reasons[1] == DhcpRenewalReason.MissingAddress && DhcpRecovery.Interpret(diagnostic).State == CheckState.Repairable,
            "Read-only command reports missing lease eligibility without invoking renewal");
        string healthyFixture = fixture.Replace("$global:lease='169.254.10.1'", "$global:lease='192.0.2.10'")
            .Replace("$global:lease='192.0.2.10'; [pscustomobject]@{ReturnValue=0}", "throw 'MUTATION_CALLED'");
        var healthyStep = DhcpRecovery.Step(false);
        var healthyArguments = healthyStep.Arguments.ToArray();
        healthyArguments[^1] = healthyFixture + "\n" + healthyArguments[^1];
        try
        {
            await DhcpRecovery.ExecuteAsync(healthyStep with { Arguments = healthyArguments }, null);
            throw new Exception("A healthy lease returned fabricated renewal evidence");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(DhcpRecovery.Read(skipped.Evidence) is { Targets.Length: 0 } &&
                !skipped.Evidence.Error.Contains("MUTATION_CALLED", StringComparison.Ordinal),
                "A lease that became healthy records an exact no-renewal result without calling the provider");
        }
        var snapshot = DhcpRecovery.Read(result);
        Require(snapshot is not null && snapshot.Targets.SequenceEqual(SingleTarget) && snapshot.Adapters.Count == 1 && DhcpRecovery.HasUsableAddress(snapshot.Adapters[1]), "Actual script selects only connected physical DHCP adapter and records renewed target: " + result.Error);
        var restart = await RunFixture(fixture.Replace("ReturnValue=0", "ReturnValue=1"));
        Require(restart.ExitCode == 3010 && DhcpRecovery.Read(restart) is null, "Provider restart requirement is retained without restarting");
        var failed = await RunFixture(fixture.Replace("ReturnValue=0", "ReturnValue=82"));
        Require(failed.ExitCode != 0 && DhcpRecovery.Read(failed) is null, "Provider renewal failure cannot verify");
        foreach (string invalidProvider in new[] {
            fixture.Replace("SettingID=", "IgnoredSettingID="),
            fixture.Replace("$global:lease='169.254.10.1'", "$global:lease='invalid'"),
            fixture.Replace("Index=$_;DHCPEnabled", "Index=1;DHCPEnabled"),
            fixture.Replace("Index=$_;PhysicalAdapter", "Index=1;PhysicalAdapter") })
        {
            var rejected = await RunFixture(invalidProvider.Replace("$global:lease='192.0.2.10'; [pscustomobject]@{ReturnValue=0}", "throw 'MUTATION_CALLED'"));
            Require(rejected.ExitCode != 0 && !rejected.Error.Contains("MUTATION_CALLED") && DhcpRecovery.Read(rejected) is null,
                "Malformed addresses or duplicate adapter identities block before any DHCP renewal");
        }
        Console.WriteLine("PASS: DHCP eligibility, target-specific verification and script execution with substituted CIM providers.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
