using System.Text.Json;
using KillerBench.Engine;

internal static class MaceCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        MaceTargets targets = MaceTargets.None;
        MaceScope scope = MaceScope.RunningAccount;
        bool preserveData = true, preserveCredentials = true, restoreFolders = false, json = false;
        foreach (string value in args.Skip(1))
        {
            switch (value)
            {
                case "--onedrive": targets |= MaceTargets.OneDrive; break;
                case "--outlook": targets |= MaceTargets.Outlook; break;
                case "--office": targets |= MaceTargets.Office; break;
                case "--project": targets |= MaceTargets.Project; break;
                case "--teams": targets |= MaceTargets.Teams; break;
                case "--all-products": targets = MaceTargets.OneDrive | MaceTargets.Outlook | MaceTargets.Office | MaceTargets.Project | MaceTargets.Teams; break;
                case "--all-users": scope = MaceScope.AllUsers; break;
                case "--remove-data": preserveData = false; break;
                case "--remove-credentials": preserveCredentials = false; break;
                case "--review-folder-restore": restoreFolders = true; break;
                case "--json": json = true; break;
                default: return Usage("Unknown Microsoft app removal review option: " + value);
            }
        }
        var options = new MaceReviewOptions(targets, scope, preserveData, preserveCredentials, restoreFolders);
        try { options.Validate(); }
        catch (ArgumentException error) { return Usage(error.Message); }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var review = await MacePlanning.ReviewCurrentAsync(options, new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(json ? JsonSerializer.Serialize(review, Capture.JsonOptions) : review.Format());
            return review.Complete ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Microsoft app removal review stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Microsoft app removal review unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    internal static async Task<int> RunPackagePlanAsync(string[] args)
    {
        MaceTargets targets = MaceTargets.None;
        string? identity = null;
        bool json = false;
        for (int index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--onedrive": targets |= MaceTargets.OneDrive; break;
                case "--outlook": targets |= MaceTargets.Outlook; break;
                case "--office": targets |= MaceTargets.Office; break;
                case "--project": targets |= MaceTargets.Project; break;
                case "--teams": targets |= MaceTargets.Teams; break;
                case "--all-products": targets = MaceTargets.OneDrive | MaceTargets.Outlook |
                    MaceTargets.Office | MaceTargets.Project | MaceTargets.Teams; break;
                case "--package" when index + 1 < args.Length && identity is null:
                    identity = args[++index];
                    break;
                case "--json": json = true; break;
                default: return PackagePlanUsage("Unknown app removal package-plan option: " + args[index]);
            }
        }
        if (string.IsNullOrWhiteSpace(identity)) return PackagePlanUsage("Select one exact package full name.");
        var options = new MaceReviewOptions(targets);
        try { options.Validate(); }
        catch (ArgumentException error) { return PackagePlanUsage(error.Message); }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            MaceRemovalReview review = await MacePlanning.ReviewCurrentAsync(options,
                new Progress<string>(Console.Error.WriteLine), stop.Token);
            MacePackageRemovalEligibility eligibility = MacePackageRemoval.Evaluate(review, identity);
            Console.WriteLine(json
                ? JsonSerializer.Serialize(new { review, eligibility }, Capture.JsonOptions)
                : review.Format() + "\n\nPACKAGE REMOVAL PLAN\nEligible: " +
                    (eligibility.Eligible ? "Yes" : "No") + "\n" + eligibility.Detail +
                    "\n\nPreview only. No removal provider is connected to the CLI.");
            return eligibility.Eligible ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("App removal package plan stopped."); return 130; }
        catch (ArgumentException error) { return PackagePlanUsage(error.Message); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("App removal package plan unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    internal static async Task<int> RecoverPackageRemovalAsync(string[] args)
    {
        if (args.Length != 1) return PackageRecoveryUsage("mace-package-recover does not accept options.");
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "MacePackageRemoval");
            MacePackageRemovalReconciliationResult result = await MacePackageRemoval.ReconcileAsync(folder,
                new WindowsMacePackageRemovalHost(), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("App removal package recovery stopped. No removal was repeated."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("App removal package recovery unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]");
        Console.Error.WriteLine("Review only. No application, package, file, credential, folder location, registry value or user data changes.");
        return 2;
    }

    private static int PackagePlanUsage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]");
        Console.Error.WriteLine("Preview only. The CLI does not connect a package-removal provider.");
        return 2;
    }

    private static int PackageRecoveryUsage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: mace-package-recover");
        Console.Error.WriteLine("Freshly reconcile one interrupted app package-removal record without repeating removal.");
        return 2;
    }
}
