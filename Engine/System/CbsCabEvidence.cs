namespace KillerBench.Engine;

public static class CbsCabEvidence
{
    public const long MaximumCabBytes = 268_435_456;
    public const long MaximumExpandedLogBytes = 536_870_912;

    public static string? DiscoverNewest(string windowsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowsDirectory);
        string directory = Path.Combine(Path.GetFullPath(windowsDirectory), "Logs", "CBS");
        if (!Directory.Exists(directory)) return null;
        if ((new DirectoryInfo(directory).Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The CBS log directory is a reparse point.");
        string[] candidates = Directory.EnumerateFiles(directory, "CbsPersist_*.cab", SearchOption.TopDirectoryOnly)
            .Take(4_097).ToArray();
        if (candidates.Length > 4_096)
            throw new InvalidDataException("CBS cabinet discovery exceeded 4,096 candidates.");
        return candidates.OrderByDescending(File.GetLastWriteTimeUtc)
            .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
    }

    public static async Task<TextEvidence> ReadNewestLogAsync(string cabinetPath, CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        string? temporaryRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinetPath);
        string cabinet = Path.GetFullPath(cabinetPath);
        var cabinetInfo = new FileInfo(cabinet);
        if (!cabinetInfo.Exists || !cabinetInfo.Extension.Equals(".cab", StringComparison.OrdinalIgnoreCase) ||
            !cabinetInfo.Name.StartsWith("CbsPersist_", StringComparison.OrdinalIgnoreCase) ||
            cabinetInfo.Length is <= 0 or > MaximumCabBytes ||
            (cabinetInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The selected CBS cabinet identity or size is unsupported.");
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("CBS cabinet inspection requires Windows expand.exe.");
        string baseRoot = Path.GetFullPath(temporaryRoot ?? Path.Combine(Path.GetTempPath(), "KillerBench", "CbsCab"));
        string extraction = Path.Combine(baseRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extraction);
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var step = new RepairStep("expand.exe", ["-F:CbsPersist_*.log", cabinet, extraction], true);
            var pending = execute(step, token) ?? throw new InvalidOperationException("CBS cabinet provider returned no task.");
            var result = await pending.WaitAsync(TimeSpan.FromSeconds(35), token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("CBS cabinet provider returned no result.");
            if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException("CBS cabinet extraction did not complete with bounded evidence.");
            var directories = Directory.EnumerateDirectories(extraction, "*", SearchOption.TopDirectoryOnly).Take(1).ToArray();
            var files = Directory.EnumerateFiles(extraction, "*", SearchOption.TopDirectoryOnly).Take(3).ToArray();
            if (directories.Length != 0 || files.Length != 1)
                throw new InvalidDataException("CBS cabinet extraction did not contain exactly one top-level log.");
            var log = new FileInfo(files[0]);
            if (!log.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) ||
                !log.Name.StartsWith("CbsPersist_", StringComparison.OrdinalIgnoreCase) ||
                log.Length is <= 0 or > MaximumExpandedLogBytes || (log.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("The expanded CBS log identity or size is unsupported.");
            var evidence = await TextEvidence.ReadTailAsync(log.FullName, token).ConfigureAwait(false);
            return evidence with { Source = cabinet };
        }
        finally
        {
            try { Directory.Delete(extraction, true); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }
}
