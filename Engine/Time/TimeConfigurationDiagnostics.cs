using System.Text.Json;

namespace KillerBench.Engine;

public static class TimeConfigurationDiagnostics
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        var snapshot = await TimePlanning.PreviewAsync(new(), token).ConfigureAwait(false);
        return Assess(snapshot.Environment);
    }

    public static WorkflowCheck Assess(TimeEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        string scope = $"Domain joined: {environment.DomainJoined?.ToString() ?? "Unknown"}; domain controller: {environment.DomainController?.ToString() ?? "Unknown"}; policy managed: {environment.PolicyManaged?.ToString() ?? "Unknown"}.\n";
        scope += "Local mode: " + (environment.LocalMode ?? "Unknown") + "; local peer list: " + (environment.LocalPeers ?? "Unknown") + ".\n";
        CheckState state = CheckState.Information;
        string detail;
        if (environment.DomainJoined is null || environment.DomainController is null || environment.PolicyManaged is null ||
            (environment.DomainController == true && environment.DomainJoined != true))
        { state = CheckState.Unknown; detail = "Role or policy ownership is incomplete. Effective source configuration cannot be established."; }
        else if (environment.PolicyManaged == true)
            detail = "Time configuration is managed. Local values may be overridden; review effective policy with its owner.";
        else if (string.Equals(environment.LocalMode, "NT5DS", StringComparison.OrdinalIgnoreCase) && environment.DomainJoined == false)
        { state = CheckState.Attention; detail = "Local mode selects the domain hierarchy on a non-domain computer. Review the intended source before changing configuration."; }
        else if (string.Equals(environment.LocalMode, "NTP", StringComparison.OrdinalIgnoreCase) && environment.LocalPeers is null)
        { state = CheckState.Unknown; detail = "Local NTP peer configuration was not available."; }
        else if (string.Equals(environment.LocalMode, "NTP", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(environment.LocalPeers))
        { state = CheckState.Attention; detail = "Local NTP mode has no peer list. Review the intended time sources."; }
        else if (string.Equals(environment.LocalMode, "NoSync", StringComparison.OrdinalIgnoreCase))
            detail = "Local synchronization is disabled. This may be intentional; the setting is preserved.";
        else if (!TimeRecovery.CanSynchronize(environment.LocalMode))
        { state = CheckState.Unknown; detail = "Local synchronization mode is unavailable or unrecognized."; }
        else detail = "Recognized local source configuration. Reachability, source trust and clock accuracy are not established.";
        if (environment.ConfigurationEvidence is { } command &&
            (command.ExitCode != 0 || command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error)))
        {
            string message = string.IsNullOrWhiteSpace(command.Error) ? command.Output : command.Error;
            detail += $" Effective configuration query was incomplete (exit {command.ExitCode}, timeout {command.TimedOut}, truncated {command.Truncated}). " +
                new string(message.Take(1000).Select(character => char.IsControl(character) ? ' ' : character).ToArray());
        }
        return new("Time source configuration", state, scope + detail + " No source or clock changes selected.",
            Evidence: new(0, JsonSerializer.Serialize(environment, Capture.JsonOptions), "", false));
    }

    public static WorkflowCheck AssessSelection(TimeOptions options, TimeEnvironment environment, TimeConfigurationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(plan);
        if (options.Source != plan.Source || !(options.Peers ?? []).SequenceEqual(plan.Peers, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("The time-source plan does not match the selected options.", nameof(plan));

        ToolResult evidence = environment.ConfigurationEvidence ??
            new(0, JsonSerializer.Serialize(environment, Capture.JsonOptions), "", false);
        if (options.Source == TimeSourceMode.Existing)
            return new("Time source configuration", CheckState.Information,
                "Keep configured source is selected. Choose Domain hierarchy or Custom NTP peers to review a source change. No source or clock settings changed.",
                Evidence: evidence);

        bool incomplete = environment.DomainJoined is null || environment.DomainController is null ||
            environment.PolicyManaged is null || !TimeRecovery.CanSynchronize(environment.LocalMode) ||
            evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error);
        string selection = options.Source == TimeSourceMode.DomainHierarchy
            ? "Domain hierarchy"
            : "Custom NTP peers: " + string.Join(", ", plan.Peers);
        if (incomplete)
            return new("Time source configuration", CheckState.Unknown,
                selection + " is selected, but role, policy, local mode or command evidence is incomplete. No source change is eligible.",
                Evidence: evidence);
        if (!plan.ReviewPassed)
            return new("Time source configuration", CheckState.Attention,
                selection + " is selected, but the guarded review is blocked: " + string.Join(" ", plan.Blockers) +
                " No source or clock settings changed.", Evidence: evidence);
        return new("Time source configuration", CheckState.Repairable,
            selection + " passed the source-change planning gates. Separate confirmation and a fresh repeated prerequisite check are required before configuration.",
            RepairKind.TimeSourceConfiguration, evidence);
    }

    public static WorkflowCheck ReviewSelection(TimeOptions options, TimeEnvironment environment,
        TimeConfigurationPlan plan)
    {
        WorkflowCheck assessed = AssessSelection(options, environment, plan);
        if (assessed.EffectiveState != CheckState.Repairable)
            return assessed;
        return assessed with
        {
            State = CheckState.Attention,
            Repair = null,
            Detail = assessed.Detail + " This read-only workflow review does not authorize or apply the source change."
        };
    }
}
