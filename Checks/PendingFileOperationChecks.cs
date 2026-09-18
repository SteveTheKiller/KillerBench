using KillerBench.Engine;

internal static class PendingFileOperationChecks
{
    internal static void Run()
    {
        var absent = PendingFileOperationDiagnostics.Check(() => null);
        Require(absent is { State: CheckState.Information, Repair: null, Evidence: not null } &&
            absent.Detail.Contains("No pending file"), "Absent registration remains informational");

        string[] entries =
        [
            @"\??\C:\Temp\old.tmp", "",
            @"\??\C:\Program Files\Vendor\new.dll", @"!\??\C:\Program Files\Vendor\current.dll",
            @"\??\C:\Temp\before.dat", @"\??\C:\Temp\after.dat"
        ];
        var snapshot = PendingFileOperationDiagnostics.Summarize(entries);
        Require(snapshot is { Complete: true, ValuePresent: true, EntryCount: 6, OperationCount: 3,
            DeleteCount: 1, ReplaceCount: 1, RenameCount: 1 }, "Paired entries classify deletes, replacements and renames");
        var pending = PendingFileOperationDiagnostics.Assess(snapshot);
        Require(pending is { State: CheckState.Attention, Repair: null, Evidence: not null } &&
            pending.Detail.Contains("3 pending file operations") && !pending.Detail.Contains("Vendor") &&
            !pending.Evidence.Output.Contains("old.tmp"), "Pending operations request restart review without retaining paths");

        foreach (object invalid in new object[]
        {
            "not a multi-string",
            new[] { @"\??\C:\Temp\source.tmp" },
            new[] { "", @"\??\C:\Temp\target.tmp" },
            new[] { @"\??\C:\Temp\source.tmp", "!" },
            Enumerable.Repeat("x", 4098).ToArray()
        })
            Require(PendingFileOperationDiagnostics.Check(() => invalid) is { State: CheckState.Unknown, Repair: null },
                "Wrong-type, unpaired, malformed and excessive registrations remain unknown");

        foreach (var invalid in new[]
        {
            snapshot with { Complete = false },
            snapshot with { EntryCount = 8 },
            snapshot with { OperationCount = 4 },
            snapshot with { DeleteCount = 2 },
            snapshot with { ValuePresent = false },
            snapshot with { CollectionIssue = "Access unavailable" }
        })
            Require(PendingFileOperationDiagnostics.Assess(invalid) is { State: CheckState.Unknown, Repair: null },
                "Inconsistent summarized evidence remains unknown");

        Console.WriteLine("PASS: privacy-bounded pending file operation classification and fail-closed evidence handling.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
