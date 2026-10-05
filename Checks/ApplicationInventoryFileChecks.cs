using System.IO;
using KillerBench.Engine;

internal static class ApplicationInventoryFileChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var raw = new ToolResult(0, "{\"scope\":\"CurrentAccount\",\"complete\":true,\"applications\":[]}", "", false);
        var inventory = InstalledApplications.Interpret(raw);
        string path = Path.Combine(scratch, "saved-applications.json");
        await ApplicationInventoryFiles.SaveAsync(inventory, path);
        var loaded = await ApplicationInventoryFiles.ReadAsync(path);
        if (!loaded.Complete || loaded.Scope != InstalledApplicationScope.CurrentAccount || loaded.Evidence != raw)
            throw new Exception("Saved inventory lost evidence or scope.");
        byte[] original = await File.ReadAllBytesAsync(path);
        try { await ApplicationInventoryFiles.SaveAsync(inventory, path); throw new Exception("Existing inventory overwritten without opt-in."); }
        catch (IOException) { }
        try { await ApplicationInventoryFiles.SaveAsync(inventory, path, overwrite: true, token: new CancellationToken(true)); throw new Exception("Canceled save completed."); }
        catch (OperationCanceledException) { }
        if (!(await File.ReadAllBytesAsync(path)).SequenceEqual(original)) throw new Exception("Failed save changed destination.");
        var partial = inventory with { Evidence = raw with { Truncated = true } };
        await ApplicationInventoryFiles.SaveAsync(partial, path, overwrite: true);
        loaded = await ApplicationInventoryFiles.ReadAsync(path);
        if (loaded.Complete || !loaded.Evidence.Truncated || loaded.Evidence.Output != raw.Output)
            throw new Exception("Saving partial evidence fabricated complete coverage.");
        if (Directory.GetFiles(scratch, "saved-applications.json.*.tmp").Length != 0)
            throw new Exception("Inventory save left temporary files.");
        Console.WriteLine("PASS: inventory file round trip, retained partial evidence, overwrite opt-in, cancellation and destination preservation.");
    }
}
