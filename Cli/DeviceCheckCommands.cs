using System.Text.Json;
using KillerBench.Engine;

internal static class DeviceCheckCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        var names = Enum.GetNames<DeviceCategory>();
        string? name = args.Length >= 2 ? names.FirstOrDefault(n => string.Equals(n, args[1], StringComparison.OrdinalIgnoreCase)) : null;
        var knowledgePaths = new List<string>();
        AudioEndpointDirection? audioDirection = null;
        AudioEndpointRole? audioRole = null;
        bool invalid = name is null;
        for (int index = 2; !invalid && index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length) { invalid = true; break; }
            string option = args[index];
            string value = args[index + 1];
            if (option == "--knowledge" && knowledgePaths.Count < 32) knowledgePaths.Add(value);
            else if (option == "--direction" && name == nameof(DeviceCategory.Audio) && audioDirection is null &&
                Enum.TryParse(value, true, out AudioEndpointDirection direction)) audioDirection = direction;
            else if (option == "--role" && name == nameof(DeviceCategory.Audio) && audioRole is null &&
                Enum.TryParse(value, true, out AudioEndpointRole role)) audioRole = role;
            else invalid = true;
        }
        if (invalid)
        {
            Console.Error.WriteLine("Usage: device-check <audio|network|storage|printing|display|input|portable|bluetooth|camera|smartcard|biometric|problems> [--knowledge PACK_PATH ...] [--direction output|input --role console|multimedia|communications] (audio filters only; up to 32 packs; read-only JSON)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Reading " + name + " device state and driver inventory. No device changes.");
            DeviceCategory category = Enum.Parse<DeviceCategory>(name!);
            KnowledgeCatalogCollection? catalogs = null;
            if (knowledgePaths.Count > 0)
            {
                catalogs = new();
                foreach (string path in knowledgePaths)
                    catalogs.AddOrReplace(await KnowledgeCatalog.ReadFileAsync(path, stop.Token));
            }
            var checks = (await DeviceDiagnostics.CheckAsync(category, catalogs, stop.Token)).ToList();
            if (category == DeviceCategory.Audio)
                checks.AddRange(await DefaultAudioEndpoints.CheckAsync(new(audioDirection, audioRole), stop.Token));
            stop.Token.ThrowIfCancellationRequested();
            Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
            return checks.All(c => c.State is CheckState.Clear or CheckState.Information) ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Device inspection stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            System.ComponentModel.Win32Exception or InvalidDataException or JsonException or ArgumentException)
        { Console.Error.WriteLine("Device inspection unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
