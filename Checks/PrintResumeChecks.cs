using System.Reflection;
using System.Text.Json;
using System.IO;
using KillerBench.Engine;

internal static class PrintResumeChecks
{
    private static readonly string[] ResumedQueueNames = ["A"];

    internal static async Task RunAsync()
    {
        const string providers = """
            $global:state=1;
            function Get-CimInstance($ClassName,$OperationTimeoutSec,$ErrorAction) {
                if($ClassName -eq 'Win32_PrintJob') { [pscustomobject]@{Name='A, 7';JobId=7;StatusMask=1}; return }
                if($ClassName -ne 'Win32_Printer') { throw 'Unexpected query' }
                [pscustomobject]@{Name='A';Local=$true;Network=$false;PrinterState=$global:state}
                [pscustomobject]@{Name='B';Local=$true;Network=$false;PrinterState=1}
                [pscustomobject]@{Name='Remote';Local=$false;Network=$true;PrinterState=1}
            }
            function Invoke-CimMethod($InputObject,$MethodName,$OperationTimeoutSec) {
                if($InputObject.Name -ne 'A' -or $MethodName -ne 'Resume') { throw 'Unexpected target' }
                $global:state=0; [pscustomobject]@{ReturnValue=0}
            }
            """;
        var method = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.Infrastructure.NativeTool")!
            .GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        async Task<ToolResult> Run(string fixture)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var args = PrintRecovery.Step(false, ["A"]).Arguments.ToArray();
            args[^1] = fixture + "\n" + args[^1];
            return await (Task<ToolResult>)method.Invoke(null, [PrintRecovery.Tool, args, false, null, timeout.Token])!;
        }
        var success = await Run(providers);
        Require(PrintRecovery.Read(success) == (1, 0), "Native script resumes only the exact selected paused local queue: " + success.Error);
        using (var json = JsonDocument.Parse(success.Output))
            Require(json.RootElement.GetProperty("targets").EnumerateArray().Select(x => x.GetString()).SequenceEqual(ResumedQueueNames),
                "Only the resumed local queue is recorded");
        foreach (string invalid in new[] { "-1", "4294967296", "'not a state'", "$null" })
        {
            var rejected = await Run(providers.Replace("PrinterState=1}", "PrinterState=" + invalid + "}", StringComparison.Ordinal)
                .Replace("$global:state=0; [pscustomobject]@{ReturnValue=0}", "throw 'MUTATION_CALLED'"));
            Require(rejected.ExitCode != 0 && !rejected.Error.Contains("MUTATION_CALLED") && PrintRecovery.Read(rejected) is null,
                "Every queue is validated before the first resume");
        }
        var duplicate = await Run(providers.Replace("Name='B'", "Name='a'")
            .Replace("$global:state=0; [pscustomobject]@{ReturnValue=0}", "throw 'MUTATION_CALLED'"));
        Require(duplicate.ExitCode != 0 && !duplicate.Error.Contains("MUTATION_CALLED"), "Duplicate identities stop before mutation");
        var failure = await Run(providers.Replace("ReturnValue=0", "ReturnValue=5"));
        Require(failure.ExitCode != 0 && PrintRecovery.Read(failure) is null, "Failed resume cannot report success");
        var noJob = await Run(providers.Replace("[pscustomobject]@{Name='A, 7';JobId=7;StatusMask=1}", "return")
            .Replace("$global:state=0; [pscustomobject]@{ReturnValue=0}", "throw 'MUTATION_CALLED'"));
        Require(noJob.ExitCode != 0 && !noJob.Error.Contains("MUTATION_CALLED"),
            "Waiting-job evidence is repeated inside the selected queue command before mutation");
        string journalFolder = Path.Combine(Path.GetTempPath(), "KillerBench-print-selected-" + Guid.NewGuid().ToString("N"));
        var selectedHost = new SelectedHost();
        var selectedResult = await Repairs.RunAsync(RepairKind.PrintQueues, journalFolder, host: selectedHost,
            selectedPrinterQueues: ["A"]);
        Require(selectedHost.Calls == 3 && Repairs.IsVerified(selectedResult) &&
            selectedResult.SelectedPrinterQueues?.SequenceEqual(["A"]) == true &&
            File.ReadAllText(selectedResult.JournalPath).Contains("selectedPrinterQueues"),
            "Shared repair execution retains exact selected queues, fresh preflight and final verification");
        var unselectedHost = new SelectedHost();
        try
        {
            await Repairs.RunAsync(RepairKind.PrintQueues, journalFolder, host: unselectedHost);
            throw new Exception("Unselected print queue repair was accepted");
        }
        catch (InvalidOperationException)
        {
            Require(unselectedHost.Calls == 0, "Generic print queue execution is blocked before a command can run");
        }
        Console.WriteLine("PASS: exact print resume selection, waiting-job recheck, pre-mutation validation and provider failure (substituted CIM only).");
    }

    private sealed class SelectedHost : IRepairHost
    {
        internal int Calls { get; private set; }
        public void CheckPrerequisites() { }
        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress)
        {
            Calls++;
            string targets = Calls == 2 ? "[\"A\"]" : "[]";
            string waiting = Calls == 2 ? "[\"A\"]" : "[]";
            string paused = Calls == 1 ? "[\"A\"]" : "[]";
            int count = Calls == 1 ? 1 : 0;
            return Task.FromResult(new ToolResult(0,
                "{\"count\":1,\"paused\":" + count + ",\"names\":[\"A\"],\"pausedNames\":" + paused + ",\"targets\":" + targets + ",\"waitingNames\":" + waiting + "}",
                "", false));
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
