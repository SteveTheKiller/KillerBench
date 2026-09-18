using System.Runtime.InteropServices;
using System.Text;

internal sealed record NtStatusMessageCsv(byte[] Bytes, int Entries);

internal static class NtStatusMessageCsvGenerator
{
    private const uint FromModule = 0x00000800;
    private const uint IgnoreInserts = 0x00000200;
    private const uint MaximumWidthMask = 0x000000FF;
    private const uint MaximumRange = 65_535;
    private const string SourceUrl = "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/596a1078-e883-4972-9bbc-49e60bebca55";

    internal static NtStatusMessageCsv Build(uint firstStatus, uint lastStatus, DateOnly verifiedOn,
        Func<uint, string?>? resolve = null, CancellationToken token = default)
    {
        if (lastStatus < firstStatus || (ulong)lastStatus - firstStatus > MaximumRange)
            throw new ArgumentOutOfRangeException(nameof(lastStatus), "An NTSTATUS range can contain at most 65536 values.");
        if (verifiedOn == default || verifiedOn > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("The verification date is missing or in the future.", nameof(verifiedOn));
        if (resolve is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The installed NTSTATUS message table is available only on Windows.");
        resolve ??= Resolve;

        var csv = new StringBuilder("Context,Code,Title,Explanation,SourceUrl,AppliesTo\r\n");
        int entries = 0;
        for (ulong value = firstStatus; value <= lastStatus; value++)
        {
            token.ThrowIfCancellationRequested();
            uint status = (uint)value;
            string? message = resolve(status);
            if (string.IsNullOrWhiteSpace(message)) continue;
            message = Normalize(message);
            if (message.Length is 0 or > 4096) continue;
            string code = "0x" + status.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            string applies = "NTSTATUS message identifier from the installed Windows ntdll message table, verified " +
                verifiedOn.ToString("yyyy-MM-dd") + ". Not automatically equivalent to Win32, HRESULT, bugcheck, installer, update or vendor codes.";
            Append(csv, "ntstatus", code, "NTSTATUS " + code, message, SourceUrl, applies);
            entries++;
        }
        if (entries == 0) throw new InvalidDataException("The selected range produced no NTSTATUS messages.");
        return new(new UTF8Encoding(false).GetBytes(csv.ToString()), entries);
    }

    private static string Normalize(string value) =>
        string.Join(" ", value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries));

    private static void Append(StringBuilder csv, params string[] fields)
    {
        for (int index = 0; index < fields.Length; index++)
        {
            if (index > 0) csv.Append(',');
            csv.Append('"').Append(fields[index].Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
        }
        csv.Append("\r\n");
    }

    private static string? Resolve(uint status)
    {
        IntPtr module = GetModuleHandle("ntdll.dll");
        if (module == IntPtr.Zero) return null;
        var buffer = new StringBuilder(4096);
        uint length = FormatMessage(FromModule | IgnoreInserts | MaximumWidthMask, module, status, 0,
            buffer, buffer.Capacity, IntPtr.Zero);
        return length == 0 ? null : buffer.ToString(0, checked((int)length));
    }

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint FormatMessage(uint flags, IntPtr source, uint messageId, uint languageId,
        StringBuilder buffer, int size, IntPtr arguments);
}
