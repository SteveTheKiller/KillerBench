using KillerBench.Engine;

internal static class InstallerOutcomeCorrelationChecks
{
    internal static void Run()
    {
        string Event(ushort id, string product, string time, ulong record) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='MsiInstaller'/><EventID>{id}</EventID><Level>{(id == 11708 ? 2 : 4)}</Level><TimeCreated SystemTime='{time}'/><EventRecordID>{record}</EventRecordID></System><EventData><Data>Product: {product} -- Installation {(id == 11708 ? "failed" : "completed successfully")}.</Data></EventData></Event>";
        WorkflowCheck History(string xml, CheckState state = CheckState.Information) =>
            new("Recent installer events", state, "fixture", Evidence: new(0, "<Events>" + xml + "</Events>", "", false));

        var superseded = InstallerOutcomeCorrelation.Check(History(
            Event(11707, "Fixture Product", "2026-09-14T12:05:00Z", 2) +
            Event(11708, "Fixture Product", "2026-09-14T12:00:00Z", 1)));
        Require(superseded.State == CheckState.Information && superseded.Detail.Contains("latest retained outcome succeeded") &&
            superseded.Detail.Contains("1 success and 1 failure") && superseded.Repair is null,
            "Later success did not supersede the earlier retained failure for review ordering");

        var unresolved = InstallerOutcomeCorrelation.Check(History(
            Event(11708, "Broken Product", "2026-09-14T13:00:00Z", 4) +
            Event(11707, "Other Product", "2026-09-14T12:00:00Z", 3)));
        Require(unresolved.State == CheckState.Attention && unresolved.Detail.Contains("Broken Product: latest retained outcome failed") &&
            unresolved.Detail.Contains("Other Product: latest retained outcome succeeded") && unresolved.Repair is null,
            "Latest failure was hidden or product identities were merged");

        var contradictory = InstallerOutcomeCorrelation.Check(History(
            Event(11708, "Fixture Product", "2026-09-14T13:00:00Z", 5) +
            Event(11707, "fixture product", "2026-09-14T13:00:00Z", 6)));
        Require(contradictory.State == CheckState.Unknown, "Contradictory latest outcomes established a result");
        Require(InstallerOutcomeCorrelation.Check(History("<broken")).State == CheckState.Unknown,
            "Malformed XML established Installer outcome coverage");
        Require(InstallerOutcomeCorrelation.Check(History(Event(11708, " ", "2026-09-14T13:00:00Z", 7))).State == CheckState.Unknown,
            "Missing product identity established correlation");
        Require(InstallerOutcomeCorrelation.Check(History(Event(11708, "Fixture Product", "2026-09-14T13:00:00Z", 7)
            .Replace("Product: ", "Produit : ", StringComparison.Ordinal))).State == CheckState.Unknown,
            "Unrecognized localized payload was guessed");
        Require(InstallerOutcomeCorrelation.Check(History("", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete source history established correlation");
        var empty = InstallerOutcomeCorrelation.Check(History(Event(1033, "Fixture Product", "2026-09-14T13:00:00Z", 8)));
        Require(empty.State == CheckState.Information && empty.Detail.Contains("No product success or failure outcomes"),
            "History without product outcomes invented a result");
        try { InstallerOutcomeCorrelation.Check(History(""), new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: exact-product Installer outcomes, later success, unresolved failure and incomplete evidence; no package changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
