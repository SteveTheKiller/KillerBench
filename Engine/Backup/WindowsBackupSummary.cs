using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine.Backup;

public static class WindowsBackupSummary
{
    private const string Name = "Windows Backup history";
    private const string Boundary = " This covers only the local WindowsServerBackup provider. It does not inspect third-party backup products, File History, destination contents, catalog integrity or restore success. No backup, schedule, catalog, destination, credential, retention setting or service changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "function Empty($available,$complete){[pscustomobject]@{providerAvailable=$available;complete=$complete;hasSummary=$false;lastBackupUtc=$null;lastSuccessUtc=$null;nextBackupUtc=$null;lastResult=$null}|ConvertTo-Json -Compress};" +
        "$command=Get-Command Get-WBSummary -ErrorAction SilentlyContinue;if($null-eq$command){Empty $false $true;exit 0};" +
        "try{$items=@(Get-WBSummary -ErrorAction Stop);if($items.Count-eq 0){Empty $true $true;exit 0};if($items.Count-ne 1){Empty $true $false;exit 0};$summary=$items[0];" +
        "function Read-Time($name){$property=$summary.PSObject.Properties[$name];if($null-eq$property-or$null-eq$property.Value){return $null};$value=[datetime]$property.Value;if($value-eq[datetime]::MinValue){return $null};return ([DateTimeOffset]$value).ToUniversalTime().ToString('o')};" +
        "$resultProperty=$summary.PSObject.Properties['LastBackupResultHR'];$result=if($null-eq$resultProperty-or$null-eq$resultProperty.Value){$null}else{[convert]::ToInt64($resultProperty.Value)};" +
        "[pscustomobject]@{providerAvailable=$true;complete=$true;hasSummary=$true;lastBackupUtc=(Read-Time 'LastBackupTime');lastSuccessUtc=(Read-Time 'LastSuccessfulBackupTime');nextBackupUtc=(Read-Time 'NextBackupTime');lastResult=$result}|ConvertTo-Json -Compress}" +
        "catch{Empty $true $false;exit 0}";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows Backup history requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Windows Backup provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Windows Backup provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.",
                new(-1, "", "Windows Backup history deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Collection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset? observedUtc = null)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            if (!Unique(root, 7) || !Boolean(root, "providerAvailable", out bool available) ||
                !Boolean(root, "complete", out bool complete) || !Boolean(root, "hasSummary", out bool hasSummary) ||
                !NullableTime(root, "lastBackupUtc", out var lastBackup) ||
                !NullableTime(root, "lastSuccessUtc", out var lastSuccess) ||
                !NullableTime(root, "nextBackupUtc", out var nextBackup) ||
                !NullableInt64(root, "lastResult", out long? lastResult))
                return Unknown("Evidence was malformed.", evidence);
            if (!complete) return Unknown("The installed provider did not return one complete summary.", evidence);
            if (!available)
            {
                if (hasSummary || lastBackup is not null || lastSuccess is not null || nextBackup is not null || lastResult is not null)
                    return Unknown("Provider availability evidence was contradictory.", evidence);
                return new(Name, CheckState.Information,
                    "The WindowsServerBackup provider is not installed or registered, so no Windows Backup summary is available." + Boundary,
                    Evidence: evidence);
            }
            if (!hasSummary)
            {
                if (lastBackup is not null || lastSuccess is not null || nextBackup is not null || lastResult is not null)
                    return Unknown("Empty summary evidence included backup results.", evidence);
                return new(Name, CheckState.Information,
                    "The installed provider returned no summary. This does not establish that no backup has ever run or that protection is unnecessary." + Boundary,
                    Evidence: evidence);
            }
            DateTimeOffset now = observedUtc ?? DateTimeOffset.UtcNow;
            if (lastBackup is null || lastResult is null || lastBackup > now.AddMinutes(5) ||
                lastSuccess > now.AddMinutes(5) || lastSuccess > lastBackup ||
                lastResult == 0 && lastSuccess is null)
                return Unknown("Summary dates or result evidence were incomplete or contradictory.", evidence);

            string result = "0x" + unchecked((uint)lastResult.Value).ToString("X8", CultureInfo.InvariantCulture);
            string schedule = nextBackup is null ? "No next scheduled time was reported." : "A next scheduled time was reported for " + nextBackup.Value.ToString("u", CultureInfo.InvariantCulture) + ".";
            if (lastResult == 0)
                return new(Name, CheckState.Information,
                    "The provider reports the last backup completed successfully at " + lastBackup.Value.ToString("u", CultureInfo.InvariantCulture) + ". " + schedule + Boundary,
                    Evidence: evidence);
            string success = lastSuccess is null ? "No last successful time was reported." : "The last successful backup was reported at " + lastSuccess.Value.ToString("u", CultureInfo.InvariantCulture) + ".";
            return new(Name, CheckState.Attention,
                "The provider reports a nonzero last-backup result " + result + " at " + lastBackup.Value.ToString("u", CultureInfo.InvariantCulture) + ". " + success + " " + schedule + " Review the exact job and event evidence; this result alone does not identify a cause or authorize repair." + Boundary,
                Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new(Name, CheckState.Unknown, detail + Boundary, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool NullableInt64(JsonElement owner, string name, out long? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt64(out long number)) return false;
        value = number;
        return true;
    }
    private static bool NullableTime(JsonElement owner, string name, out DateTimeOffset? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            !DateTimeOffset.TryParseExact(text, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp)) return false;
        value = timestamp.ToUniversalTime();
        return true;
    }
}
