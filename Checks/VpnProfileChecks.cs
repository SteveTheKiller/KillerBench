using KillerBench.Engine;

internal static class VpnProfileChecks
{
    private static readonly string[] ExpectedAuthenticationMethods = ["Eap", "MachineCertificate"];

    internal static void Run()
    {
        static ToolResult Evidence(string json) => new(0, json, "", false);
        const string json = "{\"complete\":true,\"profiles\":[{\"name\":\"Field VPN\",\"server\":\"vpn.example.test\",\"tunnel\":\"Ikev2\",\"authentication\":[\"Eap\",\"MachineCertificate\"],\"split\":true,\"remember\":false,\"status\":\"Disconnected\"}]}";
        var inventory = VpnProfileDiagnostics.Interpret(Evidence(json));
        var checks = VpnProfileDiagnostics.Describe(inventory);
        Require(inventory.Complete && inventory.Profiles.Count == 1 && inventory.Profiles[0].AuthenticationMethods.SequenceEqual(ExpectedAuthenticationMethods) &&
            checks.Count == 2 && checks.All(check => check.Repair is null) && checks[0].Evidence == inventory.Evidence &&
            checks[0].Detail.Contains("Passwords, certificates and keys are not collected", StringComparison.Ordinal),
            "Complete VPN profile evidence retains configuration without credentials or repair eligibility");
        var empty = VpnProfileDiagnostics.Interpret(Evidence("{\"complete\":true,\"profiles\":[]}"));
        Require(empty.Complete && empty.Profiles.Count == 0 && VpnProfileDiagnostics.Describe(empty)[0].State == CheckState.Information,
            "An empty complete profile inventory is distinct from unavailable coverage");
        foreach (var evidence in new[]
        {
            Evidence(json.Replace("Field VPN", "FIELD VPN").Replace("]}", "," + json.Substring(json.IndexOf('{', 2) + 1))),
            Evidence(json.Replace("\"Eap\",\"MachineCertificate\"", "\"Eap\",\"EAP\"")),
            Evidence(json.Replace("vpn.example.test", "bad\u0001server")),
            Evidence(json.Replace("\"split\":true", "\"split\":\"true\"")),
            Evidence("{\"complete\":true,\"complete\":false,\"profiles\":[]}"),
            Evidence("not json"),
            Evidence(json) with { ExitCode = 1 },
            Evidence(json) with { Error = "unavailable" },
            Evidence(json) with { TimedOut = true },
            Evidence(json) with { Truncated = true }
        })
        {
            var incomplete = VpnProfileDiagnostics.Interpret(evidence);
            Require(!incomplete.Complete && VpnProfileDiagnostics.Describe(incomplete)[0].State == CheckState.Unknown,
                "Duplicate, malformed and incomplete VPN evidence cannot claim profile coverage");
        }
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        try
        {
            VpnProfileDiagnostics.Interpret(Evidence(json), stop.Token);
            throw new InvalidOperationException("Canceled VPN profile parsing continued");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: VPN profile identities, configuration fields, credential exclusion, empty state and incomplete evidence; no connection attempted.");
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
