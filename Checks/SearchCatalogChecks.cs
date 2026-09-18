using System.Text.Json;
using KillerBench.Engine;

internal static class SearchCatalogChecks
{
    internal static void Run()
    {
        for (int status = 0; status <= 6; status++)
        for (int reason = 0; reason <= (status == 1 ? 10 : 0); reason++)
        {
            var raw = new ToolResult(0, JsonSerializer.Serialize(new { catalog = "SystemIndex", status, reason }), "", false);
            var check = SearchCatalog.Interpret(raw);
            if (check.State != (status == 1 && reason is 5 or 6 ? CheckState.Attention : CheckState.Information) ||
                check.Repair is not null || check.Evidence != raw || !check.Detail.Contains("does not establish complete search results"))
                throw new Exception("Catalog status classification or preservation failed");
        }
        foreach (string json in new[] { "{}", "[]", "{", "{\"catalog\":\"Other\",\"status\":0,\"reason\":0}",
            "{\"catalog\":\"SystemIndex\",\"status\":99,\"reason\":0}", "{\"catalog\":\"SystemIndex\",\"status\":0,\"reason\":5}",
            "{\"catalog\":\"SystemIndex\",\"status\":\"0\",\"reason\":0}", "{\"catalog\":\"SystemIndex\",\"status\":0,\"status\":0}" })
            if (SearchCatalog.Interpret(new(0, json, "", false)).State != CheckState.Unknown) throw new Exception("Invalid catalog state accepted");
        var valid = new ToolResult(0, "{\"catalog\":\"SystemIndex\",\"status\":0,\"reason\":0}", "", false);
        foreach (var failed in new[] { valid with { ExitCode = 5 }, valid with { Error = "denied" }, valid with { TimedOut = true }, valid with { Truncated = true } })
            if (SearchCatalog.Interpret(failed).State != CheckState.Unknown) throw new Exception("Failed catalog provider claimed success");
        Console.WriteLine("PASS: Search catalog states, pause reasons and incomplete evidence; no index changes.");
    }
}
