using System.Text.Json;
using KillerBench.Engine;

internal static class EventChannelCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KillerBench", "EventChannels");
        var host = new WindowsEventChannelEnableHost();
        var progress = new Progress<string>(message => Console.Error.WriteLine(message));
        if (args[0] == "event-channel-enable-recover")
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: event-channel-enable-recover");
                return 2;
            }
            try
            {
                var recovery = await EventChannelEnable.ReconcileAsync(folder, host, progress);
                Console.WriteLine(JsonSerializer.Serialize(recovery, Capture.JsonOptions));
                return recovery.GetExitCode();
            }
            catch (Exception error) when (Expected(error))
            {
                Console.Error.WriteLine(error.Message);
                return 3;
            }
        }

        bool execute = args.Length == 3 && args[2] == "--execute";
        if (args.Length is < 2 or > 3 || args.Length == 3 && !execute)
        {
            Console.Error.WriteLine("Usage: event-channel-enable CHANNEL [--execute]");
            return 2;
        }
        try
        {
            string channel = EventChannelEnable.ValidateChannel(args[1]);
            var result = await EventChannelEnable.RunAsync(channel, execute, folder, host, progress);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (ArgumentException error)
        {
            Console.Error.WriteLine(error.Message);
            return 2;
        }
        catch (Exception error) when (Expected(error))
        {
            Console.Error.WriteLine(error.Message);
            return 3;
        }
    }

    private static bool Expected(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or TimeoutException or System.Security.SecurityException or
        System.ComponentModel.Win32Exception or PlatformNotSupportedException;
}
