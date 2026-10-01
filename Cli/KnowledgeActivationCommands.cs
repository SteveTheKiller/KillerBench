using System.Text.Json;
using KillerBench.Engine;

internal static class KnowledgeActivationCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool activate = args[0] == "knowledge-activate";
        bool recover = args[0] == "knowledge-recover";
        if ((activate && args.Length != 7) || (!activate && args.Length != 5) ||
            !long.TryParse(args[activate ? 5 : 4], out long minimumRevision) || minimumRevision < 1)
        {
            Console.Error.WriteLine(activate
                ? "Usage: knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY"
                : recover
                    ? "Usage: knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION"
                    : "Usage: knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION");
            return 2;
        }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        bool userCanceled = false;
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; userCanceled = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (activate)
            {
                byte[] payload = await ReadAsync(args[1], 32_000_000, stop.Token);
                byte[] signature = await ReadAsync(args[2], 64, stop.Token);
                byte[] key = await ReadAsync(args[3], 1024, stop.Token);
                var result = await KnowledgeActivation.ActivateAsync(args[6], new(payload, signature), key, args[4], minimumRevision, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { result.Catalog.PackId, result.Catalog.Revision, result.Catalog.PublishedOn,
                    result.Catalog.Count, result.Changed, result.Detail, result.ActivePath, result.DataPath,
                    Scope = "Verified local activation only. No download, publication or repair execution." }, Capture.JsonOptions));
                return 0;
            }
            else if (!recover)
            {
                byte[] key = await ReadAsync(args[2], 1024, stop.Token);
                var (catalog, pointer, dataPath) = await KnowledgeActivation.LoadActiveAsync(args[1], key, args[3], minimumRevision, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { catalog.PackId, catalog.Revision, catalog.PublishedOn,
                    catalog.Count, pointer.ActivatedUtc, DataPath = dataPath,
                    Scope = "Verified active local reference data. No download, activation or repair execution." }, Capture.JsonOptions));
                return 0;
            }
            else
            {
                byte[] key = await ReadAsync(args[2], 1024, stop.Token);
                var result = await KnowledgeActivation.RecoverAsync(args[1], key, args[3], minimumRevision, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { result.Catalog.PackId, result.Catalog.Revision,
                    result.Catalog.PublishedOn, result.Catalog.Count, result.FilesInspected, result.FilesRejected,
                    result.Detail, result.DataPath,
                    Scope = "Verified recovery from retained local versions. No download or repair execution." }, Capture.JsonOptions));
                return 0;
            }
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine(userCanceled ? "Knowledge activation stopped." : "Knowledge activation timed out."); return userCanceled ? 130 : 1; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or JsonException or System.Security.Cryptography.CryptographicException)
        { Console.Error.WriteLine("Knowledge activation failed: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<byte[]> ReadAsync(string path, int maximum, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is <= 0 || stream.Length > maximum) throw new InvalidDataException("Knowledge activation input size is invalid.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token);
        return bytes;
    }
}
