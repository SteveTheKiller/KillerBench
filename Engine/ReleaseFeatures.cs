namespace KillerBench.Engine;

/// <summary>Features whose engine code is complete but whose release is scheduled later. Every public entry point checks these.</summary>
public static class ReleaseFeatures
{
    /// <summary>Third-party application updates ship in 1.1, after the provider lifetime lease (review finding E001) lands.</summary>
    public static bool ThirdPartyUpdates { get; } = false;

    public const string ThirdPartyUpdatesNotice = "Third-party application updates arrive in KillerBench 1.1. Windows updates are available now.";
}
