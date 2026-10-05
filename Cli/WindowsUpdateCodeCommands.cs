using System.Globalization;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsUpdateCodeCommands
{
    internal static int Run(string[] args)
    {
        if (!TryReadCode(args, "update-code", out uint code, out int failure)) return failure;
        var reference = WindowsUpdateErrorCatalog.Find(code);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Code = code,
            HexCode = "0x" + code.ToString("X8", CultureInfo.InvariantCulture),
            Reference = reference,
            Source = WindowsUpdateErrorCatalog.SourceUrl,
            Limitation = "Reference only. A result code does not identify the failing update, prove current update state or authorize repair. Collect fresh update, servicing, policy and network evidence for the suggested scope."
        }, Capture.JsonOptions));
        return reference is null ? 3 : 0;
    }

    internal static async Task<int> FollowupAsync(string[] args)
    {
        if (!TryReadCode(args, "update-followup", out uint code, out int failure)) return failure;
        var reference = WindowsUpdateErrorCatalog.Find(code);
        if (reference is null)
        {
            Console.Error.WriteLine("The code is not in the installed Windows Update reference catalog, so no diagnostic scope was inferred.");
            return 3;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Collecting fresh read-only diagnostics selected by " + reference.Symbol + ". No repair will run.");
            var checks = await UpdateFailureFollowup.CheckAsync([reference], new DiagnosticProgress(), token: stop.Token).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Code = code,
                HexCode = "0x" + code.ToString("X8", CultureInfo.InvariantCulture),
                Reference = reference,
                Checks = checks,
                Limitation = "The result code selects diagnostic scope only. Findings remain current bounded evidence and cannot authorize repair from historical error identity alone."
            }, Capture.JsonOptions));
            return checks.Count > 0 && checks.All(check => check.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Update follow-up stopped.");
            return 130;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }

    private static bool TryReadCode(string[] args, string command, out uint code, out int failure)
    {
        code = 0;
        failure = 2;
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: " + command + " DECIMAL_OR_0xHEX");
            return false;
        }
        string raw = args[1];
        bool hex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (uint.TryParse(hex ? raw[2..] : raw, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
            CultureInfo.InvariantCulture, out code))
        {
            failure = 0;
            return true;
        }
        Console.Error.WriteLine("Supply an unsigned decimal or 0x hexadecimal Windows Update code.");
        return false;
    }

    private sealed class DiagnosticProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
