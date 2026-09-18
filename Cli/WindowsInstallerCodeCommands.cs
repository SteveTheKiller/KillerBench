using System.Globalization;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsInstallerCodeCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length != 2 || !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int code) || code < 0)
        {
            Console.Error.WriteLine("Usage: msi-code NONNEGATIVE_DECIMAL");
            return 2;
        }
        var result = WindowsInstallerErrorCatalog.Find(code);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Code = code,
            Reference = result,
            Source = WindowsInstallerErrorCatalog.SourceUrl,
            Limitation = "Reference only. A return code does not identify the failing action, prove current product state or authorize repair. Review the verbose Installer log and fresh machine evidence."
        }, Capture.JsonOptions));
        return result is null ? 3 : 0;
    }
}
