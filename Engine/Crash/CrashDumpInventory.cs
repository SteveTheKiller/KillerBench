namespace KillerBench.Engine.Crash;

public sealed record CrashDumpFile(string Path, long Bytes, DateTime LastWriteUtc);
public sealed record CrashDumpInventoryResult(IReadOnlyList<CrashDumpFile> Files, bool Complete, IReadOnlyList<string> Notes);

public static class CrashDumpInventory
{
    public static async Task<CrashDumpInventoryResult> ReadAsync(CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows dump discovery requires Windows.");
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
        limit.CancelAfter(TimeSpan.FromSeconds(15));
        try { return await Task.Run(() => InspectDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Windows), limit.Token, CrashDumpConfiguration.ReadSettings(limit.Token)), limit.Token).WaitAsync(limit.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { return new([], false, ["Dump metadata discovery timed out. Coverage is unavailable."]); }
    }

    public static CrashDumpInventoryResult InspectDirectory(string windowsDirectory, CancellationToken token = default, CrashDumpSettings? settings = null)
    {
        string root = Path.GetFullPath(windowsDirectory);
        var files = new List<CrashDumpFile>();
        var notes = new List<string>();
        bool complete = true;
        var dumpPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.Combine(root, "MEMORY.DMP") };
        var miniPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.Combine(root, "Minidump") };
        if (settings is not null)
        {
            if (settings.Error is not null) { complete = false; notes.Add("Configured dump locations unavailable: " + settings.Error); }
            else
            {
                AddConfigured(settings.DumpFile, dumpPaths);
                AddConfigured(settings.MinidumpDirectory, miniPaths);
            }
        }
        void AddConfigured(string? value, HashSet<string> paths)
        {
            if (value is null) return;
            string expanded = value.Replace("%SystemRoot%", root, StringComparison.OrdinalIgnoreCase).Replace("%windir%", root, StringComparison.OrdinalIgnoreCase);
            if (expanded.Length < 3 || expanded.Length > 32767 || !char.IsAsciiLetter(expanded[0]) || expanded[1] != ':' || expanded[2] != '\\' ||
                expanded[2..].Contains(':') || expanded.IndexOfAny(['%', '*', '?', '"', '<', '>', '|']) >= 0 || expanded.Any(char.IsControl))
            { complete = false; notes.Add("Unsupported configured dump location was not searched: " + value); return; }
            try { paths.Add(Path.GetFullPath(expanded)); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
            { complete = false; notes.Add("Configured dump location could not be normalized: " + error.Message); }
        }
        void Add(string path)
        {
            token.ThrowIfCancellationRequested();
            if (!LocalUnlinked(path, File.GetAttributes, drive => new DriveInfo(drive).DriveType, token))
            { complete = false; notes.Add("Skipped nonlocal or linked dump path: " + path); return; }
            var info = new FileInfo(path);
            var attributes = File.GetAttributes(path);
            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            { complete = false; notes.Add("Skipped linked or non-file dump path: " + path); return; }
            if (!files.Any(file => string.Equals(file.Path, info.FullName, StringComparison.OrdinalIgnoreCase)))
                files.Add(new(info.FullName, info.Length, info.LastWriteTimeUtc));
        }
        foreach (string dump in dumpPaths)
        {
        try { Add(dump); }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { notes.Add("Dump file was not found: " + dump); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { complete = false; notes.Add("MEMORY.DMP metadata unavailable: " + error.Message); }
        }
        foreach (string mini in miniPaths)
        {
        try
        {
            if (!LocalUnlinked(mini, File.GetAttributes, drive => new DriveInfo(drive).DriveType, token)) { complete = false; notes.Add("Nonlocal or linked Minidump directory was skipped: " + mini); }
            else
            {
                int count = 0;
                foreach (string path in Directory.EnumerateFiles(mini, "*.dmp", SearchOption.TopDirectoryOnly))
                {
                    token.ThrowIfCancellationRequested();
                    if (++count > 256) { complete = false; notes.Add("Minidump inventory limited to 256 files; retained order is filesystem order."); break; }
                    try { Add(path); }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException) { complete = false; notes.Add("Dump metadata unavailable: " + error.Message); }
                }
            }
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { notes.Add("Minidump directory was not found: " + mini); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { complete = false; notes.Add("Minidump discovery unavailable: " + error.Message); }
        }
        notes.Add((settings is null ? "Default locations only. " : "Default and supported configured local locations. ") + "Application dumps are not searched. Contents and validity are not inspected; timestamps do not prove crash time, and absence does not prove no crashes.");
        return new(files.AsReadOnly(), complete, notes.AsReadOnly());
    }

    internal static bool LocalUnlinked(string path, Func<string, FileAttributes> attributes,
        Func<string, DriveType> driveType, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full)!;
        if (root.StartsWith("\\\\", StringComparison.Ordinal) || driveType(root) != DriveType.Fixed) return false;
        string current = root;
        foreach (string component in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Prepend(""))
        {
            token.ThrowIfCancellationRequested();
            current = Path.Combine(current, component);
            if ((attributes(current) & FileAttributes.ReparsePoint) != 0) return false;
        }
        return true;
    }

    public static WorkflowCheck Interpret(CrashDumpInventoryResult result) => new("Crash dump files",
        result.Complete ? CheckState.Information : CheckState.Unknown,
        result.Files.Count + " dump-file candidates.\n" + string.Join("\n", result.Files.Take(8).Select(file => file.Path + " | " + file.Bytes + " bytes | modified UTC " + file.LastWriteUtc.ToString("O"))) +
        (result.Files.Count > 8 ? "\nAdditional retained paths are available in saved JSON evidence." : "") +
        "\n" + string.Join("\n", result.Notes),
        Evidence: new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(result, Capture.JsonOptions), "", false));
}
