using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KillerBench.Engine.Core.Recipes;

/// <summary>Exact resolved action identity. Parameter values are hashed rather than retained.</summary>
public sealed record RecipeOperationIdentity(string RecipeId, int RecipeVersion, string Machine, string TargetHash)
{
    // Conservatively block the recipe across targets and desired values until the earlier attempt is resolved.
    public string Key => Digest(JsonSerializer.Serialize(new { RecipeId, Machine }));

    public static RecipeOperationIdentity FromResolvedRecipe(RecipeDefinition recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var action = recipe.Action ?? throw new ArgumentException("An action is required.", nameof(recipe));
        string target = JsonSerializer.Serialize(new { action.Primitive, Parameters = action.Parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray() });
        return new(recipe.Id, recipe.Version, Environment.MachineName, Digest(target));
    }

    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public interface IRecipeOperationLease : IDisposable
{
    /// <summary>Closes only an attempt proven not dispatched or successfully verified while this lease is held.</summary>
    Task CompleteAsync(CancellationToken token = default);
    /// <summary>Records that the dispatched action reported success but needs a restart, and the boot it ran in.
    /// Only a record written this way can later be verified after a newer boot.</summary>
    Task RecordRestartRequiredAsync(DateTimeOffset bootUtc, CancellationToken token = default);
}

/// <summary>Exclusive hold on a pending restart-required record. Completing it archives the record.</summary>
public interface IRecipeRestartRecovery : IDisposable
{
    DateTimeOffset RestartRequiredBootUtc { get; }
    Task CompleteAsync(CancellationToken token = default);
}

public interface IRecipeOperationStore
{
    /// <summary>Atomically prepares an attempt. Null means another attempt is active or unresolved.</summary>
    Task<IRecipeOperationLease?> TryBeginAsync(RecipeOperationIdentity identity, CancellationToken token = default);
    /// <summary>Read-only inspection. A pending result never authorizes retry or clears recovery state.</summary>
    Task<bool> IsPendingAsync(RecipeOperationIdentity identity, CancellationToken token = default);
    /// <summary>Exclusively opens a well-formed pending restart-required record. Null when there is none, another holder has it,
    /// or the record is uncertain or damaged; those stay pending for operation-specific review.</summary>
    Task<IRecipeRestartRecovery?> TryOpenRestartRecoveryAsync(RecipeOperationIdentity identity, CancellationToken token = default);
}

/// <summary>Explicitly selected non-durable barrier for fixtures. Process exit loses its recovery state.</summary>
public sealed class MemoryRecipeOperationStore : IRecipeOperationStore
{
    private sealed record Pending(RecipeOperationIdentity Identity, DateTimeOffset? RestartRequiredBootUtc);
    private readonly ConcurrentDictionary<string, Pending> pending = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> held = new(StringComparer.Ordinal);

    public Task<IRecipeOperationLease?> TryBeginAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult<IRecipeOperationLease?>(pending.TryAdd(identity.Key, new(identity, null)) ? new Lease(this, identity.Key) : null);
    }

    public Task<bool> IsPendingAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(pending.ContainsKey(identity.Key));
    }

    public Task<IRecipeRestartRecovery?> TryOpenRestartRecoveryAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!pending.TryGetValue(identity.Key, out var record) || record.RestartRequiredBootUtc is not { } boot || !held.TryAdd(identity.Key, 0))
            return Task.FromResult<IRecipeRestartRecovery?>(null);
        return Task.FromResult<IRecipeRestartRecovery?>(new Recovery(this, identity.Key, boot));
    }

    private sealed class Lease(MemoryRecipeOperationStore owner, string key) : IRecipeOperationLease
    {
        private int closed;
        public Task CompleteAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref closed, 1) != 0) throw new InvalidOperationException("The operation lease is already closed.");
            owner.pending.TryRemove(key, out _);
            return Task.CompletedTask;
        }
        public Task RecordRestartRequiredAsync(DateTimeOffset bootUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Volatile.Read(ref closed) != 0) throw new InvalidOperationException("The operation lease is already closed.");
            owner.pending[key] = owner.pending[key] with { RestartRequiredBootUtc = bootUtc };
            return Task.CompletedTask;
        }
        public void Dispose() => Interlocked.Exchange(ref closed, 1);
    }

    private sealed class Recovery(MemoryRecipeOperationStore owner, string key, DateTimeOffset boot) : IRecipeRestartRecovery
    {
        private int closed;
        public DateTimeOffset RestartRequiredBootUtc => boot;
        public Task CompleteAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Volatile.Read(ref closed) != 0) throw new InvalidOperationException("The recovery hold is already closed.");
            owner.pending.TryRemove(key, out _);
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref closed, 1) == 0) owner.held.TryRemove(key, out _);
        }
    }
}

