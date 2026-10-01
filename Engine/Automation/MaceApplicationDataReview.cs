namespace KillerBench.Engine.Automation;

public sealed record MaceDataLocationObservation(MaceTargets Target, string Location,
    bool Present, bool ReparsePoint);

public sealed record MaceApplicationDataEvidence(IReadOnlyList<MaceDataLocationObservation> Locations,
    bool Complete, string Limitation)
{
    public int PresentCount(MaceTargets targets) => Locations.Count(item =>
        (targets & item.Target) != MaceTargets.None && item.Present);
}

public static class MaceApplicationDataReview
{
    private sealed record LocationDefinition(MaceTargets Target, string Label, string Path);

    private static readonly HashSet<string> Labels = new(StringComparer.Ordinal)
    {
        "OneDrive settings", "Outlook local data", "Outlook roaming data", "Office and Project shared local data",
        "Office and Project shared roaming data", "Teams package data",
        "Teams local data", "Teams roaming data"
    };

    public static MaceApplicationDataEvidence Read(MaceTargets targets, MaceScope scope,
        CancellationToken token = default)
    {
        new MaceReviewOptions(targets, scope).Validate();
        token.ThrowIfCancellationRequested();
        if (scope != MaceScope.RunningAccount)
            return Incomplete("All-user application-data review is unavailable because KillerBench does not traverse other profiles.");
        try
        {
            var observations = new List<MaceDataLocationObservation>();
            foreach (LocationDefinition definition in Definitions())
            {
                token.ThrowIfCancellationRequested();
                if ((targets & definition.Target) == MaceTargets.None) continue;
                bool present = Directory.Exists(definition.Path);
                bool reparse = present && (File.GetAttributes(definition.Path) & FileAttributes.ReparsePoint) != 0;
                observations.Add(new(definition.Target, definition.Label, present, reparse));
            }
            return Analyze(scope, targets, observations, token: token);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Incomplete("Application-data location review failed: " + error.GetType().Name + ".");
        }
    }

    public static MaceApplicationDataEvidence Analyze(MaceScope scope, MaceTargets targets,
        IEnumerable<MaceDataLocationObservation> observations, bool complete = true,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(observations);
        new MaceReviewOptions(targets, scope).Validate();
        token.ThrowIfCancellationRequested();
        if (scope != MaceScope.RunningAccount)
            return Incomplete("All-user application-data review is unavailable because KillerBench does not traverse other profiles.");
        var retained = new List<MaceDataLocationObservation>();
        foreach (MaceDataLocationObservation item in observations)
        {
            token.ThrowIfCancellationRequested();
            const MaceTargets all = MaceTargets.OneDrive | MaceTargets.Outlook | MaceTargets.Office |
                MaceTargets.Project | MaceTargets.Teams;
            if (!Labels.Contains(item.Location) || item.Target == MaceTargets.None ||
                (item.Target & ~all) != 0 || (targets & item.Target) == MaceTargets.None || retained.Count >= Labels.Count)
                return Incomplete("Application-data evidence exceeded its identity or size boundary.");
            retained.Add(item);
        }
        if (!complete) return Incomplete("Application-data location review was incomplete.");
        return new(retained.AsReadOnly(), true,
            "Only fixed location labels and presence or reparse-point states were retained. Paths, filenames, contents, sizes, owners and timestamps were excluded.");
    }

    private static IEnumerable<LocationDefinition> Definitions()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        yield return new(MaceTargets.OneDrive, "OneDrive settings", Path.Combine(local, "Microsoft", "OneDrive"));
        yield return new(MaceTargets.Outlook, "Outlook local data", Path.Combine(local, "Microsoft", "Outlook"));
        yield return new(MaceTargets.Outlook, "Outlook roaming data", Path.Combine(roaming, "Microsoft", "Outlook"));
        yield return new(MaceTargets.Office | MaceTargets.Project, "Office and Project shared local data", Path.Combine(local, "Microsoft", "Office"));
        yield return new(MaceTargets.Office | MaceTargets.Project, "Office and Project shared roaming data", Path.Combine(roaming, "Microsoft", "Office"));
        yield return new(MaceTargets.Teams, "Teams package data", Path.Combine(local, "Packages", "MSTeams_8wekyb3d8bbwe"));
        yield return new(MaceTargets.Teams, "Teams local data", Path.Combine(local, "Microsoft", "Teams"));
        yield return new(MaceTargets.Teams, "Teams roaming data", Path.Combine(roaming, "Microsoft", "Teams"));
    }

    private static MaceApplicationDataEvidence Incomplete(string reason) =>
        new([], false, reason + " No path or file identity was retained.");
}
