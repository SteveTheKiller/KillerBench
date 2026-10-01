namespace KillerBench.Engine.Debloat;

public sealed record DebloatVendorPathTarget(string Label, string Path, string SourcePattern);
public sealed record DebloatVendorPathValue(DebloatVendorPathTarget Target, string State,
    bool? Directory, bool? ReparsePoint, string Detail);
public sealed record DebloatVendorDataEvidence(IReadOnlyList<DebloatVendorPathValue> Paths,
    bool Complete, string Detail);

public static class DebloatVendorDataReview
{
    public static IReadOnlyList<DebloatVendorPathTarget> Targets(string programData,
        string programFiles, string windows)
    {
        string[] roots = [programData, programFiles, windows];
        if (roots.Any(root => string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root)))
            throw new ArgumentException("Vendor data roots must be absolute paths.");
        return Array.AsReadOnly<DebloatVendorPathTarget>(
        [
            new DebloatVendorPathTarget("McAfee program data", Path.Combine(programData, "McAfee"), @"$env:ProgramData\McAfee"),
            new DebloatVendorPathTarget("Norton Security program files", Path.Combine(programFiles, "Norton Security"), @"$env:ProgramFiles\Norton Security"),
            new DebloatVendorPathTarget("Rivet Networks driver folder", Path.Combine(windows, "System32", "drivers", "RivetNetworks"), @"C:\Windows\System32\drivers\RivetNetworks")
        ]);
    }

    public static DebloatVendorDataEvidence Review(Func<DebloatVendorPathTarget, DebloatVendorPathValue>? reader = null,
        CancellationToken token = default)
    {
        reader ??= Read;
        var targets = Targets(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        var values = new List<DebloatVendorPathValue>();
        foreach (var target in targets)
        {
            token.ThrowIfCancellationRequested();
            var value = reader(target);
            if (value.Target != target) throw new InvalidOperationException("Vendor path reader changed the requested target.");
            values.Add(value);
        }
        bool complete = values.All(value => value.State is "Present" or "Absent");
        return new(values.AsReadOnly(), complete,
            "Only exact machine paths from the source script were inspected. No directory was traversed and no file contents, profile data or browser data were read. Presence does not establish ownership or safe deletion.");
    }

    private static DebloatVendorPathValue Read(DebloatVendorPathTarget target)
    {
        try
        {
            if (!Directory.Exists(target.Path) && !File.Exists(target.Path))
                return new(target, "Absent", null, null, "The exact path was not present.");
            var attributes = File.GetAttributes(target.Path);
            bool directory = attributes.HasFlag(FileAttributes.Directory);
            bool reparse = attributes.HasFlag(FileAttributes.ReparsePoint);
            return new(target, "Present", directory, reparse,
                "The exact path exists. Type and reparse-point status were recorded without enumeration.");
        }
        catch (Exception error) when (error is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        { return new(target, "Unavailable", null, null, "The exact path could not be inspected: " + error.GetType().Name); }
    }
}
