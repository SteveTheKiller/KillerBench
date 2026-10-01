using System.Text.Json;
using System.Text.Json.Serialization;

namespace KillerBench.Engine.Scheduling;

public static class ScheduledTaskPlanJson
{
    public const int MaximumBytes = 128 * 1024;
    public static byte[] Serialize(ScheduledTaskPlan plan)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(plan.Validate(), Options);
        if (data.Length > MaximumBytes) throw new ArgumentException("Task plan exceeds 128 KiB.");
        return data;
    }
    public static async Task<ScheduledTaskPlan> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(15));
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        var bytes = new byte[MaximumBytes + 1];
        int count = 0;
        while (count < bytes.Length)
        {
            int read = await file.ReadAsync(bytes.AsMemory(count), stop.Token);
            if (read == 0) break;
            count += read;
        }
        stop.Token.ThrowIfCancellationRequested();
        return Parse(bytes.AsMemory(0, count));
    }
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        MaxDepth = 12,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static ScheduledTaskPlan Parse(ReadOnlyMemory<byte> utf8)
    {
        if (utf8.Length > MaximumBytes) throw new ArgumentException("Task plan exceeds 128 KiB.");
        if (utf8.Span.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) utf8 = utf8[3..];
        using var document = JsonDocument.Parse(utf8, new JsonDocumentOptions { MaxDepth = 12 });
        RejectDuplicates(document.RootElement);
        return (document.RootElement.Deserialize<ScheduledTaskPlan>(Options) ?? throw new JsonException("Task plan is null.")).Validate();
    }

    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException("Duplicate task-plan field: " + property.Name);
                RejectDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicates(item);
    }
}
