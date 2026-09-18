using System.Runtime.InteropServices;
using System.Text;

internal sealed record WindowsMessageCsv(byte[] Bytes, int Entries);

internal static class WindowsMessageCsvGenerator
{
    private const int MaximumCode = 15_999;
    private const uint FromSystem = 0x00001000;
    private const uint IgnoreInserts = 0x00000200;
    private const uint MaximumWidthMask = 0x000000FF;

    internal static WindowsMessageCsv Build(int firstCode, int lastCode, DateOnly verifiedOn,
        Func<int, string?>? resolve = null, CancellationToken token = default)
    {
        if (firstCode < 0 || lastCode < firstCode || lastCode > MaximumCode)
            throw new ArgumentOutOfRangeException(nameof(lastCode), "Windows message ranges must stay between 0 and 15999.");
        if (verifiedOn == default || verifiedOn > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("The verification date is missing or in the future.", nameof(verifiedOn));
        if (resolve is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The installed Windows message table is available only on Windows.");
        resolve ??= Resolve;
        var csv = new StringBuilder("Context,Code,Title,Explanation,SourceUrl,AppliesTo\r\n");
        int entries = 0;
        for (int code = firstCode; code <= lastCode; code++)
        {
            token.ThrowIfCancellationRequested();
            string? message = resolve(code);
            if (string.IsNullOrWhiteSpace(message)) continue;
            message = Normalize(message);
            if (message.Length is 0 or > 4096) continue;
            string source = SourceUrl(code);
            string applies = "Win32 system error return value from the installed Windows message table, verified " +
                verifiedOn.ToString("yyyy-MM-dd") + ". Not automatically equivalent to HRESULT, NTSTATUS, installer, update or vendor codes.";
            Append(csv, "win32", code.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "Windows system error " + code.ToString(System.Globalization.CultureInfo.InvariantCulture), message, source, applies);
            entries++;
        }
        if (entries == 0) throw new InvalidDataException("The selected range produced no Windows system messages.");
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

    private static string SourceUrl(int code) => code switch
    {
        <= 499 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-",
        <= 999 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--500-999-",
        <= 1299 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1000-1299-",
        <= 1699 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1300-1699-",
        <= 3999 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1700-3999-",
        <= 5999 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--4000-5999-",
        <= 8199 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--6000-8199-",
        <= 8999 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--8200-8999-",
        <= 11999 => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--9000-11999-",
        _ => "https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--12000-15999-"
    };

    private static string? Resolve(int code)
    {
        var buffer = new StringBuilder(4096);
        uint length = FormatMessage(FromSystem | IgnoreInserts | MaximumWidthMask, IntPtr.Zero,
            unchecked((uint)code), 0, buffer, buffer.Capacity, IntPtr.Zero);
        return length == 0 ? null : buffer.ToString(0, checked((int)length));
    }

    [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint FormatMessage(uint flags, IntPtr source, uint messageId, uint languageId,
        StringBuilder buffer, int size, IntPtr arguments);
}
