using System.IO;
using KillerBench.Engine;

internal static class SystemRestoreCreationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var request = SystemRestoreCreation.CreateRequest("Before maintenance", SystemRestorePointReason.ModifySettings);
        Require(request.Description == "Before maintenance" && SystemRestoreCreation.ReasonCode(request.Reason) == 12,
            "Restore-point requests normalize descriptions and retain exact reason codes");
        foreach (string invalid in new[] { "", " ", new string('x', 65), "bad\nname" })
            try { SystemRestoreCreation.CreateRequest(invalid, SystemRestorePointReason.ModifySettings); throw new Exception("Invalid description accepted"); }
            catch (ArgumentException) { }
        try { SystemRestoreCreation.CreateRequest("Valid", (SystemRestorePointReason)99); throw new Exception("Invalid reason accepted"); }
        catch (ArgumentOutOfRangeException) { }

        string previewFolder = Path.Combine(scratch, "restore-create-preview");
        var previewHost = new Host();
        var preview = await SystemRestoreCreation.RunAsync(request, false, previewFolder, previewHost);
        Require(preview.Outcome == SystemRestoreCreationOutcome.Preview && previewHost.Calls == 0 && !Directory.Exists(previewFolder),
            "Preview performs no inventory, creation or journal write");

        var reviewHost = new Host();
        var review = await SystemRestoreCreation.ReviewAsync(request, reviewHost);
        var reviewCheck = SystemRestoreCreation.ToWorkflowCheck(review);
        Require(review.Outcome == SystemRestoreCreationOutcome.Preview && reviewHost.Inspections == 1 && reviewHost.Creates == 0 &&
            reviewCheck.State == CheckState.Repairable && reviewCheck.Repair == RepairKind.SystemRestorePoint,
            "Complete client eligibility exposes explicit restore-point configuration without mutation");
        var unavailableReview = await SystemRestoreCreation.ReviewAsync(request, new Host { Before = Inventory(false) });
        var unavailableCheck = SystemRestoreCreation.ToWorkflowCheck(unavailableReview);
        Require(unavailableReview.Outcome == SystemRestoreCreationOutcome.Blocked && unavailableCheck.State == CheckState.Attention &&
            unavailableCheck.Repair is null,
            "Incomplete restore-point inventory cannot expose creation");

        foreach (var host in new[] { new Host { Administrator = false }, new Host { Before = Inventory(false) },
            new Host { SupportedClient = false }, new Host { CreationInterval = null },
            new Host { Before = Inventory(true, new SystemRestorePoint(8, "Recent", 12, DateTimeOffset.UtcNow.AddMinutes(-5))) } })
        {
            var blocked = await SystemRestoreCreation.RunAsync(request, true,
                Path.Combine(scratch, "restore-create-blocked-" + Guid.NewGuid().ToString("N")), host);
            Require(blocked.Outcome == SystemRestoreCreationOutcome.Blocked && host.Creates == 0,
                "Standard-user, server, unreadable policy, recent-point and incomplete inventory evidence block creation");
        }

        string verifiedFolder = Path.Combine(scratch, "restore-create-verified");
        var verifiedHost = new Host();
        var verified = await SystemRestoreCreation.RunAsync(request, true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == SystemRestoreCreationOutcome.Verified && verified.GetExitCode() == 0 &&
            verifiedHost.Inspections == 3 && verifiedHost.Creates == 1 &&
            File.Exists(verified.JournalPath) && File.Exists(verified.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(verifiedFolder, "system-restore-create.pending.json")) &&
            verified.Detail.Contains("not that a future restore will succeed"),
            "Creation requires durable preparation and one exact new inventory entry with a limited claim");

        string recentRaceFolder = Path.Combine(scratch, "restore-create-recent-race");
        var recentRaceHost = new Host
        {
            Immediate = Inventory(true, new SystemRestorePoint(9, "Other request", 12, DateTimeOffset.UtcNow.AddMinutes(-2)))
        };
        var recentRace = await SystemRestoreCreation.RunAsync(request, true, recentRaceFolder, recentRaceHost);
        Require(recentRace.Outcome == SystemRestoreCreationOutcome.Blocked && recentRaceHost.Inspections == 2 &&
            recentRaceHost.Creates == 0 && recentRace.After?.Points.Any(point => point.Sequence == 9) == true &&
            !File.Exists(Path.Combine(recentRaceFolder, "system-restore-create.pending.json")) &&
            File.Exists(recentRace.JournalPath + ".prepared.json") &&
            recentRace.Detail.Contains("No restore-point request", StringComparison.Ordinal),
            "A restore point created after preparation blocks a duplicate request and closes the guard");

        string unavailableRaceFolder = Path.Combine(scratch, "restore-create-unavailable-race");
        var unavailableRaceHost = new Host { ImmediateError = new IOException("inventory unavailable") };
        var unavailableRace = await SystemRestoreCreation.RunAsync(request, true, unavailableRaceFolder, unavailableRaceHost);
        Require(unavailableRace.Outcome == SystemRestoreCreationOutcome.Blocked && unavailableRaceHost.Inspections == 2 &&
            unavailableRaceHost.Creates == 0 &&
            !File.Exists(Path.Combine(unavailableRaceFolder, "system-restore-create.pending.json")) &&
            File.Exists(unavailableRace.JournalPath + ".prepared.json") &&
            unavailableRace.Detail.Contains("No restore-point request was sent", StringComparison.Ordinal),
            "Unavailable final eligibility sends no creation request and archives prepared no-write evidence");

        string canceledRaceFolder = Path.Combine(scratch, "restore-create-canceled-race");
        var canceledRaceHost = new Host { ImmediateError = new OperationCanceledException("inspection canceled") };
        var canceledRace = await SystemRestoreCreation.RunAsync(request, true, canceledRaceFolder, canceledRaceHost);
        Require(canceledRace.Outcome == SystemRestoreCreationOutcome.Stopped && canceledRaceHost.Inspections == 2 &&
            canceledRaceHost.Creates == 0 &&
            !File.Exists(Path.Combine(canceledRaceFolder, "system-restore-create.pending.json")) &&
            File.Exists(canceledRace.JournalPath + ".prepared.json") &&
            canceledRace.Detail.Contains("before any request was sent", StringComparison.Ordinal),
            "Canceled final eligibility sends no creation request and archives prepared no-write evidence");

        string failedFolder = Path.Combine(scratch, "restore-create-failed");
        var failedHost = new Host { Command = new(1, "", "provider failure", false) };
        var failed = await SystemRestoreCreation.RunAsync(request, true, failedFolder, failedHost);
        Require(failed.Outcome == SystemRestoreCreationOutcome.Failed &&
            File.Exists(Path.Combine(failedFolder, "system-restore-create.pending.json")),
            "Failed creation retains the no-repeat guard");
        int failedCreates = failedHost.Creates;
        var failedRecovery = await SystemRestoreCreation.ReconcileAsync(failedFolder, failedHost);
        Require(failedRecovery.Outcome == SystemRestoreCreationOutcome.Uncertain && failedHost.Creates == failedCreates,
            "An incomplete pending command is never repeated during reconciliation");

        string uncertainFolder = Path.Combine(scratch, "restore-create-uncertain");
        var uncertainHost = new Host { After = Inventory(true) };
        var uncertain = await SystemRestoreCreation.RunAsync(request, true, uncertainFolder, uncertainHost);
        Require(uncertain.Outcome == SystemRestoreCreationOutcome.Uncertain,
            "Successful command without an exact new point remains unverified");
        uncertainHost.After = Inventory(true, NewPoint(request));
        int beforeRecoveryCreates = uncertainHost.Creates;
        var recovered = await SystemRestoreCreation.ReconcileAsync(uncertainFolder, uncertainHost);
        Require(recovered.Outcome == SystemRestoreCreationOutcome.Verified && uncertainHost.Creates == beforeRecoveryCreates &&
            !File.Exists(Path.Combine(uncertainFolder, "system-restore-create.pending.json")),
            "Reconciliation verifies later evidence without repeating the request");

        string foreignFolder = Path.Combine(scratch, "restore-create-foreign");
        var foreignHost = new Host { After = Inventory(true) };
        var foreignAttempt = await SystemRestoreCreation.RunAsync(request, true, foreignFolder, foreignHost);
        string foreignPending = Path.Combine(foreignFolder, "system-restore-create.pending.json");
        Require(foreignAttempt.Outcome == SystemRestoreCreationOutcome.Uncertain && File.Exists(foreignPending),
            "Unverified creation retains a durable restore-point guard");
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        int foreignInspections = foreignHost.Inspections;
        var foreignRecovery = await SystemRestoreCreation.ReconcileAsync(foreignFolder, foreignHost);
        Require(foreignRecovery.Outcome == SystemRestoreCreationOutcome.Uncertain &&
            foreignHost.Inspections == foreignInspections && foreignHost.Creates == 1 && File.Exists(foreignPending),
            "A foreign-machine restore-point record remains guarded without inspecting or changing the host");

        var duplicateHost = new Host { After = Inventory(true, NewPoint(request, 2), NewPoint(request, 3)) };
        var duplicate = await SystemRestoreCreation.RunAsync(request, true,
            Path.Combine(scratch, "restore-create-duplicate"), duplicateHost);
        Require(duplicate.Outcome == SystemRestoreCreationOutcome.Uncertain,
            "Multiple matching new points do not produce a false exact verification");

        Console.WriteLine("PASS: restore-point preview, repeated eligibility, durable creation, exact verification and no-repeat reconciliation; substituted provider only.");
    }

    private static SystemRestorePoint NewPoint(SystemRestoreCreationRequest request, uint sequence = 2) =>
        new(sequence, request.Description, SystemRestoreCreation.ReasonCode(request.Reason), DateTimeOffset.UtcNow.AddSeconds(1));
    private static SystemRestoreInventory Inventory(bool complete, params SystemRestorePoint[] additional) =>
        new(complete, new[] { new SystemRestorePoint(1, "Existing", 12, DateTimeOffset.UtcNow.AddDays(-2)) }.Concat(additional).ToArray());
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Host : ISystemRestoreCreationHost
    {
        public bool Administrator = true;
        public bool IsAdministrator => Administrator;
        public bool? SupportedClient = true;
        public bool? IsSupportedClientOperatingSystem => SupportedClient;
        public TimeSpan? CreationInterval = TimeSpan.FromHours(24);
        public TimeSpan? MinimumCreationInterval => CreationInterval;
        public SystemRestoreInventory Before = Inventory(true);
        public SystemRestoreInventory Immediate = Inventory(true);
        public Exception? ImmediateError;
        public SystemRestoreInventory? After;
        public ToolResult Command = new(0, "created", "", false);
        public int Inspections;
        public int Creates;
        public int Calls => Inspections + Creates;
        public Task<SystemRestoreInventory> InspectAsync(CancellationToken token)
        {
            Inspections++;
            if (Inspections == 2 && ImmediateError is not null) return Task.FromException<SystemRestoreInventory>(ImmediateError);
            return Task.FromResult(Inspections switch
            {
                1 => Before,
                2 => Immediate,
                _ => After ?? Inventory(true, NewPoint(new("Before maintenance", SystemRestorePointReason.ModifySettings)))
            });
        }
        public Task<ToolResult> CreateAsync(SystemRestoreCreationRequest request, IProgress<string>? progress, CancellationToken token)
        { Creates++; return Task.FromResult(Command); }
    }
}
