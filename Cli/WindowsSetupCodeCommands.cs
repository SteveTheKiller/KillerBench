using System.Globalization;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsSetupCodeCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: setup-code DECIMAL_OR_0xHEX");
            return 2;
        }
        string raw = args[1];
        bool hex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (!uint.TryParse(hex ? raw[2..] : raw, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
            CultureInfo.InvariantCulture, out uint code))
        {
            Console.Error.WriteLine("Supply an unsigned decimal or 0x hexadecimal Windows Setup code.");
            return 2;
        }
        var reference = WindowsSetupErrorCatalog.Find(code);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Code = code,
            HexCode = "0x" + code.ToString("X8", CultureInfo.InvariantCulture),
            Reference = reference,
            Source = WindowsSetupErrorCatalog.SourceUrl,
            Limitation = "Reference only. A Setup result does not identify the failing device, application, phase or operation and does not authorize repair. Review the extend code and fresh Setup evidence for the suggested scope."
        }, Capture.JsonOptions));
        return reference is null ? 3 : 0;
    }
}
