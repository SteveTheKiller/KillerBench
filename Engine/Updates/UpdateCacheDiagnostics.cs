using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record UpdateCacheArea(bool Complete, bool Exists, bool ReparsePoint,
    long FileCount, long TotalBytes, bool Capped);

public static class UpdateCacheDiagnostics
{
    private const long FileLimit = 100_000;
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "function Measure-Area([string]$path){" +
        "if(-not [IO.Directory]::Exists($path)){return [pscustomobject]@{complete=$true;exists=$false;reparse=$false;files=0L;bytes=0L;capped=$false}};" +
        "$root=Get-Item -LiteralPath $path -Force -ErrorAction Stop;" +
        "if(($root.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){return [pscustomobject]@{complete=$false;exists=$true;reparse=$true;files=0L;bytes=0L;capped=$false}};" +
        "$items=@(Get-ChildItem -LiteralPath $path -Force -File -Recurse -ErrorAction Stop | Select-Object -First 100001);" +
        "$capped=$items.Count -gt 100000;if($capped){$items=@($items | Select-Object -First 100000)};" +
        "$bytes=0L;foreach($item in $items){$bytes+=[long]$item.Length};" +
        "[pscustomobject]@{complete=(-not $capped);exists=$true;reparse=$false;files=[long]$items.Count;bytes=$bytes;capped=$capped}};" +
        "$root=Join-Path $env:windir 'SoftwareDistribution';" +
        "[pscustomobject]@{download=(Measure-Area (Join-Path $root 'Download'));dataStore=(Measure-Area (Join-Path $root 'DataStore'))}|ConvertTo-Json -Compress -Depth 3";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows Update cache inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var step = new RepairStep(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);
            var evidence = await (execute(step, deadline.Token)
                ?? throw new InvalidOperationException("Update-cache provider returned no task."))
                .WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Update-cache provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Windows Update cache inspection exceeded its overall deadline.",
                new(-1, "", "Update-cache inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Windows Update cache inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Windows Update cache collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !root.TryGetProperty("download", out var downloadElement) ||
                !root.TryGetProperty("dataStore", out var dataStoreElement) ||
                !TryRead(downloadElement, out var download) || !TryRead(dataStoreElement, out var dataStore))
                return Unknown("Windows Update cache evidence was malformed or inconsistent.", evidence);
            if (!download.Complete || !dataStore.Complete)
                return Unknown("Windows Update cache inventory reached its file limit or encountered a reparse-point root. Counts are partial.", evidence);
            string detail = "Download cache: " + Describe(download) + ". Data store: " + Describe(dataStore) +
                ". These counts do not establish corruption, update applicability or a need for cleanup. No filename, file content, service, cache, update or policy setting changed.";
            return new("Windows Update cache", CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Windows Update cache evidence could not be interpreted.", evidence);
        }
    }

    private static string Describe(UpdateCacheArea area) => area.Exists
        ? area.FileCount + " files, " + FormatBytes(area.TotalBytes)
        : "directory not present";

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return value.ToString(unit == 0 ? "0" : "0.##", System.Globalization.CultureInfo.InvariantCulture) + " " + units[unit];
    }

    private static bool TryRead(JsonElement value, out UpdateCacheArea area)
    {
        area = new(false, false, false, 0, 0, false);
        if (!Unique(value, 6) || !Boolean(value, "complete", out bool complete) ||
            !Boolean(value, "exists", out bool exists) || !Boolean(value, "reparse", out bool reparse) ||
            !Integer(value, "files", FileLimit, out long files) || !Integer(value, "bytes", long.MaxValue, out long bytes) ||
            !Boolean(value, "capped", out bool capped)) return false;
        if (!exists && (!complete || reparse || files != 0 || bytes != 0 || capped) ||
            exists && complete == (capped || reparse)) return false;
        area = new(complete, exists, reparse, files, bytes, capped);
        return true;
    }

    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected &&
        value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool result)
    {
        result = false;
        if (!owner.TryGetProperty(name, out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        result = value.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, long maximum, out long result)
    {
        result = 0;
        return owner.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt64(out result) && result >= 0 && result <= maximum;
    }
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Windows Update cache", CheckState.Unknown,
        detail + " No filename, file content, service, cache, update or policy setting changed.", Evidence: evidence);
}
