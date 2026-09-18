using System.Text.Json;

namespace KillerBench.Engine;

public static class ServicingPackageDiagnostics
{
    private const int MaximumPackages = 4096;
    private static readonly HashSet<string> States = new(StringComparer.Ordinal)
    {
        "NotPresent", "UninstallPending", "Staged", "Resolved", "Removed", "Installed", "InstallPending", "Superseded", "PartiallyInstalled"
    };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($package in @(Get-WindowsPackage -Online -ErrorAction Stop|Sort-Object PackageName)){if($rows.Count-ge 4097){break};" +
            "$rows.Add([pscustomobject]@{identity=[string]$package.PackageName;state=[string]$package.PackageState;releaseType=[string]$package.ReleaseType})};" +
            "[pscustomobject]@{complete=($rows.Count-le 4096);packages=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromMinutes(2), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "Servicing packages";
        WorkflowCheck Unknown(string detail = "Windows servicing package state is unavailable or incomplete. No package or servicing setting changed.") =>
            new(title, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var completeValue) ||
                completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("packages", out var packagesValue) || packagesValue.ValueKind != JsonValueKind.Array) return Unknown();
            if (!completeValue.GetBoolean() || packagesValue.GetArrayLength() > MaximumPackages)
                return Unknown("The servicing package inventory exceeded its evidence bound. Coverage is incomplete and no change was selected.");
            var packages = new List<(string Identity, string State, string ReleaseType)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packagesValue.EnumerateArray())
            {
                if (package.ValueKind != JsonValueKind.Object ||
                    !package.TryGetProperty("identity", out var identityValue) || identityValue.ValueKind != JsonValueKind.String ||
                    !package.TryGetProperty("state", out var stateValue) || stateValue.ValueKind != JsonValueKind.String ||
                    !package.TryGetProperty("releaseType", out var releaseValue) || releaseValue.ValueKind != JsonValueKind.String) return Unknown();
                string identity = identityValue.GetString()!, state = stateValue.GetString()!, releaseType = releaseValue.GetString()!;
                if (!DriverReference.ValidText(identity, 512) || identity != identity.Trim() || !States.Contains(state) ||
                    !DriverReference.ValidText(releaseType, 128) || releaseType != releaseType.Trim() || !seen.Add(identity)) return Unknown();
                packages.Add((identity, state, releaseType));
            }
            var pending = packages.Where(package => package.State is "InstallPending" or "UninstallPending" or "PartiallyInstalled").ToArray();
            string counts = string.Join(", ", packages.GroupBy(package => package.State, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => group.Key + ": " + group.Count()));
            string detail = $"Reviewed {packages.Count} servicing package{(packages.Count == 1 ? "" : "s")}. " + (counts.Length == 0 ? "No packages were returned." : counts + ".");
            if (pending.Length > 0) detail += "\nPending or partial packages: " + string.Join("; ", pending.Take(20).Select(package => package.Identity + " (" + package.State + ")")) + (pending.Length > 20 ? "; additional packages remain in evidence." : ".");
            detail += "\nPackage state does not establish component-store health or authorize installation, removal, cleanup or rollback. No package, source, policy or servicing setting changed.";
            return new(title, pending.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }
}
