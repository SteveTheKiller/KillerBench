using System.Text.Json;
using KillerBench.Engine;

internal static class PrivacyChangeCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("Clipboard history controls require Windows.");
                return 3;
            }
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "Privacy");
            if (args[0] is "privacy-setting" or "privacy-setting-recover" or "privacy-setting-undo")
                return await RunBooleanAsync(args, folder).ConfigureAwait(false);
            if (args[0] == "privacy-clipboard-history")
            {
                if (args.Length < 2 || !TryState(args[1], out bool enabled)) return Usage();
                bool execute = false;
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] == "--execute" && !execute) { execute = true; continue; }
                    if (args[i] == "--journal-folder" && ++i < args.Length) { folder = args[i]; continue; }
                    return Usage();
                }
                var result = await PrivacyClipboardHistoryExecution.RunAsync(enabled, execute, folder,
                    new WindowsPrivacyClipboardHistoryHost()).ConfigureAwait(false);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }

            if (args[0] == "privacy-clipboard-history-recover")
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--journal-folder" && ++i < args.Length) { folder = args[i]; continue; }
                    return Usage();
                }
                var recovery = await PrivacyClipboardHistoryExecution.ReconcileAsync(folder,
                    new WindowsPrivacyClipboardHistoryHost()).ConfigureAwait(false);
                Console.WriteLine(JsonSerializer.Serialize(recovery, Capture.JsonOptions));
                return recovery.GetExitCode();
            }

            if (args.Length < 2) return Usage();
            string journal = args[1];
            bool confirmed = false;
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--execute" && !confirmed) { confirmed = true; continue; }
                if (args[i] == "--journal-folder" && ++i < args.Length) { folder = args[i]; continue; }
                return Usage();
            }
            if (!confirmed)
            {
                Console.WriteLine("Undo preview only. Add --execute to restore the exact prior current-user value recorded by the verified journal. Current state and machine policy will be rechecked first.");
                return 0;
            }
            var undone = await PrivacyClipboardHistoryExecution.UndoAsync(journal, folder,
                new WindowsPrivacyClipboardHistoryHost()).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(undone, Capture.JsonOptions));
            return undone.GetExitCode();
        }
        catch (OperationCanceledException) { return 130; }
        catch (Exception error) when (error is ArgumentException or IOException or InvalidDataException or
            UnauthorizedAccessException or System.Security.SecurityException)
        {
            Console.Error.WriteLine(error.Message);
            return 3;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static async Task<int> RunBooleanAsync(string[] args, string folder)
    {
        if (args.Length < 2 || PrivacyBooleanSettingCatalog.Find(args[1]) is not { } definition) return Usage();
        var host = new WindowsPrivacyBooleanSettingHost(definition);
        if (args[0] == "privacy-setting")
        {
            if (args.Length < 3 || !TryState(args[2], out bool enabled)) return Usage();
            bool execute = false;
            for (int i = 3; i < args.Length; i++)
            {
                if (args[i] == "--execute" && !execute) { execute = true; continue; }
                if (args[i] == "--journal-folder" && ++i < args.Length) { folder = args[i]; continue; }
                return Usage();
            }
            var result = await PrivacyBooleanSettingExecution.RunAsync(definition, enabled, execute, folder, host).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        if (args[0] == "privacy-setting-recover")
        {
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--journal-folder" && ++i < args.Length) { folder = args[i]; continue; }
                return Usage();
            }
            var result = await PrivacyBooleanSettingExecution.ReconcileAsync(definition, folder, host).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.GetExitCode();
        }
        if (args.Length < 3) return Usage();
        string journal = args[2];
        bool executeUndo = false;
        for (int i = 3; i < args.Length; i++)
        {
            if (args[i] == "--execute" && !executeUndo) { executeUndo = true; continue; }
            if (args[i] == "--journal-folder" && ++i < args.Length) { folder = args[i]; continue; }
            return Usage();
        }
        if (!executeUndo)
        {
            Console.WriteLine("Undo preview only. Add --execute to restore the exact prior current-user value after fresh value and policy checks.");
            return 0;
        }
        var undo = await PrivacyBooleanSettingExecution.UndoAsync(definition, journal, folder, host).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(undo, Capture.JsonOptions));
        return undo.GetExitCode();
    }

    private static bool TryState(string value, out bool enabled)
    {
        enabled = false;
        if (value.Equals("on", StringComparison.OrdinalIgnoreCase)) { enabled = true; return true; }
        return value.Equals("off", StringComparison.OrdinalIgnoreCase);
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: privacy-clipboard-history on|off [--execute] [--journal-folder PATH]");
        Console.Error.WriteLine("   or: privacy-clipboard-history-recover [--journal-folder PATH]");
        Console.Error.WriteLine("   or: privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute] [--journal-folder PATH]");
        Console.Error.WriteLine("   or: privacy-setting KEY on|off [--execute] [--journal-folder PATH]");
        Console.Error.WriteLine("   or: privacy-setting-recover KEY [--journal-folder PATH]");
        Console.Error.WriteLine("   or: privacy-setting-undo KEY VERIFIED_JOURNAL [--execute] [--journal-folder PATH]");
        Console.Error.WriteLine("Keys: " + string.Join("|", PrivacyBooleanSettingCatalog.All.Select(setting => setting.Key)));
        return 2;
    }
}
