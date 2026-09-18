using System.Text.Json;

namespace KillerBench.Engine;

public sealed record AppLockerCollectionStatus(string Collection, string EnforcementMode, int RuleCount);

public static class AppLockerPolicyDiagnostics
{
    private static readonly string[] CollectionNames = ["Executable", "Windows Installer", "Script", "DLL", "Packaged app"];
    private static readonly string[] CollectionTypes = ["Exe", "Msi", "Script", "Dll", "Appx"];

    public static RepairStep QueryStep()
    {
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "[xml]$policy=Get-AppLockerPolicy -Effective -Xml -ErrorAction Stop; $rows=@(); " +
            "foreach($type in @('Exe','Msi','Script','Dll','Appx')){ " +
            "$matches=@($policy.AppLockerPolicy.RuleCollection | Where-Object { [string]$_.Type -eq $type }); " +
            "if($matches.Count -gt 1){ throw ('Duplicate AppLocker collection: '+$type) }; " +
            "if($matches.Count -eq 0){ $rows += [pscustomobject]@{collection=$type;mode='NotConfigured';rules=0} } else { " +
            "$node=$matches[0]; $count=@($node.ChildNodes | Where-Object { $_.NodeType -eq 'Element' }).Count; " +
            "$rows += [pscustomobject]@{collection=$type;mode=[string]$node.EnforcementMode;rules=[int]$count} } }; " +
            "[pscustomobject]@{complete=$true;collections=$rows}|ConvertTo-Json -Depth 3 -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ToolResult evidence = await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        WorkflowCheck[] unknown = CollectionNames.Select(name => new WorkflowCheck("AppLocker " + name,
            CheckState.Unknown, "Effective AppLocker policy is unavailable or incomplete. Rule identities, paths, publishers, hashes, users and SIDs were not retained. No application-control policy or rule was changed.", Evidence: evidence)).ToArray();
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return unknown;
        try
        {
            using JsonDocument document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            JsonElement root = document.RootElement;
            if (!Unique(root, "complete", "collections") || Boolean(root, "complete") != true ||
                !root.TryGetProperty("collections", out JsonElement rows) || rows.ValueKind != JsonValueKind.Array ||
                rows.GetArrayLength() != CollectionTypes.Length) return unknown;

            var parsed = new Dictionary<string, AppLockerCollectionStatus>(StringComparer.Ordinal);
            foreach (JsonElement row in rows.EnumerateArray())
            {
                if (!Unique(row, "collection", "mode", "rules") || Text(row, "collection", 8) is not string collection ||
                    !CollectionTypes.Contains(collection, StringComparer.Ordinal) || Text(row, "mode", 32) is not string mode ||
                    mode is not ("Enabled" or "AuditOnly" or "NotConfigured") || Integer(row, "rules", 0, 1_000_000) is not int rules ||
                    !parsed.TryAdd(collection, new(collection, mode, rules))) return unknown;
            }
            if (parsed.Count != CollectionTypes.Length) return unknown;

            return Array.AsReadOnly(CollectionTypes.Select((type, index) => Describe(CollectionNames[index], parsed[type], evidence)).ToArray());
        }
        catch (JsonException) { return unknown; }
    }

    private static WorkflowCheck Describe(string name, AppLockerCollectionStatus status, ToolResult evidence)
    {
        string detail = status.EnforcementMode switch
        {
            "Enabled" => $"Effective AppLocker enforcement is enabled with {status.RuleCount} aggregate rules.",
            "AuditOnly" => $"Effective AppLocker policy is audit-only with {status.RuleCount} aggregate rules. Review whether enforcement is intended.",
            _ => "No effective AppLocker collection is configured. AppLocker may be optional for this Windows edition and security baseline."
        };
        return new("AppLocker " + name, CheckState.Information,
            detail + " Rule identities, paths, publishers, hashes, users and SIDs were not retained. No application-control policy or rule was changed.", Evidence: evidence);
    }

    private static bool Unique(JsonElement value, params string[] names) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
            .SequenceEqual(names.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal);
    private static bool? Boolean(JsonElement root, string name) => root.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
    private static int? Integer(JsonElement root, string name, int minimum, int maximum) => root.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && number >= minimum && number <= maximum ? number : null;
    private static string? Text(JsonElement root, string name, int maximum) => root.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind == JsonValueKind.String && value.GetString() is string text && text.Length is > 0 && text.Length <= maximum &&
        text == text.Trim() && !text.Any(char.IsControl) ? text : null;
}
