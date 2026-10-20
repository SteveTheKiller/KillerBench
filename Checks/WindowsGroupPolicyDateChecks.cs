using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsGroupPolicyDateChecks
{
    internal static async Task RunAsync()
    {
        var recipe = WindowsSystemPack.Build().Recipes.Single(recipe => recipe.Id == "identity.group-policy-age");
        foreach (string mode in new[] { "datetime", "utc", "dmtf", "missing", "malformed", "wrong-type", "future", "future-near", "no-history", "not-domain" })
        {
            var probe = new WindowsGroupPolicyAgeProbe((script, token) => ExecuteAsync(script, mode, token));
            var result = await probe.ObserveAsync(new Dictionary<string, string>(), TimeSpan.FromSeconds(5), default);
            bool dated = mode is "datetime" or "utc" or "dmtf";
            bool complete = dated || mode is "no-history" or "not-domain";
            if (result.Complete != complete) throw new Exception("Group Policy " + mode + " did not preserve expected evidence completeness.");
            if (dated && (!double.TryParse(result.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours) || hours != 2))
                throw new Exception("Group Policy " + mode + " did not report the known two-hour age.");
            if (!complete && Expectation.Evaluate(recipe.Detect.Expect, result).Verdict != ProbeVerdict.Unknown)
                throw new Exception("Group Policy " + mode + " did not remain Unknown.");
            if (mode == "no-history" && result.Value != "NeverApplied" || mode == "not-domain" && result.Value != "NotDomainJoined")
                throw new Exception("Group Policy absence/domain controls changed.");
        }
    }

    private static async Task<ToolResult> ExecuteAsync(string script, string mode, CancellationToken token)
    {
        const string now = "[datetime]::new(2026,10,20,14,0,0,[DateTimeKind]::Local)";
        const string end = "[datetime]::new(2026,10,20,12,0,0,[DateTimeKind]::Local)";
        string value = mode switch
        {
            "datetime" => end,
            "utc" => "(" + end + ").ToUniversalTime()",
            "dmtf" => "[Management.ManagementDateTimeConverter]::ToDmtfDateTime(" + end + ")",
            "malformed" => "'not-a-date'",
            "wrong-type" => "42",
            "future" => "[datetime]::new(2026,10,20,16,0,0,[DateTimeKind]::Local)",
            "future-near" => "[datetime]::new(2026,10,20,14,0,1,[DateTimeKind]::Local)",
            _ => "$null"
        };
        string history = mode == "no-history" ? "return" : "[pscustomobject]@{endTime=(" + value + ")}";
        string setup = "[Threading.Thread]::CurrentThread.CurrentCulture=[Globalization.CultureInfo]::GetCultureInfo('de-DE');" +
            "function Get-Date { " + now + " };" +
            "function Get-CimInstance { [CmdletBinding()]param($ClassName,$Namespace);" +
            "if($ClassName -eq 'Win32_ComputerSystem'){[pscustomobject]@{PartOfDomain=$" + (mode == "not-domain" ? "false" : "true") + "};return};" + history + " };";
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\WindowsPowerShell\v1.0\powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(setup + script)) }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new Exception("Harmless Group Policy fixture did not start.");
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
