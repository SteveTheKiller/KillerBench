namespace KillerBench.Engine;

public sealed record ProfileSnapshot(string Sid, string Path, bool? Loaded, bool? Special,
    DateTimeOffset? LastUsedUtc, bool CurrentAccount);
public sealed record ProfileChoice(ProfileSnapshot Profile, bool ReviewEligible, string Reason);
public sealed record ProfileSelectionPlan(IReadOnlyList<ProfileSnapshot> Targets, IReadOnlyList<string> Blockers)
{
    public bool ReviewPassed => Targets.Count > 0 && Blockers.Count == 0;
    public string Limitation => "Review only. A native backup and removal host is not enabled. Any future removal requires exact fresh backup evidence, fresh prerequisites and absence verification.";
}

public static class ProfilePlanning
{
    public static bool IsLocalProfilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length is < 4 or > 32767 ||
            !char.IsAsciiLetter(path[0]) || path[1] != ':' || path[2] != '\\' || path.Any(char.IsControl)) return false;
        var parts = path[3..].Split('\\');
        return parts.All(part => part.Length > 0 && part is not ("." or "..") &&
            !part.EndsWith(' ') && !part.EndsWith('.') && !part.Any(c => c is ':' or '/' or '*' or '?' or '"' or '<' or '>' or '|'));
    }

    public static void ValidateSelection(IReadOnlyList<string> selectedSids)
    {
        ArgumentNullException.ThrowIfNull(selectedSids);
        if (selectedSids.Count is < 1 or > 32 || selectedSids.Any(sid => !IsSid(sid)) ||
            selectedSids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != selectedSids.Count)
            throw new ArgumentException("Select 1 to 32 unique, valid profile SIDs.", nameof(selectedSids));
    }

    public static bool IsSid(string? sid)
    {
        if (sid is null || sid.Length > 184) return false;
        var parts = sid.Split('-');
        if (parts.Length is < 4 or > 18 || !parts[0].Equals("S", StringComparison.OrdinalIgnoreCase) || parts[1] != "1") return false;
        for (int i = 2; i < parts.Length; i++)
        {
            string part = parts[i];
            if (part.Length == 0 || (part.Length > 1 && part[0] == '0') || part.Any(c => c is < '0' or > '9') ||
                !ulong.TryParse(part, out ulong value) || value > (i == 2 ? 281474976710655UL : uint.MaxValue)) return false;
        }
        return true;
    }

    public static ProfileSelectionPlan Select(ProfileInventoryResult inventory, ProfileDependencyResult dependencies, ProfileRegistrationResult registrations,
        IReadOnlyList<string> selectedSids, int minimumDays, DateTimeOffset now, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(selectedSids);
        token.ThrowIfCancellationRequested();
        ValidateSelection(selectedSids);
        var review = Review(inventory.Profiles, minimumDays, now, inventory.Complete, token);
        var targets = new List<ProfileSnapshot>();
        var blockers = new List<string>();
        if (!dependencies.Complete) blockers.Add("Dependency coverage is incomplete.");
        if (!registrations.Complete) blockers.Add("Profile registration coverage is incomplete.");
        foreach (string sid in selectedSids)
        {
            token.ThrowIfCancellationRequested();
            var matches = review.Where(c => c.Profile.Sid.Equals(sid, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) { blockers.Add(sid + ": profile is missing or ambiguous."); continue; }
            var match = matches[0];
            targets.Add(match.Profile);
            if (!match.ReviewEligible) blockers.Add(sid + ": " + match.Reason);
            foreach (var dependency in dependencies.ForSid(sid)) blockers.Add(sid + ": " + dependency.Kind + " " + dependency.Name + " uses this account.");
            var registrationMatches = registrations.ForSid(sid);
            var ordinary = registrationMatches.Where(item => !item.BackupKey).ToArray();
            if (ordinary.Length != 1 || !SamePath(ordinary[0].Path, match.Profile.Path))
                blockers.Add(sid + ": exact ordinary ProfileList registration is missing or ambiguous.");
            if (registrationMatches.Any(item => item.BackupKey))
                blockers.Add(sid + ": a backup ProfileList registration requires review.");
            if (ordinary.Length == 1 && ordinary[0].State != 0)
                blockers.Add(sid + ": ProfileList state is unavailable or nonzero.");
            if (ordinary.Length == 1 && ordinary[0].RefCount != 0)
                blockers.Add(sid + ": ProfileList reference count is unavailable or nonzero.");
        }
        return new(targets.AsReadOnly(), blockers.AsReadOnly());
    }

    private static bool SamePath(string left, string right) =>
        IsLocalProfilePath(left) && IsLocalProfilePath(right) &&
        Path.GetFullPath(left).TrimEnd('\\').Equals(Path.GetFullPath(right).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<ProfileChoice> Review(IReadOnlyList<ProfileSnapshot> profiles,
        int minimumDays, DateTimeOffset now, bool complete, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        if (minimumDays is < 1 or > 3650) throw new ArgumentOutOfRangeException(nameof(minimumDays));
        if (profiles.Count > 1024) throw new ArgumentException("Profile inventory exceeds the supported limit.", nameof(profiles));
        var duplicates = profiles.GroupBy(p => p.Sid, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() != 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var output = new List<ProfileChoice>();
        foreach (var profile in profiles)
        {
            token.ThrowIfCancellationRequested();
            string? reason = !complete ? "Inventory is incomplete." :
                !IsSid(profile.Sid) || duplicates.Contains(profile.Sid) ? "Profile identity is missing or ambiguous." :
                profile.CurrentAccount ? "This is the account running KillerBench." :
                profile.Special != false ? "System profile or unknown system-profile state." :
                profile.Loaded != false ? "Profile is loaded or its loaded state is unknown." :
                !IsLocalProfilePath(profile.Path) ? "Profile path is unavailable or is not an unambiguous local folder." :
                profile.LastUsedUtc is null || profile.LastUsedUtc > now ? "Last-use time is unavailable or in the future." :
                now - profile.LastUsedUtc.Value < TimeSpan.FromDays(minimumDays) ? "Profile was used within the selected age threshold." : null;
            output.Add(new(profile, reason is null, reason ?? "Meets age and snapshot checks for review. This does not authorize deletion or prove data is backed up."));
        }
        return output.AsReadOnly();
    }
}
