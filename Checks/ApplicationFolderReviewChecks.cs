using KillerBench.Engine;

internal static class ApplicationFolderReviewChecks
{
    internal static void Run()
    {
        ApplicationFolderObservation Reader(string area) => area switch
        {
            "Local application data" => new(area, "Directory", false, "Fixture"),
            "Roaming application data" => new(area, "Directory", true, "Fixture"),
            "Shared application data" => new(area, "File", false, "Fixture"),
            "Program files" => new(area, "Absent", null, "Fixture"),
            "Program files (x86)" => new(area, "Unavailable", null, "Fixture"),
            _ => throw new InvalidOperationException("Unexpected fixture area.")
        };
        ApplicationFolderReviewResult result = ApplicationFolderReview.Review("Fixture", Reader);
        if (result.Complete || result.Locations.Count != 5 || result.Locations.Any(item => item.Area.Contains("\\") || item.Detail.Contains("\\")) ||
            result.Locations.Single(item => item.Area == "Roaming application data").ReparsePoint != true)
            throw new Exception("Application-folder review lost its fixed-root, privacy or incomplete-coverage boundary.");
        foreach (string invalid in new[] { "", " ", ".", "..", "Folder\\Child" })
        {
            try { _ = ApplicationFolderReview.Review(invalid, Reader); throw new Exception("Invalid folder name was accepted."); }
            catch (ArgumentException) { }
        }
        try
        {
            _ = ApplicationFolderReview.Review("Fixture", _ => new("Changed", "Absent", null, "Fixture"));
            throw new Exception("Application-folder reader changed the requested location identity.");
        }
        catch (InvalidOperationException) { }
        Console.WriteLine("PASS: exact application-folder review has fixed roots, no traversal and no deletion; fixtures only.");
    }
}
