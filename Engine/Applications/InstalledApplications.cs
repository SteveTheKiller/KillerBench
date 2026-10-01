using System.Text.Json;

namespace KillerBench.Engine.Applications;

public sealed record InstalledApplication(string View, string Key, string Name, string? Version, string? Publisher,
    bool HasUninstallCommand = false, bool HasInstallLocation = false);
public enum InstalledApplicationScope { Machine, CurrentAccount }
public sealed record InstalledApplicationInventory(IReadOnlyList<InstalledApplication> Applications, bool Complete, ToolResult Evidence)
{
    public InstalledApplicationScope Scope { get; init; } = InstalledApplicationScope.Machine;
}

public static class InstalledApplications
{
    public static IReadOnlyList<WorkflowCheck> Describe(InstalledApplicationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var observed = Interpret(inventory.Evidence);
        if (!observed.Complete && observed.Applications.Count == 0) observed = observed with { Scope = inventory.Scope };
        var checks = new List<WorkflowCheck>
        {
            new("Application registrations", observed.Complete ? CheckState.Information : CheckState.Unknown,
                $"{observed.Applications.Count} named uninstall registrations; scope: {observed.Scope}. " +
                "CurrentAccount means the executing account. Other accounts and portable apps are excluded. Registrations can be stale; operation, ownership and removal eligibility are unverified. No installers executed.", Evidence: observed.Evidence)
        };
        checks.AddRange(observed.Applications.Select(app => new WorkflowCheck(app.Name, CheckState.Information,
            $"Identity: {(observed.Scope == InstalledApplicationScope.Machine ? "HKLM" : "HKCU")}/{app.View}/{app.Key}. Version: {app.Version ?? "not reported"}. Publisher: {app.Publisher ?? "not reported"}. Uninstall command registered: {(app.HasUninstallCommand ? "yes" : "no")}. Install location registered: {(app.HasInstallLocation ? "yes" : "no")}. Registration only; no application changes.")));
        return checks.AsReadOnly();
    }

    public static Task<InstalledApplicationInventory> ReadAsync(CancellationToken token = default) => ReadAsync(InstalledApplicationScope.Machine, token);

    public static async Task<InstalledApplicationInventory> ReadAsync(InstalledApplicationScope scope, CancellationToken token = default)
    {
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Installed application inventory requires Windows.");
        string script = "$ErrorActionPreference='Stop'; $items=[Collections.Generic.List[object]]::new(); $complete=$true; " +
            "foreach($view in @('Registry64','Registry32')) { $base=$null; $root=$null; try { " +
            "$base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::" + (scope == InstalledApplicationScope.Machine ? "LocalMachine" : "CurrentUser") + ",[Microsoft.Win32.RegistryView]::$view); " +
            "$root=$base.OpenSubKey('SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall'); " +
            "if($null -ne $root){$names=$root.GetSubKeyNames(); if($names.Count -gt 4096){$complete=$false}; " +
            "foreach($name in ($names | Sort-Object | Select-Object -First 4096)){ $key=$null; try { $key=$root.OpenSubKey($name); " +
            "if($null -eq $key){$complete=$false;continue}; $display=$key.GetValue('DisplayName'); " +
            "if($display -is [string] -and -not [string]::IsNullOrWhiteSpace($display)){ $uninstall=$key.GetValue('UninstallString'); $location=$key.GetValue('InstallLocation'); $items.Add([pscustomobject]@{view=$view;key=$name;name=$display;version=$key.GetValue('DisplayVersion');publisher=$key.GetValue('Publisher');hasUninstallCommand=($uninstall -is [string] -and -not [string]::IsNullOrWhiteSpace($uninstall));hasInstallLocation=($location -is [string] -and -not [string]::IsNullOrWhiteSpace($location))}) } " +
            "} catch{$complete=$false} finally{if($null -ne $key){$key.Dispose()}} } } " +
            "} catch{$complete=$false} finally{if($null -ne $root){$root.Dispose()};if($null -ne $base){$base.Dispose()}} }; " +
            "[pscustomobject]@{scope='" + scope + "';complete=$complete;applications=@($items.ToArray())}|ConvertTo-Json -Depth 3 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(evidence) with { Scope = scope };
    }

    public static InstalledApplicationInventory Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return new([], false, evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (!Unique(root)) return new([], false, evidence);
            var scope = InstalledApplicationScope.Machine;
            if (root.TryGetProperty("scope", out var scopeValue))
            {
                if (scopeValue.ValueKind != JsonValueKind.String || scopeValue.GetString() is not ("Machine" or "CurrentAccount")) return new([], false, evidence);
                scope = scopeValue.GetString() == "Machine" ? InstalledApplicationScope.Machine : InstalledApplicationScope.CurrentAccount;
            }
            bool complete = root.GetProperty("complete").GetBoolean();
            var rows = root.GetProperty("applications");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 8192) return new([], false, evidence);
            var applications = new List<InstalledApplication>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                try
                {
                    if (!Unique(row)) { complete = false; continue; }
                    string? view = row.GetProperty("view").GetString(), key = row.GetProperty("key").GetString(), name = row.GetProperty("name").GetString(),
                        version = row.GetProperty("version").GetString(), publisher = row.GetProperty("publisher").GetString();
                    bool hasUninstallCommand = row.GetProperty("hasUninstallCommand").GetBoolean(), hasInstallLocation = row.GetProperty("hasInstallLocation").GetBoolean();
                    if (view is not ("Registry32" or "Registry64") || !Valid(key) || key!.Contains('\\') || !Valid(name) ||
                        (version is not null && !Valid(version)) || (publisher is not null && !Valid(publisher)) || !seen.Add(view + "\\" + key))
                    { complete = false; continue; }
                    applications.Add(new(view, key, name!, version, publisher, hasUninstallCommand, hasInstallLocation));
                }
                catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException) { complete = false; }
            }
            return new(Array.AsReadOnly(applications.OrderBy(app => app.View, StringComparer.Ordinal).ThenBy(app => app.Key, StringComparer.Ordinal).ToArray()), complete, evidence) { Scope = scope };
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return new([], false, evidence); }
    }

    private static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 && !value.Any(char.IsControl);
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
}
