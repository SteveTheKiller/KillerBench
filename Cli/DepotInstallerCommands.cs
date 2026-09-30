using System.Text.Json;
using KillerBench.Engine;

internal static class DepotInstallerCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 3) return Usage();
        DepotApplications? application = args[1].ToLowerInvariant() switch
        {
            "onedrive" => DepotApplications.OneDrive,
            "teams" => DepotApplications.Teams,
            "chrome" => DepotApplications.Chrome,
            "acrobat" => DepotApplications.AcrobatReader,
            "zoom" => DepotApplications.Zoom,
            "7zip" => DepotApplications.SevenZip,
            _ => null
        };
        if (application is null || !Path.IsPathFullyQualified(args[2])) return Usage();
        if (!OperatingSystem.IsWindows()) { Console.Error.WriteLine("Installer verification requires Windows."); return 3; }
        try
        {
            var result = await DepotInstallerTrust.VerifyFileAsync(application.Value, args[2]);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Outcome == DepotInstallerTrustOutcome.Trusted ? 0 : 3;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or TimeoutException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Installer verification is unavailable. The installer is not trusted: " + error.Message); return 3; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH. Read-only; nothing is run or installed.");
        return 2;
    }
}
