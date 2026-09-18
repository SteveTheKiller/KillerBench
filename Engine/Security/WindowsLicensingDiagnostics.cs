using System.Text.Json;

namespace KillerBench.Engine;

public static class WindowsLicensingDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$rows=@(Get-CimInstance -ClassName SoftwareLicensingProduct -Filter \"ApplicationID='55c92734-d682-4d71-983e-d6ec3f16059f' AND PartialProductKey IS NOT NULL\" -OperationTimeoutSec 15 | Select-Object -First 65); " +
        "$items=@($rows | ForEach-Object {[pscustomobject]@{id=[string]$_.ID;name=[string]$_.Name;description=[string]$_.Description;status=[int]$_.LicenseStatus;grace=[uint32]$_.GracePeriodRemaining}}); " +
        "[pscustomobject]@{complete=($rows.Count -le 64);products=$items} | ConvertTo-Json -Depth 3 -Compress";

    public static RepairStep Step() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows licensing collection requires Windows.");
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(25), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown() => new("Windows activation", CheckState.Unknown,
            "Windows licensing evidence was unavailable, incomplete or malformed. Activation state was not inferred and no activation was attempted.", Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !Unique(root, "complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !complete.GetBoolean() ||
                !Unique(root, "products", out var products) || products.ValueKind != JsonValueKind.Array || products.GetArrayLength() is < 1 or > 64)
                return Unknown();
            int licensed = 0, review = 0;
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var details = new List<string>();
            foreach (var product in products.EnumerateArray())
            {
                if (product.ValueKind != JsonValueKind.Object || product.EnumerateObject().Count() != 5 ||
                    !Unique(product, "id", out var id) || id.ValueKind != JsonValueKind.String || !Guid.TryParse(id.GetString(), out var parsedId) || !identities.Add(parsedId.ToString("D")) ||
                    !Unique(product, "name", out var name) || name.ValueKind != JsonValueKind.String || !DriverReference.ValidText(name.GetString(), 256) ||
                    !Unique(product, "description", out var description) || description.ValueKind != JsonValueKind.String || !DriverReference.ValidText(description.GetString(), 512) ||
                    !Unique(product, "status", out var statusValue) || !statusValue.TryGetInt32(out int status) || status is < 0 or > 6 ||
                    !Unique(product, "grace", out var graceValue) || !graceValue.TryGetUInt32(out uint grace) || grace > 5256000)
                    return Unknown();
                if (status == 1) licensed++; else review++;
                details.Add(name.GetString()!.Trim() + ": " + Status(status) + (status is 2 or 3 or 4 or 6 ? $", {grace} grace minutes reported" : ""));
            }
            CheckState state = review == 0 ? CheckState.Clear : CheckState.Attention;
            string summary = review == 0
                ? $"{licensed} installed Windows license entr{(licensed == 1 ? "y reports" : "ies report")} Licensed."
                : $"{products.GetArrayLength()} installed Windows license entries evaluated: {licensed} Licensed, {review} require review.";
            return new("Windows activation", state, summary + " " + string.Join("; ", details) +
                ". Provider status does not prove subscription entitlement, activation-server reachability or future validity. No product key was collected and no activation was attempted.", Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return Unknown(); }
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }

    private static string Status(int status) => status switch
    {
        0 => "Unlicensed",
        1 => "Licensed",
        2 => "Initial grace period",
        3 => "Additional grace period",
        4 => "Non-genuine grace period",
        5 => "Notification",
        6 => "Extended grace period",
        _ => "Unknown"
    };
}
