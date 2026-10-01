namespace KillerBench.Engine.Automation;

public enum MaceRemovalProviderKind
{
    None,
    CurrentAccountAppPackage,
    ProvisionedAppPackage,
    OfficeClickToRunProduct,
    UninstallRegistration
}

public sealed record MaceRemovalProviderCandidate(MaceTargets Target, string RegistrationType,
    string Scope, string Identity, MaceRemovalProviderKind ProviderKind, bool ProviderIdentified,
    string Reason);

public sealed record MaceRemovalProviderEvidence(IReadOnlyList<MaceRemovalProviderCandidate> Candidates,
    bool Complete, string Limitation)
{
    public int UnclassifiedCount => Candidates.Count(item => !item.ProviderIdentified);
}

public static class MaceRemovalProviderReview
{
    private const int MaximumCandidates = 512;

    public static MaceRemovalProviderEvidence Classify(IReadOnlyList<MaceCandidate> candidates,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        token.ThrowIfCancellationRequested();
        if (candidates.Count > MaximumCandidates)
            return new([], false, "The exact candidate inventory exceeded the bounded removal-provider review.");

        var results = new List<MaceRemovalProviderCandidate>(candidates.Count);
        foreach (MaceCandidate candidate in candidates)
        {
            token.ThrowIfCancellationRequested();
            results.Add(Classify(candidate));
        }
        return new(results.AsReadOnly(), true,
            "Provider classification used exact registration type, scope and package protection only. No uninstall command or repair plan was created.");
    }

    private static MaceRemovalProviderCandidate Classify(MaceCandidate candidate)
    {
        if (candidate.Protected)
            return Result(candidate, MaceRemovalProviderKind.None, false,
                "Windows reports this package as non-removable.");

        return (candidate.RegistrationType, candidate.Scope) switch
        {
            ("Registered app package", "CurrentAccount") when candidate.DevelopmentMode => Result(candidate,
                MaceRemovalProviderKind.CurrentAccountAppPackage, true,
                "An exact current-account development package identifies the data-preserving provider boundary. Removal remains disabled."),
            ("Registered app package", "CurrentAccount") => Result(candidate,
                MaceRemovalProviderKind.CurrentAccountAppPackage, false,
                "Windows can preserve application data during package removal only for loose-file development packages. This installed package is not eligible for that safeguard."),
            ("Registered app package", _) => Result(candidate, MaceRemovalProviderKind.None, false,
                "Automatic app package removal outside the running account is not supported."),
            ("Provisioned app package", "NewUsers") => Result(candidate,
                MaceRemovalProviderKind.ProvisionedAppPackage, false,
                "Provisioned-package removal affects new users and is not supported for automatic execution."),
            ("Office Click-to-Run product", "Machine") => Result(candidate,
                MaceRemovalProviderKind.OfficeClickToRunProduct, false,
                "Click-to-Run suite composition and a supported product-removal configuration are not yet verified."),
            ("Uninstall registration", _) => Result(candidate,
                MaceRemovalProviderKind.UninstallRegistration, false,
                "The inventory does not retain a verified uninstall provider. Display names cannot establish a safe removal command."),
            _ => Result(candidate, MaceRemovalProviderKind.None, false,
                "No exact supported removal provider is defined for this registration type and scope.")
        };
    }

    private static MaceRemovalProviderCandidate Result(MaceCandidate candidate,
        MaceRemovalProviderKind kind, bool identified, string reason) =>
        new(candidate.Target, candidate.RegistrationType, candidate.Scope, candidate.Identity,
            kind, identified, reason);
}
