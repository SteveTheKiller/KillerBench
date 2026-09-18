using System.Runtime.InteropServices;
using System.Text;

internal sealed record HResultMessageCsv(byte[] Bytes, int Entries);

internal static class HResultMessageCsvGenerator
{
    private const uint FromSystem = 0x00001000;
    private const uint IgnoreInserts = 0x00000200;
    private const uint MaximumWidthMask = 0x000000FF;
    private const uint MaximumRange = 65_535;
    private const string SourceUrl = "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/0642cb2f-2075-4469-918c-4441e69c548a";

    internal static HResultMessageCsv Build(uint firstResult, uint lastResult, DateOnly verifiedOn,
        Func<uint, string?>? resolve = null, CancellationToken token = default)
    {
        if (lastResult < firstResult || (ulong)lastResult - firstResult > MaximumRange)
            throw new ArgumentOutOfRangeException(nameof(lastResult), "An HRESULT range can contain at most 65536 values.");
        if (verifiedOn == default || verifiedOn > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("The verification date is missing or in the future.", nameof(verifiedOn));
        if (resolve is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The installed HRESULT message table is available only on Windows.");
        resolve ??= Resolve;

        var csv = new StringBuilder("Context,Code,Title,Explanation,SourceUrl,AppliesTo\r\n");
        int entries = 0;
        for (ulong value = firstResult; value <= lastResult; value++)
        {
            token.ThrowIfCancellationRequested();
            uint result = (uint)value;
            string? message = resolve(result);
            if (string.IsNullOrWhiteSpace(message)) continue;
            message = Normalize(message);
            if (message.Length is 0 or > 4096) continue;
            string code = "0x" + result.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            string applies = "HRESULT message identifier from the installed Windows system message table, verified " +
                verifiedOn.ToString("yyyy-MM-dd") + ". Not automatically equivalent to Win32, NTSTATUS, bugcheck, installer, update or vendor codes.";
            Append(csv, "hresult", code, "HRESULT " + code, message, SourceUrl, applies);
            entries++;
        }
        if (entries == 0) throw new InvalidDataException("The selected range produced no HRESULT messages.");
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

    private static string? Resolve(uint result)
    {
        var buffer = new StringBuilder(4096);
        uint length = FormatMessage(FromSystem | IgnoreInserts | MaximumWidthMask, IntPtr.Zero,
            result, 0, buffer, buffer.Capacity, IntPtr.Zero);
        return length == 0 ? null : buffer.ToString(0, checked((int)length));
    }

    [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint FormatMessage(uint flags, IntPtr source, uint messageId, uint languageId,
        StringBuilder buffer, int size, IntPtr arguments);
}
