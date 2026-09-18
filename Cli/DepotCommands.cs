using System.Text.Json;
using KillerBench.Engine;

internal static class DepotCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        DepotApplications apps = DepotApplications.None;
        DepotConfiguration config = DepotConfiguration.None;
        bool json = false;
        foreach (string value in args.Skip(1))
        {
            switch (value)
            {
                case "--microsoft-365": apps |= DepotApplications.Microsoft365; break;
                case "--onedrive": apps |= DepotApplications.OneDrive; break;
                case "--teams": apps |= DepotApplications.Teams; break;
                case "--chrome": apps |= DepotApplications.Chrome; break;
                case "--acrobat-reader": apps |= DepotApplications.AcrobatReader; break;
                case "--zoom": apps |= DepotApplications.Zoom; break;
                case "--7zip": apps |= DepotApplications.SevenZip; break;
                case "--windows-updates": config |= DepotConfiguration.WindowsUpdates; break;
                case "--computer-name": config |= DepotConfiguration.ComputerName; break;
                case "--local-admin": config |= DepotConfiguration.LocalAdministrator; break;
                case "--admin-review": config |= DepotConfiguration.AdministratorReview; break;
                case "--browser-extensions": config |= DepotConfiguration.BrowserExtensions; break;
                case "--cleanup": config |= DepotConfiguration.Cleanup; break;
                case "--edge": config |= DepotConfiguration.Edge; break;
                case "--privacy": config |= DepotConfiguration.Privacy; break;
                case "--taskbar": config |= DepotConfiguration.Taskbar; break;
                case "--start-menu": config |= DepotConfiguration.StartMenu; break;
                case "--notifications": config |= DepotConfiguration.Notifications; break;
                case "--json": json = true; break;
                default: return Usage("Unknown DEPOT review option: " + value);
            }
        }
        var options = new DepotOptions(apps, config);
        try { options.Validate(); }
        catch (ArgumentException error) { return Usage(error.Message); }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var review = await DepotPlanning.ReviewCurrentAsync(options, new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(json ? JsonSerializer.Serialize(review, Capture.JsonOptions) : review.Format());
            return review.Complete ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("DEPOT review stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("DEPOT review unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: depot-review [--microsoft-365] [--onedrive] [--teams] [--chrome] [--acrobat-reader] [--zoom] [--7zip] [--windows-updates] [--computer-name] [--local-admin] [--admin-review] [--browser-extensions] [--cleanup] [--edge] [--privacy] [--taskbar] [--start-menu] [--notifications] [--json]");
        Console.Error.WriteLine("Select at least one item. Review only; no provisioning actions occur.");
        return 2;
    }
}
