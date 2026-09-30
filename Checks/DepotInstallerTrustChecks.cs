using System.IO;
using KillerBench.Engine;

internal static class DepotInstallerTrustChecks
{
    public static async Task RunAsync()
    {
        const string google = "CN=Google LLC, O=Google LLC, L=Mountain View, S=California, C=US, SERIALNUMBER=3582691";
        const string microsoft = "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US";
        var chrome = DepotInstallerTrust.Find(DepotApplications.Chrome)!;
        var oneDrive = DepotInstallerTrust.Find(DepotApplications.OneDrive)!;
        const string thumb = "0123456789ABCDEF0123456789ABCDEF01234567";

        Require(DepotInstallerTrust.Organization(google) == "Google LLC" &&
            DepotInstallerTrust.Organization("CN=\"Contoso, Inc.\", O=\"Contoso, Inc.\", C=US") == "Contoso, Inc." &&
            DepotInstallerTrust.Organization("CN=Google LLC, C=US") is null &&
            DepotInstallerTrust.Organization("O=Google LLC, O=Other") is null &&
            DepotInstallerTrust.Organization("CN=\"Broken, O=Google LLC") is null,
            "Signer organization parsing honors quotes and rejects missing, repeated and unbalanced values");
        Require(DepotInstallerTrust.Evaluate(chrome, 1000, "Valid", google, thumb).Outcome == DepotInstallerTrustOutcome.Trusted &&
            DepotInstallerTrust.Evaluate(oneDrive, 1000, "Valid", microsoft, thumb).Outcome == DepotInstallerTrustOutcome.Trusted,
            "A valid signature from the confirmed vendor organization is trusted");
        Require(DepotInstallerTrust.Evaluate(chrome, 1000, "Valid", microsoft, thumb).Outcome == DepotInstallerTrustOutcome.Untrusted &&
            DepotInstallerTrust.Evaluate(chrome, 1000, "Valid", "CN=Google LLC Fake, O=Google LLC Fake", thumb).Outcome == DepotInstallerTrustOutcome.Untrusted &&
            DepotInstallerTrust.Evaluate(chrome, 1000, "HashMismatch", google, thumb).Outcome == DepotInstallerTrustOutcome.Untrusted &&
            DepotInstallerTrust.Evaluate(chrome, 1000, "NotSigned", null, null).Outcome == DepotInstallerTrustOutcome.Untrusted &&
            DepotInstallerTrust.Evaluate(chrome, 0, "Valid", google, thumb).Outcome == DepotInstallerTrustOutcome.Untrusted &&
            DepotInstallerTrust.Evaluate(chrome, 1000, null, null, null).Outcome == DepotInstallerTrustOutcome.Unknown,
            "Another vendor, a look-alike name, a broken signature, no signature, an empty file and missing evidence are never trusted");
        foreach (var application in new[] { DepotApplications.AcrobatReader, DepotApplications.Zoom, DepotApplications.SevenZip })
            Require(DepotInstallerTrust.Evaluate(DepotInstallerTrust.Find(application)!, 1000, "Valid", google, thumb).Outcome == DepotInstallerTrustOutcome.Untrusted,
                application + " stays untrusted until its signer is confirmed");
        Require(DepotInstallerTrust.Find(DepotApplications.Microsoft365) is null &&
            DepotInstallerTrust.Sources.All(s => s.Url.StartsWith("https://", StringComparison.Ordinal)) &&
            DepotInstallerTrust.Sources.Select(s => s.Application).Distinct().Count() == DepotInstallerTrust.Sources.Count,
            "Every source is unique and HTTPS, and Microsoft 365 has no single-installer source");
        string json = "{\"length\":1000,\"status\":\"Valid\",\"signer\":\"" + google + "\",\"thumbprint\":\"" + thumb + "\"}";
        Require(DepotInstallerTrust.Interpret(chrome, new ToolResult(0, json, "", false)).Outcome == DepotInstallerTrustOutcome.Trusted &&
            DepotInstallerTrust.Interpret(chrome, new ToolResult(0, json.Replace("\"length\":1000,", ""), "", false)).Outcome == DepotInstallerTrustOutcome.Unknown &&
            DepotInstallerTrust.Interpret(chrome, new ToolResult(0, json.Replace(thumb, "XYZ"), "", false)).Outcome == DepotInstallerTrustOutcome.Unknown &&
            DepotInstallerTrust.Interpret(chrome, new ToolResult(1, json, "denied", false)).Outcome == DepotInstallerTrustOutcome.Unknown,
            "Installer signature evidence is parsed strictly");
        if (OperatingSystem.IsWindows())
        {
            string pwsh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            var live = await DepotInstallerTrust.VerifyFileAsync(DepotApplications.OneDrive, pwsh);
            Require(live.Outcome is DepotInstallerTrustOutcome.Trusted or DepotInstallerTrustOutcome.Untrusted or DepotInstallerTrustOutcome.Unknown,
                "Live read-only signature inspection returns a bounded outcome");
        }
        Console.WriteLine("PASS: onboarding installer trust requires a valid Authenticode signature from the confirmed vendor organization; unconfirmed and unsigned vendors stay untrusted; read-only signature evidence only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
