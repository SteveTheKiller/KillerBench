using System.Text.Json;
using System.Text.RegularExpressions;
using KillerBench.Engine.Automation;

namespace KillerBench.Engine.Infrastructure;

/// <summary>Proves that the requested native executable was never started.</summary>
public sealed class ToolNotStartedException(string message, Exception? innerException = null) : IOException(message, innerException);

/// <summary>Cancellation prevented the requested executable from reaching process startup.</summary>
public sealed class ToolStartCanceledException(CancellationToken token, Exception? innerException = null)
    : OperationCanceledException("The native executable was canceled before process startup.", innerException, token);

/// <summary>Resolves only the registered, signed App Installer binary in the protected system package directory.</summary>
public static class WinGetExecutableResolver
{
    private const string Publisher = "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "Import-Module ($PSHOME+'\\Modules\\Appx\\Appx.psd1') -ErrorAction Stop;" +
        "Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;" +
        "$all=@(Get-AppxPackage -Name Microsoft.DesktopAppInstaller -ErrorAction Stop);" +
        "if($all.Count-ne 1){[pscustomobject]@{count=$all.Count}|ConvertTo-Json -Compress;exit 0};$p=$all[0];" +
        "$path=Join-Path $p.InstallLocation 'winget.exe';$exists=Test-Path -LiteralPath $path -PathType Leaf;" +
        "$status='Missing';$signer='';$original='';$redirected=$true;" +
        "if($exists){$item=Get-Item -LiteralPath $path -ErrorAction Stop;$root=Get-Item -LiteralPath $p.InstallLocation -ErrorAction Stop;" +
        "$redirected=(($item.Attributes-band [IO.FileAttributes]::ReparsePoint)-ne 0)-or (($root.Attributes-band [IO.FileAttributes]::ReparsePoint)-ne 0);" +
        "$signature=Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop;$status=[string]$signature.Status;" +
        "$signer=[string]$signature.SignerCertificate.Subject;$original=[string]$item.VersionInfo.OriginalFilename};" +
        "[pscustomobject]@{count=1;name=[string]$p.Name;family=[string]$p.PackageFamilyName;fullName=[string]$p.PackageFullName;" +
        "publisher=[string]$p.Publisher;publisherId=[string]$p.PublisherId;development=[bool]$p.IsDevelopmentMode;packageStatus=[string]$p.Status;" +
        "installLocation=[string]$p.InstallLocation;path=$path;exists=[bool]$exists;redirected=[bool]$redirected;signature=$status;signer=$signer;original=$original}|ConvertTo-Json -Compress";

    public static RepairStep QueryStep() => new("powershell.exe", ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);

    public static async Task<string> ResolveAsync(CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));

    public static string Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ToolNotStartedException Rejected() => new("A unique trusted registered App Installer executable is unavailable. WinGet was not started.");
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 16_384)
            throw Rejected();
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            var root = json.RootElement;
            string[] fields = ["count", "name", "family", "fullName", "publisher", "publisherId", "development", "packageStatus", "installLocation", "path", "exists", "redirected", "signature", "signer", "original"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
                !fields.All(field => root.TryGetProperty(field, out _)) || root.GetProperty("count").GetInt32() != 1) throw Rejected();
            string Text(string field) => root.GetProperty(field).GetString() ?? "";
            string fullName = Text("fullName");
            if (Text("name") != "Microsoft.DesktopAppInstaller" || Text("family") != "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe" ||
                Text("publisher") != Publisher || Text("publisherId") != "8wekyb3d8bbwe" || root.GetProperty("development").GetBoolean() ||
                Text("packageStatus") != "Ok" || !root.GetProperty("exists").GetBoolean() || root.GetProperty("redirected").GetBoolean() ||
                Text("signature") != "Valid" || DepotInstallerTrust.Organization(Text("signer")) != "Microsoft Corporation" ||
                !string.Equals(Text("original"), "winget.exe", StringComparison.OrdinalIgnoreCase) ||
                !Regex.IsMatch(fullName, @"\AMicrosoft\.DesktopAppInstaller_[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+_(?:x64|x86|arm64|neutral)__8wekyb3d8bbwe\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                throw Rejected();
            string location = Text("installLocation"), path = Text("path");
            string expectedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", fullName);
            if (!Path.IsPathFullyQualified(location) || !Path.IsPathFullyQualified(path) ||
                !string.Equals(Path.GetFullPath(location), expectedRoot, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(path), Path.Combine(expectedRoot, "winget.exe"), StringComparison.OrdinalIgnoreCase)) throw Rejected();
            return Path.GetFullPath(path);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or ArgumentException or OverflowException)
        { throw new ToolNotStartedException("App Installer executable trust evidence was malformed. WinGet was not started.", error); }
    }
}
