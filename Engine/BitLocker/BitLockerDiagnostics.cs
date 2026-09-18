using System.Text.Json;

namespace KillerBench.Engine;

public static class BitLockerDiagnostics
{
    public static RepairStep QueryStep(string? volume = null)
    {
        if (volume is not null) new BitLockerOptions(volume).Validate();
        return new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$items=@(Get-BitLockerVolume " + (volume is null ? "" : "-MountPoint '" + volume.ToUpperInvariant() + "' ") + "-ErrorAction Stop | ForEach-Object { " +
        "[pscustomobject]@{mount=$_.MountPoint; status=[string]$_.VolumeStatus; protection=[string]$_.ProtectionStatus; percent=$_.EncryptionPercentage; locked=[string]$_.LockStatus} }); " +
        "[pscustomobject]@{volumes=$items} | ConvertTo-Json -Depth 3 -Compress"], true);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckVolumeAsync(string volume, CancellationToken token = default)
    {
        var result = await BoundedTool.RunAsync(QueryStep(volume), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return InterpretVolume(result, volume);
    }

    public static IReadOnlyList<WorkflowCheck> InterpretVolume(ToolResult result, string volume)
    {
        new BitLockerOptions(volume).Validate();
        var checks = Interpret(result);
        if (checks.All(c => c.State == CheckState.Unknown)) return checks;
        if (checks.Count == 1 && checks[0].Name.Equals("BitLocker " + volume, StringComparison.OrdinalIgnoreCase)) return checks;
        return [new("BitLocker " + volume, CheckState.Unknown, "The selected volume was not uniquely identified. No protection settings changed.")];
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false));

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        WorkflowCheck Unknown() => new("BitLocker", CheckState.Unknown,
            "BitLocker status is unavailable or incomplete. Check elevation and BitLocker support. No protection settings changed.");
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut) return [Unknown()];
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var volumes = json.RootElement.GetProperty("volumes");
            if (volumes.ValueKind != JsonValueKind.Array || volumes.GetArrayLength() is 0 or > 256) return [Unknown()];
            var checks = new List<WorkflowCheck>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var volume in volumes.EnumerateArray())
            {
                string? mount = volume.GetProperty("mount").GetString();
                string? status = volume.GetProperty("status").GetString();
                string? protection = volume.GetProperty("protection").GetString();
                string? locked = volume.GetProperty("locked").GetString();
                int percent = volume.GetProperty("percent").GetInt32();
                if (string.IsNullOrWhiteSpace(mount) || mount.Length > 260 || !seen.Add(mount) || percent is < 0 or > 100 ||
                    locked is not ("Locked" or "Unlocked") || protection is not ("On" or "Off") ||
                    status is not ("FullyDecrypted" or "FullyEncrypted" or "EncryptionInProgress" or "DecryptionInProgress" or "EncryptionPaused" or "DecryptionPaused") ||
                    (status == "FullyEncrypted" && percent != 100) || (status == "FullyDecrypted" && (percent != 0 || protection != "Off")))
                { checks.Add(Unknown()); continue; }
                var state = CheckState.Information;
                string detail = $"{status}, {percent}% encrypted, protection {protection}, {locked}. ";
                if (status == "FullyEncrypted" && protection == "On")
                { state = CheckState.Clear; detail += "Protection is reported on. Recovery-key escrow and boot recovery are not verified."; }
                else if (status == "FullyEncrypted" && protection == "Off")
                { state = CheckState.Attention; detail += "Encrypted volume protection is off, possibly suspended for maintenance. Review the suspension reason and recovery readiness before resuming."; }
                else if (status is "EncryptionPaused" or "DecryptionPaused")
                { state = CheckState.Attention; detail += "Conversion is paused. Review maintenance and organizational policy before continuing."; }
                else detail += "Check the intended encryption policy. No encryption change is selected automatically.";
                checks.Add(new("BitLocker " + mount, state, detail));
            }
            return checks;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return [Unknown()]; }
    }
}
