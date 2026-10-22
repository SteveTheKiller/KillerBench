using System.Globalization;
using KillerBench.Engine.Core;
using Microsoft.Win32;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Bounded registry primitives. Keys are limited to HKLM and HKCU relative paths without traversal; values are read and written one at a time with the prior value retained in the action's raw evidence.</summary>
public static class WindowsRegistryPrimitives
{
    /// <summary>
    /// A write that returned, or failed with an I/O error mid-write, may have changed the value, so it stays dispatched and uncertain.
    /// Only a refusal before the write (access denied, or a failure before SetValue) is reported as no change.
    /// </summary>
    public static ActionResult WriteFailed(bool attempted, bool written, Exception exception) =>
        written || (attempted && exception is IOException)
            ? new(true, false, "", "The registry write was sent but could not be confirmed: " + exception.Message + " Check the current value before retrying.")
            : new(false, false, "", "Registry value could not be written: " + exception.Message);

    public const string KeyPattern = @"[A-Za-z0-9_][A-Za-z0-9_\\ .{}()-]{0,255}";
    public const string NamePattern = @"[A-Za-z0-9_ .(){}-]{1,255}";

    public static readonly PrimitiveParameter HiveParameter = new("hive", true, "HKLM or HKCU") { Pattern = "HKLM|HKCU" };
    public static readonly PrimitiveParameter KeyParameter = new("key", true, "Key path relative to the hive, backslash separated") { Pattern = KeyPattern };
    public static readonly PrimitiveParameter NameParameter = new("name", true, "Value name") { Pattern = NamePattern };
    public static readonly PrimitiveParameter ViewParameter = new("view", false, "Registry view: 64 (default) or 32") { Pattern = "64|32" };

    public static bool TryOpen(IReadOnlyDictionary<string, string> parameters, bool writable, out RegistryKey? key, out string error)
    {
        key = null;
        error = "";
        if (!OperatingSystem.IsWindows()) { error = "Registry access requires Windows."; return false; }
        string hive = parameters.GetValueOrDefault("hive") ?? "";
        string path = parameters.GetValueOrDefault("key") ?? "";
        if (path.Contains("..", StringComparison.Ordinal) || path.StartsWith('\\') || path.EndsWith('\\') || path.Contains("\\\\", StringComparison.Ordinal))
        { error = "Registry key path is malformed."; return false; }
        var root = hive switch { "HKLM" => RegistryHive.LocalMachine, "HKCU" => RegistryHive.CurrentUser, _ => (RegistryHive?)null };
        if (root is null) { error = "Registry hive must be HKLM or HKCU."; return false; }
        var view = parameters.GetValueOrDefault("view") == "32" ? RegistryView.Registry32 : RegistryView.Registry64;
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(root.Value, view);
            key = baseKey.OpenSubKey(path, writable);
            return true;
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            error = "Registry key could not be opened: " + exception.Message;
            return false;
        }
    }

    public static string? Render(object? value, RegistryValueKind _) => value switch
    {
        null => null,
        int number => unchecked((uint)number).ToString(CultureInfo.InvariantCulture),
        long number => number.ToString(CultureInfo.InvariantCulture),
        string text => text,
        string[] lines => string.Join(";", lines),
        byte[] bytes => Convert.ToHexString(bytes),
        _ => value.ToString()
    };
}

/// <summary>Reads one registry value. Value is the rendered data (DWORDs as unsigned decimal); an absent key or value is a complete observation with no value, so packs can use absent and present clauses.</summary>
public sealed class WindowsRegistryValueProbe : IProbePrimitive
{
    public string Name => "registry.value";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } =
        [WindowsRegistryPrimitives.HiveParameter, WindowsRegistryPrimitives.KeyParameter, WindowsRegistryPrimitives.NameParameter, WindowsRegistryPrimitives.ViewParameter];

    public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(PrimitiveResult.Unavailable("Registry access requires Windows."));
        if (!WindowsRegistryPrimitives.TryOpen(parameters, false, out var key, out string error)) return Task.FromResult(PrimitiveResult.Unavailable(error));
        using (key)
        {
            string name = parameters["name"];
            if (key is null) return Task.FromResult(new PrimitiveResult(true, null, "key absent") { Detail = "The registry key does not exist." });
            try
            {
                object? data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (data is null) return Task.FromResult(new PrimitiveResult(true, null, "value absent") { Detail = "The registry value does not exist." });
                var kind = key.GetValueKind(name);
                string? rendered = WindowsRegistryPrimitives.Render(data, kind);
                return Task.FromResult(new PrimitiveResult(true, rendered, kind + ": " + rendered) { Detail = name + " is " + rendered + " (" + kind + ")." });
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return Task.FromResult(PrimitiveResult.Unavailable("Registry value could not be read: " + exception.Message));
            }
        }
    }
}

/// <summary>Lists one key's immediate subkey names as a semicolon-joined value, so packs can use contains and matches clauses.</summary>
public sealed class WindowsRegistrySubkeysProbe : IProbePrimitive
{
    public string Name => "registry.subkeys";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } =
        [WindowsRegistryPrimitives.HiveParameter, WindowsRegistryPrimitives.KeyParameter, WindowsRegistryPrimitives.ViewParameter];

    public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(PrimitiveResult.Unavailable("Registry access requires Windows."));
        if (!WindowsRegistryPrimitives.TryOpen(parameters, false, out var key, out string error)) return Task.FromResult(PrimitiveResult.Unavailable(error));
        using (key)
        {
            if (key is null) return Task.FromResult(new PrimitiveResult(true, null, "key absent") { Detail = "The registry key does not exist." });
            try
            {
                var names = key.GetSubKeyNames();
                if (names.Length > 4096) return Task.FromResult(new PrimitiveResult(false, null, names.Length + " subkeys", "Too many subkeys to retain.") { Truncated = true });
                string joined = string.Join(";", names.Order(StringComparer.OrdinalIgnoreCase));
                return Task.FromResult(new PrimitiveResult(true, joined, joined) { Detail = names.Length + " subkey(s)." });
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return Task.FromResult(PrimitiveResult.Unavailable("Registry subkeys could not be read: " + exception.Message));
            }
        }
    }
}

