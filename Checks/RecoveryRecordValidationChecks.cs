using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using KillerBench.Engine;
using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Evidence;
using KillerBench.Engine.Profiles;
using KillerBench.Engine.Security;

internal static class RecoveryRecordValidationChecks
{
    public static async Task RunAsync(string scratch)
    {
        await CompletedJournalIdentityAsync(scratch);
        await BooleanRecordAsync(scratch, record => record["machine"] = null);
        await RemovalRecordAsync(scratch, record => record["target"] = null);
        await ClipboardRecordAsync(scratch, record => record["before"] = null);
        await BooleanRecordAsync(scratch, record => record["journalPath"] = " ");
        foreach (string field in new[] { "machine", "settingKey", "before", "journalPath" })
        {
            await BooleanRecordAsync(scratch, record => record[field] = null);
            await BooleanRecordAsync(scratch, record => record.Remove(field));
            await BooleanRecordAsync(scratch, record => record[field] = new JsonArray());
            await BooleanRecordAsync(scratch, record => record[field.ToUpperInvariant()] = record[field]!.DeepClone());
        }
        foreach (string field in new[] { "machine", "before", "journalPath" })
        {
            await ClipboardRecordAsync(scratch, record => record[field] = null);
            await ClipboardRecordAsync(scratch, record => record.Remove(field));
            await ClipboardRecordAsync(scratch, record => record[field] = new JsonArray());
            await ClipboardRecordAsync(scratch, record => record[field.ToUpperInvariant()] = record[field]!.DeepClone());
        }
        foreach (string field in new[] { "machine", "target", "backup", "journal" })
        {
            await RemovalRecordAsync(scratch, record => record[field] = null);
            await RemovalRecordAsync(scratch, record => record.Remove(field));
            await RemovalRecordAsync(scratch, record => record[field] = new JsonArray());
            await RemovalRecordAsync(scratch, record => record[field.ToUpperInvariant()] = record[field]!.DeepClone());
        }
        foreach (string field in new[] { "sid", "path" })
            await RemovalRecordAsync(scratch, record => record["target"]![field] = null);
        foreach (string field in new[] { "sourceSid", "sourcePath", "destinationPath", "manifestSha256" })
            await RemovalRecordAsync(scratch, record => record["backup"]![field] = null);
        await BooleanRecordAsync(scratch, record => record["before"]!["detail"] = null);
        await ClipboardRecordAsync(scratch, record => record["before"]!["detail"] = null);
        await ClipboardRecordAsync(scratch, record =>
        {
            record["schema"] = 1;
            record.Remove("targetValuePresent"); record.Remove("targetValue");
            record["sourceJournalPath"] = "fixture.json";
            record["beforeUndo"] = record["before"]!.DeepClone();
            record.Remove("before"); record["original"] = null;
        });
        await ValidAndCanceledAsync(scratch);
        Console.WriteLine("PASS: recovery validation covers 57 malformed records, valid no-repeat reconciliation, absent-profile resolution and cancellation.");
    }

