using System.Text.Json;
using System.Text.Json.Serialization;

namespace KillerBench.Engine;

public enum CollectionStatus { Collected, Unavailable, AccessDenied, Failed, Canceled, Truncated }
public sealed record CaptureSource(string Name, string Kind, CollectionStatus Status, string Detail, TextEvidence? Evidence);
public sealed record CaptureSession(int SchemaVersion, DateTimeOffset StartedUtc, DateTimeOffset FinishedUtc,
    string Machine, string OperatingSystem, string Architecture, string Elevation, IReadOnlyList<CaptureSource> Sources)
{
    [JsonIgnore] public bool Partial => Sources.Any(s => s.Status != CollectionStatus.Collected);
}
public interface IEvidenceCollector
{
    string Name { get; }
    Task<CaptureSource> CollectAsync(CancellationToken cancellationToken);
}

public static class Capture
{
    public const int MaximumSources = 24;
    public const int MaximumFileBytes = 64 * 1024 * 1024;
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }, MaxDepth = 32
    };

    public static async Task<CaptureSession> CollectAsync(IEnumerable<IEvidenceCollector> collectors,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var selected = collectors.Take(MaximumSources + 1).ToArray();
        if (selected.Length > MaximumSources) throw new ArgumentException("Too many collectors.", nameof(collectors));
        var started = DateTimeOffset.UtcNow;
        var sources = new List<CaptureSource>();
        foreach (var collector in selected)
        {
            progress?.Report("Collecting: " + collector.Name);
            CaptureSource result;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                result = await collector.CollectAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { result = new(collector.Name, "Unavailable", CollectionStatus.Canceled, cancellationToken.IsCancellationRequested ? "Scan stopped. Source not collected." : "Source exceeded its 15-second limit.", null); }
            catch (UnauthorizedAccessException)
            { result = new(collector.Name, "Unavailable", CollectionStatus.AccessDenied, "Access denied. Try an elevated run if this source is needed.", null); }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            { result = new(collector.Name, "Unavailable", CollectionStatus.Unavailable, "Source is not present.", null); }
            catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or System.Xml.XmlException or InvalidOperationException)
            { result = new(collector.Name, "Unavailable", CollectionStatus.Failed, ex.Message[..Math.Min(ex.Message.Length, 1024)], null); }
            sources.Add(result);
            progress?.Report($"{result.Name}: {result.Status}. {result.Detail}");
        }
        string elevation = "Unknown";
        if (OperatingSystem.IsWindows())
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            elevation = new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? "Administrator" : "Standard user";
        }
        var session = new CaptureSession(1, started, DateTimeOffset.UtcNow, Environment.MachineName,
            System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(), elevation, sources);
        Validate(session);
        return session;
    }

    public static InvestigationReport Analyze(CaptureSource source, CancellationToken cancellationToken = default)
    {
        if (source.Evidence is null) throw new InvalidOperationException("Source has no evidence.");
        return source.Kind == "Events" ? EventEvidence.Analyze(source.Evidence, cancellationToken) : Investigation.Analyze(source.Evidence, cancellationToken);
    }

    public static string ReportJson(CaptureSession session) => JsonSerializer.Serialize(new
    {
        session.SchemaVersion, session.StartedUtc, session.FinishedUtc, session.Machine, session.OperatingSystem,
        session.Architecture, session.Elevation, session.Partial,
        sources = session.Sources.Select(s => new { s.Name, s.Status, s.Detail, report = s.Evidence is null ? null : Analyze(s) })
    }, JsonOptions);

    public static async Task SaveAsync(CaptureSession session, string path, CancellationToken cancellationToken = default, bool overwrite = true)
    {
        Validate(session);
        string full = Path.GetFullPath(path);
        if (session.Sources.Any(s => s.Evidence is not null && string.Equals(s.Evidence.Source, full, StringComparison.OrdinalIgnoreCase)))
            throw new IOException("Choose a destination separate from source evidence.");
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(session, JsonOptions);
        if (data.Length > MaximumFileBytes) throw new InvalidDataException("Capture exceeds the file limit.");
        string temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, data, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, full, overwrite);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static async Task<CaptureSession> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        if (file.Length > MaximumFileBytes) throw new InvalidDataException("Capture exceeds the 64 MB limit.");
        try
        {
            var session = await JsonSerializer.DeserializeAsync<CaptureSession>(file, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (session is null) throw new InvalidDataException("Empty capture.");
            Validate(session);
            return session;
        }
        catch (JsonException ex) { throw new InvalidDataException("Invalid KillerBench capture.", ex); }
    }

    private static void Validate(CaptureSession session)
    {
        if (session.SchemaVersion != 1 || session.Sources is null || session.Sources.Count > MaximumSources ||
            session.StartedUtc > session.FinishedUtc || !Valid(session.Machine) || !Valid(session.OperatingSystem) || !Valid(session.Architecture) || !Valid(session.Elevation))
            throw new InvalidDataException("Unsupported or invalid capture metadata.");
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in session.Sources)
        {
            if (source is null || !Valid(source.Name) || !Valid(source.Kind) || !Valid(source.Detail) || !Enum.IsDefined(source.Status))
                throw new InvalidDataException("Invalid capture source.");
            if (source.Evidence is not { } evidence)
            {
                if (source.Status is CollectionStatus.Collected or CollectionStatus.Truncated) throw new InvalidDataException("Collected evidence is missing.");
                continue;
            }
            if (source.Status is not (CollectionStatus.Collected or CollectionStatus.Truncated) || source.Kind is not ("Text" or "Events") ||
                !Valid(evidence.Source) || !identities.Add(evidence.Source) || evidence.Lines is null || evidence.Lines.Length > TextEvidence.MaximumCharacters + 1 ||
                evidence.Lines.Any(l => l is null || l.Contains('\0') || l.Contains('\n') || l.Contains('\r')) ||
                evidence.Lines.Sum(l => (long)l.Length) + Math.Max(0, evidence.Lines.Length - 1) > TextEvidence.MaximumCharacters ||
                evidence.Truncated != (source.Status == CollectionStatus.Truncated))
                throw new InvalidDataException("Invalid or oversized evidence.");
        }
        static bool Valid(string? value) => value is not null && value.Length <= 4096;
    }
}
