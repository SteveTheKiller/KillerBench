using System.Text.Json;

namespace KillerBench.Engine;

public static class NetworkProfileDiagnostics
{
    private const string Scope = " Current profile assignment only; discovery rules, interface reachability and neighboring devices are not tested. No network settings are changed.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Network profile collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$profiles=@(Get-NetConnectionProfile -ErrorAction Stop | Sort-Object InterfaceIndex | Select-Object -First 33 InterfaceAlias,InterfaceIndex,@{n='NetworkCategory';e={$_.NetworkCategory.ToString()}},@{n='IPv4Connectivity';e={$_.IPv4Connectivity.ToString()}},@{n='IPv6Connectivity';e={$_.IPv6Connectivity.ToString()}}); " +
            "[pscustomobject]@{profiles=$profiles} | ConvertTo-Json -Depth 4 -Compress";
        try
        {
            var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return Interpret(result);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return [new("Network profiles", CheckState.Unknown,
                "Current network profile collection was unavailable." + Scope, Evidence: new(-1, "", error.Message, false))];
        }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? result)
    {
        WorkflowCheck Unknown(string detail) => new("Network profiles", CheckState.Unknown, detail + Scope, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Current network profile collection failed or was incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("profiles", out var profiles) || profiles.ValueKind != JsonValueKind.Array)
                return [Unknown("Current network profile data was malformed.")];
            if (profiles.GetArrayLength() > 32)
                return [Unknown("More than 32 current network profiles were returned, so coverage was capped.")];
            if (profiles.GetArrayLength() == 0)
                return [new("Network profiles", CheckState.Information,
                    "No current connection profiles were reported. Function Discovery may have no active network on which to publish or discover resources." + Scope,
                    Evidence: result)];

            var checks = new List<WorkflowCheck>();
            var indexes = new HashSet<int>();
            foreach (var profile in profiles.EnumerateArray())
            {
                if (profile.ValueKind != JsonValueKind.Object || profile.EnumerateObject().Count() != 5 ||
                    !profile.TryGetProperty("InterfaceAlias", out var aliasValue) || aliasValue.ValueKind != JsonValueKind.String ||
                    !DriverReference.ValidText(aliasValue.GetString(), 256) || aliasValue.GetString() != aliasValue.GetString()!.Trim() ||
                    !profile.TryGetProperty("InterfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index <= 0 || !indexes.Add(index) ||
                    !profile.TryGetProperty("NetworkCategory", out var categoryValue) || categoryValue.ValueKind != JsonValueKind.String ||
                    categoryValue.GetString() is not ("Public" or "Private" or "DomainAuthenticated") ||
                    !profile.TryGetProperty("IPv4Connectivity", out var ipv4Value) || ipv4Value.ValueKind != JsonValueKind.String ||
                    !ValidConnectivity(ipv4Value.GetString()) ||
                    !profile.TryGetProperty("IPv6Connectivity", out var ipv6Value) || ipv6Value.ValueKind != JsonValueKind.String ||
                    !ValidConnectivity(ipv6Value.GetString()))
                    return [Unknown("Current network profile identities or values were invalid or duplicated.")];

                string alias = aliasValue.GetString()!;
                string category = categoryValue.GetString()!;
                string detail = category == "Public"
                    ? "This interface uses the Public profile. Network discovery is commonly restricted on Public networks; this may be intentional. Review the network's trust before changing its category."
                    : category == "Private"
                        ? "This interface uses the Private profile. This supports a trusted-network discovery scenario but does not prove discovery rules or services are working."
                        : "This interface is domain authenticated. Domain policy can control discovery behavior; effective rules and policy ownership remain separate checks.";
                checks.Add(new("Network profile: " + alias,
                    category == "Public" ? CheckState.Attention : CheckState.Information,
                    $"Interface index {index}; category {category}; IPv4 {ipv4Value.GetString()}; IPv6 {ipv6Value.GetString()}. {detail}" + Scope,
                    Evidence: result));
            }
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return [Unknown("Current network profile data could not be interpreted.")];
        }
    }

    private static bool ValidConnectivity(string? value) =>
        value is "Disconnected" or "NoTraffic" or "Subnet" or "LocalNetwork" or "Internet";
}
