namespace KillerBench.Engine.Profiles;

public static class ProfileDependencyAssessment
{
    public static WorkflowCheck Check(IReadOnlyList<ProfileChoice> choices, ProfileDependencyResult? dependencies,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(choices);
        token.ThrowIfCancellationRequested();
        const string name = "Profile account dependencies";
        const string boundary = " Service and scheduled-task account evidence does not cover application, file, share, credential, synchronization, policy or backup dependencies. No service, task, account, profile or file is changed.";
        if (dependencies is null)
            return new(name, CheckState.Information, "Optional service and scheduled-task account dependency collection was not requested." + boundary);
        if (!dependencies.Complete)
            return new(name, CheckState.Unknown, "Service or scheduled-task account dependency evidence was incomplete. Missing matches cannot establish that a profile is unused." + boundary);
        var candidateSids = choices.Where(choice => choice.ReviewEligible).Select(choice => choice.Profile.Sid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matches = dependencies.Items.Where(item => item.Sid is not null && candidateSids.Contains(item.Sid)).ToArray();
        string detail = $"Checked {candidateSids.Count} review candidate{(candidateSids.Count == 1 ? "" : "s")} against {dependencies.Items.Count} service and scheduled-task account registrations; {matches.Length} matching dependencies found.";
        if (matches.Length > 0)
            detail += " Matches: " + string.Join("; ", matches.Take(16).Select(item => item.Kind + " " + item.Name)) +
                (matches.Length > 16 ? $"; {matches.Length - 16} additional matches remain in collected evidence." : ".");
        return new(name, matches.Length > 0 ? CheckState.Attention : CheckState.Information, detail + boundary);
    }
}

public static class ProfileOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck inventory, WorkflowCheck registrations,
        WorkflowCheck dependencies, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(dependencies);
        token.ThrowIfCancellationRequested();
        const string name = "Profile review conclusion";
        const string boundary = " Age and snapshot checks do not prove that profile data is backed up, restorable, unused or safe to remove. No profile, folder, registry key, account, service, task, credential, policy or file is changed, and no removal is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        if (inventory.Name != "Profile inventory" || registrations.Name != "Profile registrations" ||
            dependencies.Name != "Profile account dependencies")
            return Unknown("Profile evidence carried an unexpected identity.");
        var evidence = new[] { inventory, registrations, dependencies };
        int incomplete = evidence.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = evidence.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        if (incomplete > 0)
            return Unknown($"Profile review has {incomplete} incomplete source{(incomplete == 1 ? "" : "s")}. Recollect required evidence before selecting any profile for backup or removal review.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                $"Profile review has {attention} source{(attention == 1 ? "" : "s")} requiring narrower identity-based review. Resolve every registration and dependency concern before planning backup or removal." + boundary);
        return new(name, CheckState.Information,
            "Profile inventory, exact ProfileList registrations and the selected dependency scope were reviewed without an unresolved source. Any listed candidates remain review candidates only." + boundary);
    }
}
