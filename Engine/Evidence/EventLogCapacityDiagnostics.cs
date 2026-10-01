using System.Text.Json;

namespace KillerBench.Engine.Evidence;

public sealed record EventLogCapacitySnapshot(string Channel, bool? Enabled, string? Mode, long? FileSize,
    long? MaximumSize, long? RecordCount, bool? Full);

public static class EventLogCapacityDiagnostics
{
    public static IReadOnlyList<string> Channels { get; } = Array.AsReadOnly<string>(
    [
        "System", "Application", "Setup", "Microsoft-Windows-WindowsUpdateClient/Operational"
    ]);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<string, CancellationToken, Task<ToolResult>>? collect = null)
    {
        token.ThrowIfCancellationRequested();
        if (collect is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Event log capacity inspection requires Windows.");
        collect ??= CollectAsync;
        var checks = new List<WorkflowCheck>();
        foreach (string channel in Channels)
        {
            token.ThrowIfCancellationRequested();
            ToolResult evidence;
            try
            {
                evidence = await (collect(channel, token) ?? throw new InvalidOperationException("Capacity collector returned no task."))
                    .WaitAsync(TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
                TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
            { evidence = new(-1, "", error.GetType().Name + ": " + error.Message, false); }
            checks.Add(Interpret(channel, evidence));
        }
        return checks.AsReadOnly();
    }

    public static WorkflowCheck Interpret(string channel, ToolResult evidence)
    {
        if (!Channels.Contains(channel, StringComparer.Ordinal)) throw new ArgumentException("Choose a supported diagnostic event channel.", nameof(channel));
        ArgumentNullException.ThrowIfNull(evidence);
        WorkflowCheck Unknown(string detail) => new("Event log capacity: " + channel, CheckState.Unknown,
            detail + " No log content or configuration changed.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 8192)
            return Unknown("Capacity evidence was unavailable or incomplete.");
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            if (!Unique(document.RootElement)) return Unknown("Capacity evidence contained conflicting fields.");
            var value = document.RootElement.Deserialize<EventLogCapacitySnapshot>(Capture.JsonOptions);
            if (value is null || !string.Equals(value.Channel, channel, StringComparison.Ordinal) || value.Enabled is null ||
                value.Mode is not ("Circular" or "AutoBackup" or "Retain") || value.FileSize is null or < 0 ||
                value.MaximumSize is null or <= 0 || value.FileSize > value.MaximumSize * 2 || value.RecordCount is null or < 0 || value.Full is null)
                return Unknown("Capacity fields were missing, malformed or referred to another channel.");
            double used = Math.Min(100, value.FileSize.Value * 100d / value.MaximumSize.Value);
            bool saturationRisk = value.Enabled == true && value.Mode == "Retain" && (value.Full == true || used >= 95);
            CheckState state = value.Enabled == false || saturationRisk ? CheckState.Attention : CheckState.Information;
            string detail = "Enabled: " + value.Enabled + ". Mode: " + value.Mode + ". Size: " + value.FileSize + " of " +
                value.MaximumSize + " bytes (" + used.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "%). Records: " + value.RecordCount + ".";
            if (value.Enabled == false) detail += " The channel is disabled, so new diagnostic events may be missing.";
            if (saturationRisk) detail += " Retain mode is at or near capacity and can stop accepting new records. Preserve required evidence and review the archive policy before clearing or resizing the log.";
            else if (value.Mode == "Circular" && used >= 95) detail += " Circular mode can overwrite the oldest records as new events arrive.";
            else if (value.Mode == "AutoBackup" && used >= 95) detail += " Automatic backup mode can rotate the log when its size limit is reached; archive creation and free space were not verified.";
            detail += " This snapshot does not prove successful event delivery or available archive storage. No event, channel setting, permission or policy changed.";
            return new("Event log capacity: " + channel, state, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        { return Unknown("Capacity evidence could not be interpreted."); }
    }

    private static Task<ToolResult> CollectAsync(string channel, CancellationToken token)
    {
        string literal = channel.Replace("'", "''", StringComparison.Ordinal);
        string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$l=Get-WinEvent -ListLog '" + literal +
            "' -Force -ErrorAction Stop;[pscustomobject]@{channel=$l.LogName;enabled=$l.IsEnabled;mode=if($null -eq $l.LogMode){$null}else{$l.LogMode.ToString()};fileSize=$l.FileSize;maximumSize=$l.MaximumSizeInBytes;recordCount=$l.RecordCount;full=$l.IsLogFull}|ConvertTo-Json -Compress";
        return BoundedTool.RunAsync(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(15), token: token);
    }

    private static bool Unique(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 7) return false;
        return value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 7;
    }
}
