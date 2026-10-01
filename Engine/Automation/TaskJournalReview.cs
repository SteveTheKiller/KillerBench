using System.Text.Json;
using System.Text.Json.Serialization;

namespace KillerBench.Engine.Automation;

public sealed record TaskJournalReviewResult(string State, string Detail, JsonElement? RecordedEvidence);

public static class TaskJournalReview
{
    public static async Task<TaskJournalReviewResult> ReadPairAsync(string pendingPath, string completionPath, CancellationToken token = default)
    {
        var pending = await ReadAsync(pendingPath, token).ConfigureAwait(false);
        var completion = await ReadAsync(completionPath, token).ConfigureAwait(false);
        if (pending.State != "Pending" || completion.State is not ("DefinitionVerified" or "NotDispatched") ||
            pending.RecordedEvidence is not { } intent || completion.RecordedEvidence is not { } result)
            return new("Unverified", "Valid linked intent and outcome records are required.", null);
        if (!intent.TryGetProperty("attemptId", out var first) || first.ValueKind != JsonValueKind.String || !first.TryGetGuid(out var id) || id == Guid.Empty ||
            !result.TryGetProperty("attemptId", out var second) || second.ValueKind != JsonValueKind.String || !second.TryGetGuid(out var other) || id != other ||
            !result.TryGetProperty("machine", out var machine) || machine.ValueKind != JsonValueKind.String ||
            !string.Equals(intent.GetProperty("machine").GetString(), machine.GetString(), StringComparison.OrdinalIgnoreCase))
            return new("Unverified", "Records do not identify the same attempt and machine.", null);
        if (result.GetProperty("recordedUtc").GetDateTimeOffset() < intent.GetProperty("recordedUtc").GetDateTimeOffset())
            return new("Unverified", "Outcome chronology precedes the prepared intent.", null);
        if (completion.State == "NotDispatched")
            return new("NotDispatched", "Linked historical records confirm that this attempt sent no task creation request. Current task state still requires fresh inspection.",
                JsonSerializer.SerializeToElement(new { intent, outcome = result }));
        var plan = intent.GetProperty("plan").Deserialize<ScheduledTaskPlan>(Options)!;
        var verification = result.GetProperty("verification").Deserialize<TaskRegistrationVerification>(Options)!;
        if (!ScheduledTaskPlanJson.Serialize(plan).SequenceEqual(ScheduledTaskPlanJson.Serialize(verification.Observed!)))
            return new("Unverified", "Completion does not match the intended definition or chronology.", null);
        return new("DefinitionVerified", "Linked historical records only. Current task state and retry eligibility require fresh inspection.",
            JsonSerializer.SerializeToElement(new { intent, completion = result }));
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16
    };

    public static async Task<TaskJournalReviewResult> ReadAsync(string path, CancellationToken token = default)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            stop.Token.ThrowIfCancellationRequested();
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                65536, FileOptions.Asynchronous);
            byte[] bytes = new byte[1024 * 1024 + 1];
            int count = 0;
            while (count < bytes.Length)
            {
                int read = await file.ReadAsync(bytes.AsMemory(count), stop.Token).ConfigureAwait(false);
                if (read == 0) break;
                count += read;
            }
            if (count == bytes.Length) throw new JsonException("Journal exceeds 1 MiB.");
            using var document = JsonDocument.Parse(bytes.AsMemory(0, count), new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            CheckDuplicates(root);
            int schema = root.GetProperty("schemaVersion").GetInt32();
            if (schema is not (1 or 2) || !root.GetProperty("recordedUtc").TryGetDateTimeOffset(out _))
                throw new JsonException("Unsupported journal schema or timestamp.");
            string? state = root.GetProperty("state").GetString();
            if (state == "Pending")
            {
                var plan = root.GetProperty("plan").Deserialize<ScheduledTaskPlan>(Options)?.Validate() ?? throw new JsonException("Missing plan.");
                var preflight = root.GetProperty("preflight").Deserialize<TaskRegistrationPreflight>(Options) ?? throw new JsonException("Missing preflight.");
                if (!plan.Identity.Matches(preflight.Target) || !preflight.Eligible || !Complete(preflight.Evidence) ||
                    !root.GetProperty("attemptId").TryGetGuid(out var attemptId) || attemptId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(root.GetProperty("machine").GetString())) throw new JsonException("Invalid pending evidence.");
                if (schema == 2 && (!root.TryGetProperty("startDate", out var startDate) ||
                    startDate.ValueKind != JsonValueKind.String || !DateOnly.TryParseExact(startDate.GetString(), "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)))
                    throw new JsonException("Invalid task start date.");
            }
            else if (state == "DefinitionVerified")
            {
                var verification = root.GetProperty("verification").Deserialize<TaskRegistrationVerification>(Options) ?? throw new JsonException("Missing verification.");
                if (root.GetProperty("taskExecuted").GetBoolean() || verification.Observed is null || !Complete(verification.Evidence))
                    throw new JsonException("Invalid completion evidence.");
                verification.Observed.Validate();
            }
            else if (state == "NotDispatched")
            {
                if (root.GetProperty("taskExecuted").GetBoolean() ||
                    !root.GetProperty("attemptId").TryGetGuid(out var attemptId) || attemptId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(root.GetProperty("machine").GetString()))
                    throw new JsonException("Invalid no-dispatch evidence.");
                string? detail = root.GetProperty("detail").GetString();
                if (string.IsNullOrWhiteSpace(detail) || detail.Length > 4096 || detail.Any(char.IsControl))
                    throw new JsonException("Invalid no-dispatch detail.");
            }
            else throw new JsonException("Unknown journal state.");
            return new(state, "Historical record only. Current task state and retry eligibility require fresh inspection.", root.Clone());
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { return new("Unreadable", error.Message, null); }
    }

    private static bool Complete(ToolResult? evidence) => TaskRegistration.Complete(evidence);
    private static void CheckDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException("Duplicate journal field.");
                CheckDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) CheckDuplicates(child);
    }
}
