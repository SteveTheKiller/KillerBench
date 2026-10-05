using System.Text.Json;

namespace KillerBench.Engine.Updates;

public static class ReservedStorageDiagnostics
{
    private const string Scope = " Reserved storage can improve servicing reliability, but disabled state can be intentional on managed systems. This check does not establish update applicability, sufficient total capacity or policy ownership. No reserved-storage, package, feature, update, disk or policy setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';Import-Module Dism -ErrorAction Stop;" +
        "$x=Get-WindowsReservedStorageState -ErrorAction Stop;[pscustomobject]@{state=[string]$x.ReservedStorageState}|ConvertTo-Json -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Reserved storage inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Reserved-storage provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Reserved-storage provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Reserved-storage inspection exceeded its overall deadline.", new(-1, "", "Reserved-storage inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Reserved-storage inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown("Reserved-storage collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            if (!Unique(root, 1) || !Text(root, "state", out string state)) return Unknown("Reserved-storage evidence was malformed or incomplete.", evidence);
            if (state.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
                return new("Windows reserved storage", CheckState.Information, "Reserved storage is enabled for the online Windows image." + Scope, Evidence: evidence);
            if (state.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
                return new("Windows reserved storage", CheckState.Information, "Reserved storage is disabled for the online Windows image. Keep this as servicing context and review management intent before considering any separate change." + Scope, Evidence: evidence);
            if (state.Equals("InUse", StringComparison.OrdinalIgnoreCase))
                return new("Windows reserved storage", CheckState.Information, "Reserved storage is currently in use by the online Windows image. Servicing activity should be allowed to finish before any separate configuration review." + Scope, Evidence: evidence);
            return Unknown("Reserved storage reported an unsupported state: " + state + ".", evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Unknown("Reserved-storage evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Windows reserved storage", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Text(JsonElement owner, string name, out string value) { value = ""; if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || text.Length is < 1 or > 64 || text != text.Trim() || text.Any(char.IsControl)) return false; value = text; return true; }
}
