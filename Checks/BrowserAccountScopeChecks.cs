using System.IO;
using KillerBench.Engine.Applications;

internal static class BrowserAccountScopeChecks
{
    internal static void Run(string scratch)
    {
        var result = BrowserExtensionReview.Read();
        if (result.Scope != BrowserExtensionReviewScope.RunningProcessAccount ||
            !result.Detail.Contains("running process account", StringComparison.Ordinal) ||
            !result.Detail.Contains("desktop user", StringComparison.Ordinal))
            throw new Exception("Browser review must disclose its running-account scope even when the account has no browser profiles.");
        string accounts = Path.Combine(scratch, "browser-account-scope");
        try
        {
            string desktop = Path.Combine(accounts, "desktop");
            string administrator = Path.Combine(accounts, "administrator");
            Directory.CreateDirectory(Path.Combine(desktop, "Default", "Extensions", new string('a', 32)));
            Directory.CreateDirectory(administrator);
            var desktopResult = BrowserExtensionReview.Read([new("Fixture", desktop)]);
            var administratorResult = BrowserExtensionReview.Read([new("Fixture", administrator)]);
            if (desktopResult.Extensions.Count != 1 || administratorResult.Extensions.Count != 0 ||
                !desktopResult.Complete || !administratorResult.Complete ||
                desktopResult.Scope != BrowserExtensionReviewScope.SuppliedRoots ||
                administratorResult.Scope != BrowserExtensionReviewScope.SuppliedRoots ||
                !administratorResult.Detail.Contains("supplied roots only", StringComparison.Ordinal) ||
                administratorResult.Detail.Contains(accounts, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Disjoint supplied browser roots must keep separate scope, including complete zero results, without account paths.");
        }
        finally { if (Directory.Exists(accounts)) Directory.Delete(accounts, true); }
        Console.WriteLine("PASS: 2 browser scope checks (running-account disclosure and disjoint supplied roots).");
    }
}
