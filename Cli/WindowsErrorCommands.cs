using System.Globalization;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsErrorCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("Usage: win32-code DECIMAL_OR_0xHEX"); return 2; }
        string raw = args[1];
        bool hex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (!uint.TryParse(hex ? raw[2..] : raw, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
            CultureInfo.InvariantCulture, out uint code))
        { Console.Error.WriteLine("Supply an unsigned decimal or 0x hexadecimal system code."); return 2; }
        try
        {
            var result = WindowsErrorReference.Lookup(code);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Message is null ? 3 : 0;
        }
        catch (PlatformNotSupportedException error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
