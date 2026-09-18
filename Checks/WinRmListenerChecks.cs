using KillerBench.Engine;

internal static class WinRmListenerChecks
{
    internal static void Run()
    {
        const string http = "{\"identity\":\"listener-1\",\"address\":\"*\",\"transport\":\"HTTP\",\"port\":null,\"enabled\":true,\"urlPrefix\":\"wsman\",\"hasCertificate\":false}";
        const string https = "{\"identity\":\"listener-2\",\"address\":\"IP:10.0.0.8\",\"transport\":\"HTTPS\",\"port\":7443,\"enabled\":false,\"urlPrefix\":\"management\",\"hasCertificate\":true}";
        var evidence = new ToolResult(0, "{\"complete\":true,\"listeners\":[" + http + "," + https + "]}", "", false);
        var inventory = WinRmListeners.Parse(evidence);
        var checks = WinRmListeners.Describe(inventory);
        Require(inventory.Complete && inventory.Listeners.Count == 2 && checks.Count == 3, "HTTP and HTTPS listeners retained");
        Require(checks[1].Detail.Contains("5985 (transport default)") && checks[2].Detail.Contains("7443") &&
            checks[2].Detail.Contains("certificate configured") && checks[2].State == CheckState.Attention,
            "Listener defaults, explicit ports, certificates and disabled state described");
        Require(checks.All(check => check.Repair is null && ReferenceEquals(check.Evidence, evidence)), "Listener evidence never selects repair");
        Require(!checks.Any(check => check.Detail.Contains("listener-1") || check.Detail.Contains("listener-2")), "Listener identities omitted from summaries");
        var empty = WinRmListeners.Describe(WinRmListeners.Parse(evidence with { Output = "{\"complete\":true,\"listeners\":[]}" })).Single();
        Require(empty.State == CheckState.Information && empty.Detail.Contains("No listener registrations"), "Complete empty inventory remains informational");
        foreach (var invalid in new[]
        {
            evidence with { ExitCode = 1 }, evidence with { Error = "access denied" }, evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "{}" }, evidence with { Output = evidence.Output.Replace("\"complete\":true", "\"complete\":false") },
            evidence with { Output = evidence.Output.Replace("listener-2", "listener-1") }, evidence with { Output = evidence.Output.Replace("HTTPS", "SSH") },
            evidence with { Output = evidence.Output.Replace("7443", "70000") }, evidence with { Output = evidence.Output.Replace("\"enabled\":false", "\"enabled\":null") },
            evidence with { Output = evidence.Output.Replace("\"urlPrefix\":\"management\"", "\"urlPrefix\":\"management\",\"extra\":1") }
        })
            Require(WinRmListeners.Describe(WinRmListeners.Parse(invalid)).Single().State == CheckState.Unknown,
                "Incomplete or malformed listener evidence remains unknown");
        var oversized = evidence with { Output = "{\"complete\":false,\"listeners\":[" + string.Join(',', Enumerable.Repeat(http, 33)) + "]}" };
        Require(!WinRmListeners.Parse(oversized).Complete, "Oversized listener inventory rejected");
        try { WinRmListeners.Parse(evidence, new CancellationToken(true)); throw new Exception("Listener parsing ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: WinRM listener HTTP, HTTPS, empty, inaccessible, malformed, duplicate, bounded and cancellation evidence; no configuration changed.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
