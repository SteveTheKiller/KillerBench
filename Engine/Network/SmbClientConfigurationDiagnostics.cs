using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class SmbClientConfigurationDiagnostics
{
    private const string Scope = " This is client configuration evidence only. Effective server negotiation, policy ownership, name resolution, firewall access, authentication and file transfer are not tested. No SMB setting is changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("SMB client configuration collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$c=Get-SmbClientConfiguration -ErrorAction Stop; [pscustomobject]@{" +
            "EnableSecuritySignature=[bool]$c.EnableSecuritySignature;RequireSecuritySignature=[bool]$c.RequireSecuritySignature;" +
            "EnableInsecureGuestLogons=[bool]$c.EnableInsecureGuestLogons;EnableMultiChannel=[bool]$c.EnableMultiChannel;" +
            "EnableLargeMtu=[bool]$c.EnableLargeMtu;ConnectionCountPerRssNetworkInterface=[int]$c.ConnectionCountPerRssNetworkInterface;" +
            "MaximumConnectionCountPerServer=[int]$c.MaximumConnectionCountPerServer} | ConvertTo-Json -Compress";
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
            return Unknown(new(-1, "", error.Message, false), "Current SMB client configuration collection was unavailable.");
        }
    }

    public static WorkflowCheck Interpret(ToolResult? result)
    {
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return Unknown(result, "Current SMB client configuration collection failed or was incomplete.");
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 2 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 7 ||
                !Boolean(root, "EnableSecuritySignature", out bool signingEnabled) || !Boolean(root, "RequireSecuritySignature", out bool signingRequired) ||
                !Boolean(root, "EnableInsecureGuestLogons", out bool insecureGuest) || !Boolean(root, "EnableMultiChannel", out bool multiChannel) ||
                !Boolean(root, "EnableLargeMtu", out bool largeMtu) || !Positive(root, "ConnectionCountPerRssNetworkInterface", out int rssConnections) ||
                !Positive(root, "MaximumConnectionCountPerServer", out int maximumConnections))
                return Unknown(result, "Current SMB client configuration data was malformed.");
            string headline = insecureGuest
                ? "Insecure guest logons are enabled. Review the required share and managed policy before allowing unauthenticated access."
                : "Insecure guest logons are disabled.";
            string detail = headline +
                $" Signing enabled: {Yes(signingEnabled)}; signing required: {Yes(signingRequired)}; multichannel: {Yes(multiChannel)}; large MTU: {Yes(largeMtu)}; RSS connections per interface: {rssConnections}; maximum connections per server: {maximumConnections}." + Scope;
            return new("SMB client configuration", insecureGuest ? CheckState.Attention : CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Unknown(result, "Current SMB client configuration data could not be interpreted.");
        }
    }

    private static bool Boolean(JsonElement root, string name, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(name, out var field) || field.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = field.GetBoolean();
        return true;
    }

    private static bool Positive(JsonElement root, string name, out int value)
    {
        value = 0;
        return root.TryGetProperty(name, out var field) && field.TryGetInt32(out value) && value is > 0 and <= 65_535;
    }

    private static string Yes(bool value) => value ? "yes" : "no";
    private static WorkflowCheck Unknown(ToolResult? evidence, string detail) =>
        new("SMB client configuration", CheckState.Unknown, detail + Scope, Evidence: evidence);
}
