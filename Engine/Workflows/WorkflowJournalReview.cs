using System.Text.Json;

namespace KillerBench.Engine.Workflows;

public sealed record WorkflowJournalSnapshot(string Source, string Machine, string Kind, string RecordedStatus,
    DateTimeOffset StartedUtc, int CheckCount, int RepairCount, JsonElement Record)
{
    public static string Scope => "Historical file contents only. Outcomes are recorded claims, not current verification. No commands run and no retry is authorized.";
    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    public string HistoricalScope => Scope;
}

public static class WorkflowJournalReview
{
    public const int MaximumBytes = 16 * 1024 * 1024;

    public static async Task<WorkflowJournalSnapshot> ReadAsync(string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (file.Length is < 2 or > MaximumBytes) throw new InvalidDataException("Workflow journal size is outside the supported range.");
        byte[] data = new byte[checked((int)file.Length)];
        await file.ReadExactlyAsync(data, deadline.Token).ConfigureAwait(false);
        deadline.Token.ThrowIfCancellationRequested();
        try
        {
            using var document = JsonDocument.Parse(data, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("record", out var retained))
            {
                if (root.EnumerateObject().Select(p => p.Name).Distinct().Count() != root.EnumerateObject().Count())
                    throw new InvalidDataException("Workflow review wrapper is ambiguous.");
                root = retained;
            }
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Select(p => p.Name).Distinct().Count() != root.EnumerateObject().Count())
                throw new InvalidDataException("Workflow journal root is missing or ambiguous.");
            string Text(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! : throw new InvalidDataException("Missing workflow field: " + name);
            int Count(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
                ? value.GetArrayLength() : throw new InvalidDataException("Missing workflow array: " + name);
            string kind = Text("kind"), machine = Text("machine"), status = Text("status");
            if (!Enum.TryParse<WorkflowKind>(kind, out var parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != kind ||
                !root.TryGetProperty("startedUtc", out var started) || started.ValueKind != JsonValueKind.String || !started.TryGetDateTimeOffset(out var timestamp))
                throw new InvalidDataException("Workflow kind or timestamp is invalid.");
            var snapshot = new WorkflowJournalSnapshot(Path.GetFullPath(path), machine, kind, status, timestamp,
                Count("checks"), Count("repairs"), root.Clone());
            deadline.Token.ThrowIfCancellationRequested();
            return snapshot;
        }
        catch (JsonException error) { throw new InvalidDataException("Workflow journal JSON is incomplete or invalid.", error); }
    }
}
