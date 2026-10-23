using System.IO;
using KillerBench.Engine;

internal static class UacPolicyChecks
{
    public static void Run()
    {
        var protectedPolicy = UacPolicyDiagnostics.Interpret(new(1, 2, 0, 1, true, "fixture"));
        Require(protectedPolicy.Count == 3 && protectedPolicy[0].State == CheckState.Clear &&
            protectedPolicy[1].State == CheckState.Information && protectedPolicy[2].State == CheckState.Information &&
            protectedPolicy.All(check => check.Repair is null),
            "Enabled secure-desktop UAC configuration remains read only");

        var exposed = UacPolicyDiagnostics.Interpret(new(0, 0, 3, 0, true, "fixture"));
        Require(exposed.Count == 3 && exposed.All(check => check.State == CheckState.Attention) &&
            exposed[0].Detail.Contains("restart") && exposed[1].Detail.Contains("without an interactive prompt") &&
            exposed[2].Detail.Contains("outside the secure desktop"),
            "Disabled and nonisolated UAC settings request specific review");

        var credentialPrompt = UacPolicyDiagnostics.Interpret(new(1, 1, 1, 1, true, "fixture"));
        Require(credentialPrompt.All(check => check.State is CheckState.Clear or CheckState.Information) &&
            credentialPrompt[1].Detail.Contains("credentials on the secure desktop"),
            "Secure credential prompts are described without a false finding");

        foreach (var snapshot in new UacPolicySnapshot?[]
        {
            null,
            new(null, null, null, null, false, "denied"),
            new(null, 2, 0, 1, true, "missing"),
            new(1, 6, 0, 1, true, "future"),
            new(1, 2, 2, 1, true, "invalid"),
            new(1, 2, 0, 2, true, "invalid")
        })
        {
            var result = UacPolicyDiagnostics.Interpret(snapshot);
            Require(result.Count == 1 && result[0].State == CheckState.Unknown && result[0].Repair is null,
                "Incomplete or unrecognized UAC evidence fails closed");
        }

        string source = File.ReadAllText(CheckSource.Find(Path.Combine("Engine", "Security", "UacPolicyDiagnostics.cs")));
        Require(!source.Contains("SetValue", StringComparison.Ordinal) && !source.Contains("DeleteValue", StringComparison.Ordinal) &&
            !source.Contains("CreateSubKey", StringComparison.Ordinal), "UAC collector contains no registry mutation");
        foreach (string excluded in new[] { "WindowsIdentity.GetCurrent", "CredRead", "CredentialCache", "GetTokenInformation", "ProcessStartInfo" })
            Require(!source.Contains(excluded, StringComparison.Ordinal), "UAC collector excludes " + excluded);

        Console.WriteLine("PASS: UAC enablement and elevation-prompt policy remain deterministic, privacy bounded and read only.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
