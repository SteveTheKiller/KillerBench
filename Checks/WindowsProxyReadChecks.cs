using System.Diagnostics;
using System.Text;
using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsProxyReadChecks
{
    internal static async Task RunAsync()
    {
        foreach (string mode in new[] { "denied", "provider-failed", "wrong-type", "short", "bad-length", "empty-proxy", "missing-key", "missing-value", "direct", "proxy" })
        {
            var probe = new WindowsWinHttpProxyProbe((script, token) => ExecuteAsync(script, mode, token));
            var result = await probe.ObserveAsync(new Dictionary<string, string>(), TimeSpan.FromSeconds(5), default);
            bool absent = mode is "missing-key" or "missing-value" or "direct";
            bool complete = absent || mode == "proxy";
            if (result.Complete != complete || result.Value != (absent ? "Direct" : mode == "proxy" ? "Proxy" : null))
                throw new Exception("WinHTTP registry fixture " + mode + " produced incorrect proxy coverage.");
            if (mode == "proxy") continue;
            var action = new WindowsWinHttpProxyResetAction((script, token) => ExecuteAsync(script, mode, token));
            var reset = await action.ExecuteAsync(new Dictionary<string, string>(), TimeSpan.FromSeconds(5), null, default);
            if (reset.Dispatched || reset.Succeeded != absent || reset.Raw.Contains("RESET-ATTEMPT", StringComparison.Ordinal))
                throw new Exception("WinHTTP registry fixture " + mode + " accepted a failed/malformed read or attempted reset.");
        }
    }

    private static async Task<ToolResult> ExecuteAsync(string script, string mode, CancellationToken token)
    {
        string value = mode switch
        {
            "wrong-type" => "'untrusted registry text'",
            "short" => "[byte[]](0,0)",
            "bad-length" => "[byte[]](0,0,0,0,0,0,0,0,0,0,0,0,255,255,255,127)",
            "empty-proxy" => "[byte[]](0,0,0,0,0,0,0,0,2,0,0,0,0,0,0,0)",
            "proxy" => "[byte[]](0,0,0,0,0,0,0,0,2,0,0,0,1,0,0,0,112)",
            _ => "[byte[]](0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0)"
        };
        string getItem = mode switch
        {
            "denied" => "throw [System.UnauthorizedAccessException]::new('Substituted access denied')",
            "provider-failed" => "throw [System.IO.IOException]::new('Substituted provider failure')",
            "missing-key" => "throw [System.Management.Automation.ItemNotFoundException]::new('Substituted missing key')",
            _ => "$key=[pscustomobject]@{Value=(" + value + ")};" +
                "$key|Add-Member -MemberType ScriptMethod -Name GetValueNames -Value {" + (mode == "missing-value" ? "@()" : "@('WinHttpSettings')") + "};" +
                "$key|Add-Member -MemberType ScriptMethod -Name GetValue -Value {param($name,$default,$options) ,$this.Value};$key"
        };
        string setup = "function Join-Path { throw 'RESET-ATTEMPT: native execution forbidden by fixture' };" +
            "function Get-Item { [CmdletBinding()]param($LiteralPath); " + getItem + " };";
        var start = new ProcessStartInfo(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\WindowsPowerShell\v1.0\powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(setup + script)) }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new Exception("Harmless PowerShell fixture did not start.");
        var output = process.StandardOutput.ReadToEndAsync(token);
        var error = process.StandardError.ReadToEndAsync(token);
        try
        {
            await process.WaitForExitAsync(token).WaitAsync(TimeSpan.FromSeconds(15), token);
            return new ToolResult(process.ExitCode, await output, await error, false);
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }
}
