using System.Globalization;
using System.Text.Json;
using System.Xml;

namespace KillerBench.Engine;

public sealed record SetupLogObservation(string Kind, DateTimeOffset LastWriteUtc, long Length, string FeatureCode = "", string Mode = "");

public sealed record SetupActivitySnapshot(IReadOnlyList<SetupLogObservation> Logs, bool Complete);

public static class SetupActivityDiagnostics
{
    private static readonly (string Kind, Func<string> Path)[] Sources =
    [
        ("Upgrade activity", () => Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "$WINDOWS.~BT", "Sources", "Panther", "setupact.log")),
        ("Rollback activity", () => Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "$WINDOWS.~BT", "Sources", "Rollback", "setupact.log")),
        ("Windows activity", () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Panther", "setupact.log")),
        ("Upgrade error", () => Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "$WINDOWS.~BT", "Sources", "Panther", "setuperr.log")),
        ("Rollback error", () => Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "$WINDOWS.~BT", "Sources", "Rollback", "setuperr.log")),
        ("Windows error", () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Panther", "setuperr.log"))
    ];

    public static WorkflowCheck Check()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Setup activity inspection requires Windows.");
        try
        {
            var logs = new List<SetupLogObservation>();
            foreach (var source in Sources)
            {
                string path = source.Path();
                if (!File.Exists(path)) continue;
                var file = new FileInfo(path);
                var tail = ReadTail(path);
                logs.Add(new(source.Kind, file.LastWriteTimeUtc, file.Length, tail.FeatureCode, tail.Mode));
            }
            return Assess(new(logs.AsReadOnly(), true), DateTimeOffset.UtcNow);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return new("Windows Setup activity", CheckState.Unknown,
                "The bounded Windows Setup log metadata is unavailable. No setup file was changed.",
                Evidence: new(-1, "", error.GetType().Name, false));
        }
    }

    public static WorkflowCheck Assess(SetupActivitySnapshot snapshot, DateTimeOffset capturedUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!snapshot.Complete || snapshot.Logs.Count > Sources.Length ||
            snapshot.Logs.Select(log => log.Kind).Distinct(StringComparer.Ordinal).Count() != snapshot.Logs.Count ||
            snapshot.Logs.Any(log => !Sources.Any(source => source.Kind == log.Kind) || log.Length < 0 || log.LastWriteUtc > capturedUtc.AddMinutes(5) ||
                (!string.IsNullOrEmpty(log.FeatureCode) && !IsFeatureCode(log.FeatureCode)) || !ValidMode(log.Mode)))
            return new("Windows Setup activity", CheckState.Unknown,
                "Windows Setup activity metadata is incomplete or outside its bounds. No setup state was inferred.");

        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
        if (snapshot.Logs.Count == 0)
            return new("Windows Setup activity", CheckState.Information,
                "No fixed Panther activity or error log is present. This does not prove that setup never ran or that servicing is idle.", Evidence: evidence);

        var newest = snapshot.Logs.OrderByDescending(log => log.LastWriteUtc).First();
        TimeSpan age = capturedUtc - newest.LastWriteUtc;
        bool recent = age >= TimeSpan.Zero && age <= TimeSpan.FromSeconds(30);
        var codes = snapshot.Logs.Select(log => log.FeatureCode).Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToArray();
        var currentCodes = snapshot.Logs.Where(log => log.FeatureCode.Length > 0 &&
                capturedUtc - log.LastWriteUtc >= TimeSpan.Zero && capturedUtc - log.LastWriteUtc <= TimeSpan.FromHours(12))
            .Select(log => log.FeatureCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var references = codes.Select(code => (Code: code, Reference: FindReference(code))).ToArray();
        bool requiresReview = currentCodes.Any(code => FindReference(code)?.RequiresReview != false);
        var classified = snapshot.Logs.Where(log => log.Mode.Length > 0).OrderByDescending(log => log.LastWriteUtc).FirstOrDefault();
        string detail = $"{snapshot.Logs.Count} fixed Panther log{(snapshot.Logs.Count == 1 ? " is" : "s are")} present. Newest kind: {newest.Kind}; age: {Math.Max(0, age.TotalSeconds):F0} seconds; recent write: {(recent ? "yes" : "no")}.";
        if (classified is not null)
            detail += $" Latest classified Setup mode: {classified.Mode} ({classified.Kind}, {Math.Max(0, (capturedUtc - classified.LastWriteUtc).TotalSeconds):F0} seconds old).";
        if (codes.Length > 0)
        {
            detail += " Bounded tails contain feature-upgrade code" + (codes.Length == 1 ? " " : "s ") + string.Join(", ", codes) +
                (currentCodes.Length > 0 ? "." : "; the code-bearing logs are outside the 12-hour current-review window.");
            foreach (var item in references)
                detail += item.Reference is null
                    ? $" {item.Code}: no installed reference is available; retain the exact code and review fresh Setup evidence."
                    : $" {item.Code} {item.Reference.Symbol}: {item.Reference.Meaning} Next: {item.Reference.DiagnosticScope}";
        }
        detail += " File paths and log contents are excluded. A recent write is activity evidence, not proof of progress, completion, a stall or a fault. No setup file was changed.";
        return new("Windows Setup activity", requiresReview ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
    }

    private static (string FeatureCode, string Mode) ReadTail(string path)
    {
        const int maximumBytes = 256 * 1024;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        long start = Math.Max(0, stream.Length - maximumBytes);
        stream.Seek(start, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false);
        string text = reader.ReadToEnd();
        string code = "";
        for (int index = text.Length - 10; index >= 0; index--)
        {
            string candidate = text.Substring(index, 10).ToUpperInvariant();
            if (IsFeatureCode(candidate)) { code = candidate; break; }
        }
        return (code, ClassifyMode(text));
    }

    public static string ClassifyMode(string boundedTail)
    {
        ArgumentNullException.ThrowIfNull(boundedTail);
        if (boundedTail.Contains("/Compat ScanOnly", StringComparison.OrdinalIgnoreCase)) return "Compatibility Scan";
        if (boundedTail.Contains("/PreDownload", StringComparison.OrdinalIgnoreCase)) return "PreDownload / Package";
        if (boundedTail.Contains("/auto upgrade", StringComparison.OrdinalIgnoreCase)) return "In-Place Upgrade";
        if (boundedTail.Contains("/Finalize", StringComparison.OrdinalIgnoreCase)) return "Finalize";
        return boundedTail.Contains("SetupHost::Initialize", StringComparison.OrdinalIgnoreCase) ? "Windows Setup" : "";
    }

    private static bool IsFeatureCode(string value) => value.Length == 10 && value.StartsWith("0XC190", StringComparison.Ordinal) &&
        value.AsSpan(6).ToString().All(Uri.IsHexDigit);

    private static WindowsSetupCodeReference? FindReference(string value) =>
        uint.TryParse(value.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint code)
            ? WindowsSetupErrorCatalog.Find(code)
            : null;

    private static bool ValidMode(string value) => value is "" or "Compatibility Scan" or "PreDownload / Package" or "In-Place Upgrade" or "Finalize" or "Windows Setup";
}

