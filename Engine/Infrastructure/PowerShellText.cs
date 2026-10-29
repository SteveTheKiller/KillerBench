using System.Text;

namespace KillerBench.Engine.Infrastructure;

/// <summary>Puts arbitrary text into a PowerShell command as a parenthesized expression that decodes
/// it from base64. Doubling the ASCII apostrophe is not enough for a single-quoted literal, because
/// PowerShell also ends one at U+2018 through U+201B, so a queue name or description containing a
/// typographic quote could break out of the literal and run as script, often elevated.</summary>
public static class PowerShellText
{
    public static string Literal(string value) =>
        "([Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('" + Convert.ToBase64String(Encoding.Unicode.GetBytes(value)) + "')))";
}
