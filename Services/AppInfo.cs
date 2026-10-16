using System.Reflection;

namespace KillerBench.Services;

/// <summary>
/// Application identity and the build stamps baked into the assembly. One place for the values the
/// About card, the update check and the release links all need.
/// </summary>
internal static class AppInfo
{
    /// <summary>owner/repo used for update checks and release links.</summary>
    internal const string GitHubRepo = "SteveTheKiller/KillerBench";

    /// <summary>Release asset of the installer, and the on-disk name of the installed exe.</summary>
    internal const string ExeName = "KillerBench.exe";

    /// <summary>Name shown in prompts.</summary>
    internal const string DisplayName = "KillerBench";

    /// <summary>Certificate subject fragment that unlocks the alias line on the About card. The
    /// subject is a legal name, so the card ties it back to the publicly known alias. A build signed
    /// by anyone else must not claim that alias, and an unsigned build has no subject. The same
    /// check decides whether a downloaded update is Steve's package.</summary>
    internal const string SignerName = "Stephen Riley";

    /// <summary>The alias shown when the signature verifies and the subject matches.</summary>
    internal const string AkaName = "Steve the Killer";

    /// <summary>Display version, from the informational version attribute.</summary>
    internal static string Version =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "1.0.0";

    /// <summary>Numeric version used for comparing against a release tag.</summary>
    internal static System.Version? AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version;

    /// <summary>Release date baked in from the csproj ReleaseDate property, so a user can see how old
    /// their build is. Empty when the attribute is missing, in which case the version line shows the
    /// version alone.</summary>
    internal static string ReleaseDate =>
        Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "ReleaseDate")?.Value ?? "";
}
