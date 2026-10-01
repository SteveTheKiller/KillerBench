using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record ThirdPartyUpdateCandidate(string Id, string Name, string InstalledVersion, string Source,
    IReadOnlyList<string> AvailableVersions);

public sealed record ThirdPartyUpdateResult(bool ProviderAvailable, string ProviderVersion, bool Complete, int Scanned,
    IReadOnlyList<ThirdPartyUpdateCandidate> Candidates, string Detail)
{
    public ToolResult? Evidence { get; init; }

    public string Format() => "THIRD-PARTY APPLICATION UPDATES / DISCOVERY ONLY\n" + Detail + "\n" +
        $"Provider available: {ProviderAvailable}; provider version: {(string.IsNullOrEmpty(ProviderVersion) ? "unavailable" : ProviderVersion)}; installed packages scanned: {Scanned}; candidates: {Candidates.Count}; complete: {Complete}\n\n" +
        string.Join("\n", Candidates.Select(candidate => $"{candidate.Name}\n  ID: {candidate.Id}; installed: {candidate.InstalledVersion}; source: {candidate.Source}; available: {string.Join(", ", candidate.AvailableVersions)}")) +
        "\n\nNo application was downloaded, updated or removed. An empty result does not prove that every installed application is current.";
}

public static class ThirdPartyUpdateDiscovery
{
    public const string Tool = "pwsh.exe";
    private const int MaximumInstalledPackages = 4096;
    private const int MaximumCandidates = 256;
    private const int MaximumVersions = 64;
    private const string Scope = " The Microsoft.WinGet.Client provider is read only in this workflow. It can omit uncorrelated packages and currently has known provider-specific detection limits. No source, agreement, application or policy changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$module=@(Get-Module -ListAvailable -Name Microsoft.WinGet.Client|Sort-Object Version -Descending|Select-Object -First 1);" +
        "if($module.Count-ne 1){[pscustomobject]@{providerAvailable=$false;providerVersion='';complete=$false;scanned=0;detail='Microsoft.WinGet.Client is not installed for PowerShell 7.';candidates=@()}|ConvertTo-Json -Depth 5 -Compress;exit 0};" +
        "Import-Module $module[0].Path -ErrorAction Stop;" +
        "$all=@(Get-WinGetPackage -Count 4097 -ErrorAction Stop);$complete=($all.Count-le 4096);$rows=@();" +
        "foreach($package in @($all|Select-Object -First 4096)){try{$available=[bool]$package.IsUpdateAvailable}catch{$complete=$false;continue};if(!$available){continue};" +
        "try{$versions=@($package.AvailableVersions|ForEach-Object{[string]$_}|Select-Object -First 65)}catch{$complete=$false;continue};" +
        "if($versions.Count-gt 64){$complete=$false;$versions=@($versions|Select-Object -First 64)};" +
        "$rows+=,[pscustomobject]@{id=[string]$package.Id;name=[string]$package.Name;installedVersion=[string]$package.InstalledVersion;source=[string]$package.Source;availableVersions=$versions}};" +
        "if($rows.Count-gt 256){$complete=$false};$rows=@($rows|Select-Object -First 256);" +
        "[pscustomobject]@{providerAvailable=$true;providerVersion=[string]$module[0].Version;complete=$complete;scanned=[Math]::Min(4096,$all.Count);detail='Microsoft.WinGet.Client inspected installed package correlations and reported update candidates.';candidates=$rows}|ConvertTo-Json -Depth 5 -Compress";

    public static RepairStep QueryStep() => new(Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);

