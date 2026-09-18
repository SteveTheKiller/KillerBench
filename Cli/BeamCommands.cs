using System.Text.Json;
using KillerBench.Engine;

internal static class BeamCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string? source = null, appName = null, publisher = null, switches = null, tenantDomain = null;
        BeamInstallerType? type = null;
        var targets = new List<string>();
        bool json = false;
        try
        {
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--source": source = Value(args, ref i, "--source"); break;
                    case "--type":
                        string typeText = Value(args, ref i, "--type");
                        if (!Enum.TryParse(typeText, true, out BeamInstallerType parsedType) || !Enum.IsDefined(parsedType))
                            return Usage("Installer type must be Exe or Msi.");
                        type = parsedType;
                        break;
                    case "--name": appName = Value(args, ref i, "--name"); break;
                    case "--publisher": publisher = Value(args, ref i, "--publisher"); break;
                    case "--switches": switches = Value(args, ref i, "--switches"); break;
                    case "--target": targets.Add(Value(args, ref i, "--target")); break;
                    case "--tenant-domain": tenantDomain = Value(args, ref i, "--tenant-domain"); break;
                    case "--json": json = true; break;
                    default: return Usage("Unknown BEAM review option: " + args[i]);
                }
            }
        }
        catch (ArgumentException error) { return Usage(error.Message); }
        if (source is null || type is null || appName is null || publisher is null || tenantDomain is null || targets.Count == 0)
            return Usage("Source, type, app name, publisher, tenant domain and at least one target are required.");
        var options = new BeamOptions(source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? BeamSourceKind.HttpsUrl : BeamSourceKind.LocalFile, source, type.Value, appName, publisher,
            switches ?? "", targets.AsReadOnly(), tenantDomain);
        try { options.Validate(); }
        catch (ArgumentException error) { return Usage(error.Message); }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var review = await BeamPlanning.ReviewAsync(options, new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(json ? JsonSerializer.Serialize(review, Capture.JsonOptions) : review.Format());
            return review.Ready ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("BEAM review stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        { Console.Error.WriteLine("BEAM review unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static string Value(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length) throw new ArgumentException("Missing value for " + option + ".");
        return args[++index];
    }

    private static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: beam-review --source URL_OR_PATH --type Exe|Msi --name APP_NAME --publisher PUBLISHER --target HOST [--target HOST ...] --tenant-domain DOMAIN [--switches ARGS] [--json]");
        Console.Error.WriteLine("Review only. No authentication, download, packaging, tenant change, assignment, sync or installation occurs.");
        return 2;
    }
}
