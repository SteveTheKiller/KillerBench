using System.Net;
using System.Net.Sockets;
using KillerBench.Engine;

internal static class DnsChecks
{
    internal static async Task RunAsync()
    {
        foreach (int missing in new[] { 0, 1, 2 })
        {
            bool rejected = false;
            try { await DnsDiagnostics.CheckAsync("example.test", resolver: (_, _) => missing == 0 ? null! : Task.FromResult<IPAddress[]>(missing == 1 ? null! : [null!])); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "Missing DNS provider tasks, results and addresses cannot establish resolution");
        }
        var result = await DnsDiagnostics.CheckAsync("example.test", resolver: (_, _) => Task.FromResult(new[] { IPAddress.Loopback, IPAddress.IPv6Loopback, IPAddress.Loopback }));
        Require(result.State == DnsLookupState.Resolved && result.Addresses.Length == 2, "Deduplicate answers without claiming reachability");
        foreach (var state in Enum.GetValues<DnsLookupState>())
        {
            var check = DnsDiagnostics.ToWorkflowCheck(result with { State = state });
            Require(check.Repair is null && check.State == (state == DnsLookupState.Resolved ? CheckState.Clear : state == DnsLookupState.TimedOut ? CheckState.Unknown : CheckState.Attention),
                "DNS outcomes cannot authorize repairs");
            var retained = System.Text.Json.JsonSerializer.Deserialize<DnsLookupResult>(check.Evidence!.Output, Capture.JsonOptions)!;
            Require(retained.Host == result.Host && retained.State == state && retained.Addresses.SequenceEqual(result.Addresses) &&
                check.Evidence.TimedOut == (state == DnsLookupState.TimedOut), "Workflow retains structured DNS evidence and timeout state");
        }
        Require(new WindowsWorkflowHost().NetworkLookupHost is null, "Lookup is opt-in");
        foreach (var (code, phrase) in new[] { (SocketError.HostNotFound, "did not recognize"), (SocketError.NoData, "address data"),
            (SocketError.TryAgain, "temporary failure"), (SocketError.NetworkDown, "subsystem"), (SocketError.HostUnreachable, "intended route"),
            (SocketError.AccessDenied, "permissions"), (SocketError.TimedOut, "timeout"), (SocketError.Fault, "reported socket error") })
        {
            var failed = await DnsDiagnostics.CheckAsync("example.test", TimeSpan.FromSeconds(3), resolver: (_, _) => Task.FromException<IPAddress[]>(new SocketException((int)code)));
            Require(failed.State == DnsLookupState.Failed && failed.ErrorCode == code && failed.Detail.Contains(phrase) &&
                DnsDiagnostics.ToWorkflowCheck(failed).Repair is null, "Resolver error retains its code and specific non-mutating guidance");
            var saved = DnsDiagnostics.ToWorkflowCheck(failed).Evidence!.Output;
            var restored = System.Text.Json.JsonSerializer.Deserialize<DnsLookupResult>(saved, Capture.JsonOptions)!;
            Require(restored.ErrorCode == code && restored.Timeout == TimeSpan.FromSeconds(3),
                "Structured resolver error and configured timeout survive workflow evidence serialization");
        }
        Require(new WindowsWorkflowHost { NetworkLookupHost = "example.test" }.NetworkLookupHost == "example.test", "Host retains explicit lookup target");
        Require((await DnsDiagnostics.CheckAsync("example.test", resolver: (_, _) => Task.FromResult(Array.Empty<IPAddress>()))).State == DnsLookupState.NoAddresses, "Empty response is distinct");
        Require((await DnsDiagnostics.CheckAsync("example.test", resolver: (_, _) => Task.FromException<IPAddress[]>(new SocketException((int)SocketError.HostNotFound)))).State == DnsLookupState.Failed, "Preserve resolver failure");
        var timedOut = await DnsDiagnostics.CheckAsync("example.test", TimeSpan.FromMilliseconds(20), resolver: (_, _) => new TaskCompletionSource<IPAddress[]>().Task);
        Require(timedOut.State == DnsLookupState.TimedOut && timedOut.Timeout == TimeSpan.FromMilliseconds(20), "Bound a resolver that ignores cancellation and retain its actual configured limit");
        using var stop = new CancellationTokenSource();
        try
        {
            await DnsDiagnostics.CheckAsync("example.test", token: stop.Token, resolver: (_, _) => { stop.Cancel(); return Task.FromResult(new[] { IPAddress.Loopback }); });
            throw new Exception("Cancellation lost");
        }
        catch (OperationCanceledException) { }
        foreach (string host in new[] { "", "https://example.test", "example.test:53", "127.0.0.1", "::1", " example.test", "example.test\n" })
        {
            try { DnsDiagnostics.ValidateHost(host); throw new Exception("Invalid hostname accepted: " + host); }
            catch (ArgumentException) { }
        }
        Console.WriteLine("PASS: DNS answers, empty response, failure, timeout, cancellation and input validation (substituted resolver).");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
