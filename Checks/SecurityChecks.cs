using KillerBench.Engine;

internal static class SecurityChecks
{
    internal static void Run()
    {
        const string good = "{\"tpm\":{\"present\":true,\"ready\":true},\"secureBoot\":true,\"guard\":{\"vbs\":2,\"configured\":[1,2],\"running\":[1,2]}}";
        var output = new ToolResult(0, good, "", false);
        Require(SecurityDiagnostics.Interpret(output).All(c => c.Evidence == output), "Security findings retain raw provider evidence");
        var mixedError = output with { Error = "Provider reported incomplete collection" };
        Require(SecurityDiagnostics.Interpret(mixedError).All(c => c.State == CheckState.Unknown && c.Evidence == mixedError && c.Repair is null),
            "Error-stream output cannot establish protection even with apparent valid JSON");
        var selected = SecurityDiagnostics.ApplySelection(SecurityDiagnostics.Interpret(output), new(false, true, false));
        Require(selected[1].State == CheckState.Clear && selected.Where((_, i) => i != 1).All(c => c.State == CheckState.Information && c.Detail.StartsWith("Not selected")), "Skipped checks are explicit and cannot claim checked protection");
        var step = SecurityDiagnostics.QueryStep(new(false, false, false));
        var arguments = step.Arguments.ToArray();
        arguments[^1] = "function Get-Tpm { $script:called=$true }; function Confirm-SecureBootUEFI { $script:called=$true }; function Get-CimInstance { $script:called=$true }; " + arguments[^1] + "; if($script:called){'UNSELECTED_PROVIDER_CALLED'}";
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var disabled = DefenderRecovery.ExecuteAsync(step with { Arguments = arguments }, token: cancellation.Token).GetAwaiter().GetResult();
        Require(disabled.ExitCode == 0 && !disabled.Output.Contains("UNSELECTED_PROVIDER_CALLED"), "Disabled collectors are not invoked");
        Require(SecurityDiagnostics.Interpret(output).All(c => c.State == CheckState.Clear && c.Repair is null), "Running protections have limited clear outcomes and no mutations");
        var stopped = SecurityDiagnostics.Interpret(output with { Output = good.Replace("\"running\":[1,2]", "\"running\":[0]") });
        Require(stopped[3].State == CheckState.Attention && stopped[4].State == CheckState.Attention, "Configured protection not running requires review");
        var partial = SecurityDiagnostics.Interpret(output with { Output = good.Replace("\"tpm\":{\"present\":true,\"ready\":true}", "\"tpm\":null") });
        Require(partial[0].State == CheckState.Unknown && partial[1].State == CheckState.Clear, "Unavailable provider preserves independent evidence");
        var contradictory = SecurityDiagnostics.Interpret(output with { Output = good.Replace("\"vbs\":2", "\"vbs\":0") });
        Require(contradictory[3].State == CheckState.Unknown && contradictory[4].State == CheckState.Unknown, "Contradictory runtime evidence cannot pass");
        foreach (string value in new[] { "[0,2]", "[2,2]", "[null]", "[99]" })
            Require(SecurityDiagnostics.Interpret(output with { Output = good.Replace("\"running\":[1,2]", "\"running\":" + value) })[3].State == CheckState.Unknown, "Malformed runtime arrays cannot pass");
        Require(SecurityDiagnostics.Interpret(output with { TimedOut = true }).All(c => c.State == CheckState.Unknown), "Timed out collector does not establish protection");
        Console.WriteLine("PASS: TPM, Secure Boot, VBS, memory integrity and Credential Guard classification and partial evidence.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
