using System.Text.Json;

namespace KillerBench.Engine.Security;

public sealed record RegisteredSecurityProduct(string Name, Guid InstanceId, uint ProductState, string? Timestamp);
public sealed record SecurityProductInventory(IReadOnlyList<RegisteredSecurityProduct> Products, bool Complete, ToolResult Evidence);

public static class SecurityProductDiagnostics
{
    public static async Task<SecurityProductInventory> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Security Center inventory requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $items=@(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct -OperationTimeoutSec 15 | Select-Object -First 65); " +
            "$rows=@($items | Select-Object -First 64 | ForEach-Object {[pscustomobject]@{name=$_.displayName;id=[string]$_.instanceGuid;state=[uint32]$_.productState;timestamp=[string]$_.timestamp}}); " +
            "[pscustomobject]@{complete=($items.Count -le 64);products=$rows}|ConvertTo-Json -Depth 3 -Compress";
        ToolResult evidence;
        try
        {
            evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            evidence = new(-1, "", error.Message, false);
        }
        return Interpret(evidence, token);
    }

    public static SecurityProductInventory Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return new([], false, evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (!Unique(root) || root.GetProperty("complete").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return new([], false, evidence);
            bool complete = root.GetProperty("complete").GetBoolean();
            var rows = root.GetProperty("products");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 64) return new([], false, evidence);
            var products = new List<RegisteredSecurityProduct>();
            var seen = new HashSet<Guid>();
            foreach (var row in rows.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (!Unique(row)) { complete = false; continue; }
                    string? name = row.GetProperty("name").GetString(), identity = row.GetProperty("id").GetString();
                    string? timestamp = row.GetProperty("timestamp").GetString();
                    if (!Valid(name) || !Guid.TryParse(identity, out Guid id) || id == Guid.Empty || !seen.Add(id) ||
                        !row.GetProperty("state").TryGetUInt32(out uint state) || state > 0xFFFFFF ||
                        (timestamp is not null && timestamp.Length > 128) || (timestamp?.Any(char.IsControl) ?? false))
                    { complete = false; continue; }
                    products.Add(new(name!, id, state, string.IsNullOrWhiteSpace(timestamp) ? null : timestamp));
                }
                catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException) { complete = false; }
            }
            return new(Array.AsReadOnly(products.OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase).ThenBy(product => product.InstanceId).ToArray()), complete, evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return new([], false, evidence); }
    }

    public static IReadOnlyList<WorkflowCheck> Describe(SecurityProductInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var observed = Interpret(inventory.Evidence);
        var checks = new List<WorkflowCheck>
        {
            new("Registered antivirus products", observed.Complete ? CheckState.Information : CheckState.Unknown,
                observed.Complete
                    ? observed.Products.Count + " antivirus product registrations reported by Windows Security Center. Registration and numeric provider state are retained without interpreting protection health. No security product, policy or setting changed."
                    : "Windows Security Center product inventory is unavailable or incomplete. Product absence and protection health cannot be inferred. No security product, policy or setting changed.", Evidence: observed.Evidence)
        };
        checks.AddRange(observed.Products.Select(product => new WorkflowCheck("Security product: " + product.Name, CheckState.Information,
            $"Instance: {product.InstanceId:B}. Provider state: 0x{product.ProductState:X6}. Timestamp: {product.Timestamp ?? "not reported"}. Registration evidence only; protection, signature freshness and operation remain unverified.")));
        return checks.AsReadOnly();
    }

    private static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && value == value.Trim() && !value.Any(char.IsControl);
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
}
