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
}

public interface IRecipeOperationStore
{
    /// <summary>Atomically prepares an attempt. Null means another attempt is active or unresolved.</summary>
    Task<IRecipeOperationLease?> TryBeginAsync(RecipeOperationIdentity identity, CancellationToken token = default);
    /// <summary>Read-only inspection. A pending result never authorizes retry or clears recovery state.</summary>
    Task<bool> IsPendingAsync(RecipeOperationIdentity identity, CancellationToken token = default);
}

/// <summary>Explicitly selected non-durable barrier for fixtures. Process exit loses its recovery state.</summary>
public sealed class MemoryRecipeOperationStore : IRecipeOperationStore
{
    private readonly ConcurrentDictionary<string, RecipeOperationIdentity> pending = new(StringComparer.Ordinal);

    public Task<IRecipeOperationLease?> TryBeginAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult<IRecipeOperationLease?>(pending.TryAdd(identity.Key, identity) ? new Lease(this, identity.Key) : null);
    }

    public Task<bool> IsPendingAsync(RecipeOperationIdentity identity, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(pending.ContainsKey(identity.Key));
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
        public void Dispose() => Interlocked.Exchange(ref closed, 1);
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
            await using var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { Schema = 1, Identity = identity, PreparedUtc = DateTimeOffset.UtcNow });
            await stream.WriteAsync(bytes, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
            stream.Flush(true);
            return new Lease(gate, pending);
        }
        catch { gate.Dispose(); throw; }
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

    private sealed class Lease(FileStream gate, string pending) : IRecipeOperationLease
    {
        private int closed;
        public Task CompleteAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref closed, 1) != 0) throw new InvalidOperationException("The operation lease is already closed.");
            File.Move(pending, pending + "." + Guid.NewGuid().ToString("N") + ".completed");
            return Task.CompletedTask;
        }
        public void Dispose() { Interlocked.Exchange(ref closed, 1); gate.Dispose(); }
    }
}
