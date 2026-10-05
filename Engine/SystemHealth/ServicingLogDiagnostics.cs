namespace KillerBench.Engine.SystemHealth;

public static class ServicingLogDiagnostics
{
    private sealed record Source(string Label, string Path, bool Expected, bool Compressed = false);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<string, CancellationToken, Task<TextEvidence>>? read = null,
        Func<string, bool>? exists = null,
        Func<string?>? newestCbsArchive = null,
        Func<string?>? newestCbsCab = null,
        Func<string, CancellationToken, Task<TextEvidence>>? readCompressed = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (read is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows servicing-log inspection requires Windows.");
        read ??= TextEvidence.ReadTailAsync;
        exists ??= File.Exists;
        var sources = Sources().ToList();
        var checks = new List<WorkflowCheck>();
        try
        {
            string? archive = (newestCbsArchive ?? (() => DiscoverNewestPlainTextCbsArchive(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows))))();
            if (!string.IsNullOrWhiteSpace(archive))
                sources.Add(new Source("Newest CBS plain-text archive", Path.GetFullPath(archive), false));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            NotSupportedException or System.Security.SecurityException)
        {
            checks.Add(new("CBS archive discovery", CheckState.Unknown,
                "Plain-text CBS archive discovery could not complete within its fixed directory and item bound. Current logs remain independent; no repair was selected.",
                Evidence: new ToolResult(-1, "", error.GetType().Name + ": " + error.Message, false)));
        }
        try
        {
            string? cabinet = (newestCbsCab ?? (() => CbsCabEvidence.DiscoverNewest(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows))))();
            if (!string.IsNullOrWhiteSpace(cabinet))
                sources.Add(new Source("Newest compressed CBS archive", Path.GetFullPath(cabinet), false, true));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            NotSupportedException or System.Security.SecurityException)
        {
            checks.Add(new("CBS cabinet discovery", CheckState.Unknown,
                "Compressed CBS archive discovery could not complete within its fixed directory and item bound. Current logs remain independent; no repair was selected.",
                Evidence: new ToolResult(-1, "", error.GetType().Name + ": " + error.Message, false)));
        }
        int available = 0;
        foreach (var source in sources)
        {
            token.ThrowIfCancellationRequested();
            if (!exists(source.Path))
            {
                if (source.Expected)
                    checks.Add(new(source.Label, CheckState.Unknown,
                        "The expected log was not present when inspected. No current servicing conclusion or repair was selected."));
                continue;
            }
            available++;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            try
            {
                Func<string, CancellationToken, Task<TextEvidence>> selectedReader = source.Compressed
                    ? readCompressed ?? ((path, cancellationToken) => CbsCabEvidence.ReadNewestLogAsync(path, token: cancellationToken))
                    : read;
                var pending = selectedReader(source.Path, deadline.Token)
                    ?? throw new InvalidOperationException("Servicing-log reader returned no task.");
                var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Servicing-log reader returned no evidence.");
                token.ThrowIfCancellationRequested();
                var report = Investigation.Analyze(evidence, token);
                string coverage = report.Partial
                    ? "The newest bounded portion was inspected; earlier content was not inspected."
                    : "The complete file present at collection time was inspected.";
                string findings = report.Findings.Count == 0
                    ? "No supported deterministic finding was recorded in this portion."
                    : report.Findings.Count + " supported finding groups were recorded: " +
                        string.Join(", ", report.Findings.Select(item => item.Title).Distinct(StringComparer.Ordinal).Take(8)) + ".";
                checks.Add(new(source.Label, report.Findings.Count == 0 ? CheckState.Information : CheckState.Attention,
                    coverage + " " + findings +
                    " Historical entries do not establish current machine health and cannot authorize a repair.",
                    Evidence: new ToolResult(0, Investigation.ToJson(report), "", false)));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
                InvalidOperationException or NotSupportedException or System.Security.SecurityException or
                System.ComponentModel.Win32Exception or OperationCanceledException)
            {
                checks.Add(new(source.Label, CheckState.Unknown,
                    "The log could not be inspected within its bound. Other logs remain independent; no repair was selected.",
                    Evidence: new ToolResult(-1, "", error.GetType().Name + ": " + error.Message, false)
                    { TimedOut = deadline.IsCancellationRequested }));
            }
        }
        checks.Add(new("Servicing log coverage", CheckState.Information,
            available + " of " + sources.Count + " selected log locations were present. CBS.log and DISM.log are expected; upgrade, rollback and archived CBS logs appear only when applicable. The newest plain-text archive and newest compressed CAB archive are included when present. CAB content is expanded only into a bounded temporary directory, the newest bounded log portion is inspected, and temporary content is removed afterward. Missing optional logs do not establish a problem. No source log or servicing setting changed."));
        return checks.AsReadOnly();
    }

    public static string? DiscoverNewestPlainTextCbsArchive(string windowsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowsDirectory);
        string directory = Path.Combine(Path.GetFullPath(windowsDirectory), "Logs", "CBS");
        if (!Directory.Exists(directory)) return null;
        if ((new DirectoryInfo(directory).Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The CBS log directory is a reparse point.");
        string[] candidates = [.. Directory.EnumerateFiles(directory, "CbsPersist_*.log", SearchOption.TopDirectoryOnly)
            .Take(4_097)];
        if (candidates.Length > 4_096)
            throw new InvalidDataException("CBS archive discovery exceeded 4,096 plain-text candidates.");
        return candidates
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<Source> Sources()
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string drive = Path.GetPathRoot(windows) ?? windows;
        return Array.AsReadOnly<Source>(
        [
            new Source("CBS servicing log", Path.Combine(windows, "Logs", "CBS", "CBS.log"), true),
            new Source("DISM servicing log", Path.Combine(windows, "Logs", "DISM", "dism.log"), true),
            new Source("Windows Setup activity log", Path.Combine(windows, "Panther", "setupact.log"), false),
            new Source("Windows Setup error log", Path.Combine(windows, "Panther", "setuperr.log"), false),
            new Source("Upgrade Setup activity log", Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Panther", "setupact.log"), false),
            new Source("Upgrade Setup error log", Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Panther", "setuperr.log"), false),
            new Source("Rollback Setup activity log", Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Rollback", "setupact.log"), false),
            new Source("Rollback Setup error log", Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Rollback", "setuperr.log"), false)
        ]);
    }
}
