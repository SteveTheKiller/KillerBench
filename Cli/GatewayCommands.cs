using System.Text.Json;
using KillerBench.Engine;

internal static class GatewayCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: gateway-check (read current default routes and test only their exact next-hop addresses)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Reading default routes before bounded gateway checks. No neighboring address will be scanned.");
            var routes = await RouteDiagnostics.CheckAsync(stop.Token);
            var result = await GatewayReachabilityDiagnostics.CheckAsync(routes, token: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.State is CheckState.Information or CheckState.Clear ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Gateway check stopped."); return 130; }
        catch (Exception error) when (error is InvalidOperationException or NotSupportedException or
            System.Net.NetworkInformation.PingException or System.Net.Sockets.SocketException)
        { Console.Error.WriteLine("Gateway check unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
