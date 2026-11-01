using System.IO;
using System.Text.Json;

namespace KillerBench.Services;

internal static class Preferences
{
    internal static string FilePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "appearance.json");
    private static Dictionary<string, string> values = [];
    internal static string? Get(string key) => values.GetValueOrDefault(key);
    internal static void Set(string key, string value) => values[key] = value;
    /// <summary>True once the saved file was read, kept aside as unreadable, or there was none. A file
    /// that could not be opened stays false, so Save never overwrites preferences this run never saw.</summary>
    internal static bool Loaded { get; private set; }
    internal static void Load()
    {
        Loaded = false;
        try
        {
            if (!File.Exists(FilePath)) { Loaded = true; return; }
            if (new FileInfo(FilePath).Length >= 32_768) { KeepAside(); return; }
            try { values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? []; }
            catch (JsonException) { KeepAside(); return; }
            Loaded = true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
    private static void KeepAside()
    {
        File.Move(FilePath, FilePath + ".bad-" + Guid.NewGuid().ToString("N"));
        values = [];
        Loaded = true;
    }
    internal static bool Save()
    {
        if (!Loaded) return false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(values));
            File.Move(temp, FilePath, true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }
}
