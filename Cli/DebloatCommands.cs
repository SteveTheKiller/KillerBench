using System.Text.Json;
using KillerBench.Engine;

internal static class DebloatCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        DebloatAreas areas = DebloatAreas.None;
        DebloatUserScope scope = DebloatUserScope.RunningAccount;
        bool json = false;
        bool applyHighPerformance = false;
        bool applyPowerTimers = false;
        bool disableHibernation = false;
        string? retainedOfficeCulture = null;
        uint? sleepAc = null, sleepDc = null, displayAc = null, displayDc = null;
        try
        {
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--area":
                        string name = Value(args, ref i, "--area");
                        if (!Enum.TryParse(name, true, out DebloatAreas area) || !Enum.IsDefined(area) || area == DebloatAreas.None)
                            return Usage("Unknown declutter area: " + name);
                        areas |= area;
                        break;
                    case "--all-profiles": scope = DebloatUserScope.AllProfilesAndDefault; break;
                    case "--keep-office-culture": retainedOfficeCulture = Value(args, ref i, "--keep-office-culture"); break;
                    case "--sleep-ac": sleepAc = Minutes(args, ref i, "--sleep-ac"); break;
                    case "--sleep-dc": sleepDc = Minutes(args, ref i, "--sleep-dc"); break;
                    case "--display-ac": displayAc = Minutes(args, ref i, "--display-ac"); break;
                    case "--display-dc": displayDc = Minutes(args, ref i, "--display-dc"); break;
                    case "--json": json = true; break;
                    case "--apply-high-performance": applyHighPerformance = true; break;
                    case "--apply-power-timers": applyPowerTimers = true; break;
                    case "--disable-hibernation": disableHibernation = true; break;
                    default: return Usage("Unknown declutter review option: " + args[i]);
                }
            }
        }
        catch (ArgumentException error) { return Usage(error.Message); }
        bool anyTimer = sleepAc.HasValue || sleepDc.HasValue || displayAc.HasValue || displayDc.HasValue;
        if (anyTimer && !(sleepAc.HasValue && sleepDc.HasValue && displayAc.HasValue && displayDc.HasValue))
            return Usage("All four power timeout values are required together.");
        var options = new DebloatOptions(areas, scope, RetainedOfficeCulture: retainedOfficeCulture,
            PowerRequest: anyTimer ? new(sleepAc!.Value, sleepDc!.Value, displayAc!.Value, displayDc!.Value) : null);
        try { options.Validate(); }
        catch (ArgumentException error) { return Usage(error.Message); }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var review = await DebloatPlanning.ReviewAsync(options, new Progress<string>(Console.Error.WriteLine), stop.Token);
            if (!applyHighPerformance && !applyPowerTimers && !disableHibernation)
            {
                Console.WriteLine(json ? JsonSerializer.Serialize(review, Capture.JsonOptions) : review.Format());
                return review.Ready ? 0 : 3;
            }
            if ((applyHighPerformance ? 1 : 0) + (applyPowerTimers ? 1 : 0) + (disableHibernation ? 1 : 0) > 1)
                return Usage("Select only one declutter execution option at a time.");
            string debloatFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Debloat");
            if (disableHibernation)
            {
                if (areas != DebloatAreas.Hibernation)
                    return Usage("Disabling hibernation requires Hibernation as the only selected area.");
                var host = new WindowsDebloatHibernationHost();
                if (File.Exists(Path.Combine(debloatFolder, "debloat-hibernation.pending.json")))
                {
                    var reconciliation = await DebloatPowerReconciliation.RunHibernationAsync(debloatFolder, host, new Progress<string>(Console.Error.WriteLine), stop.Token);
                    Console.WriteLine(json ? JsonSerializer.Serialize(reconciliation, Capture.JsonOptions) : reconciliation.Detail);
                    return reconciliation.GetExitCode();
                }
                var hibernation = await DebloatHibernationExecution.RunAsync(true, debloatFolder, host,
                    new Progress<string>(Console.Error.WriteLine), stop.Token);
                Console.WriteLine(json ? JsonSerializer.Serialize(hibernation, Capture.JsonOptions) : hibernation.Detail);
                return hibernation.GetExitCode();
            }
            if (applyPowerTimers)
            {
                if (areas != DebloatAreas.SleepAndDisplayTimers || options.PowerRequest is null)
                    return Usage("Power-timer changes require SleepAndDisplayTimers as the only area and all four timeout values.");
                var host = new WindowsDebloatPowerTimerHost();
                if (File.Exists(Path.Combine(debloatFolder, "debloat-power-timers.pending.json")))
                {
                    var reconciliation = await DebloatPowerReconciliation.RunTimersAsync(debloatFolder, host, new Progress<string>(Console.Error.WriteLine), stop.Token);
                    Console.WriteLine(json ? JsonSerializer.Serialize(reconciliation, Capture.JsonOptions) : reconciliation.Detail);
                    return reconciliation.GetExitCode();
                }
                var timers = await DebloatPowerTimerExecution.RunAsync(options.PowerRequest, true, debloatFolder, host,
                    new Progress<string>(Console.Error.WriteLine), stop.Token);
                Console.WriteLine(json ? JsonSerializer.Serialize(timers, Capture.JsonOptions) : timers.Detail);
                return timers.GetExitCode();
            }
            if (areas != DebloatAreas.HighPerformancePower)
                return Usage("Power-plan activation requires HighPerformancePower as the only selected area.");
            var planHost = new WindowsDebloatPowerPlanChangeHost();
            if (File.Exists(Path.Combine(debloatFolder, "debloat-power-plan.pending.json")))
            {
                var reconciliation = await DebloatPowerReconciliation.RunPlanAsync(debloatFolder, planHost, new Progress<string>(Console.Error.WriteLine), stop.Token);
                Console.WriteLine(json ? JsonSerializer.Serialize(reconciliation, Capture.JsonOptions) : reconciliation.Detail);
                return reconciliation.GetExitCode();
            }
            var result = await DebloatPowerPlanExecution.RunAsync(DebloatPowerPlanExecution.CreateHighPerformance(), true,
                debloatFolder, planHost, new Progress<string>(Console.Error.WriteLine), stop.Token);
            Console.WriteLine(json ? JsonSerializer.Serialize(result, Capture.JsonOptions) : result.Detail);
            return result.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Declutter review stopped."); return 130; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static string Value(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length) throw new ArgumentException("Missing value for " + option + ".");
        return args[++index];
    }

    private static uint Minutes(string[] args, ref int index, string option)
    {
        string value = Value(args, ref index, option);
        if (!uint.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out uint minutes) || minutes > 1440)
            throw new ArgumentException(option + " must be from 0 to 1440 minutes. Zero means Never.");
        return minutes;
    }

    private static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Usage: debloat-review --area AREA [--area AREA ...] [--all-profiles] [--keep-office-culture CULTURE] [--sleep-ac MINUTES --sleep-dc MINUTES --display-ac MINUTES --display-dc MINUTES] [--apply-high-performance|--apply-power-timers|--disable-hibernation] [--json]");
        Console.Error.WriteLine("Areas: " + string.Join("|", Enum.GetNames<DebloatAreas>().Where(name => name != nameof(DebloatAreas.None))));
        Console.Error.WriteLine("Review only unless an explicit power execution option is selected for its matching area.");
        return 2;
    }
}
