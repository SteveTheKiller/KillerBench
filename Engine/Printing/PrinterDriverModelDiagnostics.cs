using System.Text.Json;

namespace KillerBench.Engine;

public static class PrinterDriverModelDiagnostics
{
    private const int MaximumDrivers = 256;
    private const string Scope = " Driver model and package-awareness are registration evidence. Version 3 and non-package-aware drivers can be valid, especially for local or specialized devices. This check does not establish package trust, compatibility, Point and Print policy, rendering or physical output. No driver, package, queue, policy or setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-PrinterDriver -ErrorAction Stop|Sort-Object PrinterEnvironment,Name|Select-Object -First 257);" +
        "$rows=@($all|Select-Object -First 256|ForEach-Object{[pscustomobject]@{name=[string]$_.Name;environment=[string]$_.PrinterEnvironment;majorVersion=[int64]$_.MajorVersion;packageAware=[bool]$_.IsPackageAware}});" +
        "[pscustomobject]@{complete=($all.Count-le 256);drivers=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Printer driver model inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Printer-driver model provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Printer-driver model provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Printer-driver model inspection exceeded its overall deadline.", new(-1, "", "Printer-driver model inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Printer-driver model inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown("Printer-driver model collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete || !root.TryGetProperty("drivers", out var drivers) || drivers.ValueKind != JsonValueKind.Array || drivers.GetArrayLength() > MaximumDrivers)
                return Unknown("Printer-driver model evidence was malformed, incomplete or exceeded its bound.", evidence);
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int version3 = 0, version4 = 0, other = 0, packageAware = 0;
            foreach (var driver in drivers.EnumerateArray())
            {
                if (!Unique(driver, 4) || !Text(driver, "name", out string name) || !Text(driver, "environment", out string environment) ||
                    !Integer(driver, "majorVersion", 0, 255, out int version) || !Boolean(driver, "packageAware", out bool aware) || !identities.Add(name + "\0" + environment))
                    return Unknown("Printer-driver model evidence contained an invalid or duplicate registration.", evidence);
                if (version == 3) version3++; else if (version == 4) version4++; else other++;
                if (aware) packageAware++;
            }
            if (drivers.GetArrayLength() == 0) return new("Printer driver models", CheckState.Information, "No local printer driver registrations were reported for this process account." + Scope, Evidence: evidence);
            string detail = $"Reviewed {drivers.GetArrayLength()} registrations: {version3} version 3, {version4} version 4 and {other} other model versions. {packageAware} are package-aware and {drivers.GetArrayLength() - packageAware} are not package-aware.";
            if (other > 0) detail += " One or more registrations reported a model version outside the recognized version 3 and version 4 values. Review the exact registered package before changing it.";
            else detail += " Version 3 and non-package-aware registrations are retained as troubleshooting context, not classified as broken.";
            return new("Printer driver models", other > 0 ? CheckState.Attention : CheckState.Information, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Printer-driver model evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Printer driver models", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value) && value >= minimum && value <= maximum; }
    private static bool Text(JsonElement owner, string name, out string value) { value = ""; if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || text.Length is < 1 or > 512 || text != text.Trim() || text.Any(char.IsControl)) return false; value = text; return true; }
}
