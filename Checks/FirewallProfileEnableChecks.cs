using KillerBench.Engine;
using System.IO;

internal static class FirewallProfileEnableChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var options = new FirewallProfileEnableOptions([FirewallProfileTarget.Private, FirewallProfileTarget.Public]);
        var disabled = States(false, false, false);
        if (!FirewallProfileEnable.Eligible(options, disabled)) throw new Exception("Eligible local disabled firewall profiles were rejected");
        var eligibleCheck = FirewallProfileEnable.Assess(disabled);
        if (eligibleCheck is not { State: CheckState.Repairable, Repair: RepairKind.FirewallProfiles } ||
            !eligibleCheck.Detail.Contains("Domain, Private, Public", StringComparison.Ordinal) || eligibleCheck.Evidence is null)
            throw new Exception("Disabled unmanaged firewall profiles were not exposed as an exact option-dependent repair");
        var selectedReview = FirewallProfileEnable.ReviewSelection(options, disabled);
        if (selectedReview is not { State: CheckState.Attention, Repair: null } ||
            !selectedReview.Detail.Contains("separate guarded enable action", StringComparison.Ordinal))
            throw new Exception("Selected firewall review granted repair authority or lost its guarded-action boundary");
        var selectedEnabled = FirewallProfileEnable.ReviewSelection(options, States(false, true, true));
        if (selectedEnabled is not { State: CheckState.Information, Repair: null })
            throw new Exception("Enabled selected firewall profiles did not remain informational");
        var selectedManaged = FirewallProfileEnable.ReviewSelection(options,
            disabled.Select(item => item.Profile == FirewallProfileTarget.Private ? item with { Managed = true } : item).ToArray());
        if (selectedManaged is not { State: CheckState.Attention, Repair: null } ||
            !selectedManaged.Detail.Contains("policy owner", StringComparison.Ordinal))
            throw new Exception("Managed selected firewall profile did not retain policy ownership boundary");
        var selectedIncomplete = FirewallProfileEnable.ReviewSelection(options, disabled.Take(2).ToArray());
        if (selectedIncomplete is not { State: CheckState.Unknown, Repair: null })
            throw new Exception("Incomplete selected firewall evidence did not remain unknown");
        const string nativeJson = "[{\"Profile\":\"Domain\",\"Enabled\":true,\"Managed\":false},{\"Profile\":\"Private\",\"Enabled\":false,\"Managed\":false},{\"Profile\":\"Public\",\"Enabled\":true,\"Managed\":null}]";
        var parsed = WindowsFirewallProfileEnableHost.InterpretInspection(nativeJson);
        if (parsed.Count != 3 || parsed[0] != new FirewallProfileState(FirewallProfileTarget.Domain, true, false) ||
            parsed[1] != new FirewallProfileState(FirewallProfileTarget.Private, false, false) ||
            parsed[2] != new FirewallProfileState(FirewallProfileTarget.Public, true, null))
            throw new Exception("Native firewall profile names or nullable policy state were parsed incorrectly");
        foreach (string invalidJson in new[]
        {
            nativeJson.Replace("\"Public\"", "\"Private\"", StringComparison.Ordinal),
            nativeJson.Replace("\"Domain\"", "\"domain\"", StringComparison.Ordinal),
            nativeJson.Replace("\"Enabled\":true", "\"Enabled\":\"True\"", StringComparison.Ordinal)
        }) try { WindowsFirewallProfileEnableHost.InterpretInspection(invalidJson); throw new Exception("Invalid native firewall evidence was accepted"); }
        catch (InvalidDataException) { }
        var enabledCheck = FirewallProfileEnable.Assess(States(true, true, true));
        if (enabledCheck is not { State: CheckState.Clear, Repair: null })
            throw new Exception("Enabled firewall profiles did not remain clear");
        var managed = disabled.Select(item => item with { Managed = true }).ToArray();
        var managedCheck = FirewallProfileEnable.Assess(managed);
        if (managedCheck is not { State: CheckState.Attention, Repair: null } || !managedCheck.Detail.Contains("policy owner", StringComparison.Ordinal))
            throw new Exception("Managed disabled firewall profiles offered a local repair");
        var incompleteCheck = FirewallProfileEnable.Assess(disabled.Take(2).ToArray());
        if (incompleteCheck is not { State: CheckState.Unknown, Repair: null })
            throw new Exception("Incomplete firewall profile state offered a repair or health claim");
        foreach (var invalid in new IReadOnlyList<FirewallProfileState>[]
        {
            disabled.Take(2).ToArray(), disabled.Concat([disabled[0]]).ToArray(),
            disabled.Select(item => item.Profile == FirewallProfileTarget.Private ? item with { Managed = true } : item).ToArray(),
            disabled.Select(item => item.Profile == FirewallProfileTarget.Public ? item with { Enabled = null } : item).ToArray(),
            disabled.Select(item => item.Profile == FirewallProfileTarget.Private ? item with { Enabled = true } : item).ToArray()
        }) if (FirewallProfileEnable.Eligible(options, invalid)) throw new Exception("Unsafe firewall profile state was accepted");
        foreach (var bad in new[]
        {
            new FirewallProfileEnableOptions([]), new([FirewallProfileTarget.Private, FirewallProfileTarget.Private]),
            new([(FirewallProfileTarget)99])
        }) try { FirewallProfileEnable.Validate(bad); throw new Exception("Invalid firewall selection was accepted"); } catch (ArgumentException) { }

        var step = WindowsFirewallProfileEnableHost.EnableStep([FirewallProfileTarget.Public, FirewallProfileTarget.Domain]);
        if (step.Verification || step.Tool != PrintRecovery.Tool || !step.Arguments[^1].Contains("'Domain','Public'", StringComparison.Ordinal) ||
            !step.Arguments[^1].Contains("PersistentStore", StringComparison.Ordinal) || step.Arguments[^1].Contains("-All", StringComparison.Ordinal))
            throw new Exception("Firewall enable command changed selection or policy store");

        string previewFolder = Folder(scratch, "firewall-preview");
        var previewHost = new Host(disabled, States(false, true, true));
        var preview = await FirewallProfileEnable.RunAsync(options, false, previewFolder, previewHost);
        if (preview.Outcome != FirewallProfileEnableOutcome.Preview || previewHost.Commands != 0 || Directory.Exists(previewFolder))
            throw new Exception("Firewall preview changed state or wrote a recovery record");

        string verifiedFolder = Folder(scratch, "firewall-verified");
        var verifiedHost = new Host(disabled, States(false, true, true));
        var verified = await FirewallProfileEnable.RunAsync(options, true, verifiedFolder, verifiedHost);
        if (verified.Outcome != FirewallProfileEnableOutcome.Verified || verifiedHost.Commands != 1 || verifiedHost.Inspections != 4 ||
            File.Exists(Path.Combine(verifiedFolder, "firewall-profile-enable.pending.json")) ||
            Directory.GetFiles(verifiedFolder, "*.prepared.json").Length != 1)
            throw new Exception("Verified firewall profile enablement lost its evidence or command bound");

        string changedFolder = Folder(scratch, "firewall-changed");
        var changedHost = new Host(disabled, States(false, true, true)) { ChangeSecondInspection = true };
        var changed = await FirewallProfileEnable.RunAsync(options, true, changedFolder, changedHost);
        if (changed.Outcome != FirewallProfileEnableOutcome.Blocked || changedHost.Commands != 0 || Directory.Exists(changedFolder))
            throw new Exception("Changed firewall prerequisites allowed a mutation");

        string finalChangedFolder = Folder(scratch, "firewall-final-changed");
        var finalChangedHost = new Host(disabled, States(false, true, true)) { ChangeFinalInspection = true };
        var finalChanged = await FirewallProfileEnable.RunAsync(options, true, finalChangedFolder, finalChangedHost);
        if (finalChanged.Outcome != FirewallProfileEnableOutcome.Blocked || finalChangedHost.Commands != 0 ||
            File.Exists(Path.Combine(finalChangedFolder, "firewall-profile-enable.pending.json")) ||
            Directory.GetFiles(finalChangedFolder, "*.prepared.json").Length != 1)
            throw new Exception("A final firewall state or policy change did not close its no-command guard");

        string finalAdminFolder = Folder(scratch, "firewall-final-admin");
        var finalAdminHost = new Host(disabled, States(false, true, true)) { FinalAdministrator = false };
        var finalAdmin = await FirewallProfileEnable.RunAsync(options, true, finalAdminFolder, finalAdminHost);
        if (finalAdmin.Outcome != FirewallProfileEnableOutcome.Blocked || finalAdminHost.Commands != 0 ||
            Directory.GetFiles(finalAdminFolder, "*.prepared.json").Length != 1)
            throw new Exception("Lost final administrator access allowed a firewall mutation");

        string finalFailureFolder = Folder(scratch, "firewall-final-failure");
        var finalFailureHost = new Host(disabled, States(false, true, true)) { FailFinalInspection = true };
        var finalFailure = await FirewallProfileEnable.RunAsync(options, true, finalFailureFolder, finalFailureHost);
        if (finalFailure.Outcome != FirewallProfileEnableOutcome.Blocked || finalFailureHost.Commands != 0 ||
            Directory.GetFiles(finalFailureFolder, "*.prepared.json").Length != 1)
            throw new Exception("Unavailable final firewall evidence left a false mutation possibility");

        string uncertainFolder = Folder(scratch, "firewall-uncertain");
        var uncertainHost = new Host(disabled, States(false, true, true)) { CommandResult = new(5, "", "denied", false) };
        var uncertain = await FirewallProfileEnable.RunAsync(options, true, uncertainFolder, uncertainHost);
        if (uncertain.Outcome != FirewallProfileEnableOutcome.Uncertain || uncertainHost.Commands != 1 ||
            !File.Exists(Path.Combine(uncertainFolder, "firewall-profile-enable.pending.json")))
            throw new Exception("Uncertain firewall command lost its no-repeat guard");
        try { await FirewallProfileEnable.RunAsync(options, true, uncertainFolder, uncertainHost); throw new Exception("Pending firewall operation repeated"); }
        catch (InvalidOperationException) { }
        if (uncertainHost.Commands != 1) throw new Exception("Pending firewall operation executed twice");

        uncertainHost.Current = States(false, true, true);
        var recovered = await FirewallProfileEnable.ReconcileAsync(uncertainFolder, uncertainHost);
        if (recovered.Outcome != FirewallProfileEnableOutcome.Verified || uncertainHost.Commands != 1 ||
            File.Exists(Path.Combine(uncertainFolder, "firewall-profile-enable.pending.json")))
            throw new Exception("Firewall reconciliation failed or repeated the command");

        string retainedFolder = Folder(scratch, "firewall-retained");
        var retainedHost = new Host(disabled, States(false, true, true)) { CommandResult = new(5, "", "denied", false) };
        await FirewallProfileEnable.RunAsync(options, true, retainedFolder, retainedHost);
        retainedHost.Current = disabled.Select(item => item.Profile == FirewallProfileTarget.Private ? item with { Managed = true } : item).ToArray();
        var retained = await FirewallProfileEnable.ReconcileAsync(retainedFolder, retainedHost);
        if (retained.Outcome != FirewallProfileEnableOutcome.Uncertain || retainedHost.Commands != 1 ||
            !File.Exists(Path.Combine(retainedFolder, "firewall-profile-enable.pending.json")))
            throw new Exception("Changed firewall policy did not retain the guard");

        string foreignFolder = Folder(scratch, "firewall-foreign-machine");
        var foreignSeed = new Host(disabled, States(false, true, true)) { CommandResult = new(5, "", "denied", false) };
        await FirewallProfileEnable.RunAsync(options, true, foreignFolder, foreignSeed);
        string foreignPath = Path.Combine(foreignFolder, "firewall-profile-enable.pending.json");
        string foreignJson = await File.ReadAllTextAsync(foreignPath);
        await File.WriteAllTextAsync(foreignPath, foreignJson.Replace(Environment.MachineName, "OTHER-MACHINE",
            StringComparison.OrdinalIgnoreCase));
        var foreignHost = new Host(disabled, States(false, true, true));
        var foreign = await FirewallProfileEnable.ReconcileAsync(foreignFolder, foreignHost);
        if (foreign.Outcome != FirewallProfileEnableOutcome.Uncertain || foreignHost.Commands != 0 ||
            foreignHost.Inspections != 0 || !File.Exists(foreignPath))
            throw new Exception("A foreign-machine firewall record did not remain guarded before inspection");

        Console.WriteLine("PASS: selected local firewall profile enablement uses policy gates, durable evidence, exact verification and no-repeat recovery; simulated provider only.");
    }

    private static FirewallProfileState[] States(bool domain, bool privateProfile, bool publicProfile) =>
    [new(FirewallProfileTarget.Domain, domain, false), new(FirewallProfileTarget.Private, privateProfile, false), new(FirewallProfileTarget.Public, publicProfile, false)];
    private static string Folder(string scratch, string name) => Path.Combine(scratch, name + "-" + Guid.NewGuid().ToString("N"));

    private sealed class Host(IReadOnlyList<FirewallProfileState> before, IReadOnlyList<FirewallProfileState> after) : IFirewallProfileEnableHost
    {
        internal int Commands;
        internal bool ChangeSecondInspection;
        internal bool ChangeFinalInspection;
        internal bool FailFinalInspection;
        internal bool? FinalAdministrator;
        internal ToolResult CommandResult = new(0, "Enabled", "", false);
        internal IReadOnlyList<FirewallProfileState>? Current;
        private int inspections;
        private int administratorReads;
        internal int Inspections => inspections;
        public bool IsAdministrator => ++administratorReads > 1 && FinalAdministrator.HasValue
            ? FinalAdministrator.Value : true;
        public Task<IReadOnlyList<FirewallProfileState>> InspectAsync(CancellationToken token)
        {
            inspections++;
            if (FailFinalInspection && inspections == 3)
                return Task.FromException<IReadOnlyList<FirewallProfileState>>(new InvalidDataException("fixture final inspection failure"));
            IReadOnlyList<FirewallProfileState> value = Current ?? (Commands > 0 ? after : before);
            if (ChangeSecondInspection && inspections == 2)
                value = before.Select(item => item.Profile == FirewallProfileTarget.Private ? item with { Managed = true } : item).ToArray();
            if (ChangeFinalInspection && inspections == 3)
                value = before.Select(item => item.Profile == FirewallProfileTarget.Private ? item with { Managed = true } : item).ToArray();
            return Task.FromResult(value);
        }
        public Task<ToolResult> EnableAsync(IReadOnlyList<FirewallProfileTarget> profiles, CancellationToken token)
        {
            Commands++;
            return Task.FromResult(CommandResult);
        }
    }
}
