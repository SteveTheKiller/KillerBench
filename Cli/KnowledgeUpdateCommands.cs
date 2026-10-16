using System.Text.Json;
using KillerBench.Engine;

internal static class KnowledgeUpdateCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool update = args[0] == "knowledge-update";
        if (args.Length != 3)
        {
            Console.Error.WriteLine(update
                ? "Usage: knowledge-update CONFIG_JSON STORAGE_ROOT"
                : "Usage: knowledge-status CONFIG_JSON STORAGE_ROOT");
            return 2;
        }
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(update ? 10 : 2));
        bool userCanceled = false;
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; userCanceled = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var configuration = await KnowledgeUpdateConfiguration.ReadFileAsync(args[1], stop.Token);
            if (!update)
            {
                var statuses = await configuration.InspectAsync(args[2], stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Databases = statuses,
                    Scope = "Verified installed database status. No download, activation or repair execution."
                }, Capture.JsonOptions));
                return statuses.All(status => status.Verified) ? 0 : 3;
            }

            var plan = configuration.CreatePlan(args[2]);
            using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("KillerBench/1.0.0");
            var source = new HttpKnowledgeCandidateSource(client, plan.Endpoints);
            var progress = new Progress<KnowledgeUpdateProgress>(item =>
                Console.Error.WriteLine($"{item.PackId}: {item.Stage}. {item.Detail}"));
            var results = await KnowledgeUpdates.UpdateAllAsync(plan.Requests, source, progress, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Databases = results,
                Scope = "Signed HTTPS database update. Each candidate is verified before atomic activation; prior immutable versions are retained. No repair execution."
            }, Capture.JsonOptions));
            return results.All(result => result.State != KnowledgeUpdateState.Failed) ? 0 : 3;
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine(userCanceled ? "Database operation stopped." : "Database operation timed out."); return userCanceled ? 130 : 1; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            ArgumentException or JsonException or System.Security.Cryptography.CryptographicException or HttpRequestException)
        { Console.Error.WriteLine("Database operation failed: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
