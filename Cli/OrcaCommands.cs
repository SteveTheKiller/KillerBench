using System.Text.Json;
using KillerBench.Engine;

internal static class OrcaCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args[0] == "orca-cache-reset-recover")
        {
            if (args.Length != 1) return CacheUsage("Recovery does not accept additional options.");
            var recovery = await OrcaCacheReset.ReconcileAsync(JournalFolder());
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                recovery.Outcome,
                recovery.Detail,
                Journal = recovery.ArchivePath is null ? null : Path.GetFileName(recovery.ArchivePath),
                CacheLabels = recovery.Pending?.Targets.Select(target => target.Label).ToArray() ?? []
            }, Capture.JsonOptions));
            return recovery.GetExitCode();
        }

        OrcaOutlookMode mode = OrcaOutlookMode.Both;
        OrcaScope scope = OrcaScope.RunningAccount;
        OrcaActions actions = OrcaActions.None;
        bool json = false;
        bool execute = false;
        for (int index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--mode" when index + 1 < args.Length && Enum.TryParse(args[++index], true, out OrcaOutlookMode parsed): mode = parsed; break;
                case "--all-users": scope = OrcaScope.AllUsers; break;
                case "--clear-caches": actions |= OrcaActions.ClearCaches; break;
                case "--reset-profile-settings": actions |= OrcaActions.ResetProfileSettings; break;
                case "--reset-authentication": actions |= OrcaActions.ResetAuthentication; break;
                case "--delete-ost": actions |= OrcaActions.DeleteOstFiles; break;
                case "--remove-add-ins": actions |= OrcaActions.RemoveAddIns; break;
                case "--reinstall-new-outlook": actions |= OrcaActions.ReinstallNewOutlook; break;
                case "--json": json = true; break;
                case "--execute" when args[0] == "orca-cache-reset": execute = true; break;
                default: return args[0] == "orca-review" ? Usage("Unknown ORCA review option: " + args[index]) :
                    CacheUsage("Unknown ORCA cache option: " + args[index]);
            }
        }
        if (args[0] == "orca-cache-reset" && (scope != OrcaScope.RunningAccount ||
            actions is not (OrcaActions.None or OrcaActions.ClearCaches)))
            return CacheUsage("ORCA cache reset supports the running account and cache selection only.");
        if (args[0] == "orca-cache-reset") actions = OrcaActions.ClearCaches;
        var options = new OrcaReviewOptions(mode, scope, actions);
        try { options.Validate(); }
        catch (ArgumentException error) { return args[0] == "orca-review" ? Usage(error.Message) : CacheUsage(error.Message); }
        try
        {
            if (args[0] == "orca-cache-reset")
            {
                using var cancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
                Console.CancelKeyPress += handler;
                try
                {
                    var progress = new Progress<string>(message => Console.Error.WriteLine(message));
                    var reset = await OrcaCacheReset.RunAsync(options, execute, JournalFolder(), progress: progress,
                        token: cancellation.Token);
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        reset.Outcome,
                        reset.Detail,
                        Journal = reset.JournalPath is null ? null : Path.GetFileName(reset.JournalPath),
                        CacheLabels = reset.Targets?.Select(target => target.Label).ToArray() ?? []
                    }, Capture.JsonOptions));
                    return reset.GetExitCode();
                }
                finally { Console.CancelKeyPress -= handler; }
            }
            var review = OrcaPlanning.ReviewCurrent(options);
            Console.WriteLine(json ? JsonSerializer.Serialize(review, Capture.JsonOptions) : review.Format());
            return review.Complete ? 0 : 3;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("ORCA review unavailable: " + error.Message); return 1; }
    }

    private static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]");
        Console.Error.WriteLine("Review only. No process, package, file, credential, registry value, add-in or user data changes.");
        return 2;
    }

    private static int CacheUsage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: orca-cache-reset --mode New|Classic|Both [--execute]");
        Console.Error.WriteLine("Preview by default. Retains exact running-account cache directories under unique recovery names. No retained cache is deleted.");
        Console.Error.WriteLine("Usage: orca-cache-reset-recover");
        return 2;
    }

    private static string JournalFolder() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Workflows");
}
