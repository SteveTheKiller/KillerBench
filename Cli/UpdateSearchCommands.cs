using System.Text.Json;
using KillerBench.Engine;

internal static class UpdateSearchCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool software = true, drivers = false, downloaded = false;
        UpdateSearchSource source = UpdateSearchSource.Configured;
        string title = "";
        Guid? classification = null, product = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 1; i < args.Length; i++)
        {
            if (!seen.Add(args[i])) return Usage();
            switch (args[i])
            {
                case "--drivers": drivers = true; break;
                case "--no-software": software = false; break;
                case "--downloaded-only": downloaded = true; break;
                case "--source" when i + 1 < args.Length:
                    if (!Enum.TryParse(args[++i], true, out source) || !Enum.IsDefined(source)) return Usage();
                    break;
                case "--product" when i + 1 < args.Length:
                    if (!Guid.TryParse(args[++i], out var productIdentity) || productIdentity == Guid.Empty) return Usage();
                    product = productIdentity;
                    break;
                case "--classification" when i + 1 < args.Length:
                    if (!Guid.TryParse(args[++i], out var category) || category == Guid.Empty) return Usage();
                    classification = category;
                    break;
                case "--filter" when i + 1 < args.Length:
                    title = args[++i];
                    if (title.Length > 256 || title.Any(char.IsControl) || title.StartsWith("--", StringComparison.Ordinal)) return Usage();
                    break;
                default: return Usage();
            }
        }
        if (!software && !drivers) return Usage();
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var discovery = await UpdateDiscovery.SearchAsync(new(software, drivers, source), new Progress<string>(Console.Error.WriteLine), token: stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Discovery = discovery,
                Filter = title,
                DownloadedOnly = downloaded,
                ClassificationId = classification,
                AvailableClassifications = discovery.GetClassifications(),
                ProductId = product,
                AvailableProducts = discovery.GetProducts(),
                Matches = discovery.Filter(title, downloaded, classification, product)
            }, Capture.JsonOptions));
            return discovery.Partial ? 3 : 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Update search stopped. No installation started."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Update search unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: update-search [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]. Read-only JSON discovery and matching identities; no installation options.");
        return 2;
    }
}
