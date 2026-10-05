using System.Text.Json;
using KillerBench.Engine;

internal static class MsiProductCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool repair = args.Length > 0 && args[0] == "msi-repair";
        bool recover = args.Length > 0 && args[0] == "msi-repair-recover";
        bool execute = args.Length == 3 && args[2] == "--execute";
        if ((recover && args.Length != 1) || (!repair && !recover && args.Length != 2) ||
            (repair && args.Length is not (2 or 3)) ||
            (repair && args.Length == 3 && !execute))
        {
            Console.Error.WriteLine(recover
                ? "Usage: msi-repair-recover (recheck an interrupted exact-product repair without repeating it)"
                : repair
                ? "Usage: msi-repair {PRODUCT-CODE} [--execute] (preview by default; exact eligible product only)"
                : "Usage: msi-product {PRODUCT-CODE} (read-only exact MSI registration state; JSON evidence)");
            return 2;
        }
        string? productCode = null;
        try { if (!recover) productCode = MsiProductDiagnostics.NormalizeProductCode(args[1]); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "MsiRepair");
            var progress = new Progress<string>(message => Console.Error.WriteLine(message));
            if (recover)
            {
                var result = await MsiRepairReconciliation.RunAsync(folder, new WindowsMsiRepairHost(), progress, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            if (repair)
            {
                var result = await MsiRepair.RunAsync(productCode!, execute, folder, new WindowsMsiRepairHost(), progress, token: stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            Console.Error.WriteLine("Reading exact MSI product registration state. No installation, repair, removal or package enumeration.");
            var check = await MsiProductDiagnostics.CheckAsync(productCode!, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.State is CheckState.Information or CheckState.Clear ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("MSI product check stopped."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        { Console.Error.WriteLine((repair || recover ? "MSI repair unavailable: " : "MSI product check unavailable: ") + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
