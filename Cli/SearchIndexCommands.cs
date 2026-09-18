using System.Text.Json;
using KillerBench.Engine;

internal static class SearchIndexCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool recover = args.Length > 0 && args[0] == "search-index-rebuild-recover";
        bool execute = !recover && args.Length == 2 && args[1] == "--execute";
        if (recover ? args.Length != 1 : args.Length is < 1 or > 2 || (args.Length == 2 && !execute)) return Usage();
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "SearchIndexRebuild");
            if (recover)
            {
                var result = await SearchIndexRebuildReconciliation.RunAsync(folder, new WindowsSearchIndexRebuildHost(), stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.GetExitCode();
            }
            var progress = new Progress<string>(Console.Error.WriteLine);
            var rebuild = await SearchIndexRebuild.RunAsync(execute, folder, new WindowsSearchIndexRebuildHost(), progress, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(rebuild, Capture.JsonOptions));
            return rebuild.GetExitCode();
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Search index operation stopped. Reconcile before retrying."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        { Console.Error.WriteLine("Search index operation unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: search-index-rebuild [--execute] | search-index-rebuild-recover. Preview is the default; recovery never repeats a repair command.");
        return 2;
    }
}
