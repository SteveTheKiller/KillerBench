using System.IO;
using KillerBench.Engine;

internal static class UpdateCacheChecks
{
    internal static async Task RunAsync()
    {
        const string complete = "{\"download\":{\"complete\":true,\"exists\":true,\"reparse\":false,\"files\":12,\"bytes\":1048576,\"capped\":false},\"dataStore\":{\"complete\":true,\"exists\":false,\"reparse\":false,\"files\":0,\"bytes\":0,\"capped\":false}}";
        var evidence = new ToolResult(0, complete, "", false);
        var finding = UpdateCacheDiagnostics.Interpret(evidence);
        Require(finding is { State: CheckState.Information, Repair: null } && finding.Evidence == evidence &&
            finding.Detail.Contains("12 files, 1 MiB") && finding.Detail.Contains("directory not present") &&
            !finding.Detail.Contains("SoftwareDistribution", StringComparison.OrdinalIgnoreCase),
            "Complete cache aggregates remain informational and omit paths and filenames");

        string capped = complete.Replace("\"complete\":true,\"exists\":true", "\"complete\":false,\"exists\":true")
            .Replace("\"files\":12", "\"files\":100000").Replace("\"capped\":false", "\"capped\":true");
        Require(UpdateCacheDiagnostics.Interpret(evidence with { Output = capped }).State == CheckState.Unknown,
            "A capped inventory remains incomplete");
        string reparse = complete.Replace("\"complete\":true,\"exists\":true,\"reparse\":false,\"files\":12,\"bytes\":1048576", "\"complete\":false,\"exists\":true,\"reparse\":true,\"files\":0,\"bytes\":0");
        Require(UpdateCacheDiagnostics.Interpret(evidence with { Output = reparse }).State == CheckState.Unknown,
            "A reparse-point cache root is not traversed or presented as complete");

        foreach (var invalid in new[]
        {
            evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true },
            evidence with { Truncated = true }, evidence with { Output = "{}" }, evidence with { Output = "not json" },
            evidence with { Output = complete.Replace("\"files\":12", "\"files\":-1") },
            evidence with { Output = complete.Replace("\"files\":12", "\"files\":100001") },
            evidence with { Output = complete.Replace("\"download\":", "\"download\":{},\"download\":") }
        }) Require(UpdateCacheDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, malformed, conflicting and out-of-bound cache evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await UpdateCacheDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(evidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Last().Contains("SoftwareDistribution", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Select-Object -First 100001", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Remove-Item", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Rename-Item", StringComparison.Ordinal),
            "Native plan reads bounded fixed cache areas without mutation commands");
        Require((await UpdateCacheDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Provider failure remains unknown");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await UpdateCacheDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents cache collection");
        Console.WriteLine("PASS: bounded Windows Update cache aggregates, privacy, incomplete evidence and read-only planning; fixtures only.");
    }

    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
