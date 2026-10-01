using KillerBench.Engine;
using System.Text.Json;

internal static class FileSignatureChecks
{
    internal static void Run()
    {
        var action = new ScheduledTaskAction(@"C:\Tools\fixture.exe", "");
        foreach (string status in new[] { "Valid", "UnknownError", "NotSigned", "HashMismatch", "NotTrusted", "NotSupportedFileFormat", "Incompatible" })
        {
            var evidence = new ToolResult(0, JsonSerializer.Serialize(new { path = action.Executable, status,
                signer = "CN=Fixture", thumbprint = new string('A', 40), message = "Fixture status" }), "", false);
            var result = FileSignature.Interpret(action, evidence);
            if (result.Status != status || result.Evidence != evidence || !result.Limitation.Contains("does not establish"))
                throw new Exception("Signature status or limits lost");
            foreach (var invalid in new[] { evidence with { TimedOut = true }, evidence with { ExitCode = 5 }, evidence with { Truncated = true },
                evidence with { Error = "denied" }, evidence with { Output = "{}" }, evidence with { Output = "[]" },
                evidence with { Output = evidence.Output.Replace("fixture.exe", "other.exe") },
                evidence with { Output = evidence.Output.Replace(new string('A', 40), "invalid") },
                evidence with { Output = evidence.Output.Replace("\"status\":", "\"status\":\"Valid\",\"status\":") } })
                if (FileSignature.Interpret(action, invalid).Status is not null) throw new Exception("Invalid signature evidence accepted");
        }
        var missingSigner = new ToolResult(0, JsonSerializer.Serialize(new { path = action.Executable, status = "Valid", signer = (string?)null,
            thumbprint = (string?)null, message = "Fixture" }), "", false);
        if (FileSignature.Interpret(action, missingSigner).Status is not null) throw new Exception("Valid signature accepted without signer");
        var literal = new ScheduledTaskAction(@"C:\Tools\literal '$;[1].exe", "");
        var command = FileSignature.CreateReadStep(literal);
        var arguments = command.Arguments.ToArray();
        arguments[^1] = "function Get-AuthenticodeSignature { param([string]$LiteralPath) " +
            "[pscustomobject]@{Path=$LiteralPath;Status='NotSigned';SignerCertificate=$null;StatusMessage='Fixture unsigned'} }; " + arguments[^1];
        var runner = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.Infrastructure.BoundedTool")!
            .GetMethod("RunAsync", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var output = ((Task<ToolResult>)runner.Invoke(null, [command with { Arguments = arguments },
            TimeSpan.FromSeconds(15), null, CancellationToken.None])!).GetAwaiter().GetResult();
        var observed = FileSignature.Interpret(literal, output);
        if (observed.Status != "NotSigned" || observed.Path != literal.Executable || observed.Signer is not null)
            throw new Exception("Signature command failed to preserve the literal path with a substituted provider");
        Console.WriteLine("PASS: signature status, exact path, malformed evidence and substituted native command; no selected file executed.");
    }
}
