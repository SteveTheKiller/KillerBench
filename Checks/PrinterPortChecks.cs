using KillerBench.Engine;
using System.IO;

internal static class PrinterPortChecks
{
    internal static async Task RunAsync()
    {
        foreach (Exception failure in new Exception[] { new IOException("Fixture provider failure"),
            new UnauthorizedAccessException("Fixture denied"), new TimeoutException("Fixture timeout") })
        {
            var failed = await PrinterPortDiagnostics.CheckAsync(execute: (_, _) => throw failure);
            Require(failed.Single() is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 } &&
                failed[0].Evidence!.Error == failure.Message, "Provider failures retain evidence without interrupting the caller");
        }
        var missing = await PrinterPortDiagnostics.CheckAsync(execute: (_, _) => Task.FromResult<ToolResult>(null!));
        Require(missing.Single().State == CheckState.Unknown, "Missing provider evidence remains unknown");
        using var stop = new CancellationTokenSource();
        try
        {
            await PrinterPortDiagnostics.CheckAsync(stop.Token, (_, _) =>
            {
                stop.Cancel();
                return new TaskCompletionSource<ToolResult>().Task;
            }).WaitAsync(TimeSpan.FromSeconds(2));
            throw new Exception("Printer port collection ignored Stop");
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        const string valid = "{\"queues\":[{\"Name\":\"Office\",\"PortName\":\"IP_1\",\"Type\":0}],\"ports\":[{\"Name\":\"ip_1\"}]}";
        var evidence = new ToolResult(0, valid, "", false);
        var found = PrinterPortDiagnostics.Interpret(evidence).Single();
        Require(found.State == CheckState.Information && found.Repair is null && ReferenceEquals(found.Evidence, evidence), "Registered port retains evidence without claiming physical printer health");
        var absent = evidence with { Output = valid.Replace("ip_1", "another") };
        Require(PrinterPortDiagnostics.Interpret(absent).Single().State == CheckState.Attention, "Unmatched local registration gets review guidance");
        foreach (string type in new[] { "1", "null", "\"Local\"", "99" })
            Require(PrinterPortDiagnostics.Interpret(absent with { Output = absent.Output.Replace("\"Type\":0", "\"Type\":" + type) }).Single().State == CheckState.Unknown,
                "Nonlocal or unsupported queue types cannot establish missing local registration");
        Require(PrinterPortDiagnostics.Interpret(absent with { Output = absent.Output.Replace("IP_1", "A,B") }).Single().State == CheckState.Unknown, "Pooled assignments are not guessed");
        foreach (var failed in new[] { evidence with { ExitCode = 5 }, evidence with { Truncated = true },
            evidence with { TimedOut = true }, evidence with { Error = "provider failed" } })
            Require(PrinterPortDiagnostics.Interpret(failed).Single().State == CheckState.Unknown, "Failed collection cannot establish registration status");
        foreach (string invalid in new[] { "{", "[]", "{}", valid.Replace("\"ports\":[", "\"ports\":[],\"ports\":["),
            valid.Replace("\"Name\":\"ip_1\"", "\"Name\":null"), valid.Replace("\"ports\":[{\"Name\":\"ip_1\"}]", "\"ports\":[{\"Name\":\"ip_1\"},{\"Name\":\"IP_1\"}]"),
            "{\"queues\":[],\"ports\":[" + string.Join(',', Enumerable.Repeat("{\"Name\":\"x\"}", 513)) + "]}" })
            Require(PrinterPortDiagnostics.Interpret(evidence with { Output = invalid }).Single().State == CheckState.Unknown, "Malformed, duplicate and excessive inventory remains unknown");
        Require(PrinterPortDiagnostics.Interpret(evidence with { Output = "{\"queues\":[],\"ports\":[]}" }).Single().State == CheckState.Information, "Empty process-account inventory has limited scope");
        Console.WriteLine("PASS: printer port registration, unavailable coverage, ambiguous assignments and retained evidence; fixtures only.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
