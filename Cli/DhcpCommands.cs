using System.Text.Json;
using KillerBench.Engine;

internal static class DhcpCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool check = args[0] == "dhcp-check";
        bool recover = args[0] == "dhcp-renew-recover";
        bool renew = args[0] == "dhcp-renew";
        bool execute = renew && args.Length == 2 && args[1] == "--execute";
        if (check && args.Length != 1 || recover && args.Length != 1 || renew && args.Length != 1 && !execute || !check && !recover && !renew)
        {
            Console.Error.WriteLine("Usage: dhcp-check | dhcp-renew [--execute] | dhcp-renew-recover");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (recover)
            {
                Console.Error.WriteLine("Rechecking interrupted DHCP renewal without requesting another lease.");
                var reconciliation = await DhcpLeaseReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(),
                    DhcpRecovery.InspectEvidenceAsync, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(reconciliation, Capture.JsonOptions));
                return reconciliation.Outcome is DhcpLeaseReconciliationOutcome.NoPending or DhcpLeaseReconciliationOutcome.Verified ? 0 : 3;
            }
            Console.Error.WriteLine("Reading connected physical DHCP adapter state. No leases will be renewed.");
            var eligibility = await DhcpRecovery.CheckAsync(stop.Token);
            if (check)
            {
                Console.WriteLine(JsonSerializer.Serialize(eligibility, Capture.JsonOptions));
                return eligibility.State == CheckState.Clear ? 0 : 3;
            }
            var plan = Repairs.Plan(RepairKind.DhcpLease);
            Console.WriteLine(plan.Title + "\nTARGET: THIS PC (" + Environment.MachineName + ")\n\n" + plan.Effects +
                "\n\nPreview only. Add --execute to request the guarded repair after fresh evidence.");
            Console.WriteLine(JsonSerializer.Serialize(eligibility, Capture.JsonOptions));
            if (!execute) return 0;
            if (eligibility is not { State: CheckState.Repairable, Repair: RepairKind.DhcpLease })
            {
                Console.Error.WriteLine("Fresh evidence did not identify an exact missing or expired DHCP lease. No renewal was requested.");
                return 3;
            }
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Repairs");
            var result = await Repairs.RunAsync(RepairKind.DhcpLease, folder, new ConsoleProgress(), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode(stop.IsCancellationRequested);
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("DHCP check stopped."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("DHCP check unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
