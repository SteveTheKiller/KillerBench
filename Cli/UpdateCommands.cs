using System.Text.Json;
using KillerBench.Engine;

internal static class UpdateCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        var selected = new List<UpdateSelection>();
        bool drivers = false, software = true, restart = false, featureUpgrade = false, json = false, execute = false;
        UpdateSearchSource source = UpdateSearchSource.Configured;
        bool recover = args[0] == "update-recover";
        if (recover && args.Length != 1) return Usage();
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--drivers": drivers = true; break;
                case "--no-software": software = false; break;
                case "--allow-restart-required": restart = true; break;
                case "--allow-feature-upgrade": featureUpgrade = true; break;
                case "--json": json = true; break;
                case "--execute": execute = true; break;
                case "--source" when i + 1 < args.Length:
                    if (!Enum.TryParse(args[++i], true, out source) || !Enum.IsDefined(source)) return Usage();
                    break;
                case "--select" when i + 1 < args.Length:
                    var parts = args[++i].Split(':');
                    if (parts.Length != 2 || !Guid.TryParse(parts[0], out var id) || id == Guid.Empty ||
                        !int.TryParse(parts[1], out int revision) || revision < 0) return Usage();
                    selected.Add(new(id, revision));
                    break;
                default: return Usage();
            }
        }
        if (!recover && (selected.Count is < 1 or > 500 || selected.Distinct().Count() != selected.Count || (!drivers && !software))) return Usage();
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Updates");
            if (recover)
            {
                var recovery = await UpdateReconciliation.ResolveAsync(folder, new WindowsUpdateExecutionHost(), stop.Token);
                Console.WriteLine(recovery.Detail);
                Console.WriteLine("Recovery records: " + folder);
                return recovery.Resolved ? 0 : 3;
            }
            if (execute)
            {
                var result = await UpdateExecution.RunAsync(new(software, drivers, source), selected, new(restart, featureUpgrade), folder,
                    new WindowsUpdateExecutionHost(), new Progress<string>(Console.Error.WriteLine), stop.Token);
                Console.WriteLine(json ? JsonSerializer.Serialize(result, Capture.JsonOptions) : result.Format());
                return result.GetExitCode();
            }
            var discovery = await UpdateDiscovery.SearchAsync(new(software, drivers, source), new Progress<string>(Console.Error.WriteLine), stop.Token);
            if (discovery.Partial)
            {
                Console.Error.WriteLine("Update discovery was incomplete. No installation plan prepared.");
                return 3;
            }
            var plan = UpdatePlanning.Create(discovery, selected, new(restart, featureUpgrade));
            Console.WriteLine(json ? JsonSerializer.Serialize(plan, Capture.JsonOptions) : plan.Format());
            return plan.Eligible ? 0 : 3;
        }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
        catch (OperationCanceledException) { Console.Error.WriteLine(execute ? "Update operation stopped. Review the journal before another attempt." : "Update check stopped. No installation started."); return 130; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Update operation unavailable: " + e.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("update-plan --select GUID:REVISION [--select GUID:REVISION ...] [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--allow-restart-required] [--allow-feature-upgrade] [--json] [--execute]. Preview by default; feature upgrades require both allowance flags and must be selected alone. update-recover takes no options and never installs updates.");
        return 2;
    }
}
