using System.Text.Json;
using KillerBench.Engine;

internal static class DeviceProblemCodeCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length != 2 || !DeviceProblemCodeCatalog.TryParse(args[1], out uint code))
        {
            Console.Error.WriteLine("Usage: device-code DECIMAL_OR_0xHEX (offline Device Manager problem-code reference)");
            return 2;
        }
        var result = DeviceProblemCodeCatalog.Find(code);
        Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
        return result.Symbol == "CM_PROB_UNKNOWN" ? 3 : 0;
    }
}
