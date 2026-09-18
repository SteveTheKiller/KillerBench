using System.IO;
using KillerBench.Engine;

internal static class VolumeOptimizationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var request = VolumeOptimization.CreateRequest("d:\\", VolumeOptimizationMode.Recommended);
        Require(request.Volume == "D:", "Drive letters are normalized");
        foreach (string invalid in new[] { "", "C:\\Windows", "\\\\server\\share", "-C" })
            try { VolumeOptimization.CreateRequest(invalid, VolumeOptimizationMode.Recommended); throw new Exception("Invalid volume accepted"); }
            catch (ArgumentException) { }

        var analyze = VolumeOptimization.CreateAnalysisStep("D:");
        var recommended = VolumeOptimization.CreateOptimizationStep("D:", VolumeOptimizationMode.Recommended);
        var retrim = VolumeOptimization.CreateOptimizationStep("D:", VolumeOptimizationMode.ReTrim);
        var defrag = VolumeOptimization.CreateOptimizationStep("D:", VolumeOptimizationMode.Defragment);
        Require(analyze.Arguments[^1].Contains("-Analyze") && !recommended.Arguments[^1].Contains("-Analyze") &&
            !recommended.Arguments[^1].Contains("-ReTrim") && !recommended.Arguments[^1].Contains("-Defrag") &&
            retrim.Arguments[^1].Contains("-ReTrim") && defrag.Arguments[^1].Contains("-Defrag"),
            "Analysis, recommended, retrim and defragment commands remain distinct");

        string previewFolder = Path.Combine(scratch, "optimize-preview");
        var previewHost = new Host();
        var preview = await VolumeOptimization.RunAsync(request, false, previewFolder, previewHost);
        Require(preview.Outcome == VolumeOptimizationOutcome.Preview && previewHost.Calls == 0 && !Directory.Exists(previewFolder),
            "Preview performs no inspection, analysis, optimization or journal write");
        var reviewHost = new Host();
        var review = await VolumeOptimization.ReviewAsync(request, reviewHost);
        Require(review.Outcome == VolumeOptimizationOutcome.Preview && reviewHost.Inspections == 1 &&
            reviewHost.Analyses == 1 && reviewHost.Optimizations == 0 && review.Detail.Contains("No optimization was performed"),
            "Read-only review inspects and analyzes without optimizing");
        var recipeCheck = VolumeOptimization.ToWorkflowCheck(review);
        Require(recipeCheck.State == CheckState.Repairable && recipeCheck.Repair == RepairKind.VolumeOptimization &&
            recipeCheck.Evidence == review.BeforeAnalysis,
            "Complete read-only volume review exposes option-dependent optimization with retained analysis evidence");
        var unavailableCheck = VolumeOptimization.ToWorkflowCheck(new(request, VolumeOptimizationOutcome.Blocked,
            "Fixture unavailable."));
        Require(unavailableCheck.State == CheckState.Unknown && unavailableCheck.Repair is null,
            "Unavailable volume review never exposes optimization");

        foreach (var item in new[]
        {
            new Host { State = Healthy(media: VolumeMediaKind.Unknown) },
            new Host { State = Healthy(media: VolumeMediaKind.SolidState), Administrator = false },
            new Host { State = Healthy(fileSystem: "FAT32") },
            new Host { State = Healthy(health: "Warning") },
            new Host { State = Healthy(volume: "E:") }
        })
        {
            var blocked = await VolumeOptimization.RunAsync(request, true,
                Path.Combine(scratch, "optimize-blocked-" + Guid.NewGuid().ToString("N")), item);
            Require(blocked.Outcome == VolumeOptimizationOutcome.Blocked && item.Optimizations == 0,
                "Unknown media, standard user, unsupported filesystem, unhealthy state and identity mismatch block execution");
        }

        var mismatchTrim = await VolumeOptimization.RunAsync(new("D:", VolumeOptimizationMode.ReTrim), true,
            Path.Combine(scratch, "optimize-trim-mismatch"), new Host { State = Healthy(media: VolumeMediaKind.HardDisk) });
        var mismatchDefrag = await VolumeOptimization.RunAsync(new("D:", VolumeOptimizationMode.Defragment), true,
            Path.Combine(scratch, "optimize-defrag-mismatch"), new Host { State = Healthy(media: VolumeMediaKind.SolidState) });
        Require(mismatchTrim.Outcome == VolumeOptimizationOutcome.Blocked && mismatchDefrag.Outcome == VolumeOptimizationOutcome.Blocked,
            "Explicit optimization modes require compatible confirmed media");

        var incompleteHost = new Host { Analysis = new(0, "partial", "", true) };
        var incomplete = await VolumeOptimization.RunAsync(request, true, Path.Combine(scratch, "optimize-analysis"), incompleteHost);
        Require(incomplete.Outcome == VolumeOptimizationOutcome.Blocked && incompleteHost.Optimizations == 0,
            "Incomplete pre-analysis never authorizes optimization");

        string verifiedFolder = Path.Combine(scratch, "optimize-verified");
        var verifiedHost = new Host();
        var verified = await VolumeOptimization.RunAsync(request, true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == VolumeOptimizationOutcome.Verified && verified.GetExitCode() == 0 &&
            verifiedHost.Inspections == 3 && verifiedHost.Analyses == 3 && verifiedHost.Optimizations == 1 &&
            !File.Exists(Path.Combine(verifiedFolder, "volume-optimization.pending.json")) &&
            File.Exists(verified.JournalPath) && File.Exists(verified.JournalPath + ".prepared.json") &&
            verified.Detail.Contains("not improved performance"),
            "Exact-volume optimization requires durable preparation and complete post-analysis with a limited claim");

        string finalChangedFolder = Path.Combine(scratch, "optimize-final-changed");
        var finalChangedHost = new Host { FinalState = Healthy(identity: "volume-2") };
        var finalChanged = await VolumeOptimization.RunAsync(request, true, finalChangedFolder, finalChangedHost);
        Require(finalChanged.Outcome == VolumeOptimizationOutcome.Blocked && finalChangedHost.Optimizations == 0 &&
            finalChanged.JournalPath is not null && File.Exists(finalChanged.JournalPath) &&
            File.Exists(finalChanged.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(finalChangedFolder, "volume-optimization.pending.json")),
            "Volume identity changed after preparation prevents optimization and closes the prepared guard");
        var finalAnalysisHost = new Host { FinalAnalysis = new(0, "partial", "", true) };
        Require((await VolumeOptimization.RunAsync(request, true, Path.Combine(scratch, "optimize-final-analysis"), finalAnalysisHost)).Outcome ==
            VolumeOptimizationOutcome.Blocked && finalAnalysisHost.Optimizations == 0,
            "Incomplete final analysis prevents optimization");
        var finalAdminHost = new Host { FinalAdministrator = false };
        Require((await VolumeOptimization.RunAsync(request, true, Path.Combine(scratch, "optimize-final-admin"), finalAdminHost)).Outcome ==
            VolumeOptimizationOutcome.Blocked && finalAdminHost.Optimizations == 0,
            "Administrator access changed after preparation prevents optimization");

        string unavailableFinalFolder = Path.Combine(scratch, "optimize-final-unavailable");
        var unavailableFinalHost = new Host { FinalInspectionError = new IOException("volume unavailable") };
        var unavailableFinal = await VolumeOptimization.RunAsync(request, true, unavailableFinalFolder, unavailableFinalHost);
        Require(unavailableFinal.Outcome == VolumeOptimizationOutcome.Blocked && unavailableFinalHost.Optimizations == 0 &&
            !File.Exists(Path.Combine(unavailableFinalFolder, "volume-optimization.pending.json")) &&
            File.Exists(unavailableFinal.JournalPath + ".prepared.json") &&
            unavailableFinal.Detail.Contains("No optimization was started", StringComparison.Ordinal),
            "Unavailable final eligibility sends no optimization request and archives prepared no-write evidence");

        string canceledFinalFolder = Path.Combine(scratch, "optimize-final-canceled");
        var canceledFinalHost = new Host { FinalAnalysisError = new OperationCanceledException("analysis canceled") };
        var canceledFinal = await VolumeOptimization.RunAsync(request, true, canceledFinalFolder, canceledFinalHost);
        Require(canceledFinal.Outcome == VolumeOptimizationOutcome.Stopped && canceledFinalHost.Optimizations == 0 &&
            !File.Exists(Path.Combine(canceledFinalFolder, "volume-optimization.pending.json")) &&
            File.Exists(canceledFinal.JournalPath + ".prepared.json") &&
            canceledFinal.Detail.Contains("before any request was sent", StringComparison.Ordinal),
            "Canceled final analysis sends no optimization request and archives prepared no-write evidence");

        string changedFolder = Path.Combine(scratch, "optimize-changed");
        var changedHost = new Host { AfterState = Healthy(identity: "volume-2") };
        var changed = await VolumeOptimization.RunAsync(request, true, changedFolder, changedHost);
        Require(changed.Outcome == VolumeOptimizationOutcome.Uncertain &&
            File.Exists(Path.Combine(changedFolder, "volume-optimization.pending.json")),
            "Changed post-command identity remains pending and unverified");

        string failedFolder = Path.Combine(scratch, "optimize-failed");
        var failedHost = new Host { Command = new(5, "", "failure", false) };
        var failed = await VolumeOptimization.RunAsync(request, true, failedFolder, failedHost);
        Require(failed.Outcome == VolumeOptimizationOutcome.Failed &&
            File.Exists(Path.Combine(failedFolder, "volume-optimization.pending.json")),
            "Failed command retains the no-repeat guard");
        int beforeRecover = failedHost.Optimizations;
        var failedRecovery = await VolumeOptimization.ReconcileAsync(failedFolder, failedHost);
        Require(failedRecovery.Outcome == VolumeOptimizationOutcome.Uncertain && failedHost.Optimizations == beforeRecover,
            "Incomplete pending command is never repeated during reconciliation");

        string foreignFolder = Path.Combine(scratch, "optimize-foreign-machine");
        var foreignSeed = new Host { Command = new(5, "", "failed", false) };
        await VolumeOptimization.RunAsync(request, true, foreignFolder, foreignSeed);
        string foreignPath = Path.Combine(foreignFolder, "volume-optimization.pending.json");
        string foreignJson = await File.ReadAllTextAsync(foreignPath);
        await File.WriteAllTextAsync(foreignPath, foreignJson.Replace(Environment.MachineName, "OTHER-MACHINE",
            StringComparison.OrdinalIgnoreCase));
        var foreignHost = new Host();
        var foreign = await VolumeOptimization.ReconcileAsync(foreignFolder, foreignHost);
        Require(foreign.Outcome == VolumeOptimizationOutcome.Uncertain && foreignHost.Calls == 0 && File.Exists(foreignPath),
            "A foreign-machine optimization record remains guarded without inspecting or changing a volume");

        string recoverFolder = Path.Combine(scratch, "optimize-recover");
        var recoverHost = new Host { AfterAnalysis = new(0, "", "post analysis failure", false) };
        var uncertain = await VolumeOptimization.RunAsync(request, true, recoverFolder, recoverHost);
        Require(uncertain.Outcome == VolumeOptimizationOutcome.Uncertain, "Incomplete post-analysis retains pending state");
        recoverHost.AfterAnalysis = new(0, "complete", "", false);
        recoverHost.Analyses = 0;
        int optimizationCount = recoverHost.Optimizations;
        var recovered = await VolumeOptimization.ReconcileAsync(recoverFolder, recoverHost);
        Require(recovered.Outcome == VolumeOptimizationOutcome.Verified && recoverHost.Optimizations == optimizationCount &&
            !File.Exists(Path.Combine(recoverFolder, "volume-optimization.pending.json")),
            "Recovery verifies a completed command without repeating optimization");

        Console.WriteLine("PASS: exact-volume optimization preview, media eligibility, durable execution, final eligibility, bounded verification and no-repeat reconciliation; substituted provider only.");
    }

    private static VolumeOptimizationState Healthy(string volume = "D:", string identity = "volume-1",
        string fileSystem = "NTFS", string health = "Healthy", VolumeMediaKind media = VolumeMediaKind.SolidState) =>
        new(volume, identity, true, true, fileSystem, health, "OK", media);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Host : IVolumeOptimizationHost
    {
        public bool Administrator = true;
        public bool? FinalAdministrator;
        public bool IsAdministrator => Inspections > 1 && FinalAdministrator.HasValue
            ? FinalAdministrator.Value : Administrator;
        public VolumeOptimizationState State = Healthy();
        public VolumeOptimizationState? FinalState;
        public VolumeOptimizationState? AfterState;
        public Exception? FinalInspectionError;
        public ToolResult Analysis = new(0, "analysis", "", false);
        public ToolResult? FinalAnalysis;
        public ToolResult? AfterAnalysis;
        public Exception? FinalAnalysisError;
        public ToolResult Command = new(0, "optimized", "", false);
        public int Inspections, Analyses, Optimizations;
        public int Calls => Inspections + Analyses + Optimizations;
        public Task<VolumeOptimizationState> InspectAsync(string volume, CancellationToken token)
        {
            Inspections++;
            if (Inspections == 2 && FinalInspectionError is not null)
                return Task.FromException<VolumeOptimizationState>(FinalInspectionError);
            return Task.FromResult(Inspections == 1 ? State : Inspections == 2 ? FinalState ?? State : AfterState ?? FinalState ?? State);
        }
        public Task<ToolResult> AnalyzeAsync(string volume, IProgress<string>? progress, CancellationToken token)
        {
            Analyses++;
            if (Analyses == 2 && FinalAnalysisError is not null) return Task.FromException<ToolResult>(FinalAnalysisError);
            return Task.FromResult(Analyses == 1 ? Analysis : Analyses == 2 ? FinalAnalysis ?? Analysis : AfterAnalysis ?? FinalAnalysis ?? Analysis);
        }
        public Task<ToolResult> OptimizeAsync(string volume, VolumeOptimizationMode mode, IProgress<string>? progress, CancellationToken token)
        { Optimizations++; return Task.FromResult(Command); }
    }
}
