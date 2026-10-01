using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class SmbServerConfigurationDiagnostics
{
    private const string Scope = " This is server configuration evidence only. Effective client negotiation, policy ownership, firewall access, authentication and data transfer are not tested. No SMB setting is changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("SMB server configuration collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$c=Get-SmbServerConfiguration -ErrorAction Stop; [pscustomobject]@{" +
            "EnableSMB1Protocol=[bool]$c.EnableSMB1Protocol;EnableSMB2Protocol=[bool]$c.EnableSMB2Protocol;" +
            "EnableSecuritySignature=[bool]$c.EnableSecuritySignature;RequireSecuritySignature=[bool]$c.RequireSecuritySignature;" +
            "EncryptData=[bool]$c.EncryptData;RejectUnencryptedAccess=[bool]$c.RejectUnencryptedAccess;" +
            "EnableMultiChannel=[bool]$c.EnableMultiChannel;EnableLeasing=[bool]$c.EnableLeasing} | ConvertTo-Json -Compress";
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
            return Unknown(new(-1, "", error.Message, false), "Current SMB server configuration collection was unavailable.");
        }
    }

    public static WorkflowCheck Interpret(ToolResult? result)
    {
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return Unknown(result, "Current SMB server configuration collection failed or was incomplete.");
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 2 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 8 ||
                !Boolean(root, "EnableSMB1Protocol", out bool smb1) || !Boolean(root, "EnableSMB2Protocol", out bool smb2) ||
                !Boolean(root, "EnableSecuritySignature", out bool signingEnabled) || !Boolean(root, "RequireSecuritySignature", out bool signingRequired) ||
                !Boolean(root, "EncryptData", out bool encrypted) || !Boolean(root, "RejectUnencryptedAccess", out bool rejectPlain) ||
                !Boolean(root, "EnableMultiChannel", out bool multiChannel) || !Boolean(root, "EnableLeasing", out bool leasing))
                return Unknown(result, "Current SMB server configuration data was malformed.");
            var concerns = new List<string>();
            if (smb1) concerns.Add("SMB 1 is enabled");
            if (!smb2) concerns.Add("SMB 2 and newer protocols are disabled");
            string headline = concerns.Count == 0
                ? "SMB 1 is disabled and SMB 2 or newer is enabled."
                : string.Join("; ", concerns) + ". Review compatibility and security policy before changing protocol support.";
            string detail = headline +
                $" Signing enabled: {Yes(signingEnabled)}; signing required: {Yes(signingRequired)}; default share encryption: {Yes(encrypted)}; reject unencrypted access: {Yes(rejectPlain)}; multichannel: {Yes(multiChannel)}; leasing: {Yes(leasing)}." + Scope;
            return new("SMB server configuration", concerns.Count == 0 ? CheckState.Information : CheckState.Attention, detail, Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Unknown(result, "Current SMB server configuration data could not be interpreted.");
        }
    }

    private static bool Boolean(JsonElement root, string name, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(name, out var field) || field.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = field.GetBoolean();
        return true;
    }

    private static string Yes(bool value) => value ? "yes" : "no";
    private static WorkflowCheck Unknown(ToolResult? evidence, string detail) =>
        new("SMB server configuration", CheckState.Unknown, detail + Scope, Evidence: evidence);
}