public sealed record CompatibilityReportSnapshot(bool Present, DateTimeOffset LastWriteUtc, int HardBlocks, int DriverBlocks, bool Complete);

public static class SetupCompatibilityDiagnostics
{
    public static WorkflowCheck Check()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Setup compatibility inspection requires Windows.");
        try
        {
            string root = Path.GetPathRoot(Environment.SystemDirectory)!;
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var files = Directory.Exists(Path.Combine(root, "$WINDOWS.~BT", "Sources", "Panther"))
                ? Directory.EnumerateFiles(Path.Combine(root, "$WINDOWS.~BT", "Sources", "Panther"), "CompatData*.xml").Take(257).ToList()
                : [];
            if (Directory.Exists(Path.Combine(windows, "Panther")))
                files.AddRange(Directory.EnumerateFiles(Path.Combine(windows, "Panther"), "CompatData*.xml").Take(Math.Max(0, 257 - files.Count)));
            if (files.Count > 256) return Assess(new(false, DateTimeOffset.MinValue, 0, 0, false), DateTimeOffset.UtcNow);
            string? selected = files.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (selected is null) return Assess(new(false, DateTimeOffset.MinValue, 0, 0, true), DateTimeOffset.UtcNow);

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16 * 1024 * 1024 };
            int hard = 0, drivers = 0;
            using var reader = XmlReader.Create(selected, settings);
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;
                if (string.Equals(reader.GetAttribute("BlockingType"), "Hard", StringComparison.OrdinalIgnoreCase)) hard++;
                if (reader.LocalName == "DriverPackage" && string.Equals(reader.GetAttribute("BlockMigration"), "True", StringComparison.OrdinalIgnoreCase)) drivers++;
                if (hard > 4096 || drivers > 4096) return Assess(new(true, File.GetLastWriteTimeUtc(selected), hard, drivers, false), DateTimeOffset.UtcNow);
            }
            return Assess(new(true, File.GetLastWriteTimeUtc(selected), hard, drivers, true), DateTimeOffset.UtcNow);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or XmlException or InvalidOperationException)
        {
            return new("Windows Setup compatibility", CheckState.Unknown,
                "The bounded compatibility report is unavailable or could not be parsed. No compatibility conclusion was selected.",
                Evidence: new(-1, "", error.GetType().Name, false));
        }
    }

    public static WorkflowCheck Assess(CompatibilityReportSnapshot snapshot, DateTimeOffset capturedUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!snapshot.Complete || snapshot.HardBlocks is < 0 or > 4096 || snapshot.DriverBlocks is < 0 or > 4096 ||
            (snapshot.Present && snapshot.LastWriteUtc > capturedUtc.AddMinutes(5)) || (!snapshot.Present && snapshot.LastWriteUtc != DateTimeOffset.MinValue))
            return new("Windows Setup compatibility", CheckState.Unknown,
                "Compatibility report evidence is incomplete or outside its bounds. No compatibility conclusion was selected.");
        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
        if (!snapshot.Present)
            return new("Windows Setup compatibility", CheckState.Information,
                "No bounded CompatData report is present. Absence does not establish compatibility or readiness.", Evidence: evidence);
        int total = snapshot.HardBlocks + snapshot.DriverBlocks;
        TimeSpan age = capturedUtc - snapshot.LastWriteUtc;
        bool currentReviewWindow = age >= TimeSpan.Zero && age <= TimeSpan.FromHours(12);
        string detail = $"Latest bounded CompatData report contains {snapshot.HardBlocks} hard block node{(snapshot.HardBlocks == 1 ? "" : "s")} and {snapshot.DriverBlocks} blocked driver package node{(snapshot.DriverBlocks == 1 ? "" : "s")}.";
        detail += total > 0 && currentReviewWindow
            ? " The report is inside the 12-hour current-review window. Review it with current setup evidence before continuing an upgrade. Report paths, application names and driver identities are excluded."
            : total > 0
            ? " The report is outside the 12-hour current-review window, so its blocks are retained as historical evidence without claiming a current block. Report paths, application names and driver identities are excluded."
            : " No matching block node was detected, but this does not establish complete compatibility.";
        return new("Windows Setup compatibility", total > 0 && currentReviewWindow ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
    }
}
