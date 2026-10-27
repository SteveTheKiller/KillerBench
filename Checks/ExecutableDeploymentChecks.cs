using System.IO;
using KillerBench.Services;

/// <summary>A publication whose restore fails tells the user to keep its backup, so the next publication must
/// leave that backup alone while still clearing ordinary retired backups.</summary>
internal static class ExecutableDeploymentChecks
{
    internal static void Run(string scratch)
    {
        string root = Path.Combine(scratch, "executable-deployment-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string destination = Path.Combine(root, "app.exe");
        string first = Path.Combine(root, "first.bin"), second = Path.Combine(root, "second.bin");
        File.WriteAllText(destination, "original");
        File.WriteAllText(first, "first");
        File.WriteAllText(second, "second");

        // The replace retires the destination to its backup and then fails, and every later move fails too,
        // so the destination cannot be restored and the recovery error names the backup.
        var broken = new ExecutableDeployment.FileOperations
        {
            Replace = (_, target, backup) => { File.Move(target, backup!); throw new IOException("fixture replace failure"); },
            Move = (_, _) => throw new IOException("fixture move failure")
        };
        IOException? failure = null;
        try { ExecutableDeployment.Install(first, destination, broken); }
        catch (IOException e) { failure = e; }
        string? kept = Directory.GetFiles(root, "app.exe.*.bak").SingleOrDefault();
        Require(failure is not null && kept is not null && failure.Message.Contains("Backup: " + kept, StringComparison.Ordinal),
            "A failed restore reports the backup it kept: " + failure?.Message);

        string retired = destination + "." + Guid.NewGuid().ToString("N") + ".bak";
        File.WriteAllText(destination, "restored by hand");
        File.WriteAllText(retired, "retired");
        ExecutableDeployment.Install(second, destination);
        Require(File.ReadAllText(destination) == "second", "The next publication installs");
        Require(File.Exists(kept!) && File.ReadAllText(kept!) == "original",
            "The backup a failed restore told the user to keep survives the next publication");
        Require(!File.Exists(retired), "An ordinary retired backup is still cleared");
        Console.WriteLine("PASS: executable publication keeps a backup named by a failed restore and clears only ordinary retired backups.");
        try { Directory.Delete(root, true); } catch { }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
