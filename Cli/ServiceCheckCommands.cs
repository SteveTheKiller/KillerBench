using System.Text.Json;
using KillerBench.Engine;

internal static class ServiceCheckCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        var supported = Enum.GetValues<RepairKind>().Where(kind => ServiceRecovery.ServiceName(kind) is not null)
            .ToDictionary(kind => ServiceRecovery.ServiceName(kind)!, kind => kind, StringComparer.OrdinalIgnoreCase);
        RepairKind[] selected;
        if (args is ["service-check", "--all"]) selected = [.. supported.Values];
        else if (args.Length is >= 2 and <= 129 && args.Skip(1).All(supported.ContainsKey))
            selected = [.. args.Skip(1).Select(name => supported[name]).Distinct()];
        else
        {
            Console.Error.WriteLine("Usage: service-check NAME [NAME ...] | service-check --all");
            Console.Error.WriteLine("Supported services: " + string.Join(", ", supported.Keys));
            Console.Error.WriteLine("Read-only inspection. No services or startup settings are changed.");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var checks = await ServiceInspection.ReadAsync(selected, new ServiceProgress(), token: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
            return checks.All(check => check.State == CheckState.Clear) ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Service inspection stopped."); return 130; }
        catch (Exception error) when (error is NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Service inspection unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
    private sealed class ServiceProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
