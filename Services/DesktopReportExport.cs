using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using KillerBench.Engine;
using Microsoft.Win32.SafeHandles;

namespace KillerBench.Services;

public sealed class DesktopReportExport
{
    private readonly ConcurrentDictionary<string, string> destinations = new(StringComparer.OrdinalIgnoreCase);

    public static string DescribeFailure(Exception error)
    {
        if (error is not ReportPublicationException publication) return error.Message;
        string key = publication.Reason switch
        {
            ReportPublicationFailure.ChooseDestinationAgain => "Str_ExportFailure_ChooseAgain",
            ReportPublicationFailure.DestinationChanged => "Str_ExportFailure_Changed",
            ReportPublicationFailure.DestinationTooLarge => "Str_ExportFailure_TooLarge",
            ReportPublicationFailure.NotPublishedPriorRetained => "Str_ExportFailure_NotPublished",
            _ => "Str_ExportFailure_SavedPriorRetained"
        };
        return publication.RecoveryPath is null ? Controls.LocalizedText.Get(key) :
            Controls.LocalizedText.Format(key, publication.RecoveryPath);
    }

    public bool ObserveDestinationForPicker(string path)
    {
        try { return ObserveDestination(path); }
        catch (ReportPublicationException error) { throw new IOException(DescribeFailure(error), error); }
    }

    public static bool ConflictsWithEvidence(string path, IEnumerable<string?> evidencePaths)
    {
        string full = Path.GetFullPath(path);
        foreach (string? source in evidencePaths)
        {
            if (string.IsNullOrWhiteSpace(source)) continue;
            string normalized;
            try { normalized = Path.GetFullPath(source); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException) { continue; }
            if (string.Equals(full, normalized, StringComparison.OrdinalIgnoreCase)) return true;
            if (!File.Exists(full) || !File.Exists(normalized)) continue;
            try { if (Identity(full) == Identity(normalized)) return true; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return true; }
        }
        return false;
    }

    public bool ObserveDestination(string path)
    {
        string full = Path.GetFullPath(path);
        bool exists = File.Exists(full);
        if (exists)
        {
            using var file = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            destinations[full] = Fingerprint(file);
        }
        else destinations[full] = "";
        return exists;
    }

    public async Task WriteAsync(string path, string contents, bool emitBom = false, CancellationToken token = default)
    {
        string full = Path.GetFullPath(path);
        if (!destinations.TryGetValue(full, out string? before))
            throw new ReportPublicationException(ReportPublicationFailure.ChooseDestinationAgain);
        using var prepared = await PreparedReportFile.CreateAsync(full, contents, token, new UTF8Encoding(emitBom));
        token.ThrowIfCancellationRequested();
        if (before.Length == 0) prepared.Publish();
        else prepared.PublishReplacing(original => Fingerprint(original) == before, token);
    }

    private static string Fingerprint(FileStream file)
    {
        if (file.Length > 128L * 1024 * 1024)
            throw new ReportPublicationException(ReportPublicationFailure.DestinationTooLarge);
        if (!GetFileInformationByHandle(file.SafeFileHandle, out var info))
            throw new IOException("Cannot verify the export destination's file identity.", Marshal.GetLastWin32Error());
        file.Position = 0;
        return $"{info.Volume}:{info.IndexHigh}:{info.IndexLow}:" + Convert.ToHexString(SHA256.HashData(file));
    }

    private static (uint Volume, uint High, uint Low) Identity(string path)
    {
        using var file = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (!GetFileInformationByHandle(file, out var info))
            throw new IOException("Cannot verify the export destination's file identity.", Marshal.GetLastWin32Error());
        return (info.Volume, info.IndexHigh, info.IndexLow);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInformation information);
}
