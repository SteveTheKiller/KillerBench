using KillerBench.Engine;

internal static class FoxitChecks
{
    internal static void Run()
    {
        CheckTraversal();
        var settings = FoxitUpdaterSettings.Describe(new([new("Registry32", @"SOFTWARE\Foxit Software\Fixture\Updater", "UpdateMode", "String", "0")], []));
        if (settings.State != CheckState.Information || settings.Repair is not null || !settings.Detail.Contains("Registry32") ||
            !settings.Evidence!.Output.Contains("UpdateMode")) throw new Exception("Foxit setting identity or type lost");
        var partialSettings = FoxitUpdaterSettings.Describe(new([], ["Registry64: access denied"]));
        if (partialSettings.State != CheckState.Unknown || partialSettings.Repair is not null) throw new Exception("Incomplete Foxit settings reported complete");
        foreach (Exception failure in new Exception[] { new System.IO.IOException(), new UnauthorizedAccessException(), new System.Security.SecurityException() })
        {
            var failed = FoxitUpdaterSettings.CheckAsync(collector: _ => throw failure).GetAwaiter().GetResult();
            if (failed.State != CheckState.Unknown || failed.Repair is not null || failed.Evidence is not null)
                throw new Exception("Foxit collector failure fabricated complete evidence");
        }
        using (var stopped = new CancellationTokenSource())
        {
            stopped.Cancel();
            try { FoxitUpdaterSettings.CheckAsync(_ => throw new Exception("Canceled collector ran"), token: stopped.Token).GetAwaiter().GetResult(); throw new Exception("Foxit cancellation ignored"); }
            catch (OperationCanceledException) { }
        }
        const string row = "{\"view\":\"Registry64\",\"key\":\"fixture\",\"name\":\"Foxit PDF\",\"version\":\"1\",\"publisher\":null}";
        var evidence = new ToolResult(0, "{\"complete\":true,\"products\":[" + row + "]}", "", false);
        var good = FoxitInventory.Interpret(evidence);
        if (!good.Complete || good.Products.Count != 1 || good.Evidence != evidence) throw new Exception("Foxit inventory evidence lost");
        foreach (string invalid in new[] { "null", "{}", row.Replace("Registry64", "other"), row.Replace("Foxit PDF", "Other PDF") })
        {
            var partial = FoxitInventory.Interpret(evidence with { Output = "{\"complete\":true,\"products\":[" + row + "," + invalid + "]}" });
            if (partial.Complete || partial.Products.Count != 1) throw new Exception("Foxit incomplete inventory classification failed");
        }
        if (FoxitInventory.Interpret(evidence with { Truncated = true }).Complete) throw new Exception("Truncated Foxit inventory accepted");
        foreach (string payload in new[]
        {
            evidence.Output.Replace("\"complete\":true", "\"complete\":false,\"complete\":true"),
            evidence.Output.Replace("\"key\":\"fixture\"", "\"key\":\"different\",\"key\":\"fixture\""),
            "{\"complete\":true,\"products\":[" + row + "," + row + "]}"
        })
            if (FoxitInventory.Interpret(evidence with { Output = payload }).Complete) throw new Exception("Ambiguous Foxit inventory accepted");
        var distinctViews = FoxitInventory.Interpret(evidence with { Output = "{\"complete\":true,\"products\":[" + row + "," + row.Replace("Registry64", "Registry32") + "]}" });
        if (!distinctViews.Complete || distinctViews.Products.Count != 2) throw new Exception("Distinct registry views incorrectly merged");
        Console.WriteLine("PASS: Foxit product identity, partial coverage and retained evidence (fixtures only).");
    }

    private static void CheckTraversal()
    {
        const string root = @"SOFTWARE\Foxit Software";
        var empty = new Dictionary<string, FoxitRegistryValue>();
        var entries = new Dictionary<string, FoxitRegistryValue>
        {
            ["UpdateMode"] = new("DWord", 0),
            ["bNoUpdate"] = new("String", "1")
        };
        var observed = FoxitUpdaterSettings.Collect((view, path) =>
        {
            if (view == "Registry32") throw new UnauthorizedAccessException();
            if (path == root) return new(["Updater", "Missing", "Denied"], empty);
            if (path.EndsWith("Denied")) throw new System.IO.IOException();
            return path.EndsWith("Updater") ? new([], entries) : null;
        });
        if (observed.Complete || observed.Values.Count != 2 || observed.Issues.Count != 3 ||
            observed.Values.Any(v => v.View != "Registry64" || v.Path != root + "\\Updater"))
            throw new Exception("Foxit partial traversal discarded independent observations");
        foreach (object invalid in new object[] { new byte[] { 1 }, new string('x', 4097) })
        {
            var bad = FoxitUpdaterSettings.Collect((_, path) => path == root ? new(["Updater"], empty) :
                new([], new Dictionary<string, FoxitRegistryValue> { ["UpdateMode"] = new("Binary", invalid) }));
            if (bad.Complete || bad.Values.Count != 2 || bad.Values.Any(v => v.Value is not null))
                throw new Exception("Unsupported Foxit values accepted");
        }
        int calls = 0;
        var wide = FoxitUpdaterSettings.Collect((_, _) =>
        {
            calls++;
            return new([.. Enumerable.Range(0, 300).Select(i => "Key" + i)], empty);
        });
        if (wide.Complete || calls != 512) throw new Exception("Foxit key traversal exceeded its per-view bound");
        calls = 0;
        var deep = FoxitUpdaterSettings.Collect((_, _) => { calls++; return new(["Child"], empty); });
        if (deep.Complete || calls != 18 || deep.Issues.Any(i => !i.Contains("depth limit")))
            throw new Exception("Foxit depth boundary failed");
        var invalidChild = FoxitUpdaterSettings.Collect((_, path) => path == root ? new(["..\\Elsewhere"], empty) :
            throw new Exception("Foxit followed an invalid child"));
        if (invalidChild.Complete) throw new Exception("Foxit invalid path reported complete");
        using var cancel = new CancellationTokenSource();
        try
        {
            FoxitUpdaterSettings.Collect((_, _) => { cancel.Cancel(); return null; }, cancel.Token);
            throw new Exception("Foxit provider cancellation was lost");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Foxit traversal preserves partial views and bounds keys, depth, values and cancellation (substituted registry).");
    }
}
