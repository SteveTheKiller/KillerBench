namespace KillerBench.Engine.SystemHealth;

public sealed record ComputerNameContext(string? CurrentName, int? DomainRole, bool? RestartPending)
{
    public string? ConfiguredName { get; init; }
}
public sealed record ComputerNameReviewResult(string ProposedName, bool? ChangeNeeded, IReadOnlyList<string> ReviewItems);
public sealed record ComputerNameReport(ComputerNameContext Context, ComputerNameReviewResult Review);

public static class ComputerNameReview
{
    public static async Task<ComputerNameReport> ReviewCurrentAsync(string proposedName, CancellationToken token = default)
    {
        Review(proposedName, new(null, null, null));
        token.ThrowIfCancellationRequested();
        var context = await ComputerNameCollector.ReadAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return new(context, Review(proposedName, context));
    }

    public static ComputerNameReviewResult Review(string proposedName, ComputerNameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrEmpty(proposedName) || proposedName.Length > 63 ||
            proposedName.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-') || proposedName.All(char.IsAsciiDigit) ||
            proposedName[0] == '-' || proposedName[^1] == '-')
            throw new ArgumentException("Use 1 to 63 ASCII letters, digits or internal hyphens, including at least one letter or hyphen.", nameof(proposedName));
        var items = new List<string>();
        bool knownName = DriverReference.ValidText(context.CurrentName, 63) && context.CurrentName == context.CurrentName!.Trim();
        if (!knownName) items.Add("Current computer name is unavailable; recollect before planning a change.");
        bool? change = knownName ? !string.Equals(proposedName, context.CurrentName, StringComparison.OrdinalIgnoreCase) : null;
        if (change == false) items.Add("The proposed name matches the current name; no rename is needed.");
        if (proposedName.Length > 15) items.Add("The proposed name exceeds the 15-character NetBIOS compatibility length; review legacy and domain naming requirements.");
        items.Add(context.DomainRole switch
        {
            0 or 2 => "Standalone role reported. Entra membership and organizational naming policy still require review.",
            1 or 3 => "Domain member reported. Domain permissions, name availability and organizational policy require review.",
            4 or 5 => "Domain controller reported. Use a dedicated domain-controller rename procedure; generic rename is unsupported.",
            _ => "Domain role is unavailable or unrecognized; environment readiness is unknown."
        });
        if (context.RestartPending != false) items.Add(context.RestartPending == true
            ? "A restart is pending. Reassess after restart before changing the computer name."
            : "Pending-restart state is unavailable.");
        items.Add("Review only. Name uniqueness, permissions and application dependencies were not verified. No rename or restart was performed or authorized.");
        return new(proposedName, change, items);
    }
}
