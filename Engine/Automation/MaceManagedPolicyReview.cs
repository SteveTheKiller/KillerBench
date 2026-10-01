using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;

namespace KillerBench.Engine.Automation;

public sealed record MaceManagedPolicyObservation(MaceTargets Target, string Area,
    bool MachineConfigured, bool CurrentAccountConfigured);

public sealed record MaceManagedPolicyEvidence(IReadOnlyList<MaceManagedPolicyObservation> Areas,
    bool Complete, string Limitation)
{
    public int ConfiguredCount(MaceTargets targets) => Areas.Count(item =>
        (targets & item.Target) != MaceTargets.None && (item.MachineConfigured || item.CurrentAccountConfigured));
}

public static class MaceManagedPolicyReview
{
    private sealed record Definition(MaceTargets Target, string Area, string Path);

    private static readonly IReadOnlyList<Definition> Definitions =
    [
        new(MaceTargets.OneDrive, "OneDrive policy", @"Software\Policies\Microsoft\OneDrive"),
        new(MaceTargets.Outlook, "Outlook policy", @"Software\Policies\Microsoft\Office\16.0\Outlook"),
        new(MaceTargets.Office | MaceTargets.Project, "Office and Project shared policy", @"Software\Policies\Microsoft\Office\16.0\Common"),
        new(MaceTargets.Project, "Project policy", @"Software\Policies\Microsoft\Office\16.0\MS Project"),
        new(MaceTargets.Teams, "Teams policy", @"Software\Policies\Microsoft\Teams")
    ];

    public static MaceManagedPolicyEvidence Read(MaceTargets targets, MaceScope scope,
        CancellationToken token = default)
    {
        new MaceReviewOptions(targets, scope).Validate();
        token.ThrowIfCancellationRequested();
        if (scope != MaceScope.RunningAccount)
            return Incomplete("All-user policy review is unavailable because KillerBench does not load other user hives.");
        if (!OperatingSystem.IsWindows()) return Incomplete("Windows policy stores are unavailable on this platform.");
        try
        {
            var observations = new List<MaceManagedPolicyObservation>();
            foreach (Definition definition in Definitions)
            {
                token.ThrowIfCancellationRequested();
                if ((targets & definition.Target) == MaceTargets.None) continue;
                observations.Add(new(definition.Target, definition.Area,
                    IsConfigured(Registry.LocalMachine, definition.Path),
                    IsConfigured(Registry.CurrentUser, definition.Path)));
            }
            return Analyze(scope, targets, observations, token: token);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Incomplete("Managed-configuration review failed: " + error.GetType().Name + ".");
        }
    }

    public static MaceManagedPolicyEvidence Analyze(MaceScope scope, MaceTargets targets,
        IEnumerable<MaceManagedPolicyObservation> observations, bool complete = true,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(observations);
        new MaceReviewOptions(targets, scope).Validate();
        token.ThrowIfCancellationRequested();
        if (scope != MaceScope.RunningAccount)
            return Incomplete("All-user policy review is unavailable because KillerBench does not load other user hives.");
        var expected = Definitions.ToDictionary(item => item.Area, StringComparer.Ordinal);
        var retained = new List<MaceManagedPolicyObservation>();
        foreach (MaceManagedPolicyObservation item in observations)
        {
            token.ThrowIfCancellationRequested();
            if (!expected.TryGetValue(item.Area, out Definition? definition) || item.Target != definition.Target ||
                (targets & item.Target) == MaceTargets.None || retained.Count >= Definitions.Count ||
                retained.Any(existing => existing.Area == item.Area))
                return Incomplete("Managed-configuration evidence exceeded its identity or size boundary.");
            retained.Add(item);
        }
        if (!complete) return Incomplete("Managed-configuration review was incomplete.");
        return new(retained.AsReadOnly(), true,
            "Only fixed policy-area labels and machine or current-account presence states were retained. Policy names, values, accounts and provider identities were excluded.");
    }

    [SupportedOSPlatform("windows")]
    private static bool IsConfigured(RegistryKey root, string path)
    {
        using RegistryKey? key = root.OpenSubKey(path, writable: false);
        return key is not null && (key.ValueCount > 0 || key.SubKeyCount > 0);
    }

    private static MaceManagedPolicyEvidence Incomplete(string reason) =>
        new([], false, reason + " No policy name or value was retained.");
}
