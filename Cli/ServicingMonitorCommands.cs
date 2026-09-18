using System.Text.Json;
using KillerBench.Engine;

internal static class ServicingMonitorCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            int refresh = 3;
            int samples = 1;
            bool events = true, history = true, services = true, restart = true, setup = true, compatibility = true, readiness = true, transfers = true, deliveryOptimization = true, installerActivity = true, scheduledTask = true;
            for (int index = 1; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--refresh" when index + 1 < args.Length && int.TryParse(args[++index], out int value): refresh = value; break;
                    case "--samples" when index + 1 < args.Length && int.TryParse(args[++index], out int value): samples = value; break;
                    case "--skip-events": events = false; break;
                    case "--skip-history": history = false; break;
                    case "--skip-services": services = false; break;
                    case "--skip-restart": restart = false; break;
                    case "--skip-setup": setup = false; break;
                    case "--skip-compatibility": compatibility = false; break;
                    case "--skip-readiness": readiness = false; break;
                    case "--skip-transfers": transfers = false; break;
                    case "--skip-delivery-optimization": deliveryOptimization = false; break;
                    case "--skip-installer": installerActivity = false; break;
                    case "--skip-scheduled-task": scheduledTask = false; break;
                    default: return Usage();
                }
            }
            var options = new ServicingMonitorOptions(refresh, samples, events, history, services, restart, setup, compatibility, readiness, transfers, deliveryOptimization, installerActivity, scheduledTask).Validate();
            using var cancellation = new CancellationTokenSource();
            ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
            Console.CancelKeyPress += handler;
            try
            {
                var progress = new Progress<string>(message => Console.Error.WriteLine(message));
                var result = await ServicingActivityMonitor.ObserveAsync(options, progress: progress, token: cancellation.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { Options = options, Snapshots = result,
                    Limitation = "Point-in-time read-only evidence. Activity does not prove progress, completion, a stall or a fault. No update, setup, transfer, service, process, policy, restart or repair action was requested." }, Capture.JsonOptions));
                return result.Any(snapshot => snapshot.State is CheckState.Unknown or CheckState.Attention) ? 3 : 0;
            }
            finally { Console.CancelKeyPress -= handler; }
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Servicing activity observation stopped."); return 130; }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        { Console.Error.WriteLine(error.Message); return 2; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]");
        return 2;
    }
}
