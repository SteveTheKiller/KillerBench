namespace KillerBench.Engine;

public sealed record DebloatPowerRequest(uint SleepAcMinutes, uint SleepDcMinutes,
    uint DisplayAcMinutes, uint DisplayDcMinutes)
{
    public void Validate()
    {
        if (SleepAcMinutes > 1440 || SleepDcMinutes > 1440 || DisplayAcMinutes > 1440 || DisplayDcMinutes > 1440)
            throw new ArgumentOutOfRangeException(nameof(DebloatPowerRequest), "Power timeouts must be between 0 and 1,440 minutes. Zero means Never.");
    }
}

public sealed record DebloatPowerEvidence(string? ActiveSchemeId, uint? SleepAcSeconds,
    uint? SleepDcSeconds, uint? DisplayAcSeconds, uint? DisplayDcSeconds,
    bool? HibernationEnabled, bool? HibernationFilePresent, bool Complete, string Detail);

public interface IDebloatPowerReader
{
    Task<DebloatPowerEvidence> ReadAsync(CancellationToken token);
}

public sealed class WindowsDebloatPowerReader : IDebloatPowerReader
{
    public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
            return Task.FromResult(new DebloatPowerEvidence(null, null, null, null, null, null, null, false,
                "Windows power evidence is unavailable on this platform."));
        return Task.FromResult(ReadWindows());
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static DebloatPowerEvidence ReadWindows()
    {
        const string schemes = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";
        const string sleep = "238c9fa8-0aad-41ed-83f4-97be242c8f20";
        const string idle = "29f6c1db-86da-48c5-9fdb-f2b67b1f44da";
        const string video = "7516b95f-f776-4464-8c53-06167f40cc99";
        const string videoIdle = "3c0bc021-c8a8-4e07-a973-6b14bcb2b7e";
        try
        {
            using var root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(schemes);
            string? active = root?.GetValue("ActivePowerScheme") as string;
            if (!Guid.TryParse(active, out Guid scheme)) active = null;
            else active = scheme.ToString("D");
            uint? sleepAc = ReadIndex(root, active, sleep, idle, "ACSettingIndex");
            uint? sleepDc = ReadIndex(root, active, sleep, idle, "DCSettingIndex");
            uint? displayAc = ReadIndex(root, active, video, videoIdle, "ACSettingIndex");
            uint? displayDc = ReadIndex(root, active, video, videoIdle, "DCSettingIndex");
            using var power = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power");
            bool? enabled = power?.GetValue("HibernateEnabled") is int state ? state != 0 : null;
            string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string? drive = Path.GetPathRoot(systemRoot);
            bool? file = string.IsNullOrWhiteSpace(drive) ? null : File.Exists(Path.Combine(drive, "hiberfil.sys"));
            bool complete = active is not null && sleepAc.HasValue && sleepDc.HasValue && displayAc.HasValue &&
                displayDc.HasValue && enabled.HasValue && file.HasValue;
            return new(active, sleepAc, sleepDc, displayAc, displayDc, enabled, file, complete,
                complete ? "Read active power-scheme timeouts and hibernation state." :
                "Some active power-scheme or hibernation values were unavailable. No power state can be inferred for missing values.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new(null, null, null, null, null, null, null, false,
                "Power evidence could not be read: " + error.Message);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static uint? ReadIndex(Microsoft.Win32.RegistryKey? root, string? scheme,
        string subgroup, string setting, string name)
    {
        if (root is null || scheme is null) return null;
        using var key = root.OpenSubKey(scheme + "\\" + subgroup + "\\" + setting);
        return key?.GetValue(name) switch { int value when value >= 0 => (uint)value, _ => null };
    }
}
