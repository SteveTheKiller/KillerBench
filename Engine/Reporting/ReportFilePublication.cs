namespace KillerBench.Engine;

public static class ReportFilePublication
{
    public static async Task WriteAsync(string path, Func<CancellationToken, Task<string>> createReport,
        bool overwrite = false, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        string report = await createReport(token).ConfigureAwait(false);
        using var prepared = await PreparedReportFile.CreateAsync(path, report, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        prepared.Publish(overwrite);
    }
}

/// <summary>A completed report awaiting publication to its chosen destination.</summary>
public sealed class PreparedReportFile : IDisposable
{
    private readonly string destination, temporary;
    private bool owned;
    private FileStream? stream;

    private PreparedReportFile(string path)
    {
        destination = Path.GetFullPath(path);
        temporary = Path.Combine(Path.GetDirectoryName(destination)!, ".killerbench-report-" + Guid.NewGuid().ToString("N") + ".tmp");
    }

    public static async Task<PreparedReportFile> CreateAsync(string path, string report, CancellationToken token = default)
    {
        var prepared = new PreparedReportFile(path);
        try
        {
            token.ThrowIfCancellationRequested();
            prepared.stream = Infrastructure.RecoveryRecordFiles.CreateNew(prepared.temporary);
            prepared.owned = true;
            await using (var writer = new StreamWriter(prepared.stream, leaveOpen: true))
            {
                await writer.WriteAsync(report.AsMemory(), token).ConfigureAwait(false);
                await writer.FlushAsync(token).ConfigureAwait(false);
            }
            return prepared;
        }
        catch
        {
            prepared.Dispose();
            throw;
        }
    }

    public void Publish(bool overwrite = false)
    {
        if (!owned) throw new ObjectDisposedException(nameof(PreparedReportFile));
        Infrastructure.RecoveryRecordFiles.Archive(stream!, destination, overwrite);
        owned = false;
    }

    public void Dispose()
    {
        try { if (owned) Infrastructure.RecoveryRecordFiles.DeleteOwned(stream!); }
        finally
        {
            owned = false;
            stream?.Dispose();
            stream = null;
        }
    }
}
