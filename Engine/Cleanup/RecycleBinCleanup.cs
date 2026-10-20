using System.Runtime.InteropServices;

namespace KillerBench.Engine.Cleanup;

public static partial class RecycleBinCleanup
{
    public const string TargetName = "Current user Recycle Bin";
    public const string ShellRoot = "shell:RecycleBinFolder";

    public static CleanupEstimate Inspect(int maximumItems)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Recycle Bin inspection requires Windows.");
        if (maximumItems is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(maximumItems));
        var info = new QueryInfo { Size = Marshal.SizeOf<QueryInfo>() };
        int result = SHQueryRecycleBinW(null, ref info);
        if (result < 0)
            return new(TargetName, ShellRoot, 0, 0, 1, true, [], "Windows could not query the current shell Recycle Bin. HRESULT 0x" + result.ToString("X8") + ".") { AggregateAction = true };
        bool overLimit = info.Items > maximumItems;
        return new(TargetName, ShellRoot, info.Items, info.Bytes, 0, overLimit, [],
            info.Items == 0 ? "The current shell Recycle Bin is empty." :
            overLimit ? $"The Recycle Bin contains more than the selected {maximumItems:N0}-item safety limit. Increase the limit or leave this target in preview." :
            "The age setting does not apply to the Recycle Bin. Emptying permanently removes every currently reported item across its drives.") { AggregateAction = true };
    }

    public static CleanupFileOutcome EmptyAndVerify(long expectedItems, long expectedBytes, int maximumItems,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Recycle Bin cleanup requires Windows.");
        if (expectedItems is < 1 || expectedItems > maximumItems || expectedBytes < 0)
            return new(ShellRoot, "Skipped", "The retained Recycle Bin count is empty, invalid or above the selected safety limit.");
        var before = Inspect(maximumItems);
        token.ThrowIfCancellationRequested();
        if (before.Partial || before.CandidateFiles != expectedItems || before.CandidateBytes != expectedBytes)
            return new(ShellRoot, "Skipped", "Recycle Bin contents changed or could not be verified. Nothing was emptied.");
        const uint noConfirmation = 0x00000001, noProgress = 0x00000002, noSound = 0x00000004;
        token.ThrowIfCancellationRequested();
        int result = SHEmptyRecycleBinW(IntPtr.Zero, null, noConfirmation | noProgress | noSound);
        if (result < 0) return new(ShellRoot, "Unverified", "Windows rejected the request. HRESULT 0x" + result.ToString("X8") + ".");
        var after = Inspect(maximumItems);
        return !after.Partial && after.CandidateFiles == 0
            ? new(ShellRoot, "Deleted", "Windows completed the request and the current shell Recycle Bin now reports zero items.")
            : new(ShellRoot, "Unverified", "Windows accepted the request, but a complete empty post-state was not verified. No retry was made.");
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct QueryInfo { public int Size; public long Bytes; public long Items; }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHQueryRecycleBinW(string? rootPath, ref QueryInfo info);
    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHEmptyRecycleBinW(IntPtr window, string? rootPath, uint flags);
}
