using System.Text.Json;

namespace KillerBench.Engine;

public sealed record FoxitProduct(string View, string Key, string Name, string? Version, string? Publisher);
public sealed record FoxitInventoryResult(IReadOnlyList<FoxitProduct> Products, bool Complete, ToolResult Evidence);

public static class FoxitInventory
{
    public static async Task<FoxitInventoryResult> ReadAsync(CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Foxit inventory requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $items=@(); $complete=$true; " +
            "foreach($view in @('Registry64','Registry32')) { $base=$null; $root=$null; try { " +
            "$base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::$view); " +
            "$root=$base.OpenSubKey('SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall'); " +
            "if($null -ne $root) { $names=$root.GetSubKeyNames(); if($names.Count -gt 4096){$complete=$false}; " +
            "foreach($name in ($names | Select-Object -First 4096)) { $key=$null; try { $key=$root.OpenSubKey($name); " +
            "if($null -eq $key){$complete=$false; continue}; $display=$key.GetValue('DisplayName'); " +
            "if($display -is [string] -and $display -like '*Foxit*') { $items += [pscustomobject]@{view=$view; key=$name; name=$display; version=$key.GetValue('DisplayVersion'); publisher=$key.GetValue('Publisher')} " +
            "} } catch {$complete=$false} finally {if($null -ne $key){$key.Dispose()}} } } " +
            "} catch {$complete=$false} finally {if($null -ne $root){$root.Dispose()}; if($null -ne $base){$base.Dispose()}} }; " +
            "[pscustomobject]@{products=@($items); complete=$complete} | ConvertTo-Json -Depth 3 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static FoxitInventoryResult Interpret(ToolResult result)
    {
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated) return new([], false, result);
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            if (!UniqueFields(json.RootElement)) return new([], false, result);
            var rows = json.RootElement.GetProperty("products");
            bool complete = json.RootElement.GetProperty("complete").GetBoolean();
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 1024) return new([], false, result);
            var products = new List<FoxitProduct>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                try
                {
                    if (!UniqueFields(row)) { complete = false; continue; }
                    string? view = row.GetProperty("view").GetString(), key = row.GetProperty("key").GetString(), name = row.GetProperty("name").GetString();
                    string? version = row.GetProperty("version").GetString(), publisher = row.GetProperty("publisher").GetString();
                    if (view is not ("Registry64" or "Registry32") || !Valid(key, 256) || key!.Contains('\\') ||
                        !Valid(name, 512) || !name!.Contains("Foxit", StringComparison.OrdinalIgnoreCase) ||
                        (version is not null && !Valid(version, 256)) || (publisher is not null && !Valid(publisher, 512)) ||
                        !seen.Add(view + "\\" + key)) { complete = false; continue; }
                    products.Add(new(view, key, name, version, publisher));
                }
                catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException) { complete = false; }
            }
            return new(products.AsReadOnly(), complete, result);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException) { return new([], false, result); }
    }

    private static bool Valid(string? text, int max) => !string.IsNullOrWhiteSpace(text) && text.Length <= max && !text.Any(char.IsControl);
    private static bool UniqueFields(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
}
