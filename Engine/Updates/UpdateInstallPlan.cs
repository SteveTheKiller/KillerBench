namespace KillerBench.Engine.Updates;

public sealed record UpdateSelection(Guid Id, int Revision);
public sealed record UpdateInstallOptions(bool AllowRestartRequired = false, bool AllowFeatureUpgrade = false);
public sealed record PlannedUpdate(AvailableUpdate Update, IReadOnlyList<string> Blockers)
{
    public bool Eligible => Blockers.Count == 0;
}
public sealed record UpdateInstallPlan(IReadOnlyList<PlannedUpdate> Updates)
{
    public bool Eligible => Updates.Count > 0 && Updates.All(u => u.Eligible);
    public string Format() => "UPDATE INSTALLATION PLAN / PREVIEW ONLY\n" +
        string.Join("\n\n", Updates.Select(item => $"{item.Update.Title}\n{item.Update.Id} revision {item.Update.Revision}\n" +
            (item.Eligible ? "Planning checks passed." : string.Join("\n", item.Blockers)))) +
        "\n\nNo updates downloaded or installed. No license accepted or restart scheduled. Execution requires fresh prerequisite checks.";
}

public static class UpdatePlanning
{
    public static readonly Guid FeatureUpgradeClassification = Guid.Parse("3689bdc8-b205-4af4-8d4a-a63924c5e9d5");

    // A plan is a preview. Execution must search again and recheck each identity and revision.
    public static UpdateInstallPlan Create(UpdateSearchResult discovery, IEnumerable<UpdateSelection> selections, UpdateInstallOptions options)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentNullException.ThrowIfNull(options);
        discovery.Options.Criteria();
        if (discovery.Partial || discovery.Evidence is { } evidence &&
            (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)))
            throw new ArgumentException("Complete update discovery before preparing an installation plan.");
        var selected = selections.Take(501).ToArray();
        if (selected.Length is < 1 or > 500 || selected.Any(s => s.Id == Guid.Empty || s.Revision < 0) || selected.Distinct().Count() != selected.Length)
            throw new ArgumentException("Select between 1 and 500 unique update identities and revisions.");
        var planned = new List<PlannedUpdate>();
        foreach (var selection in selected)
        {
            var matches = discovery.Updates.Where(u => Guid.TryParse(u.Id, out var id) && id == selection.Id && u.Revision == selection.Revision).Take(2).ToArray();
            if (matches.Length != 1) throw new ArgumentException("A selected update is missing, duplicated or has a different revision. Search again.");
            var update = matches[0];
            var blockers = new List<string>();
            if (update.Type is not ("Software" or "Driver") || (update.Type == "Software" && !discovery.Options.Software) || (update.Type == "Driver" && !discovery.Options.Drivers))
                blockers.Add("Update type does not match the discovery options.");
            if (!update.LicenseAccepted) blockers.Add("Review and accept this update's license through its supported interface first.");
            if (update.Behavior is not { } behavior) blockers.Add("Installation requirements are unavailable.");
            else
            {
                bool featureUpgrade = behavior.Classifications?.Any(category => category is not null &&
                    Guid.TryParse(category.Id, out Guid id) && id == FeatureUpgradeClassification) == true;
                if (behavior.CanRequestInput) blockers.Add("This update can require interactive input.");
                if (behavior.Beta) blockers.Add("Beta updates are not supported by unattended installation.");
                if (behavior.Impact is < 0 or > 2 || behavior.RebootBehavior is < 0 or > 2 || !RecognizedClassifications(behavior.Classifications))
                    blockers.Add("Installation requirements are unrecognized.");
                if (behavior.RebootBehavior != 0 && !options.AllowRestartRequired)
                    blockers.Add("Allow updates that may require a later restart to include this update. No automatic restart is planned.");
                if (behavior.Impact == 2 && selected.Length != 1)
                    blockers.Add("This update requires exclusive installation. Select it alone.");
                if (featureUpgrade && !options.AllowFeatureUpgrade)
                    blockers.Add("Feature upgrades require their separate confirmation.");
                if (featureUpgrade && !options.AllowRestartRequired)
                    blockers.Add("Feature upgrades require explicit later-restart allowance. KillerBench never restarts automatically.");
                if (featureUpgrade && selected.Length != 1)
                    blockers.Add("Select a feature upgrade by itself.");
            }
            planned.Add(new(update, blockers.AsReadOnly()));
        }
        return new(planned.AsReadOnly());
    }

    private static bool RecognizedClassifications(IReadOnlyList<UpdateClassification>? categories)
    {
        if (categories is null || categories.Count is < 1 or > 32) return false;
        var identities = new HashSet<Guid>();
        return categories.All(category => category is not null && Guid.TryParse(category.Id, out var id) && id != Guid.Empty &&
            identities.Add(id) && !string.IsNullOrWhiteSpace(category.Name) && category.Name.Length <= 256 && !category.Name.Any(char.IsControl));
    }
}
