using KillerBench.Engine;
using System.IO;

internal static class ProfileRemovalChecks
{
    internal static async Task RunAsync(string scratch)
    {
        if (ProfileAbsence.FolderPresence(scratch) != true ||
            ProfileAbsence.FolderPresence(Path.Combine(scratch, "missing-profile")) != false ||
            ProfileAbsence.FolderPresence(Path.Combine(scratch, "missing-parent", "profile")) is not null ||
            ProfileAbsence.FolderPresence("relative-profile") is not null) throw new Exception("Profile filesystem absence evidence mismatch");
        string junctionTarget = Path.Combine(scratch, "profile-junction-target");
        string junction = Path.Combine(scratch, "profile-junction");
        Directory.CreateDirectory(junctionTarget);
        var start = new System.Diagnostics.ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-EncodedCommand");
        start.ArgumentList.Add(Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(
            "$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path $env:KB_TEST_LINK -Target $env:KB_TEST_TARGET | Out-Null")));
        start.Environment["KB_TEST_LINK"] = junction;
        start.Environment["KB_TEST_TARGET"] = junctionTarget;
        using (var process = System.Diagnostics.Process.Start(start) ?? throw new Exception("Junction fixture did not start"))
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try { await process.WaitForExitAsync(deadline.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
            if (process.ExitCode != 0) throw new Exception("Junction fixture failed");
        }
        if ((File.GetAttributes(junction) & FileAttributes.ReparsePoint) == 0 ||
            ProfileAbsence.FolderPresence(Path.Combine(junction, "missing-profile")) is not null)
            throw new Exception("Redirected parent was accepted as proof of profile absence");
        string sourceFolder = Path.Combine(scratch, "profile-backup-source");
        string sourceData = Path.Combine(sourceFolder, "Documents");
        Directory.CreateDirectory(sourceData);
        string firstSourceFile = Path.Combine(sourceData, "first.txt");
        string secondSourceFile = Path.Combine(sourceData, "second.bin");
        await File.WriteAllTextAsync(firstSourceFile, "verified profile content");
        await File.WriteAllBytesAsync(secondSourceFile, [0, 1, 2, 3, 4]);
        var backupTarget = new ProfileSnapshot("S-1-5-21-1-2-3-1001", sourceFolder, false, false, DateTimeOffset.UtcNow.AddDays(-120), false);
        var backup = new ProfileBackupReceipt(backupTarget.Sid, backupTarget.Path, @"D:\KillerBenchBackups\Fixture",
            DateTimeOffset.UtcNow.AddMinutes(-1), 12, 4096, new string('a', 64), true);
        if (!ProfileBackupEvidence.Matches(backupTarget, backup, DateTimeOffset.UtcNow))
            throw new Exception("Exact fresh profile backup evidence was rejected");
        foreach (var invalid in new[]
        {
            backup with { Verified = false },
            backup with { SourceSid = "S-1-5-21-1-2-3-9999" },
            backup with { SourcePath = @"C:\Users\Other" },
            backup with { DestinationPath = Path.Combine(sourceFolder, "Backup") },
            backup with { DestinationPath = Path.GetDirectoryName(sourceFolder)! },
            backup with { DestinationPath = @"\\server\share\Fixture" },
            backup with { CompletedUtc = DateTimeOffset.UtcNow.AddHours(-25) },
            backup with { CompletedUtc = DateTimeOffset.UtcNow.AddMinutes(1) },
            backup with { FileCount = -1 },
            backup with { TotalBytes = -1 },
            backup with { ManifestSha256 = "not-a-digest" }
        })
            if (ProfileBackupEvidence.Matches(backupTarget, invalid, DateTimeOffset.UtcNow))
                throw new Exception("Mismatched profile backup evidence was accepted");

        string artifactFolder = Path.Combine(scratch, "profile-backup-artifact");
        string artifactData = Path.Combine(artifactFolder, "Documents");
        Directory.CreateDirectory(artifactData);
        string firstBackupFile = Path.Combine(artifactData, "first.txt");
        string secondBackupFile = Path.Combine(artifactData, "second.bin");
        await File.WriteAllTextAsync(firstBackupFile, "verified profile content");
        await File.WriteAllBytesAsync(secondBackupFile, [0, 1, 2, 3, 4]);
        var originalEntries = new[]
        {
            Entry("Documents\\first.txt", firstBackupFile),
            Entry("Documents\\second.bin", secondBackupFile)
        };
        string artifactReceipt = Path.Combine(artifactFolder, ProfileBackupArtifactVerifier.ReceiptFileName);
        void WriteArtifact(IReadOnlyList<ProfileBackupManifestEntry> entries, long? fileCount = null, long? totalBytes = null)
        {
            var compactJson = new System.Text.Json.JsonSerializerOptions(Capture.JsonOptions) { WriteIndented = false };
            string manifest = string.Join("\n", entries.Select(entry => System.Text.Json.JsonSerializer.Serialize(entry, compactJson))) + "\n";
            string manifestPath = Path.Combine(artifactFolder, ProfileBackupArtifactVerifier.ManifestFileName);
            File.WriteAllText(manifestPath, manifest, new System.Text.UTF8Encoding(false));
            string manifestDigest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(manifestPath)));
            var artifact = new ProfileBackupArtifact(1, backupTarget.Sid, backupTarget.Path, artifactFolder,
                DateTimeOffset.UtcNow.AddMinutes(-1), fileCount ?? entries.Count, totalBytes ?? entries.Sum(entry => entry.Length), manifestDigest);
            File.WriteAllText(artifactReceipt, System.Text.Json.JsonSerializer.Serialize(artifact, Capture.JsonOptions), new System.Text.UTF8Encoding(false));
        }
        var createdArtifact = await ProfileBackupArtifactWriter.CreateForExistingCopyAsync(backupTarget, artifactFolder,
            DateTimeOffset.UtcNow.AddMinutes(-1));
        if (!createdArtifact.Verified || createdArtifact.Receipt is not { FileCount: 2, TotalBytes: 29 })
            throw new Exception("Profile backup artifact creation failed: " + createdArtifact.Detail + " Receipt: " +
                System.Text.Json.JsonSerializer.Serialize(createdArtifact.Receipt));
        var overwriteArtifact = await ProfileBackupArtifactWriter.CreateForExistingCopyAsync(backupTarget, artifactFolder);
        if (overwriteArtifact.Verified)
            throw new Exception("Profile backup artifact creation overwrote an existing fixed artifact");
        var verifiedArtifact = await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt);
        if (!verifiedArtifact.Verified || verifiedArtifact.Receipt is not { FileCount: 2, TotalBytes: 29 } ||
            !verifiedArtifact.Detail.Contains("does not prove ACL", StringComparison.Ordinal))
            throw new Exception("Exact profile backup artifact was not verified with its limitations: " +
                verifiedArtifact.Detail + " Receipt: " + System.Text.Json.JsonSerializer.Serialize(verifiedArtifact.Receipt));
        await File.AppendAllTextAsync(firstBackupFile, "changed");
        if ((await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt)).Verified)
            throw new Exception("Changed profile backup content was accepted");
        await File.WriteAllTextAsync(firstBackupFile, "verified profile content");
        await File.AppendAllTextAsync(firstSourceFile, "changed");
        if ((await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt)).Verified)
            throw new Exception("Changed source profile content was accepted");
        await File.WriteAllTextAsync(firstSourceFile, "verified profile content");
        string extraSourceFile = Path.Combine(sourceData, "source-only.txt");
        await File.WriteAllTextAsync(extraSourceFile, "missing backup content");
        if ((await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt)).Verified)
            throw new Exception("A source file missing from the backup was accepted");
        File.Delete(extraSourceFile);
        string extraBackupFile = Path.Combine(artifactData, "backup-only.txt");
        await File.WriteAllTextAsync(extraBackupFile, "unexpected backup content");
        if ((await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt)).Verified)
            throw new Exception("An unmanifested backup file was accepted");
        File.Delete(extraBackupFile);
        WriteArtifact([new("..\\outside.txt", 1, new string('a', 64))], 1, 1);
        if ((await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt)).Verified)
            throw new Exception("Profile backup manifest traversal was accepted");
        WriteArtifact(originalEntries, 3);
        if ((await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt)).Verified)
            throw new Exception("Profile backup receipt totals were trusted over the manifest");
        WriteArtifact(originalEntries);
        using (var canceledArtifact = new CancellationTokenSource())
        {
            canceledArtifact.Cancel();
            try
            {
                await ProfileBackupArtifactVerifier.VerifyAsync(backupTarget, artifactReceipt, token: canceledArtifact.Token);
                throw new Exception("Canceled profile backup verification completed");
            }
            catch (OperationCanceledException) { }
        }
        string canceledArtifactFolder = Path.Combine(scratch, "profile-backup-canceled");
        Directory.CreateDirectory(canceledArtifactFolder);
        using (var canceledWriter = new CancellationTokenSource())
        {
            canceledWriter.Cancel();
            try
            {
                await ProfileBackupArtifactWriter.CreateForExistingCopyAsync(backupTarget, canceledArtifactFolder,
                    token: canceledWriter.Token);
                throw new Exception("Canceled profile backup artifact creation completed");
            }
            catch (OperationCanceledException) { }
        }
        if (File.Exists(Path.Combine(canceledArtifactFolder, ProfileBackupArtifactVerifier.ManifestFileName)) ||
            File.Exists(Path.Combine(canceledArtifactFolder, ProfileBackupArtifactVerifier.ReceiptFileName)))
            throw new Exception("Canceled profile backup artifact creation published a fixed artifact");

        foreach (string mode in new[] { "success", "final-loaded", "incomplete", "destination-exists" })
        {
            string backupFolder = Path.Combine(scratch, "profile-backup-execution-" + mode);
            string destination = Path.Combine(scratch, "profile-backup-copy-" + mode);
            if (mode == "destination-exists") Directory.CreateDirectory(destination);
            using var backupStop = new CancellationTokenSource();
            var backupHost = new BackupFixture(mode, backupTarget, sourceFolder, backupStop);
            var updates = new ProgressFixture();
            var result = await ProfileBackupExecution.RunAsync(backupTarget, 90, destination, backupFolder,
                backupHost, updates, TimeSpan.FromSeconds(10), token: backupStop.Token);
            string expected = mode == "success" ? "Backup verified" : mode is "final-loaded" or "destination-exists" ? "Blocked" : "Needs review";
            int expectedCopies = mode is "success" or "incomplete" ? 1 : 0;
            if (result.Status != expected || backupHost.CopyCalls != expectedCopies ||
                File.Exists(Path.Combine(backupFolder, "pending-backup.json")) != (mode == "incomplete") ||
                (mode == "success" && (result.Verification?.Verified != true || !updates.Messages.Contains("Backup verified"))))
                throw new Exception("Guarded profile backup execution outcome mismatch: " + mode);
        }

        string recoveryFolder = Path.Combine(scratch, "profile-backup-execution-recovery");
        string recoveryDestination = Path.Combine(scratch, "profile-backup-copy-recovery");
        using (var recoveryStop = new CancellationTokenSource())
        {
            var recoveryHost = new BackupFixture("cancel", backupTarget, sourceFolder, recoveryStop);
            var stopped = await ProfileBackupExecution.RunAsync(backupTarget, 90, recoveryDestination, recoveryFolder,
                recoveryHost, token: recoveryStop.Token, operationLimit: TimeSpan.FromSeconds(10));
            if (stopped.Status != "Stopped" || recoveryHost.CopyCalls != 1 ||
                !File.Exists(Path.Combine(recoveryFolder, "pending-backup.json")))
                throw new Exception("Interrupted profile backup lost its recovery guard");
            var completedArtifact = await ProfileBackupArtifactWriter.CreateForExistingCopyAsync(backupTarget, recoveryDestination);
            if (!completedArtifact.Verified) throw new Exception("Interrupted profile backup fixture could not be completed");
            var recovered = await ProfileBackupRecovery.TryResolveAsync(recoveryFolder, verificationLimit: TimeSpan.FromSeconds(10));
            if (!recovered.Verified || recoveryHost.CopyCalls != 1 ||
                File.Exists(Path.Combine(recoveryFolder, "pending-backup.json")) ||
                Directory.GetFiles(recoveryFolder, "resolved-backup-*.json").Length != 1)
                throw new Exception("Profile backup recovery failed or repeated the copy");
        }
        foreach (string stage in new[] { "ready-stall", "backup-stall", "verify-stall" })
        {
            using var stop = new CancellationTokenSource();
            var host = new Fixture(stage, stop) { Folder = Path.Combine(scratch, "profile-" + stage) };
            var result = await ProfileRemoval.RunAsync(host.Target, 90, host.Folder, host, stop.Token).WaitAsync(TimeSpan.FromSeconds(3));
            bool prepared = stage == "verify-stall";
            if (result.Status != "Stopped" || host.Removed != prepared ||
                File.Exists(Path.Combine(host.Folder, "pending-removal.json")) != prepared ||
                !File.ReadAllText(result.JournalPath).Contains("Stopped; verification incomplete"))
                throw new Exception("Stalled profile stage lost cancellation or recovery state: " + stage);
            if (prepared)
            {
                string marker = Path.Combine(host.Folder, "pending-removal.json");
                string original = File.ReadAllText(marker);
                foreach (string missingStage in new[] { "ready-missing", "verify-missing", "final-ready-missing", "final-ready-false" })
                {
                    using var missingStop = new CancellationTokenSource();
                    var missingHost = new Fixture(missingStage, missingStop) { Folder = host.Folder };
                    if (await ProfileRemovalRecovery.TryResolveAsync(host.Folder, missingHost).WaitAsync(TimeSpan.FromSeconds(3)) ||
                        File.ReadAllText(marker) != original || missingHost.RemoveCalls != 0)
                        throw new Exception("Missing recovery operation cleared evidence or retried removal: " + missingStage);
                }
                foreach (string recoveryStage in new[] { "ready-stall", "verify-stall" })
                {
                    using var timeoutStop = new CancellationTokenSource();
                    var timeoutHost = new Fixture(recoveryStage, timeoutStop) { Folder = host.Folder, HoldWithoutCancel = true };
                    if (await ProfileRemovalRecovery.TryResolveAsync(host.Folder, timeoutHost, verificationLimit: TimeSpan.FromMilliseconds(25))
                        .WaitAsync(TimeSpan.FromSeconds(3))) throw new Exception("Timed out recovery claimed verified absence");
                    if (File.ReadAllText(marker) != original || timeoutHost.RemoveCalls != 0)
                        throw new Exception("Timed out recovery changed evidence or retried removal");
                    using var recoveryStop = new CancellationTokenSource();
                    var recoveryHost = new Fixture(recoveryStage, recoveryStop) { Folder = host.Folder };
                    try
                    {
                        await ProfileRemovalRecovery.TryResolveAsync(host.Folder, recoveryHost, token: recoveryStop.Token).WaitAsync(TimeSpan.FromSeconds(3));
                        throw new Exception("Stalled recovery ignored cancellation");
                    }
                    catch (OperationCanceledException) { }
                    if (File.ReadAllText(marker) != original || recoveryHost.RemoveCalls != 0)
                        throw new Exception("Canceled recovery changed prepared evidence or retried removal");
                }
            }
        }
        foreach (var mode in new[] { "ready-missing", "backup-missing", "remove-missing", "verify-missing" })
        {
            using var missingStop = new CancellationTokenSource();
            var missing = new Fixture(mode, missingStop) { Folder = Path.Combine(scratch, mode) };
            var result = await ProfileRemoval.RunAsync(missing.Target, 90, missing.Folder, missing).WaitAsync(TimeSpan.FromSeconds(3));
            bool dispatched = mode is "remove-missing" or "verify-missing";
            if (result.Status != "Needs review" || missing.RemoveCalls != (dispatched ? 1 : 0) ||
                File.Exists(Path.Combine(missing.Folder, "pending-removal.json")) != dispatched ||
                !File.ReadAllText(result.JournalPath).Contains("Failed; verification incomplete"))
                throw new Exception("Missing profile provider operation lost failure or recovery evidence: " + mode);
        }
        foreach (var mode in new[]
        {
            "removal-final-ready-false", "removal-final-ready-missing", "removal-final-loaded",
            "removal-final-dependency", "removal-final-registration", "removal-final-backup-changed",
            "removal-final-backup-missing"
        })
        {
            using var finalStop = new CancellationTokenSource();
            var finalHost = new Fixture(mode, finalStop) { Folder = Path.Combine(scratch, "profile-" + mode) };
            var result = await ProfileRemoval.RunAsync(finalHost.Target, 90, finalHost.Folder, finalHost, finalStop.Token)
                .WaitAsync(TimeSpan.FromSeconds(3));
            if (result.Status != "Blocked" || finalHost.RemoveCalls != 0 || finalHost.Removed ||
                File.Exists(Path.Combine(finalHost.Folder, "pending-removal.json")) ||
                Directory.GetFiles(finalHost.Folder, "*.prepared.json").Length != 1)
                throw new Exception("Changed final profile eligibility reached removal or lost prepared evidence: " + mode);
            string journalText = File.ReadAllText(result.JournalPath);
            bool unavailable = mode is "removal-final-ready-missing" or "removal-final-backup-missing";
            if (!journalText.Contains(unavailable ? "Final eligibility unavailable; no change requested" : "Final eligibility blocked"))
                throw new Exception("Changed final profile eligibility was not journaled: " + mode);
        }
        foreach (var mode in new[] { "success", "backup", "changed", "verify", "cancel" })
        {
            using var stop = new CancellationTokenSource();
            var host = new Fixture(mode, stop);
            if (!ProfileAbsence.Evaluate(host.Target, new([], true), false, false) ||
                ProfileAbsence.Evaluate(host.Target, new([host.Target], true), false, false) ||
                ProfileAbsence.Evaluate(host.Target, new([], false), false, false) ||
                ProfileAbsence.Evaluate(host.Target, new([], true), null, false) ||
                ProfileAbsence.Evaluate(host.Target, new([], true), false, true)) throw new Exception("Profile absence requires complete agreement");
            string folder = Path.Combine(scratch, "profile-removal-" + mode);
            host.Folder = folder;
            host.CheckRecoveryLock = mode == "success";
            var result = await ProfileRemoval.RunAsync(host.Target, 90, folder, host, stop.Token);
            bool blocked = mode is "backup" or "changed";
            if (host.Removed != !blocked || result.Status != (blocked ? "Blocked" : mode == "success" ? "Removal verified" : mode == "cancel" ? "Stopped" : "Needs review")) throw new Exception("Profile removal outcome mismatch: " + mode);
            if (File.Exists(Path.Combine(folder, "pending-removal.json")) != (mode is "verify" or "cancel")) throw new Exception("Profile removal guard mismatch");
            if (!File.ReadAllText(result.JournalPath).Contains("Started")) throw new Exception("Profile removal journal missing");
            if (mode is "verify" or "cancel")
            {
                int calls = host.RemoveCalls;
                try { await ProfileRemoval.RunAsync(host.Target, 90, folder, host); throw new Exception("Unresolved removal allowed retry"); }
                catch (InvalidOperationException) { }
                if (host.RemoveCalls != calls) throw new Exception("Retry reached removal provider");
                if (mode == "verify" && await ProfileRemovalRecovery.TryResolveAsync(folder, host)) throw new Exception("Unverified absence cleared guard");
                var verifiedHost = new Fixture("success", stop) { Target = host.Target, Folder = folder, CheckRecoveryLock = true };
                string pending = Path.Combine(folder, "pending-removal.json");
                string original = File.ReadAllText(pending);
                foreach (string invalidPath in new[] { @"C:\", @"C:\Users\..\Windows", @"\\server\profiles\user", @"C:\Users\Fixture:stream" })
                {
                    string record = System.Text.Json.JsonSerializer.Serialize(new { Target = host.Target with { Path = invalidPath } });
                    File.WriteAllText(pending, record);
                    if (await ProfileRemovalRecovery.TryResolveAsync(folder, verifiedHost) || File.ReadAllText(pending) != record)
                        throw new Exception("Recovery accepted an ambiguous target path");
                }
                foreach (string malformed in new[] { "{", "{}", "[]", "{\"Target\":null}", "{\"Target\":{},\"Target\":{}}", new string(' ', 65537) })
                {
                    File.WriteAllText(pending, malformed);
                    if (await ProfileRemovalRecovery.TryResolveAsync(folder, verifiedHost) || File.ReadAllText(pending) != malformed)
                        throw new Exception("Malformed recovery evidence changed or cleared");
                }
                string foreign = original.Replace(Environment.MachineName, "OTHER-MACHINE", StringComparison.Ordinal);
                File.WriteAllText(pending, foreign);
                if (await ProfileRemovalRecovery.TryResolveAsync(folder, verifiedHost) || File.ReadAllText(pending) != foreign)
                    throw new Exception("Foreign-machine profile recovery evidence was verified, archived or changed");
                File.WriteAllText(pending, original);
                try { await ProfileRemovalRecovery.TryResolveAsync(folder, verifiedHost, token: new CancellationToken(true)); throw new Exception("Canceled recovery succeeded"); }
                catch (OperationCanceledException) { }
                if (File.ReadAllText(pending) != original) throw new Exception("Canceled recovery changed evidence");
                if (!await ProfileRemovalRecovery.TryResolveAsync(folder, verifiedHost) || verifiedHost.RemoveCalls != 0 || File.Exists(Path.Combine(folder, "pending-removal.json")))
                    throw new Exception("Verified recovery failed or retried deletion");
                if (Directory.GetFiles(folder, "resolved-absent-*.json").Length != 1) throw new Exception("Recovery discarded prepared evidence");
                if (File.ReadAllText(Directory.GetFiles(folder, "resolved-absent-*.json").Single()) != original)
                    throw new Exception("Archived recovery evidence differs from the verified record");
            }
        }
    }

    private static ProfileBackupManifestEntry Entry(string relativePath, string filePath)
    {
        byte[] content = File.ReadAllBytes(filePath);
        return new(relativePath, content.LongLength, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)));
    }

    private sealed class ProgressFixture : IProgress<string>
    {
        internal List<string> Messages { get; } = [];
        public void Report(string value) => Messages.Add(value);
    }

    private sealed class BackupFixture(string mode, ProfileSnapshot target, string sourceFolder,
        CancellationTokenSource stop) : IProfileBackupHost
    {
        internal int CopyCalls;
        private int inventoryReads;

        public Task<bool> ReadyAsync(CancellationToken token) => Task.FromResult(true);

        public Task<ProfileInventoryResult> InventoryAsync(CancellationToken token)
        {
            inventoryReads++;
            bool changed = mode == "final-loaded" && inventoryReads == 2;
            return Task.FromResult(new ProfileInventoryResult([changed ? target with { Loaded = true } : target], true));
        }

        public Task<ProfileDependencyResult> DependenciesAsync(CancellationToken token) =>
            Task.FromResult(new ProfileDependencyResult([], true));

        public Task<ProfileRegistrationResult> RegistrationsAsync(CancellationToken token) =>
            Task.FromResult(new ProfileRegistrationResult([new(target.Sid, target.Path, target.Sid, false, 0, 0)], true));

        public Task<bool> DestinationAbsentAsync(string destinationPath, CancellationToken token) =>
            Task.FromResult(mode != "destination-exists" && !Directory.Exists(destinationPath));

        public Task CopyAsync(ProfileSnapshot selected, string destinationPath, IProgress<string>? progress,
            CancellationToken token)
        {
            CopyCalls++;
            progress?.Report("Copying fixture files");
            foreach (string sourceFile in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(sourceFolder, sourceFile);
                if (mode == "incomplete" && relative.EndsWith("second.bin", StringComparison.OrdinalIgnoreCase)) continue;
                string destinationFile = Path.Combine(destinationPath, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Copy(sourceFile, destinationFile, overwrite: false);
            }
            if (mode == "cancel")
            {
                stop.Cancel();
                return new TaskCompletionSource().Task;
            }
            return Task.CompletedTask;
        }
    }

    private sealed class Fixture(string mode, CancellationTokenSource stop) : IProfileRemovalHost
    {
        internal ProfileSnapshot Target = new("S-1-5-21-1-2-3-1001", @"C:\Users\Fixture", false, false, DateTimeOffset.UtcNow.AddDays(-120), false);
        internal bool Removed;
        internal string Folder = "";
        internal int RemoveCalls;
        internal bool CheckRecoveryLock;
        internal bool HoldWithoutCancel;
        private int reads;
        private int readinessReads;
        private int dependencyReads;
        private int registrationReads;
        private int backupReads;
        private readonly DateTimeOffset backupCompletedUtc = DateTimeOffset.UtcNow;
        private Task<bool> Stall()
        {
            if (!HoldWithoutCancel) stop.Cancel();
            return new TaskCompletionSource<bool>().Task;
        }
        private Task<ProfileBackupReceipt> StallBackup()
        {
            if (!HoldWithoutCancel) stop.Cancel();
            return new TaskCompletionSource<ProfileBackupReceipt>().Task;
        }
        public Task<bool> ReadyAsync(CancellationToken token)
        {
            readinessReads++;
            if (readinessReads == 2 && mode == "final-ready-missing") return null!;
            if (readinessReads == 2 && mode == "final-ready-false") return Task.FromResult(false);
            if (readinessReads == 3 && mode == "removal-final-ready-missing") return null!;
            if (readinessReads == 3 && mode == "removal-final-ready-false") return Task.FromResult(false);
            return mode == "ready-missing" ? null! : mode == "ready-stall" ? Stall() : Task.FromResult(true);
        }
        public Task<ProfileInventoryResult> InventoryAsync(CancellationToken token)
        {
            reads++;
            bool changed = mode == "changed" && reads > 1 || mode == "removal-final-loaded" && reads == 3;
            return Task.FromResult(new ProfileInventoryResult([changed ? Target with { Loaded = true } : Target], true));
        }
        public Task<ProfileDependencyResult> DependenciesAsync(CancellationToken token)
        {
            dependencyReads++;
            IReadOnlyList<ProfileDependency> items = mode == "removal-final-dependency" && dependencyReads == 3
                ? [new("Service", "Fixture service", Target.Sid)] : [];
            return Task.FromResult(new ProfileDependencyResult(items, true));
        }
        public Task<ProfileRegistrationResult> RegistrationsAsync(CancellationToken token)
        {
            registrationReads++;
            uint state = mode == "removal-final-registration" && registrationReads == 3 ? 1u : 0u;
            return Task.FromResult(new ProfileRegistrationResult(
                [new(Target.Sid, Target.Path, Target.Sid, false, state, 0)], true));
        }
        public Task<ProfileBackupReceipt> VerifyBackupAsync(ProfileSnapshot target, CancellationToken token)
        {
            backupReads++;
            if (mode == "backup-missing" || mode == "removal-final-backup-missing" && backupReads == 2) return null!;
            if (mode == "backup-stall") return StallBackup();
            string digest = mode == "removal-final-backup-changed" && backupReads == 2 ? new string('b', 64) : new string('a', 64);
            return Task.FromResult(new ProfileBackupReceipt(target.Sid, target.Path,
                @"D:\KillerBenchBackups\Fixture", backupCompletedUtc, 12, 4096, digest, mode != "backup"));
        }
        public Task RemoveAsync(ProfileSnapshot target, CancellationToken token)
        {
            using var journal = new FileStream(Directory.GetFiles(Folder, "*.jsonl").Single(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(journal);
            if (!File.Exists(Path.Combine(Folder, "pending-removal.json")) || !reader.ReadToEnd().Contains("Prepared"))
                throw new Exception("Removal began before durable preparation");
            RemoveCalls++;
            Removed = true;
            if (mode == "remove-missing") return null!;
            if (mode == "cancel")
            {
                stop.Cancel();
                return new TaskCompletionSource().Task;
            }
            return Task.CompletedTask;
        }
        public Task<bool> VerifyAbsentAsync(ProfileSnapshot target, CancellationToken token)
        {
            if (mode == "verify-missing") return null!;
            if (mode == "verify-stall") return Stall();
            if (CheckRecoveryLock)
            {
                try
                {
                    using var edit = new FileStream(Path.Combine(Folder, "pending-removal.json"), FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                    throw new Exception("Recovery evidence allowed editing during verification");
                }
                catch (IOException) { }
            }
            return Task.FromResult(mode != "verify");
        }
    }
}
