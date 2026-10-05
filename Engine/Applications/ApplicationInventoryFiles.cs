using System.Text.Json;

namespace KillerBench.Engine.Applications;

public static class ApplicationInventoryFiles
{
    public static async Task SaveAsync(InstalledApplicationInventory inventory, string path, bool overwrite = false, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        token.ThrowIfCancellationRequested();
        var observed = InstalledApplications.Interpret(inventory.Evidence);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(observed, Capture.JsonOptions);
        if (bytes.Length > 32 * 1024 * 1024) throw new InvalidDataException("Inventory exceeds the 32 MB file limit.");
        string full = Path.GetFullPath(path);
        string temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, full, overwrite);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static async Task<ApplicationInventoryComparisonResult> CompareAsync(string beforePath, string afterPath, CancellationToken token = default)
    {
        var before = await ReadAsync(beforePath, token).ConfigureAwait(false);
        var after = await ReadAsync(afterPath, token).ConfigureAwait(false);
        return ApplicationInventoryComparison.Compare(before, after, token);
    }

    public static async Task<InstalledApplicationInventory> ReadAsync(string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous);
        if (file.Length is <= 0 or > 32 * 1024 * 1024) throw new InvalidDataException("Inventory file must be between 1 byte and 32 MB.");
        var bytes = new byte[checked((int)file.Length)];
        await file.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        int offset = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
        using var document = JsonDocument.Parse(bytes.AsMemory(offset), new JsonDocumentOptions { MaxDepth = 16 });
        var root = document.RootElement;
        Unique(root);
        var evidence = Field(root, "evidence");
        Unique(evidence);
        var raw = new ToolResult(Field(evidence, "exitCode").GetInt32(),
            Field(evidence, "output").GetString() ?? throw new InvalidDataException("Missing output."),
            Field(evidence, "error").GetString() ?? throw new InvalidDataException("Missing error field."),
            Field(evidence, "truncated").GetBoolean()) { TimedOut = Field(evidence, "timedOut").GetBoolean() };
        token.ThrowIfCancellationRequested();
        return InstalledApplications.Interpret(raw);
    }

    private static JsonElement Field(JsonElement value, string name)
    {
        foreach (var field in value.EnumerateObject())
            if (field.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return field.Value;
        throw new InvalidDataException("Missing inventory field: " + name);
    }

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != value.EnumerateObject().Count())
            throw new InvalidDataException("Inventory contains an invalid or duplicate object field.");
    }
}
