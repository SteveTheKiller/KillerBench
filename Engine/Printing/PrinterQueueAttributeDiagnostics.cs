using System.Text.Json;

namespace KillerBench.Engine.Printing;

public static class PrinterQueueAttributeDiagnostics
{
    private const int MaximumQueues = 256;
    private const uint KnownMask = 0x00FEFFFF;
    private const string Scope = " These are point-in-time local queue attributes. They do not prove printer, spooler, driver, port, document or physical-output health. No queue, job, document, share, directory publication or printer setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-CimInstance Win32_Printer -OperationTimeoutSec 20 -ErrorAction Stop|Where-Object{$_.Local-eq $true}|Sort-Object Name|Select-Object -First 257);" +
        "$rows=@($all|Select-Object -First 256|ForEach-Object{[pscustomobject]@{name=[string]$_.Name;attributes=[uint32]$_.Attributes}});" +
        "[pscustomobject]@{complete=($all.Count-le 256);queues=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Printer queue attribute inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Printer queue attribute provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Printer queue attribute provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return [Unknown("Printer queue attribute inspection exceeded its overall deadline.", new(-1, "", "Printer queue attribute deadline expired.", false) { TimedOut = true })]; }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return [Unknown("Printer queue attribute inspection was unavailable.", new(-1, "", error.Message, false))]; }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 1_000_000)
            return [Unknown("Printer queue attribute collection failed or returned incomplete evidence.", evidence)];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("queues", out var queues) || queues.ValueKind != JsonValueKind.Array || queues.GetArrayLength() > MaximumQueues)
                return [Unknown("Printer queue attribute evidence was malformed, incomplete or exceeded its queue bound.", evidence)];
            if (queues.GetArrayLength() == 0)
                return [new("Local printer queue attributes", CheckState.Information, "No local printer queue returned an attribute record." + Scope, Evidence: evidence)];

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<WorkflowCheck>();
            foreach (var row in queues.EnumerateArray())
            {
                if (!Unique(row, 2) || !Text(row, "name", 512, out string name) || !names.Add(name) ||
                    !Unsigned(row, "attributes", out uint attributes))
                    return [Unknown("Printer queue attribute evidence contained an invalid or duplicate queue.", evidence)];

                bool queued = Has(attributes, 0x1), direct = Has(attributes, 0x2), shared = Has(attributes, 0x8),
                    hidden = Has(attributes, 0x20), keepPrinted = Has(attributes, 0x100), completeFirst = Has(attributes, 0x200),
                    workOffline = Has(attributes, 0x400), bidi = Has(attributes, 0x800), rawOnly = Has(attributes, 0x1000), published = Has(attributes, 0x2000);
                uint unknownFlags = attributes & ~KnownMask;
                bool contradictory = queued && direct;
                bool attention = direct || keepPrinted || workOffline || published && !shared;
                var details = new List<string>
                {
                    "Spooling mode: " + (queued ? "Queued" : direct ? "Direct" : "Not explicitly reported") + ".",
                    "Shared: " + YesNo(shared) + "; published: " + YesNo(published) + "; hidden: " + YesNo(hidden) + ".",
                    "Keep printed documents: " + YesNo(keepPrinted) + "; complete spooling first: " + YesNo(completeFirst) +
                    "; work offline: " + YesNo(workOffline) + "; bidirectional support: " + YesNo(bidi) + "; raw jobs only: " + YesNo(rawOnly) + "."
                };
                if (direct) details.Add("Direct printing bypasses normal queue spooling and can change troubleshooting and availability behavior.");
                if (keepPrinted) details.Add("Printed documents are retained in the queue. Review storage and document privacy requirements.");
                if (workOffline) details.Add("The queue is configured to accept work while its printer is unavailable.");
                if (published && !shared) details.Add("The queue reports directory publication without the shared attribute. Review this exact queue configuration.");
                if (!attention && !contradictory && unknownFlags == 0) details.Add("No review condition was identified in the supported queue attributes.");
                if (unknownFlags != 0) details.Add("Unsupported attribute flags were present: 0x" + unknownFlags.ToString("X8", System.Globalization.CultureInfo.InvariantCulture) + ".");
                results.Add(new("Printer queue attributes: " + name,
                    contradictory || unknownFlags != 0 ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Information,
                    string.Join(" ", details) + Scope, Evidence: evidence));
            }
            return results.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        { return [Unknown("Printer queue attribute evidence could not be interpreted.", evidence)]; }
    }

    private static bool Has(uint value, uint flag) => (value & flag) != 0;
    private static string YesNo(bool value) => value ? "Yes" : "No";
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Local printer queue attributes", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool Unsigned(JsonElement owner, string name, out uint value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt32(out value); }
    private static bool Text(JsonElement owner, string name, int maximum, out string value) { value = ""; if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false; value = text; return true; }
}