    public static async Task<ThirdPartyUpdateResult> ReadAsync(IProgress<string>? progress = null,
        CancellationToken token = default, Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Third-party update discovery requires Windows.");
        progress = progress is null ? null : new DiagnosticProgress(progress);
        progress?.Report("Searching for third-party application updates through Microsoft.WinGet.Client...");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromMinutes(5), token: cancellationToken);
            var evidence = await (execute(QueryStep(), deadline.Token)
                ?? throw new InvalidOperationException("Third-party update provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Third-party update provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return Unknown("Third-party update discovery exceeded its overall deadline.", new(-1, "", "Third-party update deadline expired.", false) { TimedOut = true }); }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return Unknown("Third-party update discovery was unavailable: " + error.Message, new(-1, "", error.Message, false)); }
    }

    public static ThirdPartyUpdateResult Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 2_000_000)
            return Unknown("Third-party update collection failed or returned incomplete evidence.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 7 });
            var root = document.RootElement;
            if (!Unique(root, 6) || !Boolean(root, "providerAvailable", out bool providerAvailable) ||
                !Text(root, "providerVersion", 64, true, out string providerVersion) || !Boolean(root, "complete", out bool complete) ||
                !Integer(root, "scanned", 0, MaximumInstalledPackages, out int scanned) || !Text(root, "detail", 512, false, out string detail) ||
                !root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() > MaximumCandidates)
                return Unknown("Third-party update evidence was malformed or exceeded its package bound.", evidence);
            if (!providerAvailable)
            {
                if (complete || scanned != 0 || providerVersion.Length != 0 || candidates.GetArrayLength() != 0)
                    return Unknown("Third-party update provider evidence was contradictory.", evidence);
                return new(false, "", false, 0, [], detail + Scope) { Evidence = evidence };
            }
            if (string.IsNullOrEmpty(providerVersion)) return Unknown("Third-party update provider identity was incomplete.", evidence);
            var identities = new HashSet<(string Id, string Source)>(new PackageIdentityComparer());
            var results = new List<ThirdPartyUpdateCandidate>();
            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!Unique(candidate, 5) || !Text(candidate, "id", 256, false, out string id) ||
                    !Text(candidate, "name", 512, false, out string name) || !Text(candidate, "installedVersion", 256, false, out string installed) ||
                    !Text(candidate, "source", 128, false, out string source) || !identities.Add((id, source)) ||
                    !candidate.TryGetProperty("availableVersions", out var versions) || versions.ValueKind != JsonValueKind.Array ||
                    versions.GetArrayLength() is < 1 or > MaximumVersions)
                    return Unknown("Third-party update evidence contained an invalid or duplicate package.", evidence);
                var available = new List<string>();
                var versionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var version in versions.EnumerateArray())
                {
                    if (version.ValueKind != JsonValueKind.String || version.GetString() is not { } value || !ValidText(value, 256, false) || !versionSet.Add(value))
                        return Unknown("Third-party update evidence contained an invalid or duplicate version.", evidence);
                    available.Add(value);
                }
                results.Add(new(id, name, installed, source, available.AsReadOnly()));
            }
            return new(true, providerVersion, complete, scanned, results.AsReadOnly(), detail + Scope) { Evidence = evidence };
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        { return Unknown("Third-party update evidence could not be interpreted.", evidence); }
    }

    public static WorkflowCheck Assess(ThirdPartyUpdateResult result) => new("Third-party application updates",
        result.ProviderAvailable && result.Complete ? CheckState.Information : CheckState.Unknown, result.Format(), Evidence: result.Evidence);

    private static ThirdPartyUpdateResult Unknown(string detail, ToolResult evidence) => new(false, "", false, 0, [], detail + Scope) { Evidence = evidence };
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value) && value >= minimum && value <= maximum; }
    private static bool Text(JsonElement owner, string name, int maximum, bool allowEmpty, out string value) { value = ""; if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || !ValidText(text, maximum, allowEmpty)) return false; value = text; return true; }
    private static bool ValidText(string text, int maximum, bool allowEmpty) => (allowEmpty || text.Length > 0) && text.Length <= maximum && text == text.Trim() && !text.Any(char.IsControl);

    private sealed class PackageIdentityComparer : IEqualityComparer<(string Id, string Source)>
    {
        public bool Equals((string Id, string Source) x, (string Id, string Source) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Id, y.Id) && StringComparer.OrdinalIgnoreCase.Equals(x.Source, y.Source);
        public int GetHashCode((string Id, string Source) value) => HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.Id), StringComparer.OrdinalIgnoreCase.GetHashCode(value.Source));
    }
}
