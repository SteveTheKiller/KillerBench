using System.IO;
using System.Text;
using KillerBench.Engine;
using KillerBench.Services;

internal static class RecordPreservationChecks
{
    internal static void Run(string scratch)
    {
        ProfileRecoveryRecords(scratch);
        UnreadPreferences(scratch);
    }

    private static void ProfileRecoveryRecords(string scratch)
    {
        const string target = """{"sid":"S-1-5-21-1-2-3-1001","path":"C:\\Users\\Fixture","loaded":false,"special":false,"lastUsedUtc":null,"currentAccount":false}""";
        string Record(string machine, string targetJson, string destination, string journal) =>
            "{\"schema\":1,\"machine\":" + machine + ",\"startedUtc\":\"2026-10-31T00:00:00+00:00\",\"target\":" + targetJson +
            ",\"destinationPath\":" + destination + ",\"journal\":" + journal + "}";
        string machine = "\"" + Environment.MachineName + "\"";
        const string destination = "\"D:\\\\Backups\\\\Fixture\"", journal = "\"profile-backup-fixture.jsonl\"";
        var cases = new Dictionary<string, string>
        {
            ["null machine"] = Record("null", target, destination, journal),
            ["null target"] = Record(machine, "null", destination, journal),
            ["null sid"] = Record(machine, target.Replace("\"S-1-5-21-1-2-3-1001\"", "null"), destination, journal),
            ["null path"] = Record(machine, target.Replace("\"C:\\\\Users\\\\Fixture\"", "null"), destination, journal),
            ["null destination"] = Record(machine, target, "null", journal),
            ["null journal"] = Record(machine, target, destination, "null"),
            ["wrong type"] = Record("7", target, destination, journal),
            ["missing member"] = "{\"schema\":1}"
        };
        foreach (var (name, json) in cases)
        {
            string folder = Path.Combine(scratch, "profile-record-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string pending = Path.Combine(folder, "pending-backup.json");
            File.WriteAllText(pending, json);
            var result = ProfileBackupRecovery.TryResolveAsync(folder).GetAwaiter().GetResult();
            if (result.Verified || File.ReadAllText(pending) != json)
                throw new Exception("A malformed pending backup record was not preserved as failed verification: " + name);
        }
        Console.WriteLine("PASS: malformed pending backup records fail verification and stay in place.");
    }

    private static void UnreadPreferences(string scratch)
    {
        string folder = Path.Combine(scratch, "preferences-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "appearance.json");
        byte[][] unread = [Encoding.UTF8.GetBytes("{ not json"), new byte[40_000]];
        string original = Preferences.FilePath;
        try
        {
            Preferences.FilePath = file;
            foreach (byte[] bytes in unread)
            {
                File.WriteAllBytes(file, bytes);
                Preferences.Load();
                Preferences.Set("fixture", "value");
                if (!Preferences.Save() || !Directory.GetFiles(folder, "appearance.json.bad-*").Any(kept => File.ReadAllBytes(kept).SequenceEqual(bytes)))
                    throw new Exception("An unreadable settings file must be kept aside and saving must recover.");
                Preferences.Load();
                if (Preferences.Get("fixture") != "value") throw new Exception("Saving did not recover after an unreadable settings file.");
            }
            File.Delete(file);
            Preferences.Load();
            Preferences.Set("fixture", "kept");
            if (!Preferences.Save()) throw new Exception("A missing settings file was not created.");
            Preferences.Load();
            if (Preferences.Get("fixture") != "kept") throw new Exception("A valid setting did not persist.");
        }
        finally { Preferences.FilePath = original; Preferences.Load(); }
        Console.WriteLine("PASS: unread settings survive every save; missing and valid files still persist.");
    }
}
