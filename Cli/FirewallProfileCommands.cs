using System.Text.Json;
using KillerBench.Engine;

internal static class FirewallProfileCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Firewall");
        var host = new WindowsFirewallProfileEnableHost();
        if (args[0] == "firewall-enable-recover")
        {
            if (args.Length != 1) { Console.Error.WriteLine("Usage: firewall-enable-recover"); return 2; }
            try
            {
                var recovered = await FirewallProfileEnable.ReconcileAsync(folder, host,
                    new Progress<string>(message => Console.Error.WriteLine(message)));
                Console.WriteLine(JsonSerializer.Serialize(recovered, Capture.JsonOptions));
                return recovered.GetExitCode();
            }
            catch (Exception error) when (Expected(error)) { Console.Error.WriteLine(error.Message); return 3; }
        }

        bool execute = false;
        var profiles = new List<FirewallProfileTarget>();
        for (int index = 1; index < args.Length; index++)
        {
            if (args[index] == "--execute") { execute = true; continue; }
            if (args[index] == "--profile" && index + 1 < args.Length &&
                Enum.TryParse<FirewallProfileTarget>(args[++index], true, out var profile)) { profiles.Add(profile); continue; }
            Console.Error.WriteLine("Usage: firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]");
            return 2;
        }
        try
        {
            var options = FirewallProfileEnable.Validate(new(profiles));
            var result = await FirewallProfileEnable.RunAsync(options, execute, folder, host,
                new Progress<string>(message => Console.Error.WriteLine(message)));
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (Expected(error)) { Console.Error.WriteLine(error.Message); return 3; }
    }

    private static bool Expected(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception or PlatformNotSupportedException;
}
