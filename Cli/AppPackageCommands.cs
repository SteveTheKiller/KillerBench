using System.Text.Json;
using KillerBench.Engine;

internal static class AppPackageCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool review = args[0] == "app-package-review";
        bool provisioned = args[0] == "provisioned-packages";
        bool installed = args[0] == "installed-apps";
        bool currentAccount = installed && args is [_, "--current-user"];
        if (installed && args.Length != 1 && !currentAccount) { Console.Error.WriteLine("Usage: installed-apps [--current-user] (registrations; no removal)"); return 2; }
        if (currentAccount) args = [args[0]];
        if (provisioned && args.Length != 1) { Console.Error.WriteLine("Usage: provisioned-packages (local Windows image; no removal)"); return 2; }
        int required = review ? 2 : 1;
        if ((args.Length != required && !(args.Length == required + 1 && args[^1] == "--all-users")) ||
            (review && (string.IsNullOrWhiteSpace(args[1]) || args[1].Length > 512 || args[1] != args[1].Trim() || args[1].Any(char.IsControl))))
        { Console.Error.WriteLine("Usage: app-packages [--all-users] | app-package-review FULL_NAME [--all-users] (no removal)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (installed)
            {
                var applications = await InstalledApplications.ReadAsync(currentAccount ? InstalledApplicationScope.CurrentAccount : InstalledApplicationScope.Machine, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { applications.Scope, applications.Applications, applications.Complete, applications.Evidence,
                    Note = "Uninstall registrations in the selected registry scope. Command and install-location availability are presence flags only: command text and paths are not exposed or executed. CurrentAccount means the executing account. Other accounts, portable apps, actual operation and managed ownership are not evaluated. Registrations can be stale. No installers or uninstall commands executed." }, Capture.JsonOptions));
                return applications.Complete ? 0 : 3;
            }
            if (provisioned)
            {
                var packages = await ProvisionedPackages.ReadAsync(stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { packages.Packages, packages.Complete, packages.Evidence,
                    Note = "Provisioned packages in the running Windows image for new users. Existing user registrations, managed ownership and removal eligibility are not evaluated. No packages changed." }, Capture.JsonOptions));
                return packages.Complete ? 0 : 3;
            }
            var inventory = await AppPackageInventory.ReadAsync(args.Length > required ? AppPackageScope.AllUsers : AppPackageScope.CurrentAccount, stop.Token);
            if (review)
            {
                var result = AppPackageReview.Review(inventory, args[1], stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return inventory.Complete && result.Selected is not null ? 0 : 3;
            }
            Console.WriteLine(JsonSerializer.Serialize(new { inventory.Scope, inventory.Complete, inventory.Packages, inventory.Evidence,
                Note = "Registered package inventory only. CurrentAccount means the running identity. Provisioned packages and Win32 applications are excluded. Package flags do not establish removal eligibility. No applications or user data changed." }, Capture.JsonOptions));
            return inventory.Complete ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Package inventory stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Package inventory unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
