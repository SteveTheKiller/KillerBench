using System.Text.Json;

namespace KillerBench.Engine;

public static class FontRegistrationDiagnostics
{
    private const int MaximumRegistrations = 1024;

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$rows=[System.Collections.Generic.List[object]]::new(); " +
            "$sources=@(@{scope='Machine';path='Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts'},@{scope='User';path='Registry::HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts'}); " +
            "foreach($source in $sources){$item=Get-ItemProperty -LiteralPath $source.path -ErrorAction SilentlyContinue; if($null -eq $item){continue}; " +
            "$properties=@($item.PSObject.Properties|Where-Object{$_.Name -notmatch '^PS'}|Sort-Object Name); foreach($property in $properties){" +
            "if($rows.Count -ge 1025){break}; $valid=$property.Value -is [string] -and -not [string]::IsNullOrWhiteSpace([string]$property.Value); $exists=$false; " +
            "if($valid){$value=[Environment]::ExpandEnvironmentVariables([string]$property.Value); $path=if([IO.Path]::IsPathRooted($value)){$value}else{Join-Path $env:WINDIR ('Fonts\\'+$value)}; $exists=[IO.File]::Exists($path)}; " +
            "$rows.Add([pscustomobject]@{scope=$source.scope;registration=$property.Name;valid=$valid;exists=$exists})}}; " +
            "[pscustomobject]@{registrations=$rows}|ConvertTo-Json -Depth 3 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        WorkflowCheck Unknown(string detail = "Installed font registration data is unavailable or incomplete. No fonts or cache files changed.") =>
            new("Font registrations", CheckState.Unknown, detail, Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return [Unknown()];
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var registrations = document.RootElement.GetProperty("registrations");
            if (registrations.ValueKind != JsonValueKind.Array) return [Unknown()];
            if (registrations.GetArrayLength() > MaximumRegistrations)
                return [Unknown("More than 1,024 installed font registrations were returned. The bounded inventory is incomplete, so missing files and overall registration state remain unknown. No fonts or cache files changed.")];
            if (registrations.GetArrayLength() == 0)
                return [new("Font registrations", CheckState.Information,
                    "No machine or current-user font registrations were returned. This does not establish whether fonts are available through other mechanisms. No fonts or cache files changed.", Evidence: result)];

            int machine = 0, user = 0;
            var missing = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var registration in registrations.EnumerateArray())
            {
                if (registration.ValueKind != JsonValueKind.Object ||
                    !registration.TryGetProperty("scope", out var scopeValue) || scopeValue.ValueKind != JsonValueKind.String ||
                    !registration.TryGetProperty("registration", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !registration.TryGetProperty("valid", out var validValue) || validValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !registration.TryGetProperty("exists", out var existsValue) || existsValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return [Unknown()];
                string scope = scopeValue.GetString()!;
                string name = nameValue.GetString()!;
                if ((scope != "Machine" && scope != "User") || !DriverReference.ValidText(name, 512) ||
                    name != name.Trim() || !seen.Add(scope + "\0" + name)) return [Unknown()];
                if (scope == "Machine") machine++; else user++;
                if (!validValue.GetBoolean() || !existsValue.GetBoolean()) missing.Add(scope + ": " + name);
            }

            string counts = $"Reviewed {machine} machine and {user} current-user font registrations.";
            if (missing.Count == 0)
                return [new("Font registrations", CheckState.Information,
                    counts + " Every registered file resolved to an existing file. This does not test font parsing, cache contents, text rendering or application behavior. No fonts or cache files changed.", Evidence: result)];
            string examples = string.Join("; ", missing.Take(12));
            return [new("Font registrations", CheckState.Attention,
                counts + $" {missing.Count} registration(s) had an invalid value or did not resolve to an existing file. Review: {examples}" +
                (missing.Count > 12 ? "; additional registrations omitted from this summary." : ".") +
                " This evidence does not authorize deleting registrations, installing fonts or rebuilding the font cache.", Evidence: result)];
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return [Unknown()];
        }
    }
}
