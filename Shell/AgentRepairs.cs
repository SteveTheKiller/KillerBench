using System.IO;
using System.Text.Json;
using System.Windows;

namespace KillerBench;

// The KillerMCP section of the About card. KillerBench owns the user's choice of which repairs an AI agent
// connected through KillerMCP may request. KillerMCP only reads the level and writes the lock after an
// uncertain result, so only a person at this PC can turn agent repairs on or clear the lock.
public partial class MainWindow
{
    private const int AgentRepairFileLimit = 16_384;

    private static string AgentRepairFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench");

    private static string AgentRepairSettingsPath => Path.Combine(AgentRepairFolder, "agent-repairs.json");
    private static string AgentRepairLockPath => Path.Combine(AgentRepairFolder, "agent-repair-lock.json");

    private void RefreshAgentRepairSettings()
    {
        string level = ReadAgentRepairLevel();
        AgentSafeRepairsCheck.IsChecked = level is "safe" or "all";
        AgentDisruptiveRepairsCheck.IsChecked = level == "all";
        AgentDisruptiveRepairsCheck.IsEnabled = AgentSafeRepairsCheck.IsChecked == true;
        RefreshAgentRepairLock();
    }

    // Click, not Checked/Unchecked, so only a person's click saves; the sync above never writes.
    private void OnAgentRepairsChanged(object sender, RoutedEventArgs e)
    {
        bool safe = AgentSafeRepairsCheck.IsChecked == true;
        if (!safe) AgentDisruptiveRepairsCheck.IsChecked = false;
        AgentDisruptiveRepairsCheck.IsEnabled = safe;
        string level = !safe ? "off" : AgentDisruptiveRepairsCheck.IsChecked == true ? "all" : "safe";
        if (!WriteAgentRepairLevel(level))
        {
            Report("The KillerMCP repair setting could not be saved.");
            RefreshAgentRepairSettings();
        }
    }

    private void OnAgentRepairClearLock(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(AgentRepairLockPath)) File.Delete(AgentRepairLockPath);
            Report("KillerMCP repair lock cleared.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Report("The KillerMCP repair lock could not be cleared: " + error.Message);
        }
        RefreshAgentRepairLock();
    }

    private void RefreshAgentRepairLock()
    {
        string? repair = ReadAgentRepairLock();
        AgentRepairLockBlock.Visibility = repair is null ? Visibility.Collapsed : Visibility.Visible;
        if (repair is null) return;
        string format = TryFindResource("Str_About_KillerMcpLocked") as string ?? "KillerMCP repairs are locked after {0}.";
        AgentRepairLockRun.Text = format.Replace("{0}", repair, StringComparison.Ordinal);
    }

    private static string ReadAgentRepairLevel()
    {
        try
        {
            var file = new FileInfo(AgentRepairSettingsPath);
            if (!file.Exists || file.Length > AgentRepairFileLimit) return "off";
            using var document = JsonDocument.Parse(File.ReadAllText(file.FullName));
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("level", out var value)
                && value.GetString() is "safe" or "all" ? value.GetString()! : "off";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return "off";
        }
    }

    private static bool WriteAgentRepairLevel(string level)
    {
        try
        {
            Directory.CreateDirectory(AgentRepairFolder);
            string temporary = AgentRepairSettingsPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { level }) + Environment.NewLine);
            File.Move(temporary, AgentRepairSettingsPath, true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    // Returns the title of the repair that caused the lock, or null when repairs are not locked.
    private static string? ReadAgentRepairLock()
    {
        try
        {
            var file = new FileInfo(AgentRepairLockPath);
            if (!file.Exists) return null;
            if (file.Length > AgentRepairFileLimit) return "an unreadable repair";
            using var document = JsonDocument.Parse(File.ReadAllText(file.FullName));
            var root = document.RootElement;
            foreach (var name in new[] { "Title", "Action" })
            {
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 and <= 200 } text) return text;
            }
            return "an unknown repair";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return File.Exists(AgentRepairLockPath) ? "an unreadable repair" : null;
        }
    }
}
