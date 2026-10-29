using KillerBench.Engine;
using System.IO;
using System.Text.Json;

internal static class PrintJobChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Snapshot(uint mask) => new(0, JsonSerializer.Serialize(new
        { jobs = new[] { new { Name = "Fixture, 1", JobId = 1, StatusMask = mask } } }), "", false);
        foreach (uint mask in new uint[] { 1, 2, 32, 64, 512, 1024, 66 })
        {
            var check = PrintJobDiagnostics.Interpret(Snapshot(mask)).Single();
            Require(check.State == CheckState.Attention && check.Repair is null && check.Evidence is not null,
                "Problem job flags retain evidence without selecting destructive recovery");
        }
        foreach (uint mask in new uint[] { 4, 8, 16, 128, 256, 2048 })
            Require(PrintJobDiagnostics.Interpret(Snapshot(mask)).Single().State == CheckState.Information,
                "Transitional or reported completion flags cannot establish printer health");
        foreach (uint mask in new uint[] { 0, 4096, uint.MaxValue })
            Require(PrintJobDiagnostics.Interpret(Snapshot(mask)).Single().State == CheckState.Unknown, "Unsupported flags stay unknown");
        foreach (var result in new[] { Snapshot(1) with { Truncated = true }, Snapshot(1) with { Error = "partial" },
            Snapshot(1) with { Output = "{\"jobs\":[{\"Name\":\"x\",\"JobId\":1,\"StatusMask\":null}]}" },
            Snapshot(1) with { Output = "{\"jobs\":[],\"jobs\":[]}" } })
            Require(PrintJobDiagnostics.Interpret(result).Single().State == CheckState.Unknown, "Incomplete job evidence cannot establish state");
        var capped = new ToolResult(0, JsonSerializer.Serialize(new { jobs = Enumerable.Range(1, 257)
            .Select(i => new { Name = "Fixture, " + i, JobId = i, StatusMask = 16 }) }), "", false);
        var findings = PrintJobDiagnostics.Interpret(capped);
        Require(findings.Count == 257 && findings[^1].State == CheckState.Unknown, "Job cap retains observed jobs and incomplete coverage");
        ToolResult Queues(string[] names, string[] paused) => new(0, JsonSerializer.Serialize(new
        { count = names.Length, paused = paused.Length, names, pausedNames = paused, targets = Array.Empty<string>() }), "", false);
        var exact = new ToolResult(0, JsonSerializer.Serialize(new
        { jobs = new[] { new { Name = "Office, Floor 2, 41", JobId = 41, StatusMask = 1u } } }), "", false);
        var parsed = PrintJobDiagnostics.ReadInventory(exact);
        Require(parsed is { Complete: true, Jobs.Count: 1 } && parsed.Jobs[0].QueueName == "Office, Floor 2",
            "Print job identity retains queue names containing commas by matching the exact job suffix");
        var eligible = PrintQueueRepairEligibility.Assess(Queues(["Office, Floor 2", "Idle"], ["Office, Floor 2", "Idle"]), exact);
        Require(eligible.Check is { State: CheckState.Repairable, Repair: RepairKind.PrintQueues } &&
            eligible.Targets.SequenceEqual(["Office, Floor 2"]),
            "Only an exact paused queue with visible waiting-job evidence becomes a resume target");
        var idle = PrintQueueRepairEligibility.Assess(Queues(["Idle"], ["Idle"]), new(0, "{\"jobs\":[]}", "", false));
        Require(idle.Check is { State: CheckState.Attention, Repair: null } && idle.Targets.Count == 0,
            "An idle intentional queue pause remains review-only");
        foreach (var incomplete in new[] { exact with { Truncated = true }, capped,
            exact with { Output = exact.Output.Replace("Office, Floor 2, 41", "Other, 41") },
            exact with { Output = exact.Output.Replace("41\"", "42\"") } })
        {
            var assessment = PrintQueueRepairEligibility.Assess(Queues(["Office, Floor 2"], ["Office, Floor 2"]), incomplete);
            Require(assessment.Check is { State: CheckState.Unknown or CheckState.Attention, Repair: null } && assessment.Targets.Count == 0,
                "Incomplete, mismatched or malformed print-job evidence cannot authorize queue resume");
        }
        var selectedPlan = PrintRecovery.Plan(["Office, Floor 2"]);
        Require(selectedPlan.Steps.Count == 2 && selectedPlan.Steps[0].Arguments[^1].Contains("Selected queue no longer has exact waiting-job evidence") &&
            selectedPlan.Steps.All(step => step.Arguments[^1].Contains(KillerBench.Engine.Infrastructure.PowerShellText.Literal("Office, Floor 2"))),
            "Selected queue repair repeats waiting-job evidence immediately before the bounded mutation");
        foreach (Exception failure in new Exception[] { new IOException("fixture"), new UnauthorizedAccessException("fixture"),
            new InvalidOperationException("fixture"), new NotSupportedException("fixture"),
            new System.Security.SecurityException("fixture"), new System.ComponentModel.Win32Exception(5) })
        {
            var unavailable = await PrintJobDiagnostics.CheckAsync(execute: (_, _) => Task.FromException<ToolResult>(failure));
            Require(unavailable.Single().State == CheckState.Unknown && unavailable.Single().Repair is null &&
                unavailable.Single().Evidence?.Error == failure.Message, "Provider failure retains evidence without interrupting other checks");
        }
        Require((await PrintJobDiagnostics.CheckAsync(execute: (_, _) => null!)).Single().State == CheckState.Unknown,
            "Missing provider task produces unavailable coverage");
        CancellationToken supplied = default;
        var stalled = await PrintJobDiagnostics.CheckAsync(timeout: TimeSpan.FromMilliseconds(20), execute: (_, ct) =>
        { supplied = ct; return new TaskCompletionSource<ToolResult>().Task; }).WaitAsync(TimeSpan.FromSeconds(3));
        Require(supplied.IsCancellationRequested && stalled.Single().Evidence?.TimedOut == true,
            "Stalled provider receives cancellation and timeout evidence survives");
        using (var stop = new CancellationTokenSource())
        {
            try
            {
                await PrintJobDiagnostics.CheckAsync((_, _) => { stop.Cancel(); return Task.FromResult(Snapshot(16)); }, token: stop.Token);
                throw new Exception("Stopped print collection returned a completed snapshot");
            }
            catch (OperationCanceledException) { }
        }
        Console.WriteLine("PASS: print job flags, provider failures, cancellation and bounded coverage; no jobs changed.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
