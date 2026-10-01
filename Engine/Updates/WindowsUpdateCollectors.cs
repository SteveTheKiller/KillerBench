using System.Diagnostics;
using System.Text;

namespace KillerBench.Engine.Updates;

public static class WindowsUpdateCollectors
{
    public static IReadOnlyList<IEvidenceCollector> Create()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Live collection requires Windows. Saved captures can be opened on other platforms.");
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string drive = Path.GetPathRoot(windows)!;
        return [
            new TextFileCollector(Path.Combine(windows, "Logs", "CBS", "CBS.log")),
            new TextFileCollector(Path.Combine(windows, "Panther", "setupact.log")),
            new TextFileCollector(Path.Combine(windows, "Panther", "setuperr.log")),
            new TextFileCollector(Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Panther", "setupact.log")),
            new TextFileCollector(Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Panther", "setuperr.log")),
            new TextFileCollector(Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Rollback", "setupact.log")),
            new TextFileCollector(Path.Combine(drive, "$WINDOWS.~BT", "Sources", "Rollback", "setuperr.log")),
            new WindowsEventCollector("Microsoft-Windows-WindowsUpdateClient/Operational"),
            new WindowsEventCollector("Setup"),
            new WindowsEventCollector("System", true),
            new TextFileCollector(Path.Combine(windows, "Logs", "DISM", "dism.log")),
            new WindowsEventCollector("Microsoft-Windows-Bits-Client/Operational"),
            new WindowsEventCollector("Application"),
            new SystemStateCollector(),
            new UpdateServicesCollector()
        ];
    }
}

public sealed class TextFileCollector(string path) : IEvidenceCollector
{
    public string Name => path;
    public async Task<CaptureSource> CollectAsync(CancellationToken cancellationToken)
    {
        var evidence = await TextEvidence.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        return new(Name, "Text", evidence.Truncated ? CollectionStatus.Truncated : CollectionStatus.Collected,
            evidence.Truncated ? "First 1,000,000 characters only; later content was not inspected." : "Text snapshot read. Active logs may change during collection.", evidence);
    }
}

public sealed class WindowsEventCollector(string channel, bool servicingOnly = false) : IEvidenceCollector
{
    public string Name => channel;
    public async Task<CaptureSource> CollectAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (channel is not ("System" or "Setup" or "Application" or "Microsoft-Windows-WindowsUpdateClient/Operational" or "Microsoft-Windows-Bits-Client/Operational" or "Microsoft-Windows-Servicing/Operational"))
            throw new ArgumentException("Unsupported event channel.");
        string query = "*[System[TimeCreated[timediff(@SystemTime) <= 604800000]" +
            (servicingOnly ? " and (Provider[@Name='Microsoft-Windows-WindowsUpdateClient'] or Provider[@Name='Microsoft-Windows-Servicing'])" : "") + "]]";
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wevtutil.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.Unicode, StandardErrorEncoding = Encoding.Unicode };
        foreach (string arg in new[] { "qe", channel, "/q:" + query, "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:true" }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Could not start Windows event query.");
        using var stop = cancellationToken.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { } });
        var outputTask = ReadBounded(process.StandardOutput, TextEvidence.MaximumCharacters, cancellationToken);
        var errorTask = ReadBounded(process.StandardError, 4096, cancellationToken);
        try
        {
            var first = await Task.WhenAny(outputTask, errorTask).ConfigureAwait(false);
            if (first.IsFaulted && !process.HasExited) process.Kill(true);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (process.ExitCode != 0)
                return new(Name, "Events", process.ExitCode == 5 ? CollectionStatus.AccessDenied : process.ExitCode == 15007 ? CollectionStatus.Unavailable : CollectionStatus.Failed,
                    $"Event query exit {process.ExitCode}: {errorTask.Result.Trim()}", null);
            return EventEvidence.FromXml(Name, outputTask.Result);
        }
        finally
        {
            if (!process.HasExited) process.Kill(true);
        }
    }

    private static async Task<string> ReadBounded(StreamReader reader, int limit, CancellationToken token)
    {
        var text = new StringBuilder();
        char[] buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) > 0)
        {
            if (text.Length + read > limit) throw new IOException("Event query output exceeded its size limit. Source not retained.");
            text.Append(buffer, 0, read);
        }
        return text.ToString().TrimStart('\uFEFF');
    }
}
