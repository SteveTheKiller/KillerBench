using System.IO;
using System.Text;
using KillerBench.Engine;

internal static class ServicingLogChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string tailPath = Path.Combine(scratch, "tail.log");
        await File.WriteAllTextAsync(tailPath, new string('x', TextEvidence.MaximumCharacters + 200) +
            "\n2026-09-15 01:02:03, Error CBS tail 0x800f081f");
        var tail = await TextEvidence.ReadTailAsync(tailPath);
        Require(tail.Truncated && tail.Lines.Length == 1 && tail.Lines[0].Contains("0x800f081f") &&
            tail.Lines.Sum(line => line.Length) <= TextEvidence.MaximumCharacters,
            "Tail reading drops the partial first line and retains the newest bounded complete line");

        string utf16Path = Path.Combine(scratch, "tail-utf16.log");
        await File.WriteAllTextAsync(utf16Path, "first\nsecond", Encoding.Unicode);
        var utf16 = await TextEvidence.ReadTailAsync(utf16Path);
        Require(!utf16.Truncated && utf16.Lines.SequenceEqual(["first", "second"]),
            "Tail reading preserves a complete UTF-16 file");
        string binaryPath = Path.Combine(scratch, "tail-binary.log");
        await File.WriteAllBytesAsync(binaryPath, [0x61, 0x00, 0x62]);
        bool binaryRejected = false;
        try { await TextEvidence.ReadTailAsync(binaryPath); }
        catch (InvalidDataException) { binaryRejected = true; }
        Require(binaryRejected, "Tail reading rejects null-bearing binary evidence");

        string archiveWindows = Path.Combine(scratch, "archive-windows");
        string archiveDirectory = Path.Combine(archiveWindows, "Logs", "CBS");
        Directory.CreateDirectory(archiveDirectory);
        string olderArchive = Path.Combine(archiveDirectory, "CbsPersist_20260914010000.log");
        string newestArchive = Path.Combine(archiveDirectory, "CbsPersist_20260915010000.log");
        string compressedArchive = Path.Combine(archiveDirectory, "CbsPersist_20260916010000.cab");
        await File.WriteAllTextAsync(olderArchive, "older");
        await File.WriteAllTextAsync(newestArchive, "newest");
        await File.WriteAllTextAsync(compressedArchive, "compressed fixture");
        File.SetLastWriteTimeUtc(olderArchive, new DateTime(2026, 9, 14, 1, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newestArchive, new DateTime(2026, 9, 15, 1, 0, 0, DateTimeKind.Utc));
        Require(ServicingLogDiagnostics.DiscoverNewestPlainTextCbsArchive(archiveWindows) == newestArchive,
            "CBS archive discovery selects the newest plain-text archive and excludes CAB files");
        Require(CbsCabEvidence.DiscoverNewest(archiveWindows) == compressedArchive,
            "CBS cabinet discovery selects the newest compressed archive independently");

        bool Exists(string path) => path.EndsWith("CBS.log", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("dism.log", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("Panther", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("$WINDOWS.~BT", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith("setupact.log", StringComparison.OrdinalIgnoreCase);
        Task<TextEvidence> Read(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string line = path.EndsWith("CBS.log", StringComparison.OrdinalIgnoreCase)
                ? "2026-09-15 01:02:03, Error CBS source missing 0x800f081f"
                : path.EndsWith("setupact.log", StringComparison.OrdinalIgnoreCase)
                    ? "2026-09-15 01:02:03, Error MOUPG setup failed"
                    : "2026-09-15 01:02:03, Info DISM completed";
            return Task.FromResult(new TextEvidence(path, [line], false));
        }
        var checks = await ServicingLogDiagnostics.CheckAsync(read: Read, exists: Exists,
            newestCbsArchive: () => null, newestCbsCab: () => null);
        Require(checks.Count == 4 && checks.Count(check => check.State == CheckState.Attention) == 2 &&
            checks.Single(check => check.Name == "CBS servicing log").Detail.Contains("Servicing source files") &&
            checks.Single(check => check.Name == "Windows Setup activity log").Detail.Contains("Windows Setup error") &&
            checks.Single(check => check.Name == "DISM servicing log").State == CheckState.Information &&
            checks.Single(check => check.Name == "Servicing log coverage").Detail.Contains("3 of 8") &&
            checks.All(check => check.Repair is null),
            "Available servicing logs are analyzed independently and historical findings never select repair");

        var missing = await ServicingLogDiagnostics.CheckAsync(read: Read, exists: _ => false,
            newestCbsArchive: () => null, newestCbsCab: () => null);
        Require(missing.Count == 3 && missing.Count(check => check.State == CheckState.Unknown) == 2 &&
            missing.Single(check => check.Name == "Servicing log coverage").State == CheckState.Information,
            "Missing expected logs remain unknown while absent upgrade logs remain ordinary coverage context");
        var isolated = await ServicingLogDiagnostics.CheckAsync(read: (path, token) =>
            path.EndsWith("dism.log", StringComparison.OrdinalIgnoreCase)
                ? throw new UnauthorizedAccessException("fixture denied") : Read(path, token),
            exists: path => path.EndsWith("CBS.log", StringComparison.OrdinalIgnoreCase) || path.EndsWith("dism.log", StringComparison.OrdinalIgnoreCase),
            newestCbsArchive: () => null, newestCbsCab: () => null);
        Require(isolated.Any(check => check.Name == "CBS servicing log" && check.State == CheckState.Attention) &&
            isolated.Any(check => check.Name == "DISM servicing log" && check.State == CheckState.Unknown),
            "One unavailable log does not discard other completed analysis");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await ServicingLogDiagnostics.CheckAsync(Read, Exists, () => null, () => null, token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents servicing-log collection");

        var archiveChecks = await ServicingLogDiagnostics.CheckAsync(read: (path, token) =>
                Task.FromResult(new TextEvidence(path,
                    ["2026-09-15 01:02:03, Error CBS source missing 0x800f081f"], false)),
            exists: path => path == newestArchive,
            newestCbsArchive: () => newestArchive, newestCbsCab: () => null);
        Require(archiveChecks.Any(check => check.Name == "Newest CBS plain-text archive" &&
                check.State == CheckState.Attention) &&
            archiveChecks.Single(check => check.Name == "Servicing log coverage").Detail.Contains("1 of 9") &&
            archiveChecks.All(check => check.Repair is null),
            "The newest CBS plain-text archive participates in isolated deterministic analysis without repair");

        string cabinetRoot = Path.Combine(scratch, "cab-extraction");
        var compressed = await CbsCabEvidence.ReadNewestLogAsync(compressedArchive, temporaryRoot: cabinetRoot,
            execute: async (step, token) =>
            {
                Require(step.Tool == "expand.exe" && step.Arguments[0] == "-F:CbsPersist_*.log" &&
                    step.Arguments[1] == compressedArchive, "CBS cabinet extraction uses fixed tool, member pattern and archive identity");
                string extracted = Path.Combine(step.Arguments[2], "CbsPersist_20260916010000.log");
                await File.WriteAllTextAsync(extracted, "2026-09-15 01:02:03, Error CBS source missing 0x800f081f", token);
                return new(0, "fixture", "", false);
            });
        Require(compressed.Source == compressedArchive && compressed.Lines.Single().Contains("0x800f081f") &&
            !Directory.EnumerateFileSystemEntries(cabinetRoot, "*", SearchOption.TopDirectoryOnly).Any(),
            "Bounded CBS cabinet extraction returns text evidence and removes temporary content");
        var cabinetChecks = await ServicingLogDiagnostics.CheckAsync(read: Read, exists: path => path == compressedArchive,
            newestCbsArchive: () => null, newestCbsCab: () => compressedArchive,
            readCompressed: (path, token) => Task.FromResult(new TextEvidence(path,
                ["2026-09-15 01:02:03, Error CBS source missing 0x800f081f"], false)));
        Require(cabinetChecks.Any(check => check.Name == "Newest compressed CBS archive" && check.State == CheckState.Attention) &&
            cabinetChecks.Single(check => check.Name == "Servicing log coverage").Detail.Contains("1 of 9") &&
            cabinetChecks.All(check => check.Repair is null),
            "The newest compressed CBS archive participates in deterministic analysis without repair");
        Console.WriteLine("PASS: bounded newest servicing logs, plain-text and compressed CBS archive discovery, deterministic findings, source isolation and no repair selection; fixtures only.");
    }

    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
