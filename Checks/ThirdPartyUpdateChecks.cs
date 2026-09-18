using System.Text.Json;
using KillerBench.Engine;

internal static class ThirdPartyUpdateChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(object[] candidates, bool available = true, bool complete = true, int scanned = 12, string version = "1.11.0") =>
            new(0, JsonSerializer.Serialize(new { providerAvailable = available, providerVersion = available ? version : "", complete = available && complete, scanned = available ? scanned : 0, detail = available ? "Fixture provider completed." : "Fixture provider unavailable.", candidates }, Capture.JsonOptions), "", false);
        var candidate = new { id = "Vendor.Tool", name = "Vendor Tool", installedVersion = "1.0", source = "winget", availableVersions = new[] { "2.0", "1.5" } };
        var clear = ThirdPartyUpdateDiscovery.Interpret(Evidence([candidate]));
        Require(clear is { ProviderAvailable: true, Complete: true, Candidates.Count: 1 } && clear.Candidates[0].AvailableVersions.SequenceEqual(new[] { "2.0", "1.5" }) &&
            ThirdPartyUpdateDiscovery.Assess(clear).State == CheckState.Information && clear.Format().Contains("No application was downloaded"),
            "Complete structured WinGet evidence retains exact identities and remains read only");
        var empty = ThirdPartyUpdateDiscovery.Interpret(Evidence([]));
        Require(empty.Complete && empty.Candidates.Count == 0 && empty.Format().Contains("does not prove"), "Empty results do not claim every application is current");
        var unavailable = ThirdPartyUpdateDiscovery.Interpret(Evidence([], false));
        Require(!unavailable.ProviderAvailable && !unavailable.Complete && ThirdPartyUpdateDiscovery.Assess(unavailable).State == CheckState.Unknown,
            "Missing optional provider stays unknown");
        var partial = ThirdPartyUpdateDiscovery.Interpret(Evidence([candidate], complete: false));
        Require(partial.ProviderAvailable && !partial.Complete && partial.Candidates.Count == 1 && ThirdPartyUpdateDiscovery.Assess(partial).State == CheckState.Unknown,
            "Partial provider evidence retains candidates without claiming complete coverage");
        foreach (var invalid in new[]
        {
            Evidence([candidate, new { id = "vendor.tool", name = "Duplicate", installedVersion = "1.0", source = "WINGET", availableVersions = new[] { "2.0" } }]),
            Evidence([new { id = "Vendor.Tool", name = "Bad\nName", installedVersion = "1.0", source = "winget", availableVersions = new[] { "2.0" } }]),
            Evidence([new { id = "Vendor.Tool", name = "Vendor Tool", installedVersion = "1.0", source = "winget", availableVersions = Array.Empty<string>() }]),
            new ToolResult(0, JsonSerializer.Serialize(new { providerAvailable = true, providerVersion = "1.11.0", complete = true, scanned = 4097, detail = "Fixture provider completed.", candidates = new[] { candidate } }, Capture.JsonOptions), "", false),
            new ToolResult(1, "{}", "failed", false), new ToolResult(0, "{}", "", true), new ToolResult(0, "{}", "", false) { TimedOut = true }
        }) Require(!ThirdPartyUpdateDiscovery.Interpret(invalid).Complete && ThirdPartyUpdateDiscovery.Interpret(invalid).Candidates.Count == 0,
            "Duplicate, malformed, bounded, failed, truncated and timed-out evidence stays unknown");
        RepairStep? observed = null;
        var collected = await ThirdPartyUpdateDiscovery.ReadAsync(execute: (step, _) => { observed = step; return Task.FromResult(Evidence([])); });
        Require(observed is { Verification: true } && observed.Tool == ThirdPartyUpdateDiscovery.Tool && observed.Arguments.Last().Contains("Get-WinGetPackage -Count 4097") &&
            observed.Arguments.Last().Contains("IsUpdateAvailable") && !observed.Arguments.Last().Contains("Update-WinGetPackage") && collected.Complete,
            "Native plan uses bounded structured package objects without an update command");
        using var stop = new CancellationTokenSource(); stop.Cancel(); bool canceled = false;
        try { await ThirdPartyUpdateDiscovery.ReadAsync(token: stop.Token, execute: (_, _) => throw new Exception("provider should not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents third-party update collection");
        Console.WriteLine("PASS: third-party application update identities, provider availability, bounds, malformed evidence and cancellation; fixtures only.");
    }

    private static void Require(bool condition, string detail) { if (!condition) throw new Exception(detail); }
}
