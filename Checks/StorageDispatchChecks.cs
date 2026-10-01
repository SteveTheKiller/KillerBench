using KillerBench.Engine;
using System.IO;
using System.Text.Json;

internal static class StorageDispatchChecks
{
    internal static async Task RunAsync()
    {
        var blockedRepair = FileSystemRepairPlanning.Create("D:", FileSystemRepairMode.SpotFix, new(true, true, "NTFS"), false, false);
        Require(!blockedRepair.VolumeEligible && blockedRepair.Step is null && blockedRepair.RequiresDismount && !blockedRepair.RequiresRestart &&
            blockedRepair.Blocker!.Contains("dismount consent"), "Non-system filesystem repair requires explicit dismount consent");
        var spotFix = FileSystemRepairPlanning.Create("d:\\", FileSystemRepairMode.SpotFix, new(true, true, "ntfs"), false, true, true);
        Require(spotFix is { VolumeEligible: true, RequiresDismount: true, RequiresRestart: false, Blocker: null } &&
            spotFix.Step is { Tool: "chkdsk.exe", Verification: false } && spotFix.Step.Arguments.SequenceEqual(["D:", "/spotfix"]),
            "Spot-fix draft retains exact selected volume and explicit dismount effects");
        var systemRepair = FileSystemRepairPlanning.Create("C:", FileSystemRepairMode.OfflineScanAndFix, new(true, true, "NTFS"), true, false, true);
        Require(systemRepair is { VolumeEligible: true, RequiresRestart: true, RequiresDismount: false } &&
            systemRepair.Step!.Arguments.SequenceEqual(["C:", "/offlinescanandfix"]),
            "System-volume offline repair draft reports restart and exact command without executing it");
        var systemAttention = new WorkflowCheck("Filesystem", CheckState.Attention, "Volume: C:. Filesystem errors were reported.",
            Evidence: new(1, "Synthetic repairable fixture", "", false));
        Require(FileSystemRepairPlanning.Review(systemRepair, systemAttention) is { EvidenceMatched: true, ExecutionEligible: true } systemReview &&
            systemReview.NextAction.Contains("restart"), "Current exact-volume evidence permits separately confirmed system-volume scheduling");
        foreach (var state in new[] { new StorageVolumeState(false, true, "NTFS"), new(true, false, "NTFS"), new(true, true, "ReFS") })
            Require(!FileSystemRepairPlanning.Create("D:", FileSystemRepairMode.SpotFix, state, false, true).VolumeEligible,
                "Unsupported volume state blocks filesystem repair planning");
        var attention = new WorkflowCheck("Filesystem", CheckState.Attention, "Volume: D:. Filesystem errors were reported.",
            Evidence: new(1, "Synthetic repairable fixture", "", false));
        var unconfirmed = FileSystemRepairPlanning.Create("D:", FileSystemRepairMode.SpotFix,
            new(true, true, "NTFS"), false, true);
        Require(FileSystemRepairPlanning.Review(unconfirmed, attention) is { EvidenceMatched: true, ExecutionEligible: false } unconfirmedReview &&
            unconfirmedReview.NextAction.Contains("backed up", StringComparison.OrdinalIgnoreCase),
            "Exact-volume evidence cannot authorize repair without explicit backup confirmation");
        var review = FileSystemRepairPlanning.Review(spotFix, attention);
        Require(review is { EvidenceMatched: true, ExecutionEligible: true } && review.NextAction.Contains("dismounted"),
            "Current exact-volume evidence permits separately confirmed non-system execution");
        var selectedReview = await FileSystemRepairReviewDiagnostics.CheckAsync(
            new("D:", FileSystemRepairMode.SpotFix, true, true),
            (volume, token) => Task.FromResult(new StorageVolumeState(true, true, "NTFS")),
            (volume, token) => Task.FromResult(attention), "C:");
        Require(selectedReview is [
                { Name: "Filesystem", State: CheckState.Attention },
                { Name: "Filesystem repair review", State: CheckState.Attention, Repair: null } selectedPlan
            ] && selectedPlan.Detail.Contains("dismounted", StringComparison.OrdinalIgnoreCase) &&
            selectedPlan.Detail.Contains("Backup confirmation is recorded", StringComparison.Ordinal) &&
            selectedPlan.Detail.Contains("does not authorize", StringComparison.Ordinal),
            "Selected repair review retains exact evidence, consequences and a separate execution boundary");
        var clearReview = await FileSystemRepairReviewDiagnostics.CheckAsync(
            new("C:", FileSystemRepairMode.OfflineScanAndFix, false, false),
            (volume, token) => Task.FromResult(new StorageVolumeState(true, true, "NTFS")),
            (volume, token) => Task.FromResult(new WorkflowCheck("Filesystem", CheckState.Clear,
                "Volume: C:. No problems.", Evidence: new(0, "Synthetic clean fixture", "", false))), "C:");
        Require(clearReview[^1] is { State: CheckState.Information, Repair: null } &&
            clearReview[^1].Detail.Contains("did not report repairable", StringComparison.Ordinal),
            "Clean selected-volume evidence does not offer filesystem repair");
        var unavailableReview = await FileSystemRepairReviewDiagnostics.CheckAsync(
            new("D:", FileSystemRepairMode.SpotFix, true, false),
            (volume, token) => Task.FromException<StorageVolumeState>(new UnauthorizedAccessException("Fixture denied")),
            (volume, token) => throw new Exception("Diagnosis must not run after unavailable volume evidence"), "C:");
        Require(unavailableReview is [{ State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 }],
            "Unavailable exact-volume evidence stops the review before diagnosis");
        string reviewFolder = Path.Combine(Path.GetTempPath(), "KillerBench-storage-review-" + Guid.NewGuid().ToString("N"));
        try
        {
            var saved = await FileSystemRepairPlanning.SaveReviewAsync(spotFix, attention, reviewFolder);
            var loaded = JsonSerializer.Deserialize<FileSystemRepairReviewRecord>(await File.ReadAllTextAsync(saved.JournalPath), Capture.JsonOptions);
            Require(loaded is { Machine.Length: > 0, Review.EvidenceMatched: true, Review.ExecutionEligible: true } &&
                loaded.Draft.Volume == "D:" && loaded.Diagnosis.Evidence!.ExitCode == 1 && loaded.JournalPath == saved.JournalPath,
                "Filesystem repair review journal retains exact target, evidence and execution eligibility");
        }
        finally { if (Directory.Exists(reviewFolder)) Directory.Delete(reviewFolder, true); }
        string canceledFolder = Path.Combine(Path.GetTempPath(), "KillerBench-storage-canceled-" + Guid.NewGuid().ToString("N"));
        try
        {
            await FileSystemRepairPlanning.SaveReviewAsync(spotFix, attention, canceledFolder, new CancellationToken(true));
            throw new Exception("Canceled filesystem review was saved");
        }
        catch (OperationCanceledException) { Require(!Directory.Exists(canceledFolder), "Cancellation before review persistence creates no journal folder"); }
        try
        {
            await FileSystemRepairReviewDiagnostics.CheckAsync(new("D:", FileSystemRepairMode.SpotFix, true, false),
                (volume, token) => Task.FromCanceled<StorageVolumeState>(token),
                (volume, token) => Task.FromResult(attention), "C:", token: new CancellationToken(true));
            throw new Exception("Canceled selected-volume review continued");
        }
        catch (OperationCanceledException) { }
        foreach (var diagnosis in new[]
        {
            attention with { Detail = "Volume: E:. Filesystem errors were reported." },
            attention with { State = CheckState.Clear },
            attention with { Evidence = attention.Evidence! with { Error = "Synthetic error" } },
            attention with { Evidence = attention.Evidence! with { Truncated = true } },
            attention with { Evidence = attention.Evidence! with { ExitCode = 0 } }
        })
            Require(!FileSystemRepairPlanning.Review(spotFix, diagnosis).EvidenceMatched,
                "Wrong-volume, non-attention and incomplete filesystem evidence cannot advance repair review");
        foreach (Exception failure in new Exception[] { new IOException("Fixture unavailable"), new UnauthorizedAccessException("Fixture denied"),
            new InvalidOperationException("Fixture changed"), new NotSupportedException("Fixture unsupported"), new TimeoutException("Fixture provider timed out"), new System.ComponentModel.Win32Exception(5), new OperationCanceledException("Fixture provider interrupted") })
        foreach (bool duringScan in new[] { false, true })
        {
            var failedProvider = new FailedHost(failure, duringScan);
            var outcome = await StorageDiagnostics.CheckVolumeAsync("D:", failedProvider);
            Require(outcome is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 } && failedProvider.Dispatched == duringScan,
                "Storage provider errors preserve unavailable scope and dispatch boundary");
        }
        var missingReadiness = new Host { State = null! };
        foreach (bool duringScan in new[] { false, true })
        {
            var missingTask = new Host { MissingInspectionTask = !duringScan, MissingCommandTask = duringScan };
            var unavailable = await StorageDiagnostics.CheckVolumeAsync("D:", missingTask);
            Require(unavailable is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 } &&
                (missingTask.Step is not null) == duringScan, "Missing provider tasks preserve unknown state and dispatch boundary");
        }
        Require((await StorageDiagnostics.CheckVolumeAsync("D:", missingReadiness)).State == CheckState.Unknown && missingReadiness.Step is null,
            "Missing readiness prevents filesystem dispatch");
        var missingResult = new Host { Result = null! };
        Require((await StorageDiagnostics.CheckVolumeAsync("D:", missingResult)).State == CheckState.Unknown,
            "Missing command evidence remains unknown");
        foreach (Exception failure in new Exception[] { new IOException("missing"), new UnauthorizedAccessException("denied"), new InvalidOperationException("changed") })
            Require(VolumeCapacity.Check("D:", _ => throw failure) is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 }, "Unavailable capacity retains an unknown diagnostic result");
        Require(VolumeCapacity.Check("D:", _ => null!).State == CheckState.Unknown &&
            VolumeCapacity.Check("D:", _ => new("E:", 100, 50)).State == CheckState.Unknown,
            "Missing or wrong-volume capacity cannot be attributed to the requested drive");
        Require(VolumeCapacity.Check("d:\\", selected => new(selected, 100, 50)).State == CheckState.Information,
            "Normalized selected volume reaches the collector");
        Require(VolumeCapacity.Assess(new("D:", 1000, 99)) is { State: CheckState.Attention, Repair: null, Evidence: not null }, "Low capacity is a review finding without deletion authority");
        Require(VolumeCapacity.Assess(new("D:", 1000, 100)).State == CheckState.Information, "Capacity threshold does not imply disk health");
        foreach (var capacity in new[] { new VolumeCapacitySnapshot("D:", 0, 0), new("D:", 100, -1), new("D:", 100, 101) })
            Require(VolumeCapacity.Assess(capacity).State == CheckState.Unknown, "Invalid capacity snapshots remain unknown");
        Require(typeof(StorageDiagnostics).GetMethod(nameof(StorageDiagnostics.CheckVolumeAsync),
            [typeof(string), typeof(IProgress<string>), typeof(CancellationToken)]) is not null,
            "Original storage API signature remains available to compiled consumers");
        try { await StorageDiagnostics.CheckVolumeAsync("D:", token: new CancellationToken(true)); throw new Exception("Default host ignored cancellation"); }
        catch (OperationCanceledException) { }
        var host = new Host();
        var check = await StorageDiagnostics.CheckVolumeAsync("d:\\", host: host);
        Require(host.Volume == "D:" && host.Step!.Arguments.SequenceEqual(["D:"]) && host.Timeout == TimeSpan.FromMinutes(10) &&
            check.Detail.StartsWith("Volume: D:") && check.State == CheckState.Clear, "Selected volume reaches bounded read-only execution and result");
        var displayFailure = new ThrowingProgress();
        var withFailedDisplay = await StorageDiagnostics.CheckVolumeAsync("D:", new Host(), displayFailure);
        Require(withFailedDisplay.State == CheckState.Clear && displayFailure.Calls == 2,
            "Progress observer failures cannot interrupt readiness or command evidence collection");
        foreach (var state in new[] { new StorageVolumeState(false, true, "NTFS"), new(true, false, "NTFS"), new(true, true, "ReFS"), new(true, true, null) })
        {
            var blocked = new Host { State = state };
            Require((await StorageDiagnostics.CheckVolumeAsync("D:", host: blocked)) is { State: CheckState.Unknown, Evidence: not null, Repair: null } && blocked.Step is null,
                "Unavailable or unsupported volumes cannot start filesystem tools");
        }
        using var cancellation = new CancellationTokenSource();
        var canceled = new Host { AfterInspect = cancellation.Cancel };
        try { await StorageDiagnostics.CheckVolumeAsync("D:", token: cancellation.Token, host: canceled); throw new Exception("Canceled volume check ran"); }
        catch (OperationCanceledException) { Require(canceled.Step is null, "Cancellation after readiness prevents dispatch"); }
        var conflicting = new ToolResult(0, "Windows has checked the file system and found no problems", "Synthetic diagnostic error", false);
        Require(StorageDiagnostics.Interpret(conflicting) is { State: CheckState.Unknown, Repair: null } &&
            StorageDiagnostics.Interpret(conflicting).Evidence == conflicting, "Error stream prevents a clean filesystem verdict and remains available for review");
        var failed = new Host { Result = new(0, "Windows has checked the file system and found no problems", "", false) { TimedOut = true } };
        Require((await StorageDiagnostics.CheckVolumeAsync("D:", host: failed)).State == CheckState.Unknown, "Timed-out dispatch cannot report clean");
        foreach (bool duringScan in new[] { false, true })
        {
            using var stop = new CancellationTokenSource();
            var stalled = new StalledHost(stop, duringScan);
            try { await StorageDiagnostics.CheckVolumeAsync("D:", stalled, token: stop.Token).WaitAsync(TimeSpan.FromSeconds(3)); throw new Exception("Stalled host ignored Stop"); }
            catch (OperationCanceledException) { Require(stalled.Dispatched == duringScan, "Stop prevents later filesystem dispatch"); }
        }
        Console.WriteLine("PASS: selected-volume dispatch, prerequisite rejection, cancellation and timeout (substituted storage host).");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class ThrowingProgress : IProgress<string>
    {
        internal int Calls;
        public void Report(string message) { Calls++; throw new InvalidOperationException("Fixture display unavailable"); }
    }
    private sealed class StalledHost(CancellationTokenSource stop, bool duringScan) : IStorageDiagnosticHost
    {
        internal bool Dispatched;
        public Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token)
        {
            if (duringScan) return Task.FromResult(new StorageVolumeState(true, true, "NTFS"));
            stop.Cancel();
            return new TaskCompletionSource<StorageVolumeState>().Task;
        }
        public Task<ToolResult> RunAsync(RepairStep step, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        {
            Dispatched = true;
            stop.Cancel();
            return new TaskCompletionSource<ToolResult>().Task;
        }
    }
    private sealed class FailedHost(Exception failure, bool duringScan) : IStorageDiagnosticHost
    {
        internal bool Dispatched;
        public Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token) => duringScan
            ? Task.FromResult(new StorageVolumeState(true, true, "NTFS")) : Task.FromException<StorageVolumeState>(failure);
        public Task<ToolResult> RunAsync(RepairStep step, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        { Dispatched = true; return Task.FromException<ToolResult>(failure); }
    }
    private sealed class Host : IStorageDiagnosticHost
    {
        public StorageVolumeState State = new(true, true, "NTFS");
        public ToolResult Result = new(0, "Windows has checked the file system and found no problems", "", false);
        public string? Volume;
        public RepairStep? Step;
        public TimeSpan Timeout;
        public Action? AfterInspect;
        public bool MissingInspectionTask;
        public bool MissingCommandTask;
        public Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token)
        { Volume = volume; AfterInspect?.Invoke(); return MissingInspectionTask ? null! : Task.FromResult(State); }
        public Task<ToolResult> RunAsync(RepairStep step, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        { Step = step; Timeout = timeout; progress?.Report("Fixture filesystem output"); return MissingCommandTask ? null! : Task.FromResult(Result); }
    }
}
