using System.Globalization;
using System.Text;
using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Shared parsing for the machine-wide WinHTTP proxy stored in the WinHttpSettings registry value.</summary>
public static class WindowsWinHttpProxy
{
    public const string KeyPath = @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Connections";
    private const int MaxBytes = 16384;

    internal const string ReadValueScript = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$k=$null;try{$k=Get-Item -LiteralPath '" + KeyPath + "' -ErrorAction Stop}catch [System.Management.Automation.ItemNotFoundException]{};" +
        "$v=$null;if($null -ne $k -and $k.GetValueNames() -contains 'WinHttpSettings'){" +
        "$v=$k.GetValue('WinHttpSettings',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames);" +
        "if($v -isnot [byte[]] -or $v.Length -lt 16 -or $v.Length -gt 16384){throw 'Invalid WinHttpSettings value.'};" +
        "$n=[BitConverter]::ToInt32($v,12);if($n -lt 0 -or $n -gt ($v.Length-16)){throw 'Invalid proxy length.'};" +
        "$p=[Text.Encoding]::ASCII.GetString($v,16,$n);$next=16+$n;$b='';" +
        "if($next -lt $v.Length){if(($v.Length-$next) -lt 4){throw 'Invalid bypass length.'};" +
        "$m=[BitConverter]::ToInt32($v,$next);if($m -lt 0 -or $m -gt ($v.Length-$next-4)){throw 'Invalid bypass length.'};$b=[Text.Encoding]::ASCII.GetString($v,$next+4,$m)};" +
        "if($p -match '[\\x00-\\x1f\\x7f]' -or $b -match '[\\x00-\\x1f\\x7f]' -or (([BitConverter]::ToInt32($v,8) -band 2) -ne 0 -and $n -eq 0)){throw 'Invalid proxy settings.'}};";

    /// <summary>Reads the value as a hex string, or Absent after a confirmed missing key or value.</summary>
    internal const string ReadScript = ReadValueScript +
        "if($null -eq $v){'Absent'}else{-join ($v|ForEach-Object{$_.ToString('x2')})}";

    /// <summary>Decodes the stored settings. Returns false when the bytes are not the documented layout.</summary>
    public static bool TryDecode(string text, out bool direct, out string server, out int bypassEntries)
    {
        direct = false; server = ""; bypassEntries = 0;
        text = (text ?? "").Trim();
        if (text == "Absent") { direct = true; return true; }
        if (text.Length == 0 || text.Length % 2 != 0 || text.Length > MaxBytes * 2) return false;
        byte[] bytes;
        try { bytes = Convert.FromHexString(text); } catch (FormatException) { return false; }
        if (bytes.Length < 16) return false;
        int flags = BitConverter.ToInt32(bytes, 8);
        if (!TryString(bytes, 12, out string proxy, out int next)) return false;
        string bypass = "";
        if (next < bytes.Length && !TryString(bytes, next, out bypass, out _)) return false;
        if ((flags & 2) == 0) { direct = true; return true; }
        if (proxy.Length == 0) return false;
        server = proxy;
        bypassEntries = bypass.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        return true;
    }

    private static bool TryString(byte[] bytes, int offset, out string value, out int next)
    {
        value = ""; next = offset;
        if (offset + 4 > bytes.Length) return false;
        int length = BitConverter.ToInt32(bytes, offset);
        if (length < 0 || offset + 4 + length > bytes.Length) return false;
        value = Encoding.ASCII.GetString(bytes, offset + 4, length);
        if (value.Any(char.IsControl)) return false;
        next = offset + 4 + length;
        return true;
    }
}

/// <summary>Reports the machine-wide WinHTTP proxy. Value is Direct or Proxy; Raw names the proxy server and the bypass entry count.</summary>
public sealed class WindowsWinHttpProxyProbe : IProbePrimitive
{
    private readonly Func<string, CancellationToken, Task<ToolResult>> runScript;
    public WindowsWinHttpProxyProbe(Func<string, CancellationToken, Task<ToolResult>>? runScript = null)
        => this.runScript = runScript ?? ((script, token) => WindowsNetAdapterPrimitives.RunAsync(script, token));
    public string Name => "winhttp.proxy";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("WinHTTP proxy inspection requires Windows.");
        return Interpret(await runScript(WindowsWinHttpProxy.ReadScript, token).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "WinHttpSettings", lines =>
    {
        if (lines.Length != 1 || !WindowsWinHttpProxy.TryDecode(lines[0], out bool direct, out string server, out int bypass))
            return PrimitiveResult.Unavailable("The stored WinHTTP proxy settings were not recognized.");
        if (direct) return new PrimitiveResult(true, "Direct", "Direct") { Detail = "WinHTTP uses direct access with no proxy server." };
        string raw = "Proxy|" + server + "|" + bypass.ToString(CultureInfo.InvariantCulture);
        return new PrimitiveResult(true, "Proxy", raw) { Detail = "WinHTTP sends traffic through " + server + " with " + bypass.ToString(CultureInfo.InvariantCulture) + " bypass entr" + (bypass == 1 ? "y." : "ies.") };
    });
}

/// <summary>Resets the machine-wide WinHTTP proxy to direct access. Browser and per-user proxy settings are not changed.</summary>
public sealed class WindowsWinHttpProxyResetAction : IActionPrimitive
{
    private readonly Func<string, CancellationToken, Task<ToolResult>> runScript;
    public WindowsWinHttpProxyResetAction(Func<string, CancellationToken, Task<ToolResult>>? runScript = null)
        => this.runScript = runScript ?? ((script, token) => WindowsNetAdapterPrimitives.RunAsync(script, token));
    public string Name => "winhttp.proxy-reset";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;
    private const string Script = WindowsWinHttpProxy.ReadValueScript +
        "if($null -eq $v -or ([BitConverter]::ToInt32($v,8) -band 2) -eq 0){'AlreadyDirect';exit 0};" +
        "& (Join-Path $env:SystemRoot 'System32\\netsh.exe') winhttp reset proxy|Out-Null;if($LASTEXITCODE -ne 0){throw ('netsh exited with '+$LASTEXITCODE)};'Reset'";

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "WinHTTP proxy changes require Windows.");
        var result = await runScript(Script, token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static ActionResult Interpret(ToolResult result)
    {
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "WinHTTP proxy reset timed out.") { TimedOut = true };
        if (result.Truncated) return new ActionResult(true, false, raw, "WinHTTP proxy reset output was truncated.");
        if (result.ExitCode != 0) return new ActionResult(false, false, raw, "WinHTTP proxy reset failed: " + result.Error.Trim());
        string text = result.Output.Trim();
        return text == "AlreadyDirect" ? new ActionResult(false, true, raw, "WinHTTP already used direct access; nothing changed.")
            : new ActionResult(true, text == "Reset", raw, text == "Reset" ? null : "Unexpected output.");
    }
}
