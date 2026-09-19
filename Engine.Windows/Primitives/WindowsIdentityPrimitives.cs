using System.Globalization;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Domain secure-channel state through Test-ComputerSecureChannel. Value is True, False or NotDomainJoined.</summary>
public sealed class WindowsSecureChannelProbe : IProbePrimitive
{
    public string Name => "domain.secure-channel";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$cs=Get-CimInstance Win32_ComputerSystem;if(-not $cs.PartOfDomain){'NotDomainJoined';exit 0};" +
        "try{if(Test-ComputerSecureChannel){'True'}else{'False'}}catch{'Error: '+$_.Exception.Message;exit 3}";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Secure channel checks require Windows.");
        var result = await NativeTool.RunAsync(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "Secure channel check timed out.") { TimedOut = true };
        string text = result.Output.Trim();
        if (result.ExitCode != 0 || text.StartsWith("Error", StringComparison.Ordinal)) return PrimitiveResult.Unavailable("Secure channel could not be tested: " + (text.Length > 0 ? text : result.Error.Trim()));
        return text switch
        {
            "NotDomainJoined" => new PrimitiveResult(true, text, raw) { Detail = "This PC is not joined to an Active Directory domain." },
            "True" => new PrimitiveResult(true, text, raw) { Detail = "The secure channel to the domain is valid." },
            "False" => new PrimitiveResult(true, text, raw) { Detail = "The secure channel to the domain is broken. Domain sign-in with cached credentials may still work until the cache expires." },
            _ => PrimitiveResult.Unavailable("Secure channel output was not recognized.")
        };
    }
}

/// <summary>Microsoft Entra registration state through dsregcmd /status. Value is AzureAdJoined/AzureAdPrt, for example YES/NO.</summary>
public sealed class WindowsEntraStatusProbe : IProbePrimitive
{
    public string Name => "entra.status";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("dsregcmd requires Windows.");
        var result = await NativeTool.RunAsync("dsregcmd.exe", ["/status"], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "dsregcmd timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("dsregcmd exited with " + result.ExitCode + ".");
        string? joined = Field(result.Output, "AzureAdJoined");
        string? domain = Field(result.Output, "DomainJoined");
        if (joined is null || domain is null) return PrimitiveResult.Unavailable("dsregcmd output did not include the device state.");
        string? prt = Field(result.Output, "AzureAdPrt");
        string value = joined + "/" + (prt ?? "NONE");
        string detail = joined == "YES"
            ? (prt == "YES" ? "The device is joined to Microsoft Entra and the current user has a primary refresh token." :
               prt == "NO" ? "The device is joined to Microsoft Entra but the current user has no primary refresh token; cloud sign-in and single sign-on will fail until it is refreshed." :
               "The device is joined to Microsoft Entra; the user token state was not reported (the SSO section only appears for the signed-in user).")
            : "The device is not joined to Microsoft Entra" + (domain == "YES" ? " (domain joined)." : ".");
        return new PrimitiveResult(true, value, raw) { Detail = detail };
    }

    private static string? Field(string output, string name)
    {
        var matches = Regex.Matches(output, @"(?m)^\s*" + Regex.Escape(name) + @"\s*:\s*(YES|NO)\s*$", RegexOptions.CultureInvariant);
        return matches.Count == 1 ? matches[0].Groups[1].Value : null;
    }
}

/// <summary>Hours since computer Group Policy was last applied, read from gpresult. Value is a number; unparseable or localized output stays incomplete.</summary>
public sealed class WindowsGroupPolicyAgeProbe : IProbePrimitive
{
    public string Name => "gpresult.hours-since-applied";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$cs=Get-CimInstance Win32_ComputerSystem;if(-not $cs.PartOfDomain){'NotDomainJoined';exit 0};" +
        "$h=Get-CimInstance -Namespace root\\rsop\\computer -ClassName RSOP_ExtensionStatus -ErrorAction Stop|Sort-Object endTime -Descending|Select-Object -First 1;" +
        "if(-not $h){'NeverApplied';exit 0};$end=[Management.ManagementDateTimeConverter]::ToDateTime($h.endTime);" +
        "[string][math]::Round(((Get-Date)-$end).TotalHours,2)";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Group Policy state requires Windows.");
        var result = await NativeTool.RunAsync(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "Group Policy state read timed out.") { TimedOut = true };
        string text = result.Output.Trim();
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("Group Policy state could not be read (exit " + result.ExitCode + "); administrator access is required for computer policy history.");
        if (text == "NotDomainJoined") return new PrimitiveResult(true, text, raw) { Detail = "This PC is not domain joined; domain Group Policy does not apply." };
        if (text == "NeverApplied") return new PrimitiveResult(true, text, raw) { Detail = "No computer Group Policy application has been recorded." };
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours) && hours >= 0 && hours < 24 * 3650)
            return new PrimitiveResult(true, hours.ToString(CultureInfo.InvariantCulture), raw) { Detail = "Computer Group Policy was last applied " + hours.ToString("0.#", CultureInfo.InvariantCulture) + " hours ago." };
        return PrimitiveResult.Unavailable("Group Policy state output was not recognized.");
    }
}