/// <summary>Writes one DWORD value. The prior value and kind are retained in the raw evidence; a value that already matches is reported as not dispatched.</summary>
public sealed class WindowsRegistrySetDwordAction : IActionPrimitive
{
    public string Name => "registry.set-dword";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } =
    [
        WindowsRegistryPrimitives.HiveParameter, WindowsRegistryPrimitives.KeyParameter, WindowsRegistryPrimitives.NameParameter,
        new("value", true, "Unsigned decimal DWORD value") { Pattern = "0|[1-9][0-9]{0,9}" }, WindowsRegistryPrimitives.ViewParameter
    ];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;

    public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(new ActionResult(false, false, "", "Registry access requires Windows."));
        if (!uint.TryParse(parameters.GetValueOrDefault("value"), NumberStyles.None, CultureInfo.InvariantCulture, out uint requested))
            return Task.FromResult(new ActionResult(false, false, "", "The value is not an unsigned DWORD."));
        if (!WindowsRegistryPrimitives.TryOpen(parameters, true, out var key, out string error)) return Task.FromResult(new ActionResult(false, false, "", error));
        using (key)
        {
            if (key is null) return Task.FromResult(new ActionResult(false, false, "", "The registry key does not exist; this primitive does not create keys."));
            string name = parameters["name"];
            bool attempted = false, wrote = false;
            try
            {
                object? prior = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                string priorText = prior is null ? "absent" : key.GetValueKind(name) + ":" + WindowsRegistryPrimitives.Render(prior, key.GetValueKind(name));
                if (prior is int existing && unchecked((uint)existing) == requested)
                    return Task.FromResult(new ActionResult(false, true, "prior=" + priorText, "The value already matches; nothing was written."));
                attempted = true;
                key.SetValue(name, unchecked((int)requested), RegistryValueKind.DWord);
                wrote = true;
                object? after = key.GetValue(name, null);
                bool verified = after is int written && unchecked((uint)written) == requested;
                return Task.FromResult(new ActionResult(true, verified, "prior=" + priorText + "; after=" + WindowsRegistryPrimitives.Render(after, RegistryValueKind.DWord),
                    verified ? null : "The value did not read back as written."));
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return Task.FromResult(WindowsRegistryPrimitives.WriteFailed(attempted, wrote, exception));
            }
        }
    }
}

/// <summary>Writes one REG_SZ value in an existing key. An existing value of another type is refused rather than converted, the prior value is retained in the raw evidence, and a value that already matches is reported as not dispatched.</summary>
public sealed class WindowsRegistrySetStringAction : IActionPrimitive
{
    public string Name => "registry.set-string";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } =
    [
        WindowsRegistryPrimitives.HiveParameter, WindowsRegistryPrimitives.KeyParameter, WindowsRegistryPrimitives.NameParameter,
        new("value", true, "String data, up to 256 printable characters") { Pattern = "[ -~]{0,256}" }, WindowsRegistryPrimitives.ViewParameter
    ];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => false;

    public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(new ActionResult(false, false, "", "Registry access requires Windows."));
        string requested = parameters.GetValueOrDefault("value") ?? "";
        if (requested.Length > 256 || requested.Any(c => c < ' ' || c > '~')) return Task.FromResult(new ActionResult(false, false, "", "The value is not printable text of 256 characters or fewer."));
        if (!WindowsRegistryPrimitives.TryOpen(parameters, true, out var key, out string error)) return Task.FromResult(new ActionResult(false, false, "", error));
        using (key)
        {
            if (key is null) return Task.FromResult(new ActionResult(false, false, "", "The registry key does not exist; this primitive does not create keys."));
            string name = parameters["name"];
            bool attempted = false, wrote = false;
            try
            {
                object? prior = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                var priorKind = prior is null ? RegistryValueKind.Unknown : key.GetValueKind(name);
                string priorText = prior is null ? "absent" : priorKind + ":" + WindowsRegistryPrimitives.Render(prior, priorKind);
                if (prior is not null && priorKind != RegistryValueKind.String)
                    return Task.FromResult(new ActionResult(false, false, "prior=" + priorText, "The existing value is " + priorKind + ", not String; it was left unchanged."));
                if (prior is string existing && string.Equals(existing, requested, StringComparison.Ordinal))
                    return Task.FromResult(new ActionResult(false, true, "prior=" + priorText, "The value already matches; nothing was written."));
                attempted = true;
                key.SetValue(name, requested, RegistryValueKind.String);
                wrote = true;
                object? after = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                bool verified = after is string written && string.Equals(written, requested, StringComparison.Ordinal) && key.GetValueKind(name) == RegistryValueKind.String;
                return Task.FromResult(new ActionResult(true, verified, "prior=" + priorText + "; after=String:" + WindowsRegistryPrimitives.Render(after, RegistryValueKind.String),
                    verified ? null : "The value did not read back as written."));
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return Task.FromResult(WindowsRegistryPrimitives.WriteFailed(attempted, wrote, exception));
            }
        }
    }
}
