using KillerBench.Engine;
using System.Text.Json;

internal static class ProfileCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        int days = 90;
        bool dependencies = false;
        bool savePlan = false;
        bool execute = false;
        string? verifyBackup = null;
        string? createBackupArtifact = null;
        string? backupTo = null;
        bool recover = args.Length == 2 && args[1] == "--recover";
        bool recoverBackup = args.Length == 2 && args[1] == "--recover-backup";
        var selected = new List<string>();
        for (int i = 1; i < args.Length; i++)
        {
            if (recover || recoverBackup) break;
            if (args[i] == "--dependencies") { dependencies = true; continue; }
            if (args[i] == "--save-plan") { savePlan = true; continue; }
            if (args[i] == "--execute") { execute = true; continue; }
            if (args[i] == "--verify-backup" && i + 1 < args.Length) { verifyBackup = args[++i]; continue; }
            if (args[i] == "--create-backup-artifact" && i + 1 < args.Length) { createBackupArtifact = args[++i]; continue; }
            if (args[i] == "--backup-to" && i + 1 < args.Length) { backupTo = args[++i]; dependencies = true; continue; }
            if (args[i] == "--select" && i + 1 < args.Length) { selected.Add(args[++i]); dependencies = true; continue; }
            if (args[i] != "--days" || ++i >= args.Length || !int.TryParse(args[i], out days) || days is < 1 or > 3650)
            { Console.Error.WriteLine("profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT|--create-backup-artifact DESTINATION|--backup-to NEW_DESTINATION [--execute]], profiles --recover, or profiles --recover-backup. Backup operations require exactly one selected SID."); return 2; }
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (recover)
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Profiles");
                bool resolved = await ProfileRemovalRecovery.TryResolveAsync(folder, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { Resolved = resolved, Note = resolved
                    ? "No unresolved removal remains. Existing recovery evidence was retained. No profiles removed."
                    : "Removal remains unresolved. Run elevated and review inventory, registry and folder evidence. No deletion retried." }, Capture.JsonOptions));
                return resolved ? 0 : 3;
            }
            if (recoverBackup)
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Profiles");
                var resolved = await ProfileBackupRecovery.TryResolveAsync(folder, token: stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    resolved.Verified,
                    resolved.Detail,
                    resolved.Receipt?.CompletedUtc,
                    resolved.Receipt?.FileCount,
                    resolved.Receipt?.TotalBytes,
                    resolved.Receipt?.ManifestSha256,
                    Note = resolved.Verified
                        ? "The interrupted backup artifact was verified and its pending record was archived. No copy or removal was retried."
                        : "The backup remains unresolved. No copy or removal was retried."
                }, Capture.JsonOptions));
                return resolved.Verified ? 0 : 3;
            }
            if (savePlan && selected.Count == 0) throw new ArgumentException("--save-plan requires at least one --select SID.");
            if (verifyBackup is not null && selected.Count != 1) throw new ArgumentException("--verify-backup requires exactly one --select SID.");
            if (createBackupArtifact is not null && selected.Count != 1) throw new ArgumentException("--create-backup-artifact requires exactly one --select SID.");
            if (backupTo is not null && selected.Count != 1) throw new ArgumentException("--backup-to requires exactly one --select SID.");
            if (execute && backupTo is null) throw new ArgumentException("--execute requires --backup-to NEW_DESTINATION.");
            if (new[] { verifyBackup, createBackupArtifact, backupTo }.Count(value => value is not null) > 1)
                throw new ArgumentException("--verify-backup, --create-backup-artifact and --backup-to cannot be combined.");
            if (selected.Count > 0) ProfilePlanning.ValidateSelection(selected);
            var inventory = await ProfileInventory.ReadAsync(stop.Token);
            var registrations = await ProfileRegistrations.ReadAsync(stop.Token);
            var registrationCheck = ProfileRegistrations.Check(registrations);
            var choices = ProfilePlanning.Review(inventory.Profiles, days, DateTimeOffset.UtcNow, inventory.Complete, stop.Token);
            var dependencyResult = dependencies ? await ProfileDependencies.ReadAsync(stop.Token) : null;
            var plan = selected.Count > 0 ? ProfilePlanning.Select(inventory, dependencyResult!, registrations, selected, days, DateTimeOffset.UtcNow, stop.Token) : null;
            string? journal = savePlan ? await ProfileReview.SavePlanAsync(plan!, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Profiles"), days, stop.Token) : null;
            ProfileBackupVerification? backupVerification = null;
            if (verifyBackup is not null)
                backupVerification = plan?.Targets.Count == 1
                    ? await ProfileBackupArtifactVerifier.VerifyAsync(plan.Targets[0], verifyBackup, token: stop.Token)
                    : new(null, false, "The selected profile identity was not available for backup verification.");
            if (createBackupArtifact is not null)
                backupVerification = plan?.Targets.Count == 1
                    ? await ProfileBackupArtifactWriter.CreateForExistingCopyAsync(plan.Targets[0], createBackupArtifact, token: stop.Token)
                    : new(null, false, "The selected profile identity was not available for backup artifact creation.");
            ProfileBackupExecutionResult? backupExecution = null;
            if (backupTo is not null && execute)
            {
                if (plan?.Targets.Count != 1) throw new InvalidOperationException("The selected profile identity is not eligible for backup execution.");
                string operationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Profiles");
                var progress = new Progress<string>(message => Console.Error.WriteLine(message));
                backupExecution = await ProfileBackupExecution.RunAsync(plan.Targets[0], days, backupTo, operationFolder,
                    new WindowsProfileBackupHost(), progress, token: stop.Token);
                backupVerification = backupExecution.Verification;
            }
            var backupSummary = backupVerification is null ? null : new
            {
                backupVerification.Verified,
                backupVerification.Detail,
                backupVerification.Receipt?.CompletedUtc,
                backupVerification.Receipt?.FileCount,
                backupVerification.Receipt?.TotalBytes,
                backupVerification.Receipt?.ManifestSha256
            };
            Console.WriteLine(JsonSerializer.Serialize(new { inventory.Complete, MinimumDays = days, Profiles = choices, Registrations = registrations, RegistrationCheck = registrationCheck, Dependencies = dependencyResult, Plan = plan, Journal = journal,
                BackupDestination = backupTo, BackupExecution = backupExecution, BackupVerification = backupSummary,
                Note = backupExecution?.Status == "Backup verified"
                    ? "The selected unloaded profile was copied into a new local destination and its complete regular-file set and contents were verified. No profile was removed. Permissions, alternate streams, encrypted-file state, application state and restore behavior remain unproven."
                    : backupExecution is not null
                    ? "The profile backup did not reach verified completion. The destination and recovery evidence were preserved, and no profile was removed. Do not retry until the pending backup is reconciled."
                    : backupTo is not null
                    ? "Preview only. Fresh profile, dependency and registration evidence was reviewed. Add --execute to request one guarded copy into an absent local NTFS destination. No profile was copied or removed."
                    : backupVerification?.Verified == true && createBackupArtifact is not null
                    ? "The existing copied backup was compared with the selected source and its fixed artifact was created. No profile was copied or removed. Metadata, application-state and restore behavior remain unproven."
                    : backupVerification?.Verified == true
                    ? "Read-only review and source-to-backup verification completed. No profile was removed. Verification does not authorize removal or prove metadata, application-state or restore behavior."
                    : createBackupArtifact is not null
                    ? "Backup artifact creation did not verify the existing copy. No profile was copied or removed. Review the returned evidence and preserve any existing copy."
                    : "Read-only review. No profiles removed. Registration, dependency and unverified backup evidence do not authorize deletion." }, Capture.JsonOptions));
            return inventory.Complete && registrationCheck.State == CheckState.Information && dependencyResult?.Complete != false &&
                plan?.ReviewPassed != false && backupVerification?.Verified != false &&
                backupExecution?.Status is not ("Blocked" or "Needs review" or "Stopped") ? 0 : 3;
        }
        catch (OperationCanceledException) { return 130; }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Console.Error.WriteLine("Profile review or save could not complete: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
