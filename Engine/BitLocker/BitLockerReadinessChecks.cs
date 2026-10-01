using System.Text.Json;

namespace KillerBench.Engine.BitLocker;

public static class BitLockerReadinessChecks
{
    public static RepairStep QueryStep(string volume)
    {
        new BitLockerOptions(volume).Validate();
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$b=Get-BitLockerVolume -MountPoint '" + volume.ToUpperInvariant() + "' -ErrorAction Stop; " +
            "$v=Get-Volume -DriveLetter '" + char.ToUpperInvariant(volume[0]) + "' -ErrorAction Stop; $t=Get-Tpm -ErrorAction Stop; " +
            "$policy=Test-Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\FVE' -ErrorAction Stop; " +
            "[pscustomobject]@{mount=$b.MountPoint; identity=$v.UniqueId; filesystem=[string]$v.FileSystemType; type=[string]$b.VolumeType; status=[string]$b.VolumeStatus; locked=[string]$b.LockStatus; protection=[string]$b.ProtectionStatus; percent=$b.EncryptionPercentage; tpm=$t.TpmReady; policy=[bool]$policy} | ConvertTo-Json -Compress"], true);
    }

    public static async Task<BitLockerReadiness> InspectAsync(string volume, CancellationToken token = default)
    {
        var step = QueryStep(volume);
        if (!OperatingSystem.IsWindows() || !WindowsState.IsAdministrator()) return new(volume, "", false, "Windows administrator access is required.");
        if (WindowsState.RebootPending()) return new(volume, "", false, "Restart and reassess before BitLocker changes.");
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false), volume);
    }

    public static BitLockerReadiness Interpret(ToolResult result, string volume)
    {
        new BitLockerOptions(volume).Validate();
        BitLockerReadiness Unknown() => new(volume, "", false, "Volume, TPM or policy readiness is unavailable. Encryption is not eligible.");
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var r = document.RootElement;
            string? id = r.GetProperty("identity").GetString();
            if (!string.Equals(r.GetProperty("mount").GetString(), volume, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(id) || id.Length > 260) return Unknown();
            var reasons = new List<string>();
            if (r.GetProperty("filesystem").GetString() != "NTFS" || r.GetProperty("type").GetString() != "OperatingSystem") reasons.Add("This encryption path requires an NTFS operating-system volume.");
            if (r.GetProperty("status").GetString() != "FullyDecrypted" || r.GetProperty("percent").GetInt32() != 0 || r.GetProperty("protection").GetString() != "Off") reasons.Add("Volume is not fully decrypted. Existing encryption is preserved.");
            if (r.GetProperty("locked").GetString() != "Unlocked") reasons.Add("Volume is locked or lock state is unavailable.");
            if (!r.GetProperty("tpm").GetBoolean()) reasons.Add("TPM is not ready. No initialization or clearing is performed.");
            if (r.GetProperty("policy").GetBoolean()) reasons.Add("BitLocker policy is present. Policy compatibility must be resolved before this encryption path can run.");
            return new(volume, id, reasons.Count == 0, reasons.Count == 0 ? "Initial volume checks passed. Recovery backup and fresh execution checks are still required." : string.Join(" ", reasons));
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException) { return Unknown(); }
    }
}
