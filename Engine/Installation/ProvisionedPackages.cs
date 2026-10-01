using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record ProvisionedPackage(string Name, string PackageName, string Version, string Architecture);
public sealed record ProvisionedPackageInventory(IReadOnlyList<ProvisionedPackage> Packages, bool Complete, ToolResult Evidence);

public static class ProvisionedPackages
{
    public static IReadOnlyList<WorkflowCheck> Describe(ProvisionedPackageInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var observed = Interpret(inventory.Evidence);
        var checks = new List<WorkflowCheck>
        {
            new("Provisioned app packages", observed.Complete ? CheckState.Information : CheckState.Unknown,
                $"{observed.Packages.Count} packages staged in the running Windows image for new users. " +
                "Existing user registrations and removal eligibility are separate. No packages changed.", Evidence: observed.Evidence)
        };
        checks.AddRange(observed.Packages.Select(package => new WorkflowCheck("Provisioned: " + package.PackageName,
            CheckState.Information, $"Name: {package.Name}. Version: {package.Version}. Architecture: {package.Architecture}. Intended for new users; installation success and managed ownership are unverified.")));
        return checks.AsReadOnly();
    }

    public static async Task<ProvisionedPackageInventory> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Provisioned package inventory requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $items=@(Get-AppxProvisionedPackage -Online | Select-Object -First 4097); " +
            "$rows=@($items | Select-Object -First 4096 | ForEach-Object { [pscustomobject]@{name=$_.DisplayName;packageName=$_.PackageName;version=[string]$_.Version;architecture=[string]$_.Architecture} }); " +
            "[pscustomobject]@{complete=($items.Count -le 4096);packages=$rows}|ConvertTo-Json -Depth 3 -Compress";
        return Interpret(await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(60), token: token).ConfigureAwait(false));
    }

    public static ProvisionedPackageInventory Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return new([], false, evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (!Unique(root)) return new([], false, evidence);
            bool complete = root.GetProperty("complete").GetBoolean();
            var rows = root.GetProperty("packages");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 4096) return new([], false, evidence);
            var packages = new List<ProvisionedPackage>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                try
                {
                    if (!Unique(row)) { complete = false; continue; }
                    string? name = row.GetProperty("name").GetString(), identity = row.GetProperty("packageName").GetString(),
                        version = row.GetProperty("version").GetString(), architecture = row.GetProperty("architecture").GetString();
                    if (!Valid(name) || !Valid(identity) || !Valid(version) || !Valid(architecture) || !seen.Add(identity!))
                    { complete = false; continue; }
                    packages.Add(new(name!, identity!, version!, architecture!));
                }
                catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException) { complete = false; }
            }
            return new(Array.AsReadOnly(packages.OrderBy(package => package.PackageName, StringComparer.Ordinal).ToArray()), complete, evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return new([], false, evidence); }
    }

    private static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 && value == value.Trim() && !value.Any(char.IsControl);
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
}
