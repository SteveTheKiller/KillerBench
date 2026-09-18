using KillerBench.Engine;

internal static class DepotPlanningChecks
{
    internal static void Run()
    {
        var ok = new ToolResult(0, "{}", "", false);
        var installed = new InstalledApplicationInventory([
            new("Registry64", "M365", "Microsoft 365 Apps for business - en-us", "16", "Microsoft Corporation"),
            new("Registry64", "Chrome", "Google Chrome", "1", "Google LLC"),
            new("Registry64", "Fake", "Google Chrome", "1", "Unrelated Vendor")], true, ok);
        var packages = new AppPackageInventoryResult(AppPackageScope.CurrentAccount, [
            new("MSTeams", "MSTeams_1_x64__8wekyb3d8bbwe", "MSTeams_8wekyb3d8bbwe", false, false)], true, ok);
        var office = new OfficeClickToRunSnapshot([new("Registry64", "O365BusinessRetail", "16", "x64", "en-us")], true);
        var options = new DepotOptions(DepotApplications.Microsoft365 | DepotApplications.Chrome | DepotApplications.Teams,
            DepotConfiguration.WindowsUpdates | DepotConfiguration.Privacy);
        var review = DepotPlanning.Review(options, installed, packages, office);
        Require(review.Complete && !review.ExecutionSupported && review.Applications.All(item => item.Detection == DepotDetection.Installed),
            "Selected exact registrations produce a complete review without execution");
        Require(review.Applications.Single(item => item.Application == DepotApplications.Chrome).Registrations.Count == 1,
            "Publisher evidence rejects an unrelated same-name registration");
        Require(review.Checks.All(check => check.Repair is null) && review.Format().Contains("Execution available: No"),
            "DEPOT review never offers provisioning execution");
        Require(!DepotPlanning.Review(options, installed with { Complete = false }, packages, office).Complete,
            "Incomplete inventory cannot establish complete provisioning review coverage");
        try { new DepotOptions(DepotApplications.None, DepotConfiguration.None).Validate(); throw new Exception("Empty DEPOT selection accepted."); }
        catch (ArgumentException) { }
        Console.WriteLine("PASS: DEPOT application, configuration, publisher and incomplete-evidence review; no provisioning available.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
