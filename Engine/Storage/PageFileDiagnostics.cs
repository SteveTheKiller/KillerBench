using System.Text.Json;

namespace KillerBench.Engine.Storage;

public static class PageFileDiagnostics
{
    private const int MaximumPageFiles = 16;
    private const string Scope = " Page-file allocation and usage are point-in-time operating-system evidence. This check does not establish sustained memory pressure, crash-dump compatibility, application capacity or disk performance. Full paths are excluded. No page file, memory setting, crash-dump setting, file, volume, registry value or policy changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$c=@(Get-CimInstance Win32_ComputerSystem -ErrorAction Stop|Select-Object -First 2);" +
        "$p=@(Get-CimInstance Win32_PageFileUsage -ErrorAction Stop|Sort-Object Name|Select-Object -First 17);" +
        "$complete=($c.Count-eq 1-and $p.Count-le 16);$rows=@();" +
        "foreach($x in @($p|Select-Object -First 16)){$root=[IO.Path]::GetPathRoot([string]$x.Name);if($root-notmatch '^[A-Za-z]:\\\\$'){$complete=$false;continue};" +
        "$rows+=[pscustomobject]@{drive=$root.Substring(0,2).ToUpperInvariant();allocatedMiB=[uint64]$x.AllocatedBaseSize;currentMiB=[uint64]$x.CurrentUsage;peakMiB=[uint64]$x.PeakUsage;temporary=[bool]$x.TempPageFile}};" +
        "[pscustomobject]@{complete=$complete;automaticManaged=$(if($c.Count-eq 1){[bool]$c[0].AutomaticManagedPagefile}else{$null});pageFiles=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Page-file inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Page-file provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Page-file provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Page-file inspection exceeded its overall deadline.",
                new(-1, "", "Page-file inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Page-file inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Page-file collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Boolean(root, "automaticManaged", out bool automaticManaged) ||
                !root.TryGetProperty("pageFiles", out var pageFiles) || pageFiles.ValueKind != JsonValueKind.Array ||
                pageFiles.GetArrayLength() > MaximumPageFiles)
                return Unknown("Page-file evidence was malformed, incomplete or exceeded its bound.", evidence);

            var rows = new List<PageFileRow>();
            var drives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in pageFiles.EnumerateArray())
            {
                if (!Unique(item, 5) || !Text(item, "drive", out string drive) ||
                    drive.Length != 2 || !char.IsAsciiLetterUpper(drive[0]) || drive[1] != ':' ||
                    !Unsigned(item, "allocatedMiB", out ulong allocated) || !Unsigned(item, "currentMiB", out ulong current) ||
                    !Unsigned(item, "peakMiB", out ulong peak) || !Boolean(item, "temporary", out bool temporary) ||
                    allocated == 0 || current > allocated || peak > allocated || current > peak || !drives.Add(drive))
                    return Unknown("Page-file evidence contained invalid, contradictory or duplicate allocation data.", evidence);
                rows.Add(new(drive, allocated, current, peak, temporary));
            }
            if (rows.Count == 0)
                return new("Paging file", CheckState.Attention,
                    $"Windows reported no currently allocated paging file. Automatic management is {(automaticManaged ? "enabled" : "disabled")}. Review application commit requirements and crash-dump requirements before changing this intentional or transitional configuration." + Scope,
                    Evidence: evidence);

            var temporaryFiles = rows.Where(row => row.Temporary).ToArray();
            var pressure = rows.Where(row => row.CurrentMiB * 10 >= row.AllocatedMiB * 9).ToArray();
            string detail = $"Windows reports {(automaticManaged ? "automatic" : "manual")} page-file management and {rows.Count} allocated paging file{(rows.Count == 1 ? "" : "s")}: " +
                string.Join("; ", rows.Select(row => $"{row.Drive} allocated {row.AllocatedMiB} MiB, current {row.CurrentMiB} MiB, peak {row.PeakMiB} MiB{(row.Temporary ? ", temporary" : "")}")) + ".";
            if (temporaryFiles.Length > 0)
                detail += " Windows identifies a temporary paging file on " + string.Join(", ", temporaryFiles.Select(row => row.Drive)) +
                    ". Review the intended configuration and boot-time event evidence before replacing or resizing anything.";
            if (pressure.Length > 0)
                detail += " Current usage is at least 90% of allocated capacity on " + string.Join(", ", pressure.Select(row => row.Drive)) +
                    ". Correlate with sustained memory and commit-limit evidence before changing size.";
            if (temporaryFiles.Length == 0 && pressure.Length == 0)
                detail += " No temporary file or current usage at or above 90% of allocation was reported.";
            return new("Paging file", temporaryFiles.Length > 0 || pressure.Length > 0 ? CheckState.Attention : CheckState.Information,
                detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Page-file evidence could not be interpreted.", evidence);
        }
    }

    private sealed record PageFileRow(string Drive, ulong AllocatedMiB, ulong CurrentMiB, ulong PeakMiB, bool Temporary);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Paging file", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Unsigned(JsonElement owner, string name, out ulong value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt64(out value) && value <= uint.MaxValue;
    }
    private static bool Text(JsonElement owner, string name, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 or > 8 || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