/// <summary>Same-user durable barrier across journals and processes. Uncertain records require operation-specific recovery review.</summary>
public sealed class FileRecipeOperationStore(string directory) : IRecipeOperationStore
{
    private readonly string root = Path.GetFullPath(directory);

    public static FileRecipeOperationStore ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "RecipeOperations"));

    public async Task<IRecipeOperationLease?> TryBeginAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(root);
        string pending = Path.Combine(root, identity.Key + ".pending.json");
        FileStream gate;
        try { gate = new FileStream(Path.Combine(root, identity.Key + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException) { return null; }
        try
        {
            if (File.Exists(pending)) { gate.Dispose(); return null; }
            var prepared = DateTimeOffset.UtcNow;
            await using var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { Schema = 1, Identity = identity, PreparedUtc = prepared });
            await stream.WriteAsync(bytes, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
            stream.Flush(true);
            return new Lease(gate, pending, identity, prepared);
        }
        catch { gate.Dispose(); throw; }
    }

    public async Task<IRecipeRestartRecovery?> TryOpenRestartRecoveryAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        token.ThrowIfCancellationRequested();
        string pending = Path.Combine(root, identity.Key + ".pending.json");
        if (!File.Exists(pending)) return null;
        FileStream gate;
        try { gate = new FileStream(Path.Combine(root, identity.Key + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException) { return null; }
        try
        {
            var info = new FileInfo(pending);
            if (!info.Exists || info.Length is <= 0 or > 16_384) { gate.Dispose(); return null; }
            using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(pending, token).ConfigureAwait(false));
            var record = document.RootElement;
            // Anything other than a complete restart record for this exact identity stays pending.
            if (record.ValueKind != JsonValueKind.Object || !record.TryGetProperty("Schema", out var schema) || schema.ValueKind != JsonValueKind.Number || schema.GetInt32() != 1 ||
                !record.TryGetProperty("Identity", out var stored) || stored.Deserialize<RecipeOperationIdentity>() is not { } storedIdentity || storedIdentity != identity ||
                !record.TryGetProperty("RestartRequiredBootUtc", out var bootValue) || bootValue.ValueKind != JsonValueKind.String || !bootValue.TryGetDateTimeOffset(out var boot))
            { gate.Dispose(); return null; }
            return new Recovery(gate, pending, boot);
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException or FormatException)
        { gate.Dispose(); return null; }
    }

    private sealed class Recovery(FileStream gate, string pending, DateTimeOffset boot) : IRecipeRestartRecovery
    {
        private int closed;
        public DateTimeOffset RestartRequiredBootUtc => boot;
        public Task CompleteAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Volatile.Read(ref closed) != 0) throw new InvalidOperationException("The recovery hold is already closed.");
            File.Move(pending, pending + "." + Guid.NewGuid().ToString("N") + ".restart-verified");
            return Task.CompletedTask;
        }
        public void Dispose() { Interlocked.Exchange(ref closed, 1); gate.Dispose(); }
    }

    public Task<bool> IsPendingAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        token.ThrowIfCancellationRequested();
        // Attribute reads expose access errors instead of interpreting them as an absent record.
        try
        {
            _ = File.GetAttributes(Path.Combine(root, identity.Key + ".pending.json"));
            return Task.FromResult(true);
        }
        catch (FileNotFoundException) { return Task.FromResult(false); }
        catch (DirectoryNotFoundException) { return Task.FromResult(false); }
    }

    private sealed class Lease(FileStream gate, string pending, RecipeOperationIdentity identity, DateTimeOffset prepared) : IRecipeOperationLease
    {
        private int closed;
        public Task CompleteAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref closed, 1) != 0) throw new InvalidOperationException("The operation lease is already closed.");
            File.Move(pending, pending + "." + Guid.NewGuid().ToString("N") + ".completed");
            return Task.CompletedTask;
        }
        public async Task RecordRestartRequiredAsync(DateTimeOffset bootUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Volatile.Read(ref closed) != 0) throw new InvalidOperationException("The operation lease is already closed.");
            string staged = pending + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await using (var stream = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { Schema = 1, Identity = identity, PreparedUtc = prepared, RestartRequiredBootUtc = bootUtc });
                await stream.WriteAsync(bytes, token).ConfigureAwait(false);
                stream.Flush(true);
            }
            File.Move(staged, pending, overwrite: true);
        }
        public void Dispose() { Interlocked.Exchange(ref closed, 1); gate.Dispose(); }
    }
}
