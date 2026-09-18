using System.IO;
using System.Text.Json;

namespace KillerBench.Services;

internal static class Preferences
{
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "appearance.json");
    private static Dictionary<string, string> values = [];
    internal static string? Get(string key) => values.GetValueOrDefault(key);
    internal static void Set(string key, string value) => values[key] = value;
    internal static void Load()
    {
        try
        {
            if (File.Exists(FilePath) && new FileInfo(FilePath).Length < 32_768)
                values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
    }
    internal static bool Save()
    {
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
