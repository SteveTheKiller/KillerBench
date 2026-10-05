using System.Text.Json;

namespace KillerBench.Engine.Printing;

public static class PrinterPortDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$queues=@(Get-Printer -ErrorAction Stop | Select-Object -First 257 Name,PortName,Type); " +
        "$ports=@(Get-PrinterPort -ErrorAction Stop | Select-Object -First 513 Name); " +
        "[pscustomobject]@{queues=$queues;ports=$ports} | ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Printer port inventory requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, ct) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: ct);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Printer port provider returned no task.");
            var result = await pending.WaitAsync(deadline.Token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return Interpret(result ?? throw new InvalidOperationException("Printer port provider returned no evidence."));
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return Interpret(new(-1, "", "Printer port collection deadline expired.", false) { TimedOut = true }); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return Interpret(new(-1, "", error.Message, false) { TimedOut = error is TimeoutException }); }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown(string detail) => new("Printer port inventory", CheckState.Unknown, detail, Evidence: result);
        const string scope = " Registration snapshot only; port reachability and physical printing were not tested. No settings changed.";
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Queue/port collection failed or was incomplete." + scope)];
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = json.RootElement;
            if (!Unique(root) || !root.TryGetProperty("queues", out var queues) || queues.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("ports", out var ports) || ports.ValueKind != JsonValueKind.Array ||
                queues.GetArrayLength() > 256 || ports.GetArrayLength() > 512)
                return [Unknown("Queue/port inventory was missing, ambiguous or exceeded its coverage limit." + scope)];
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var port in ports.EnumerateArray())
                if (!Unique(port) || Text(port, "Name") is not { } name || !known.Add(name))
                    return [Unknown("Port identities were missing or duplicated." + scope)];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var checks = new List<WorkflowCheck>();
            foreach (var queue in queues.EnumerateArray())
            {
                if (!Unique(queue) || Text(queue, "Name") is not { } name || !seen.Add(name) ||
                    Text(queue, "PortName") is not { } port)
                    return [Unknown("Queue identities or port assignments were missing or duplicated." + scope)];
                bool local = queue.TryGetProperty("Type", out var type) && type.ValueKind == JsonValueKind.Number &&
                    type.TryGetInt32(out int value) && value == 0;
                var state = known.Contains(port) ? CheckState.Information : local && !port.Contains(',') ? CheckState.Attention : CheckState.Unknown;
                string detail = known.Contains(port) ? "Configured port is registered: " + port + "." :
                    state == CheckState.Attention ? "Configured port was not found in this local inventory: " + port + ". Review the queue and repeat collection before changing its port." :
                    "Port assignment could not be matched exactly: " + port + ". Shared connections, pooled assignments and unknown queue types require separate review.";
                checks.Add(new("Printer port: " + name, state, detail + scope, Evidence: result));
            }
            if (checks.Count == 0) checks.Add(new("Printer ports", CheckState.Information,
                "No queues were reported for this process account. Other accounts and print servers were not enumerated." + scope, Evidence: result));
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        { return [Unknown("Queue/port evidence could not be interpreted." + scope)]; }
    }

    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
    private static string? Text(JsonElement value, string key) => value.TryGetProperty(key, out var item) &&
        item.ValueKind == JsonValueKind.String && DriverReference.ValidText(item.GetString(), 512) ? item.GetString() : null;
}
