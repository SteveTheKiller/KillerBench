using KillerBench.Engine;

internal static class StorageSensePolicyChecks
{
    internal static void Run()
    {
        StorageSensePolicyValue Value(string name, int? value = null, bool valid = true) => new(name, value.HasValue, valid, value);
        StorageSensePolicySnapshot Snapshot(int? global = null, int? cadence = null, int? temporary = null,
            int? recycle = null, int? downloads = null, int? cloud = null) => new(true,
            [Value("AllowStorageSenseGlobal", global), Value("ConfigStorageSenseGlobalCadence", cadence),
             Value("AllowStorageSenseTemporaryFilesCleanup", temporary), Value("ConfigStorageSenseRecycleBinCleanupThreshold", recycle),
             Value("ConfigStorageSenseDownloadsCleanupThreshold", downloads), Value("ConfigStorageSenseCloudContentDehydrationThreshold", cloud)]);

        var absent = StorageSensePolicyDiagnostics.Assess(Snapshot());
        Require(absent is { State: CheckState.Information, Repair: null } && absent.Evidence is not null &&
            absent.Detail.Contains("current-user settings", StringComparison.Ordinal), "Absent Storage Sense machine policy remains informational");
        var configured = StorageSensePolicyDiagnostics.Assess(Snapshot(1, 7, 1, 30, 0, 60));
        Require(configured.State == CheckState.Information && configured.Detail.Contains("weekly", StringComparison.Ordinal) &&
            configured.Detail.Contains("Downloads days 0", StringComparison.Ordinal), "Recognized Storage Sense policy is described exactly");
        var inactive = StorageSensePolicyDiagnostics.Assess(Snapshot(0, 30, 1, 30, 0, 30));
        Require(inactive is { State: CheckState.Attention, Repair: null } && inactive.Detail.Contains("no effect", StringComparison.Ordinal),
            "Subordinate policy under a blocked global policy requests owner review");
        foreach (var invalid in new[] { Snapshot(2), Snapshot(1, 2), Snapshot(1, 1, 2), Snapshot(1, 1, 1, 366), Snapshot() with { Complete = false } })
            Require(StorageSensePolicyDiagnostics.Assess(invalid) is { State: CheckState.Unknown, Repair: null },
                "Unsupported or incomplete Storage Sense policy remains unknown");
        var duplicate = Snapshot() with { Values = [.. Snapshot().Values.Take(5), Value("AllowStorageSenseGlobal")] };
        Require(StorageSensePolicyDiagnostics.Assess(duplicate).State == CheckState.Unknown, "Duplicate Storage Sense policy evidence is rejected");
        var wrongType = Snapshot() with { Values = [new("AllowStorageSenseGlobal", true, false, null), .. Snapshot().Values.Skip(1)] };
        Require(StorageSensePolicyDiagnostics.Assess(wrongType).State == CheckState.Unknown, "Unexpected Storage Sense registry types remain unknown");
        Require(StorageSensePolicyDiagnostics.Check(() => Snapshot(1, 1)).State == CheckState.Information,
            "Substituted Storage Sense policy is assessed");
        Require(StorageSensePolicyDiagnostics.Check(() => throw new UnauthorizedAccessException("fixture")) is
            { State: CheckState.Unknown, Evidence.ExitCode: -1 }, "Storage Sense policy access failure remains unknown");
        Console.WriteLine("PASS: Storage Sense machine policy values, inactive combinations, types and privacy remain deterministic and read only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
