using System.Text.Json;

namespace KillerBench.Engine.Core;

public enum RecipeJournalStage { Detected, PreconditionsChecked, Prepared, Dispatched, Verified, Completed }

/// <summary>One durable line in a recipe run journal. Raw evidence is retained; secrets are the primitive's responsibility to exclude.</summary>
public sealed record RecipeJournalEntry(DateTimeOffset TimestampUtc, string RecipeId, int RecipeVersion, RecipeJournalStage Stage, string Detail)
{
    public string? Raw { get; init; }
}

/// <summary>Receives journal entries. Implementations must flush before returning so an interrupted run leaves evidence behind.</summary>
public interface IRecipeJournal
{
    Task RecordAsync(RecipeJournalEntry entry, CancellationToken token);
}

public sealed class MemoryRecipeJournal : IRecipeJournal
{
    private readonly List<RecipeJournalEntry> entries = [];
    public IReadOnlyList<RecipeJournalEntry> Entries => entries.AsReadOnly();
    public Task RecordAsync(RecipeJournalEntry entry, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (entries) entries.Add(entry);
        return Task.CompletedTask;
    }
}

/// <summary>Appends one JSON object per line to a file, flushing each write.</summary>
public sealed class FileRecipeJournal(string path) : IRecipeJournal
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    private readonly SemaphoreSlim gate = new(1, 1);

    public string Path { get; } = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Journal path is required.", nameof(path)) : path;

    public async Task RecordAsync(RecipeJournalEntry entry, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string line = JsonSerializer.Serialize(entry, Options) + "\n";
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path) ?? ".");
            await using var stream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(line.AsMemory(), token).ConfigureAwait(false);
            await writer.FlushAsync(token).ConfigureAwait(false);
            stream.Flush(true);
        }
        finally { gate.Release(); }
    }
}
