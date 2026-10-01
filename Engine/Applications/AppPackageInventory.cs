using System.Text.Json;

namespace KillerBench.Engine.Applications;

public enum AppPackageScope { CurrentAccount, AllUsers }
public sealed record AppPackage(string Name, string FullName, string FamilyName, bool Framework, bool NonRemovable)
{
    public override string ToString() => FullName;
    public IReadOnlyList<string>? Dependencies { get; init; }
    public string? Status { get; init; }
    public bool DevelopmentMode { get; init; }
}
public sealed record AppPackageInventoryResult(AppPackageScope Scope, IReadOnlyList<AppPackage> Packages, bool Complete, ToolResult Evidence);

public static class AppPackageInventory
{
    public static async Task<AppPackageInventoryResult> ReadAsync(AppPackageScope scope = AppPackageScope.CurrentAccount, CancellationToken token = default)
    {
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("App package inventory requires Windows.");
        string script = "$ErrorActionPreference='Stop'; $items=@(Get-AppxPackage " +
            (scope == AppPackageScope.AllUsers ? "-AllUsers " : "") +
            "-PackageTypeFilter Main,Framework,Resource,Bundle | Select-Object -First 4097); " +
            "$rows=@($items | Select-Object -First 4096 | ForEach-Object { [pscustomobject]@{" +
            "name=$_.Name;fullName=$_.PackageFullName;familyName=$_.PackageFamilyName;framework=$_.IsFramework;nonRemovable=$_.NonRemovable;developmentMode=$_.IsDevelopmentMode;" +
            "status=if($null -eq $_.PSObject.Properties['Status']){$null}else{[string]$_.Status};" +
            "dependencies=if($null -eq $_.PSObject.Properties['Dependencies']){$null}else{,@($_.Dependencies | Select-Object -First 257 | ForEach-Object {$_.PackageFullName})}} }); " +
            "[pscustomobject]@{complete=($items.Count -le 4096);packages=$rows} | ConvertTo-Json -Depth 4 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(scope, result);
    }

    public static AppPackageInventoryResult Interpret(AppPackageScope scope, ToolResult evidence)
    {
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return new(scope, [], false, evidence);
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            if (!Unique(json.RootElement)) return new(scope, [], false, evidence);
            bool complete = json.RootElement.GetProperty("complete").GetBoolean();
            var rows = json.RootElement.GetProperty("packages");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 4096) return new(scope, [], false, evidence);
            var packages = new List<AppPackage>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                try
                {
                    if (!Unique(row)) { complete = false; continue; }
                    string? full = row.GetProperty("fullName").GetString();
                    if (!Valid(full)) { complete = false; continue; }
                    if (!seen.Add(full!)) { ambiguous.Add(full!); complete = false; continue; }
                    string? name = row.GetProperty("name").GetString(), family = row.GetProperty("familyName").GetString();
                    bool framework = row.GetProperty("framework").GetBoolean(), nonRemovable = row.GetProperty("nonRemovable").GetBoolean();
                    bool developmentMode = row.GetProperty("developmentMode").GetBoolean();
                    if (!Valid(name) || !Valid(family)) { complete = false; continue; }
                    IReadOnlyList<string>? dependencies = null;
                    if (row.TryGetProperty("dependencies", out var dependencyRows) && dependencyRows.ValueKind == JsonValueKind.Array && dependencyRows.GetArrayLength() <= 256)
                    {
                        var values = dependencyRows.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : null).ToArray();
                        if (values.All(Valid) && values.Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Length &&
                            !values.Contains(full, StringComparer.OrdinalIgnoreCase))
                            dependencies = Array.AsReadOnly(values.Select(value => value!).Order(StringComparer.Ordinal).ToArray());
                    }
                    if (dependencies is null) complete = false;
                    string? status = row.TryGetProperty("status", out var statusValue) && statusValue.ValueKind == JsonValueKind.String
                        ? statusValue.GetString() : null;
                    packages.Add(new(name!, full!, family!, framework, nonRemovable)
                    {
                        Dependencies = dependencies,
                        Status = Valid(status) ? status : null,
                        DevelopmentMode = developmentMode
                    });
                }
                catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException) { complete = false; }
            }
            return new(scope, [.. packages.Where(package => !ambiguous.Contains(package.FullName))
                .OrderBy(package => package.FullName, StringComparer.Ordinal)], complete, evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return new(scope, [], false, evidence); }
    }

    private static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 && value == value.Trim() && !value.Any(char.IsControl);
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
}
