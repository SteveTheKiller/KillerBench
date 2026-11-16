namespace KillerBench.Engine;

public enum ReportPublicationFailure
{
    ChooseDestinationAgain, DestinationChanged, DestinationTooLarge, NotPublishedPriorRetained, SavedPriorRetained
}

public sealed class ReportPublicationException : IOException
{
    public ReportPublicationFailure Reason { get; }
    public string? RecoveryPath { get; }
    public bool Saved => Reason == ReportPublicationFailure.SavedPriorRetained;

    public ReportPublicationException(ReportPublicationFailure reason, string? recoveryPath = null, Exception? innerException = null)
        : base(MessageFor(reason, recoveryPath), innerException)
    {
        Reason = reason;
        RecoveryPath = recoveryPath;
    }

    private static string MessageFor(ReportPublicationFailure reason, string? recoveryPath)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (reason is ReportPublicationFailure.NotPublishedPriorRetained or ReportPublicationFailure.SavedPriorRetained &&
            string.IsNullOrWhiteSpace(recoveryPath)) throw new ArgumentException("A retained prior file requires its recovery path.", nameof(recoveryPath));
        return reason switch
        {
            ReportPublicationFailure.ChooseDestinationAgain => "Choose the export destination again before saving.",
            ReportPublicationFailure.DestinationChanged => "The export destination changed after confirmation. Choose it again before saving.",
            ReportPublicationFailure.DestinationTooLarge => "The existing destination exceeds the 128 MiB overwrite limit. Choose a new filename.",
            ReportPublicationFailure.NotPublishedPriorRetained => "The export was not published. The prior report is preserved at: " + recoveryPath,
            _ => "The report was saved. The prior report is preserved at: " + recoveryPath
        };
    }
}

public static class ReportFilePublication
{
    public static async Task WriteAsync(string path, Func<CancellationToken, Task<string>> createReport,
        bool overwrite = false, CancellationToken token = default, System.Text.Encoding? encoding = null)
    {
        token.ThrowIfCancellationRequested();
        string report = await createReport(token).ConfigureAwait(false);
        using var prepared = await PreparedReportFile.CreateAsync(path, report, token, encoding).ConfigureAwait(false);
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

    public static async Task<PreparedReportFile> CreateAsync(string path, string report, CancellationToken token = default,
        System.Text.Encoding? encoding = null)
    {
        var prepared = new PreparedReportFile(path);
        try
        {
            token.ThrowIfCancellationRequested();
            prepared.stream = Infrastructure.RecoveryRecordFiles.CreateNew(prepared.temporary);
            prepared.owned = true;
            await using (var writer = new StreamWriter(prepared.stream, encoding ?? new System.Text.UTF8Encoding(false), leaveOpen: true))
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

    /// <summary>Retains the confirmed original by its open file handle until publication finishes.
    /// The archive notification is a filesystem boundary for recovery observers.</summary>
    public void PublishReplacing(Func<FileStream, bool> validateOriginal, CancellationToken token = default,
        Action<string>? originalArchived = null)
    {
        if (!owned) throw new ObjectDisposedException(nameof(PreparedReportFile));
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Held report replacement requires Windows.");
        using var original = Infrastructure.RecoveryRecordFiles.OpenLocked(destination);
        if (!validateOriginal(original))
            throw new ReportPublicationException(ReportPublicationFailure.DestinationChanged);
        token.ThrowIfCancellationRequested();
        string backup = Path.Combine(Path.GetDirectoryName(destination)!, ".killerbench-report-backup-" + Guid.NewGuid().ToString("N") + ".tmp");
        Infrastructure.RecoveryRecordFiles.Archive(original, backup);
        try
        {
            originalArchived?.Invoke(backup);
            token.ThrowIfCancellationRequested();
            Publish();
        }
        catch (Exception publicationError)
        {
            try { Infrastructure.RecoveryRecordFiles.Archive(original, destination); }
            catch (IOException recoveryError)
            {
                throw new ReportPublicationException(ReportPublicationFailure.NotPublishedPriorRetained, backup,
                    new AggregateException(publicationError, recoveryError));
            }
            throw;
        }
        try { Infrastructure.RecoveryRecordFiles.DeleteOwned(original); }
        catch (IOException error)
        { throw new ReportPublicationException(ReportPublicationFailure.SavedPriorRetained, backup, error); }
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
