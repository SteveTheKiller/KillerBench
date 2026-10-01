using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record UpdateApplicabilitySnapshot(bool Complete, string? ProductName, string? Edition,
    string? DisplayVersion, string? Build, int? UpdateBuildRevision, string? InstallationType,
    string? Architecture, string? CollectionIssue = null);

public static class UpdateApplicabilityDiagnostics
{
    public static WorkflowCheck Check() => Assess(Read());

    public static UpdateApplicabilitySnapshot Read()
    {
        if (!OperatingSystem.IsWindows())
            return new(false, null, null, null, null, null, null, null, "Windows applicability evidence requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key is null) return new(false, null, null, null, null, null, null, null, "Windows version registry key is unavailable.");
            return new(true, key.GetValue("ProductName") as string, key.GetValue("EditionID") as string,
                key.GetValue("DisplayVersion") as string, key.GetValue("CurrentBuildNumber") as string,
                key.GetValue("UBR") as int?, key.GetValue("InstallationType") as string,
                RuntimeInformation.OSArchitecture.ToString());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, null, null, null, null, null, null, null, error.GetType().Name);
        }
    }

    public static WorkflowCheck Assess(UpdateApplicabilitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool complete = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) &&
            Valid(snapshot.ProductName) && Valid(snapshot.Edition) && Valid(snapshot.DisplayVersion) &&
            ValidBuild(snapshot.Build) && snapshot.UpdateBuildRevision is >= 0 &&
            Valid(snapshot.InstallationType) && Valid(snapshot.Architecture);
        string detail = complete
            ? $"{snapshot.ProductName}; edition {snapshot.Edition}; version {snapshot.DisplayVersion}; " +
              $"build {snapshot.Build}.{snapshot.UpdateBuildRevision}; {snapshot.Architecture}; installation type {snapshot.InstallationType}. " +
              "Compare these values with the exact update's supported products, architecture, build range and prerequisites."
            : "Windows product, edition, build or architecture evidence was unavailable or malformed. Recollect before assessing update applicability.";
        detail += " This inventory does not prove that any specific update applies, and no update source, package or setting changed.";
        return new("Update applicability context", complete ? CheckState.Information : CheckState.Unknown, detail,
            Evidence: new(complete ? 0 : -1, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), snapshot.CollectionIssue ?? "", false));
    }

    private static bool Valid(string? value) => value is { Length: > 0 and <= 256 } && !value.Any(char.IsControl);
    private static bool ValidBuild(string? value) => value is { Length: > 0 and <= 10 } && value.All(char.IsAsciiDigit);
}
