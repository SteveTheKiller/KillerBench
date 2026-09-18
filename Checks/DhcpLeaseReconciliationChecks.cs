using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class DhcpLeaseReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "dhcp-lease-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(1100, new DateTimeOffset(2026, 9, 14, 11, 0, 0, TimeSpan.Zero));
        BootStamp? boot = firstBoot;
        const string Identity = "11111111-1111-1111-1111-111111111111";
        static ToolResult Snapshot(string identity, string address, bool targeted) => new(0,
            "{\"targets\":" + (targeted ? "[1]" : "[]") + ",\"adapters\":[{\"id\":1,\"settingId\":\"" + identity + "\",\"addresses\":[\"" + address + "\"]}]}", "", false);
        static ToolResult LeaseSnapshot(bool expired, bool targeted) => new(0,
            "{\"schema\":2,\"capturedUtc\":\"2026-09-16T12:00:00.0000000+00:00\",\"targets\":" + (targeted ? "[1]" : "[]") +
            ",\"reasons\":" + (targeted ? "[{\"id\":1,\"reason\":2}]" : "[]") +
            ",\"adapters\":[{\"id\":1,\"settingId\":\"" + Identity + "\",\"addresses\":[\"192.0.2.10\"],\"hasServer\":true,\"leaseObtainedUtc\":\"2026-09-16T10:00:00.0000000+00:00\",\"leaseExpiresUtc\":\"" +
            (expired ? "2026-09-16T11:00:00.0000000+00:00" : "2026-09-16T13:00:00.0000000+00:00") + "\"}]}", "", false);
        async Task<RepairRecoveryStore> Pending(string name, RepairKind kind, bool complete = true, ToolResult? retained = null)
        {
            string folder = Path.Combine(root, name);
            Directory.CreateDirectory(folder);
            string journal = Path.Combine(folder, "repair.json");
            IReadOnlyList<RepairStepResult> steps = complete ? [new(DhcpRecovery.Step(false), retained ?? Snapshot(Identity, "169.254.10.1", true))] : [];
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new RepairResult(Environment.MachineName, kind,
                DateTimeOffset.UtcNow, "Running", journal, steps), Capture.JsonOptions));
            var store = new RepairRecoveryStore(folder, _ => Task.FromResult<BootStamp?>(boot));
            await store.BeginAsync(journal);
            return store;
        }

        var sameBoot = await Pending("same-boot", RepairKind.DhcpLease);
        Require((await DhcpLeaseReconciliation.ReconcileAsync(sameBoot, _ => Task.FromResult(Snapshot(Identity, "192.0.2.10", false)))).Outcome == DhcpLeaseReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, "Same-boot DHCP evidence retains the guard");
        boot = firstBoot;
        var verified = await Pending("verified", RepairKind.DhcpLease);
        boot = new(1101, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DhcpLeaseReconciliation.ReconcileAsync(verified, _ => Task.FromResult(Snapshot(Identity, "192.0.2.10", false)))).Outcome == DhcpLeaseReconciliationOutcome.Verified &&
            (await verified.ReadAsync())?.Pending == false, "Usable address on the exact adapter resolves without renewal");
        boot = firstBoot;
        var renewedExpired = await Pending("expired-verified", RepairKind.DhcpLease, retained: LeaseSnapshot(expired: true, targeted: true));
        boot = new(1105, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DhcpLeaseReconciliation.ReconcileAsync(renewedExpired, _ => Task.FromResult(LeaseSnapshot(expired: false, targeted: false)))).Outcome == DhcpLeaseReconciliationOutcome.Verified &&
            (await renewedExpired.ReadAsync())?.Pending == false, "Fresh lease on the exact adapter resolves an interrupted expired-lease renewal without retry");
        boot = firstBoot;
        var stillExpired = await Pending("expired-not-applied", RepairKind.DhcpLease, retained: LeaseSnapshot(expired: true, targeted: true));
        boot = new(1106, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DhcpLeaseReconciliation.ReconcileAsync(stillExpired, _ => Task.FromResult(LeaseSnapshot(expired: true, targeted: false)))).Outcome == DhcpLeaseReconciliationOutcome.NotApplied &&
            (await stillExpired.ReadAsync())?.Pending == false, "Still-expired exact lease resolves as not applied without retry");
        boot = firstBoot;
        var notApplied = await Pending("not-applied", RepairKind.DhcpLease);
        boot = new(1102, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DhcpLeaseReconciliation.ReconcileAsync(notApplied, _ => Task.FromResult(Snapshot(Identity, "169.254.10.2", false)))).Outcome == DhcpLeaseReconciliationOutcome.NotApplied &&
            (await notApplied.ReadAsync())?.Pending == false, "Missing address on the exact adapter resolves without renewal");
        boot = firstBoot;
        var changed = await Pending("changed", RepairKind.DhcpLease);
        boot = new(1103, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DhcpLeaseReconciliation.ReconcileAsync(changed, _ => Task.FromResult(Snapshot("22222222-2222-2222-2222-222222222222", "192.0.2.10", false)))).Outcome == DhcpLeaseReconciliationOutcome.Inconclusive &&
            (await changed.ReadAsync())?.Pending == true, "Changed adapter identity retains the DHCP guard");
        boot = firstBoot;
        var incomplete = await Pending("incomplete", RepairKind.DhcpLease, false);
        boot = new(1104, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DhcpLeaseReconciliation.ReconcileAsync(incomplete, _ => throw new Exception("provider dispatched"))).Outcome == DhcpLeaseReconciliationOutcome.Inconclusive &&
            (await incomplete.ReadAsync())?.Pending == true, "Missing retained targets preserve the DHCP guard");
        Console.WriteLine("PASS: interrupted DHCP renewal requires exact retained adapter identities and never renews again; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