    private static async Task CompletedJournalIdentityAsync(string scratch)
    {
        string folder = Path.Combine(scratch, "completed-identity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var boolean = JsonSerializer.SerializeToNode(new PrivacyBooleanResult(PrivacyChangeOutcome.Verified,
            "advertising-id", true, "fixture", Before: new(true, true, 0, false, null, "fixture"),
            After: new(true, true, 1, false, null, "fixture")) { Machine = "FOREIGN-FIXTURE" }, Capture.JsonOptions)!.AsObject();
        boolean.Remove("machine");
        string booleanPath = Path.Combine(folder, "boolean.json");
        string booleanBytes = boolean.ToJsonString();
        await File.WriteAllTextAsync(booleanPath, booleanBytes);
        var booleanHost = new BooleanHost();
        bool booleanRejected = false;
        try
        {
            var result = await PrivacyBooleanSettingExecution.UndoAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
                booleanPath, Path.Combine(folder, "boolean-undo"), booleanHost);
            booleanRejected = result.Outcome == PrivacyChangeOutcome.Blocked;
        }
        catch (InvalidDataException) { booleanRejected = true; }
        Require(booleanRejected && booleanHost.Calls == 0 && !Directory.Exists(Path.Combine(folder, "boolean-undo")) &&
            await File.ReadAllTextAsync(booleanPath) == booleanBytes,
            "Missing completed privacy journal machine must not synthesize local provenance or call the provider");

        var clipboard = JsonSerializer.SerializeToNode(new PrivacyClipboardHistoryResult(PrivacyChangeOutcome.Verified,
            true, "fixture", Before: new(true, true, 0, false, null, "fixture"),
            After: new(true, true, 1, false, null, "fixture")) { Machine = "FOREIGN-FIXTURE" }, Capture.JsonOptions)!.AsObject();
        clipboard.Remove("machine");
        string clipboardPath = Path.Combine(folder, "clipboard.json");
        string clipboardBytes = clipboard.ToJsonString();
        await File.WriteAllTextAsync(clipboardPath, clipboardBytes);
        var clipboardHost = new ClipboardHost();
        bool clipboardRejected = false;
        try
        {
            var result = await PrivacyClipboardHistoryExecution.UndoAsync(clipboardPath,
                Path.Combine(folder, "clipboard-undo"), clipboardHost);
            clipboardRejected = result.Outcome == PrivacyChangeOutcome.Blocked;
        }
        catch (InvalidDataException) { clipboardRejected = true; }
        Require(clipboardRejected && clipboardHost.Calls == 0 && !Directory.Exists(Path.Combine(folder, "clipboard-undo")) &&
            await File.ReadAllTextAsync(clipboardPath) == clipboardBytes,
            "Missing completed Clipboard journal machine must be refused before provider access");
        Console.WriteLine("PASS: completed privacy undo journals require recorded machine identity before provider access.");
    }

    private static JsonObject BooleanRecord(string folder) => JsonSerializer.SerializeToNode(new
    {
        Schema = 1, Machine = Environment.MachineName, SettingKey = "advertising-id", StartedUtc = DateTimeOffset.UtcNow,
        TargetValuePresent = true, TargetValue = 1,
        Before = new PrivacyBooleanEvidence(true, true, 0, false, null, "fixture"), JournalPath = Path.Combine(folder, "change.json")
    }, Capture.JsonOptions)!.AsObject();

    private static async Task BooleanRecordAsync(string scratch, Action<JsonObject> corrupt)
    {
        string folder = Path.Combine(scratch, "boolean-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var record = BooleanRecord(folder);
        corrupt(record);
        string pending = Path.Combine(folder, "advertising-id.pending.json");
        string original = record.ToJsonString();
        await File.WriteAllTextAsync(pending, original);
        var host = new BooleanHost();
        var result = await PrivacyBooleanSettingExecution.ReconcileAsync(PrivacyBooleanSettingCatalog.AdvertisingId, folder, host);
        Require(result.Outcome == PrivacyChangeReconciliationOutcome.Inconclusive && host.Calls == 0 &&
            await File.ReadAllTextAsync(pending) == original, "Malformed privacy record must remain inconclusive before provider calls");
    }

    private static async Task ValidAndCanceledAsync(string scratch)
    {
        string folder = Path.Combine(scratch, "valid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string pending = Path.Combine(folder, "advertising-id.pending.json");
        await File.WriteAllTextAsync(pending, BooleanRecord(folder).ToJsonString());
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var host = new BooleanHost();
        try
        {
            await PrivacyBooleanSettingExecution.ReconcileAsync(PrivacyBooleanSettingCatalog.AdvertisingId, folder, host, token: canceled.Token);
            throw new Exception("Canceled privacy recovery returned normally");
        }
        catch (OperationCanceledException) { }
        Require(host.Calls == 0 && File.Exists(pending), "Cancellation must retain privacy evidence before host calls");
        var valid = await PrivacyBooleanSettingExecution.ReconcileAsync(PrivacyBooleanSettingCatalog.AdvertisingId, folder, host);
        Require(valid.Outcome == PrivacyChangeReconciliationOutcome.Verified && host.Calls == 1 && !File.Exists(pending),
            "Valid privacy recovery verifies without writes");
        pending = Path.Combine(folder, "pending-removal.json");
        await File.WriteAllTextAsync(pending, RemovalRecord(folder).ToJsonString());
        var removalHost = new RemovalHost();
        try
        {
            await ProfileRemovalRecovery.TryResolveAsync(folder, removalHost, token: canceled.Token);
            throw new Exception("Canceled profile recovery returned normally");
        }
        catch (OperationCanceledException) { }
        Require(removalHost.Calls == 0 && File.Exists(pending), "Cancellation must retain removal evidence before host calls");
        Require(await ProfileRemovalRecovery.TryResolveAsync(folder, removalHost) && removalHost.Calls == 3 && !File.Exists(pending),
            "Valid absent profile resolves through two readiness checks and one absence check");
    }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    private static async Task ClipboardRecordAsync(string scratch, Action<JsonObject> corrupt)
    {
        string folder = Path.Combine(scratch, "clipboard-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var record = JsonSerializer.SerializeToNode(new
        {
            Schema = 2, Machine = Environment.MachineName, StartedUtc = DateTimeOffset.UtcNow,
            TargetValuePresent = true, TargetValue = 1,
            Before = new PrivacyClipboardHistoryEvidence(true, true, 0, false, null, "fixture"), JournalPath = Path.Combine(folder, "change.json")
        }, Capture.JsonOptions)!.AsObject();
        corrupt(record);
        string pending = Path.Combine(folder, "privacy-clipboard-history.pending.json");
        string original = record.ToJsonString();
        await File.WriteAllTextAsync(pending, original);
        var host = new ClipboardHost();
        var result = await PrivacyClipboardHistoryExecution.ReconcileAsync(folder, host);
        Require(result.Outcome == PrivacyChangeReconciliationOutcome.Inconclusive && host.Calls == 0 &&
            await File.ReadAllTextAsync(pending) == original, "Malformed Clipboard history must retain evidence without host access");
    }

    private sealed class ClipboardHost : IPrivacyClipboardHistoryHost
    {
        public int Calls;
        public Task<PrivacyClipboardHistoryEvidence> ReadAsync(CancellationToken token)
        { Calls++; return Task.FromResult(new PrivacyClipboardHistoryEvidence(true, true, 1, false, null, "fixture")); }
        public Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token)
        { Calls++; throw new Exception("Recovery must never write"); }
    }

    private static JsonObject RemovalRecord(string folder) => JsonSerializer.SerializeToNode(new
    {
        Schema = 1, Machine = Environment.MachineName, StartedUtc = DateTimeOffset.UtcNow,
        Target = new ProfileSnapshot("S-1-5-21-1-2-3-1001", @"C:\Users\Fixture", false, false, DateTimeOffset.UtcNow.AddDays(-120), false),
        Backup = new ProfileBackupReceipt("S-1-5-21-1-2-3-1001", @"C:\Users\Fixture", @"D:\Backups\Fixture",
            DateTimeOffset.UtcNow.AddMinutes(-1), 1, 10, new string('a', 64), true),
        Journal = Path.Combine(folder, "profile-removal-fixture.jsonl")
    }, Capture.JsonOptions)!.AsObject();

    private static async Task RemovalRecordAsync(string scratch, Action<JsonObject> corrupt)
    {
        string folder = Path.Combine(scratch, "removal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var record = RemovalRecord(folder);
        corrupt(record);
        string pending = Path.Combine(folder, "pending-removal.json");
        string original = record.ToJsonString();
        await File.WriteAllTextAsync(pending, original);
        var host = new RemovalHost();
        Require(!await ProfileRemovalRecovery.TryResolveAsync(folder, host) && host.Calls == 0 &&
            await File.ReadAllTextAsync(pending) == original, "Malformed profile removal must retain evidence without host access");
    }

    private sealed class RemovalHost : IProfileRemovalHost
    {
        public int Calls;
        public Task<bool> ReadyAsync(CancellationToken token) { Calls++; return Task.FromResult(true); }
        public Task<bool> VerifyAbsentAsync(ProfileSnapshot target, CancellationToken token) { Calls++; return Task.FromResult(true); }
        public Task<ProfileInventoryResult> InventoryAsync(CancellationToken token) => throw new Exception("Unexpected inventory");
        public Task<ProfileDependencyResult> DependenciesAsync(CancellationToken token) => throw new Exception("Unexpected dependencies");
        public Task<ProfileRegistrationResult> RegistrationsAsync(CancellationToken token) => throw new Exception("Unexpected registrations");
        public Task<ProfileBackupReceipt> VerifyBackupAsync(ProfileSnapshot target, CancellationToken token) => throw new Exception("Unexpected backup");
        public Task RemoveAsync(ProfileSnapshot target, CancellationToken token) => throw new Exception("Recovery must never remove");
    }

    private sealed class BooleanHost : IPrivacyBooleanSettingHost
    {
        public int Calls;
        public Task<PrivacyBooleanEvidence> ReadAsync(CancellationToken token)
        { Calls++; return Task.FromResult(new PrivacyBooleanEvidence(true, true, 1, false, null, "fixture")); }
        public Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token)
        { Calls++; throw new Exception("Recovery must never write"); }
    }
}
