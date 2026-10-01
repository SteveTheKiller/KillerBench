using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record UpdateServiceRegistration(string Id, string Name, bool Managed, bool RegisteredWithAutomaticUpdates, bool OffersWindowsUpdates);

public static class UpdateServiceDiagnostics
{
    private const string WindowsUpdateId = "9482f4b4-e343-43b6-b170-9a65bc822c77";
    private const string MicrosoftUpdateId = "7971f918-a847-4430-9279-4a52d1efe18d";

    public static RepairStep Step() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $manager=New-Object -ComObject Microsoft.Update.ServiceManager; $services=@(); " +
        "for($i=0; $i -lt [Math]::Min(65,$manager.Services.Count); $i++){ $service=$manager.Services.Item($i); " +
        "$services+=[pscustomobject]@{id=$service.ServiceID;name=$service.Name;managed=[bool]$service.IsManaged;registered=[bool]$service.IsRegisteredWithAU;windows=[bool]$service.OffersWindowsUpdates} }; " +
        "[pscustomobject]@{total=$manager.Services.Count;services=$services} | ConvertTo-Json -Depth 4 -Compress"], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var result = await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown() => new("Windows Update services", CheckState.Unknown,
            "Update service registration evidence is unavailable, incomplete or unrecognized. No service was registered, removed or changed.", Evidence: result);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (!UniqueFields(root) || root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("total", out var totalValue) || !totalValue.TryGetInt32(out int total) || total is < 1 or > 64 ||
                !root.TryGetProperty("services", out var servicesValue) || servicesValue.ValueKind != JsonValueKind.Array || servicesValue.GetArrayLength() != total)
                return Unknown();
            var services = new List<UpdateServiceRegistration>(total);
            var identities = new HashSet<Guid>();
            foreach (var item in servicesValue.EnumerateArray())
            {
                if (!UniqueFields(item) || item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("id", out var idValue) || idValue.ValueKind != JsonValueKind.String ||
                    !Guid.TryParse(idValue.GetString(), out var id) || id == Guid.Empty || !identities.Add(id) ||
                    !item.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(nameValue.GetString()) || nameValue.GetString()!.Length > 256 || nameValue.GetString()!.Any(char.IsControl) ||
                    !item.TryGetProperty("managed", out var managed) || managed.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !item.TryGetProperty("registered", out var registered) || registered.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !item.TryGetProperty("windows", out var windows) || windows.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Unknown();
                services.Add(new(id.ToString("D"), nameValue.GetString()!, managed.GetBoolean(), registered.GetBoolean(), windows.GetBoolean()));
            }
            bool hasAutomaticUpdates = services.Any(service => service.RegisteredWithAutomaticUpdates);
            string windowsUpdate = services.Any(service => service.Id.Equals(WindowsUpdateId, StringComparison.OrdinalIgnoreCase)) ? "registered" : "not reported";
            string microsoftUpdate = services.Any(service => service.Id.Equals(MicrosoftUpdateId, StringComparison.OrdinalIgnoreCase)) ? "registered" : "not reported";
            string names = string.Join(", ", services.OrderBy(service => service.Name, StringComparer.Ordinal).Select(service =>
                $"{service.Name} [{service.Id}] (automatic updates: {service.RegisteredWithAutomaticUpdates}; managed: {service.Managed}; offers Windows updates: {service.OffersWindowsUpdates})"));
            return new("Windows Update services", hasAutomaticUpdates ? CheckState.Information : CheckState.Attention,
                $"Windows Update: {windowsUpdate}; Microsoft Update: {microsoftUpdate}. " +
                (hasAutomaticUpdates ? "At least one service is registered with Automatic Updates. " : "No reported service is registered with Automatic Updates. Review update source and policy ownership. ") +
                $"Reported services ({services.Count}): {names}. Registration does not establish source reachability, update approval or scan success. No service was registered, removed or changed.", Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException) { return Unknown(); }
    }

    public static WorkflowCheck AssessSource(UpdateSearchOptions options, ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Criteria();
        var inventory = Interpret(result);
        if (inventory.State == CheckState.Unknown)
            return new("Selected update source", CheckState.Unknown,
                "Update service registration could not be verified. The selected source was not searched.", Evidence: result);
        if (options.Source == UpdateSearchSource.Configured)
            return new("Selected update source", CheckState.Information,
                "Configured source selected. Windows Update Agent will retain the machine's current managed or Automatic Updates source selection.", Evidence: result);
        if (options.Source == UpdateSearchSource.WindowsUpdate)
            return new("Selected update source", CheckState.Information,
                "Windows Update selected explicitly. Registration, policy and Automatic Updates settings remain unchanged.", Evidence: result);
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            int matches = document.RootElement.GetProperty("services").EnumerateArray().Count(item =>
                Guid.TryParse(item.GetProperty("id").GetString(), out var id) &&
                id == Guid.Parse(UpdateSearchOptions.MicrosoftUpdateServiceId));
            return matches == 1
                ? new("Selected update source", CheckState.Information,
                    "Microsoft Update is registered and selected for this search. Registration and Automatic Updates settings remain unchanged.", Evidence: result)
                : new("Selected update source", CheckState.Attention,
                    "Microsoft Update is not registered on this PC. No search was started and no update service was registered or changed.", Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return new("Selected update source", CheckState.Unknown,
                "Microsoft Update registration could not be verified. The selected source was not searched.", Evidence: result);
        }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
}
