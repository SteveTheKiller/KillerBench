using System.Text.Json;
using KillerBench.Engine;

internal static class ComputerNameCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string command = args[0];
        bool recover = command == "name-rename-recover";
        bool rename = command == "name-rename";
        bool execute = rename && args is [_, _, "--execute"];
        if (recover ? args.Length != 1 : rename ? args.Length is < 2 or > 3 || args.Length == 3 && !execute : args.Length != 2)
        {
            Console.Error.WriteLine(recover ? "Usage: name-rename-recover" : rename
                ? "Usage: name-rename PROPOSED_NAME [--execute]" : "Usage: name-review PROPOSED_NAME (read-only review; never renames or restarts)");
            return 2;
        }
        if (!recover)
        {
            try { ComputerNameReview.Review(args[1], new(null, null, null)); }
            catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "ComputerRename");
            var progress = new Progress<string>(message => Console.Error.WriteLine(message));
            if (recover)
            {
                var result = await ComputerRename.ReconcileAsync(folder, new WindowsComputerRenameHost(), progress, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            if (rename)
            {
                var result = await ComputerRename.RunAsync(args[1], execute, folder, new WindowsComputerRenameHost(), progress, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            Console.Error.WriteLine("Reading current computer identity. No changes will be made.");
            var report = await ComputerNameReview.ReviewCurrentAsync(args[1], stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(report, Capture.JsonOptions));
            return report.Context.CurrentName is null ? 3 : 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Name review stopped."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        { Console.Error.WriteLine("Computer name operation unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
