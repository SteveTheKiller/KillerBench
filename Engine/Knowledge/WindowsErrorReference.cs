using System.Runtime.InteropServices;
using System.Text;

namespace KillerBench.Engine.Knowledge;

public sealed record WindowsErrorMessage(uint Code, string? Message, string Source, string Limitation);

public static class WindowsErrorReference
{
    public static WindowsErrorMessage Lookup(uint code)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows message lookup requires Windows.");
        var text = new StringBuilder(4097);
        uint count = FormatMessage(0x00001000 | 0x00000200, IntPtr.Zero, code, 0, text, text.Capacity, IntPtr.Zero);
        return Interpret(code, count == 0 ? null : text.ToString(0, checked((int)count)));
    }

    public static WindowsErrorMessage Interpret(uint code, string? message)
    {
        string? normalized = string.IsNullOrWhiteSpace(message) ? null : string.Join(" ", message
            .Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries));
        if (normalized?.Length > 4096) normalized = null;
        return new(code, normalized, "Installed Windows system message table",
            "Win32 system message only, in the system-selected language. This does not identify a failing component or authorize repair. Vendor message modules are not loaded.");
    }

    [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint FormatMessage(uint flags, IntPtr source, uint messageId, uint languageId,
        StringBuilder buffer, int size, IntPtr arguments);
}
