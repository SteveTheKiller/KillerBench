using System.Text.Json;
using KillerBench.Engine;

internal static class TaskRuntimeChecks
{
    internal static void Run()
    {
        foreach (uint code in new uint[] { 0, 0x41300, 0x41301, 0x41302, 0x41303, 0x41304, 0x41306, 0x41308, 0x41325, 1, 0x80070005, uint.MaxValue })
        {
            var raw = new ToolResult(0, JsonSerializer.Serialize(new { tasks = new[] {
                new { path = "\\", name = "Fixture", state = "Ready", actionCount = 1,
                    runtimeAvailable = true, lastTaskResult = code, missedRuns = 0 }
            } }), "", false);
            var inventory = ScheduledTaskInventory.Interpret(raw);
            var task = inventory.Tasks.Single();
            var check = ScheduledTaskDiagnostics.Assess(task);
            Require(inventory.Complete && task.LastTaskResult == code && task.RuntimeAvailable == true &&
                check.State == CheckState.Information && check.Repair is null,
                "Application-specific and nonterminal runtime results remain historical without automatic retry");
            if (code != 0 && code is not (0x41300 or 0x41301 or 0x41302 or 0x41303 or 0x41304 or 0x41306 or 0x41308 or 0x41325))
                Require(check.Detail.Contains("does not establish a current problem", StringComparison.Ordinal),
                    "Unclassified nonzero result explains its evidence limit");
            Require(ScheduledTaskDiagnostics.Assess(task with { LastTaskResult = 0, MissedRuns = 2 }).State == CheckState.Attention,
                "Missed runs remain visible despite zero result");
            Require(ScheduledTaskDiagnostics.Assess(task with { RuntimeAvailable = false }).State == CheckState.Unknown,
                "Unavailable runtime cannot imply success");
            var disabled = task with { State = "Disabled", LastTaskResult = 0x80070005, MissedRuns = 12 };
            Require(ScheduledTaskDiagnostics.Assess(disabled) is { State: CheckState.Information } disabledResult &&
                disabledResult.Detail.Contains("historical and do not establish a current problem", StringComparison.Ordinal) &&
                ScheduledTaskDiagnostics.Assess(disabled with { RuntimeAvailable = false }) is { State: CheckState.Information } unavailableDisabled &&
                unavailableDisabled.Detail.Contains("no current execution problem is inferred", StringComparison.Ordinal),
                "Disabled tasks retain historical outcomes without becoming current problems");
        }
        foreach (string value in new[] { "null", "-1", "4294967296", "\"0\"", "true" })
        {
            var raw = new ToolResult(0, "{\"tasks\":[{\"path\":\"\\\\\",\"name\":\"Fixture\",\"state\":\"Ready\",\"actionCount\":1,\"runtimeAvailable\":true,\"lastTaskResult\":" + value + ",\"missedRuns\":0}]}", "", false);
            var task = ScheduledTaskInventory.Interpret(raw).Tasks.Single();
            Require(task.RuntimeAvailable == false && ScheduledTaskDiagnostics.Assess(task).State == CheckState.Unknown,
                "Malformed runtime evidence remains unavailable");
        }
        var cases = new (uint Code, string Expected)[] {
            (0x41305, "properties"), (0x8004130A, "properties"), (0x41307, "triggers"), (0x80041309, "triggers"),
            (0x4131B, "events"), (0x4131C, "batch-logon"), (0x8004130F, "account information"),
            (0x80041310, "configured account"), (0x80041314, "incompatible"), (0x80041316, "XML"),
            (0x80041317, "XML"), (0x80041318, "XML"), (0x80041319, "XML"), (0x8004131A, "XML"),
            (0x8004131D, "XML"), (0x8004131E, "end boundary"), (0x8004131F, "overlap"),
            (0x80041320, "user session"), (0x80041321, "integrity"), (0x80041324, "conditions"), (0x80041328, "On-demand") };
        foreach (var (code, expected) in cases)
        {
            var task = new ScheduledTaskSnapshot(new("\\", "Fixture"), "Ready")
                { ActionCount = 1, RuntimeAvailable = true, LastTaskResult = code, MissedRuns = 0 };
            var finding = ScheduledTaskDiagnostics.Assess(task);
            Require(finding.State == CheckState.Attention && finding.Repair is null && finding.Detail.Contains(expected),
                "Documented task result retains targeted review guidance");
        }
        foreach (string stamp in new[] { "2026-09-13T01:23:45.1234567", "bad", "2026-09-13T01:23:45.1234567Z", "2026-02-30T01:23:45.1234567" })
        {
            var raw = new ToolResult(0, JsonSerializer.Serialize(new { tasks = new[] {
                new { path = "\\", name = "Fixture", state = "Ready", actionCount = 1, runtimeAvailable = true,
                    lastTaskResult = 1, missedRuns = 0, lastRunLocalTime = stamp }
            } }), "", false);
            var inventory = ScheduledTaskInventory.Interpret(raw);
            var task = inventory.Tasks.Single();
            bool valid = stamp == "2026-09-13T01:23:45.1234567";
            Require((task.LastRunLocalTime is not null) == valid && inventory.Evidence == raw,
                "Local run timestamps require exact format and preserve source evidence");
            if (valid)
            {
                Require(task.LastRunLocalTime!.Value.Kind == DateTimeKind.Unspecified &&
                    ScheduledTaskDiagnostics.Assess(task).Detail.Contains("2026-09-13 01:23:45.1234567"),
                    "Run time remains source wall clock without an invented UTC conversion");
                var roundTrip = JsonSerializer.Deserialize<ScheduledTaskSnapshot>(JsonSerializer.Serialize(task))!;
                Require(roundTrip.LastRunLocalTime == task.LastRunLocalTime, "Run time survives JSON export");
                Require(!ScheduledTaskDiagnostics.Assess(task with { LastTaskResult = 0x41303 }).Detail.Contains("2026-09-13"),
                    "Never-run result does not present a provider date as execution evidence");
            }
        }
        var mixed = new ToolResult(0, JsonSerializer.Serialize(new { tasks = new[] {
            new { path = "\\", name = "Denied", state = "Ready", actionCount = 1, runtimeAvailable = true,
                lastTaskResult = 0, missedRuns = 0, runtimeError = "Fixture access denied" },
            new { path = "\\", name = "Readable", state = "Ready", actionCount = 1, runtimeAvailable = true,
                lastTaskResult = 0, missedRuns = 0, runtimeError = "" }
        } }), "", false);
        var mixedInventory = ScheduledTaskInventory.Interpret(mixed);
        Require(mixedInventory.Complete && mixedInventory.Tasks.Count == 2 &&
            ScheduledTaskDiagnostics.Assess(mixedInventory.Tasks[0]) is { State: CheckState.Unknown, Repair: null } &&
            ScheduledTaskDiagnostics.Assess(mixedInventory.Tasks[0]).Detail.Contains("Fixture access denied") &&
            ScheduledTaskDiagnostics.Assess(mixedInventory.Tasks[1]).State == CheckState.Information,
            "Per-task provider failure preserves independent inventory and overrides success-looking fields");
        var oversized = ScheduledTaskInventory.Interpret(mixed with { Output = mixed.Output.Replace("Fixture access denied", new string('x', 3000)) });
        Require(oversized.Tasks[0].RuntimeError?.Length == 2048 && oversized.Evidence.Output.Contains(new string('x', 3000)),
            "Provider detail is bounded while raw evidence remains available");
        CheckNativeCollection().GetAwaiter().GetResult();
        Console.WriteLine("PASS: scheduled-task runtime codes and substituted native collection failures; no tasks executed.");
    }
    private static readonly string[] ExpectedTaskNames = ["Before", "Denied", "After"];
    private static async Task CheckNativeCollection()
    {
        if (!OperatingSystem.IsWindows()) return;
        const string providers = """
            function Get-ScheduledTask {
                [CmdletBinding()] param()
                foreach($name in @('Before','Denied','After')) {
                    [pscustomobject]@{TaskPath='\';TaskName=$name;State='Ready';
                        Principal=[pscustomobject]@{UserId='fixture';GroupId='';RunLevel='Limited'};
                        Actions=@([pscustomobject]@{CimClass=[pscustomobject]@{CimClassName='MSFT_TaskExecAction'}});
                        Triggers=@()}
                }
            }
            function Get-ScheduledTaskInfo {
                [CmdletBinding()] param([Parameter(ValueFromPipeline=$true)]$InputObject)
                process {
                    if($InputObject.TaskName -eq 'Denied'){throw 'Fixture access denied'}
                    [pscustomobject]@{LastTaskResult=0;NumberOfMissedRuns=0;LastRunTime=[datetime]'2026-09-13T01:23:45'}
                }
            }
            """;
        var step = ScheduledTaskInventory.CreateReadStep();
        var arguments = step.Arguments.ToArray();
        arguments[^1] = providers + "\n" + arguments[^1];
        var bounded = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.Infrastructure.BoundedTool")!
            .GetMethod("RunAsync", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var raw = await ((Task<ToolResult>)bounded.Invoke(null, [
            step with { Arguments = arguments }, TimeSpan.FromSeconds(15), null, CancellationToken.None])!);
        var inventory = ScheduledTaskInventory.Interpret(raw);
        Require(inventory.Complete && inventory.Tasks.Select(task => task.Identity.Name).SequenceEqual(ExpectedTaskNames),
            "Native collector preserves task identities across provider exceptions");
        Require(inventory.Tasks[0].RuntimeAvailable == true && inventory.Tasks[1].RuntimeAvailable == false &&
            inventory.Tasks[1].RuntimeError == "Fixture access denied" && inventory.Tasks[2].RuntimeAvailable == true,
            "Native collector continues after a denied history read without stale runtime data");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
