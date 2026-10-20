using System;
using System.IO;

internal static class LauncherPreparationProbe
{
    [STAThread]
    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "KillerBench-preparation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Documents"));
        string sentinel = Path.Combine(root, "Documents", "keep.txt");
        File.WriteAllText(sentinel, "Preparation must preserve this sentinel.");
        bool refused = false;
        try { KillerLauncher.Program.PrepareInstallDirectory(root, false); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused || File.ReadAllText(sentinel) != "Preparation must preserve this sentinel.")
            throw new Exception("Actual launcher preparation accepted or changed an unrelated folder.");
        Console.WriteLine("PASS actual launcher preparation refused an unowned destination before native install dispatch.");
        return 0;
    }
}
