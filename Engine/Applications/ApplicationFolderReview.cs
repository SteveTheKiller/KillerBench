namespace KillerBench.Engine;

public sealed record ApplicationFolderObservation(string Area, string State, bool? ReparsePoint, string Detail);
public sealed record ApplicationFolderReviewResult(string FolderName, IReadOnlyList<ApplicationFolderObservation> Locations,
    bool Complete, string Detail);

public static class ApplicationFolderReview
{
    private sealed record LocationDefinition(string Area, string Path);

    public static ApplicationFolderReviewResult Review(string folderName,
        Func<string, ApplicationFolderObservation>? reader = null, CancellationToken token = default)
    {
        ValidateFolderName(folderName);
        reader ??= area => Read(Definitions(folderName).Single(definition => definition.Area == area));
        var locations = new List<ApplicationFolderObservation>();
        foreach (LocationDefinition definition in Definitions(folderName))
        {
            token.ThrowIfCancellationRequested();
            ApplicationFolderObservation observation = reader(definition.Area);
            if (observation.Area != definition.Area || observation.State is not ("Absent" or "Directory" or "File" or "Unavailable") ||
                (observation.State is "Absent" or "Unavailable") != (observation.ReparsePoint is null))
                throw new InvalidOperationException("Application-folder reader returned invalid evidence.");
            locations.Add(observation);
        }
        bool complete = locations.All(location => location.State != "Unavailable");
        return new(folderName, locations.AsReadOnly(), complete,
            "Only exact child paths named by the caller were inspected at five fixed local roots. No directory was traversed and no path, file, content, owner, timestamp or size was retained. A matching folder does not establish application ownership or safe deletion. Nothing was changed.");
    }

    private static IReadOnlyList<LocationDefinition> Definitions(string folderName)
    {
        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.DoNotVerify),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolderOption.DoNotVerify),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolderOption.DoNotVerify)
        ];
        if (roots.Any(root => string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root)))
            throw new InvalidOperationException("A fixed application-folder root is unavailable.");
        return Array.AsReadOnly(new[]
        {
            new LocationDefinition("Local application data", Path.Combine(roots[0], folderName)),
            new LocationDefinition("Roaming application data", Path.Combine(roots[1], folderName)),
            new LocationDefinition("Shared application data", Path.Combine(roots[2], folderName)),
            new LocationDefinition("Program files", Path.Combine(roots[3], folderName)),
            new LocationDefinition("Program files (x86)", Path.Combine(roots[4], folderName))
        });
    }

    private static ApplicationFolderObservation Read(LocationDefinition definition)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(definition.Path);
            bool directory = attributes.HasFlag(FileAttributes.Directory);
            bool reparse = attributes.HasFlag(FileAttributes.ReparsePoint);
            return new(definition.Area, directory ? "Directory" : "File", reparse,
                "The exact child path exists. No contents were enumerated.");
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        { return new(definition.Area, "Absent", null, "The exact child path was not present."); }
        catch (Exception error) when (error is UnauthorizedAccessException or IOException or NotSupportedException)
        { return new(definition.Area, "Unavailable", null, "The exact child path could not be inspected: " + error.GetType().Name + "."); }
    }

    private static void ValidateFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName) || folderName.Length > 128 || folderName != folderName.Trim() ||
            folderName is "." or ".." || folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            folderName.Contains(Path.DirectorySeparatorChar) || folderName.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException("Specify one exact folder name without a path.", nameof(folderName));
    }
}
