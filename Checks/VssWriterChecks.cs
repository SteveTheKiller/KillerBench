using KillerBench.Engine;

internal static class VssWriterChecks
{
    internal static void Run()
    {
        const string output = "Writer name: 'Fixture writer'\nWriter Id: {c60dde3e-f172-4591-89a4-9413837e0ed7}\nWriter Instance Id: {f7b66d37-4f40-4557-91c2-5c962417ab80}\nState: [1] Stable\nLast error: No error\n";
        var raw = new ToolResult(0, output, "", false);
        var stable = VssWriters.Interpret(raw);
        if (stable.State != CheckState.Information || stable.Repair is not null || stable.Evidence != raw || !stable.Detail.Contains("Fixture writer")) throw new Exception("Stable writer lost evidence.");
        for (int state = 2; state <= 15; state++)
        {
            var check = VssWriters.Interpret(raw with { Output = output.Replace("[1] Stable", "[" + state + "] Reported state") });
            if (check.State != CheckState.Attention || check.Repair is not null || !check.Detail.Contains(state <= 5 ? "Waiting" : "failure")) throw new Exception("Writer state misclassified.");
        }
        if (VssWriters.Interpret(raw with { Output = output.Replace("No error", "Retryable error") }).State != CheckState.Attention) throw new Exception("Writer error ignored.");
        foreach (var invalid in new[] { raw with { Output = "" }, raw with { Output = output + output }, raw with { Output = output.Replace("[1]", "[16]") },
            raw with { Output = output.Replace("Writer name:", "Localized name:") }, raw with { Output = output.Replace("Last error: No error", "") },
            raw with { Error = "Denied" }, raw with { TimedOut = true }, raw with { Truncated = true } })
            if (VssWriters.Interpret(invalid).State != CheckState.Unknown) throw new Exception("Incomplete writer evidence claimed coverage.");
        Console.WriteLine("PASS: VSS writer identity, stable/waiting/failed states, error retention and unsupported output rejection; fixtures only.");
    }
}
