using KillerBench.Engine;

internal static class DeliveryOptimizationPolicyChecks
{
    internal static void Run()
    {
        var empty = DeliveryOptimizationPolicyDiagnostics.Assess(new(true, [], [], [], [], 0));
        Require(empty is { State: CheckState.Information, Repair: null } && empty.Detail.Contains("does not prove the device is unmanaged", StringComparison.Ordinal),
            "Empty Delivery Optimization policy surfaces remain informational");

        var classic = DeliveryOptimizationPolicyDiagnostics.Assess(new(true,
            [new("DODownloadMode", 1), new("DOMaxCacheSize", 20)], ["DOGroupID"], [], [], 0));
        Require(classic is { State: CheckState.Information, Repair: null } &&
            classic.Detail.Contains("3 recognized", StringComparison.Ordinal) && classic.Detail.Contains("1 (LAN)", StringComparison.Ordinal) &&
            classic.Detail.Contains("their values were excluded", StringComparison.Ordinal),
            "Classic numeric policy and sensitive value names are reported without sensitive values");

        var csp = DeliveryOptimizationPolicyDiagnostics.Assess(new(true, [], [],
            [new("DODownloadMode", 2), new("DOMaxBackgroundDownloadBandwidth", 500)], ["DOCacheHost"], 2));
        Require(csp.State == CheckState.Information && csp.Detail.Contains("3 recognized", StringComparison.Ordinal) &&
            csp.Detail.Contains("2 include provider metadata", StringComparison.Ordinal) && csp.Detail.Contains("2 (Group)", StringComparison.Ordinal),
            "Policy CSP settings identify their surface and provider metadata without provider identities");

        var mixed = DeliveryOptimizationPolicyDiagnostics.Assess(new(true,
            [new("DODownloadMode", 1)], [], [new("DODownloadMode", 0)], [], 1));
        Require(mixed.State == CheckState.Information && mixed.Detail.Contains("both the classic policy surface", StringComparison.Ordinal) &&
            mixed.Detail.Contains("Resultant Set of Policy or MDM review", StringComparison.Ordinal),
            "Mixed policy surfaces request authority review without inventing the winner");

        var bypass = DeliveryOptimizationPolicyDiagnostics.Assess(new(true,
            [new("DODownloadMode", 100)], [], [], [], 0));
        Require(bypass is { State: CheckState.Attention, Repair: null } && bypass.Detail.Contains("deprecated on Windows 11", StringComparison.Ordinal),
            "Deprecated bypass mode requests policy-owner review without selecting a replacement");
        var future = DeliveryOptimizationPolicyDiagnostics.Assess(new(true,
            [new("DODownloadMode", 101)], [], [], [], 0));
        Require(future.State == CheckState.Information && future.Detail.Contains("unrecognized by this build", StringComparison.Ordinal),
            "Unknown future nonnegative mode values remain visible without an outdated label");

        foreach (var invalid in new[]
        {
            new DeliveryOptimizationPolicySnapshot(false, [], [], [], [], 0),
            new DeliveryOptimizationPolicySnapshot(true, [new("UnknownValue", 1)], [], [], [], 0),
            new DeliveryOptimizationPolicySnapshot(true, [new("DODownloadMode", -1)], [], [], [], 0),
            new DeliveryOptimizationPolicySnapshot(true, [new("DODownloadMode", 1), new("DODownloadMode", 2)], [], [], [], 0),
            new DeliveryOptimizationPolicySnapshot(true, [], ["UnknownValue"], [], [], 0),
            new DeliveryOptimizationPolicySnapshot(true, [], [], [new("DODownloadMode", 1)], [], 2),
            new DeliveryOptimizationPolicySnapshot(true, [], [], [], [], 0, "Access unavailable")
        })
        {
            var unknown = DeliveryOptimizationPolicyDiagnostics.Assess(invalid);
            Require(unknown is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Incomplete, invalid, duplicate or malformed Delivery Optimization policy remains unknown");
        }
        Console.WriteLine("PASS: Delivery Optimization classic and Policy CSP settings, privacy boundaries, future modes and deprecated bypass review; no policy changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
