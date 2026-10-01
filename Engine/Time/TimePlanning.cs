namespace KillerBench.Engine.Time;

public enum TimeSourceMode { Existing, DomainHierarchy, CustomPeers }
public sealed record TimeOptions(TimeSourceMode Source = TimeSourceMode.Existing, IReadOnlyList<string>? Peers = null);
public sealed record TimeEnvironment(bool? DomainJoined, bool? DomainController, bool? PolicyManaged, bool Elevated)
{
    public string? LocalMode { get; init; }
    public string? LocalPeers { get; init; }
    public ToolResult? ConfigurationEvidence { get; init; }
}
public sealed record TimeConfigurationPlan(TimeSourceMode Source, IReadOnlyList<string> Peers, IReadOnlyList<string> Blockers)
{
    public bool ReviewPassed => Blockers.Count == 0;
    public static string Limitation => "Preview only. Execution requires separate authorization and fresh prerequisites. Synchronization and absolute clock accuracy are not established by this plan.";
    public string Format()
    {
        string source = Source switch
        {
            TimeSourceMode.Existing => "Keep the configured source",
            TimeSourceMode.DomainHierarchy => "Domain hierarchy",
            TimeSourceMode.CustomPeers => "Custom NTP peers",
            _ => "Unknown source"
        };
        var lines = new List<string> { "WINDOWS TIME / SOURCE PREVIEW", "", "Selected source: " + source };
        if (Peers.Count > 0) lines.Add("Selected peers: " + string.Join(", ", Peers));
        lines.Add("");
        if (Blockers.Count > 0)
        {
            lines.Add("Source change blocked:");
            lines.AddRange(Blockers.Select(b => "  * " + b));
        }
        else lines.Add("The recorded planning checks passed. This is not a synchronization result.");
        lines.Add("");
        lines.Add("No clock, service or source settings changed.");
        lines.Add("Source changes require separate explicit execution and fresh prerequisite checks.");
        lines.Add("To check synchronization, select Keep configured source and press GO.");
        return string.Join("\n", lines);
    }
}

public static class TimePlanning
{
    public static async Task<(TimeEnvironment Environment, TimeConfigurationPlan Plan)> PreviewAsync(TimeOptions options,
        CancellationToken token = default, Func<CancellationToken, Task<TimeEnvironment>>? collector = null)
    {
        var validated = Create(options, new(null, null, null, false));
        var retained = new TimeOptions(validated.Source, validated.Peers);
        token.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        TimeEnvironment environment;
        try
        {
            var pending = (collector ?? TimeEnvironmentCollector.ReadAsync)(deadline.Token)
                ?? throw new InvalidOperationException("Time collector returned no operation.");
            environment = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Time collector returned no environment.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { environment = new(null, null, null, false); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException or TimeoutException)
        { environment = new(null, null, null, false); }
        token.ThrowIfCancellationRequested();
        return (environment, Create(retained, environment));
    }

    public static TimeConfigurationPlan Create(TimeOptions options, TimeEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        if (!Enum.IsDefined(options.Source)) throw new ArgumentException("Choose a supported time source.", nameof(options));
        string[] peers = options.Peers?.ToArray() ?? [];
        if (options.Source == TimeSourceMode.CustomPeers)
        {
            if (peers.Length is < 1 or > 8 || peers.Any(p => !ValidPeer(p)) ||
                peers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != peers.Length)
                throw new ArgumentException("Choose 1 to 8 unique NTP hostnames or IP addresses without flags or ports.", nameof(options));
        }
        else if (peers.Length != 0) throw new ArgumentException("Peer addresses apply only to a custom source.", nameof(options));
        var blockers = new List<string>();
        if (options.Source != TimeSourceMode.Existing)
        {
            if (!environment.Elevated) blockers.Add("Administrator access is required for source changes.");
            if (!TimeRecovery.CanSynchronize(environment.LocalMode)) blockers.Add("Local synchronization is disabled or its mode is unavailable. Review the existing configuration first.");
            if (environment.PolicyManaged != false) blockers.Add("Time policy is managed or its ownership is unknown.");
            if (environment.DomainJoined is null || environment.DomainController is null) blockers.Add("Domain role is unknown.");
            if (environment.DomainController == true) blockers.Add("Domain controllers require a topology-specific time configuration review.");
            if (options.Source == TimeSourceMode.DomainHierarchy && environment.DomainJoined != true)
                blockers.Add("Domain hierarchy requires a domain-joined computer.");
            if (options.Source == TimeSourceMode.CustomPeers && environment.DomainJoined != false)
                blockers.Add("Custom peers require review of the domain time hierarchy before replacing its source.");
        }
        return new(options.Source, Array.AsReadOnly(peers), blockers.AsReadOnly());
    }

    private static bool ValidPeer(string? peer) => !string.IsNullOrWhiteSpace(peer) && peer.Length <= 253 &&
        !peer.Any(char.IsWhiteSpace) && !peer.Contains(',') && !peer.Contains('%') &&
        Uri.CheckHostName(peer) is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6;
}
