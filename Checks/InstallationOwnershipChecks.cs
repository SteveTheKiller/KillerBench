using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using KillerBench.Services;

internal static class InstallationOwnershipChecks
{
    internal static void Run(string scratch)
    {
        string directory = Path.Combine(scratch, "unowned-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "Documents"));
        string sentinel = Path.Combine(directory, "Documents", "keep.txt");
        File.WriteAllText(sentinel, "Unrelated sentinel must remain intact.");
        bool refused = false;
        try { InstallationOwnership.ValidateDestination(directory, ["KillerBench.exe"], null, _ => true); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused || File.ReadAllText(sentinel) != "Unrelated sentinel must remain intact.")
            throw new Exception("Setup accepted a nonempty unowned destination containing unrelated sentinel files.");
        string empty = Path.Combine(scratch, "empty-" + Guid.NewGuid().ToString("N"));
        InstallationOwnership.ValidateDestination(empty, ["KillerBench.exe"], null, _ => false);
        Directory.CreateDirectory(empty);
        InstallationOwnership.ValidateDestination(empty, ["KillerBench.exe"], null, _ => false);

        string owned = Path.Combine(scratch, "owned-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(owned);
        string exe = Path.Combine(owned, "KillerBench.exe");
        File.WriteAllText(exe, "Synthetic old product executable");
        string inventory = Entry(exe, "KillerBench.exe");
        File.WriteAllText(Path.Combine(owned, "payload.manifest"), inventory);
        InstallationOwnership.ValidateDestination(owned, ["KillerBench.exe"], exe, _ => true);
        string removed = Path.Combine(owned, "old-component.dll");
        File.WriteAllText(removed, "Verified prior payload component removed from the new package");
        File.WriteAllText(Path.Combine(owned, "payload.manifest"), inventory + Entry(removed, "old-component.dll"));
        string priorDigest = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(owned, "payload.manifest"))));
        InstallationOwnership.ValidateDestination(owned, ["KillerBench.exe"], exe, _ => true, [priorDigest]);
        File.AppendAllText(Path.Combine(owned, "payload.manifest"), Entry(removed, "claimed-user-file.txt"));
        refused = false;
        try { InstallationOwnership.ValidateDestination(owned, ["KillerBench.exe"], exe, _ => true, [priorDigest]); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused || !File.Exists(removed)) throw new Exception("A forged prior inventory was accepted or previous payload was lost.");
        File.Delete(removed);
        File.WriteAllText(Path.Combine(owned, "payload.manifest"), inventory);
        foreach (string invalid in new[] { "wrong-registration", "untrusted", "changed-payload", "unknown-file", "forged-inventory" })
        {
            string extra = Path.Combine(owned, "keep.txt");
            File.WriteAllText(exe, invalid == "changed-payload" ? "Modified data" : "Synthetic old product executable");
            File.WriteAllText(Path.Combine(owned, "payload.manifest"), inventory);
            if (invalid is "unknown-file" or "forged-inventory") File.WriteAllText(extra, "Preserved unrelated document");
            if (invalid == "forged-inventory") File.AppendAllText(Path.Combine(owned, "payload.manifest"), Entry(extra, "keep.txt"));
            refused = false;
            try { InstallationOwnership.ValidateDestination(owned, ["KillerBench.exe"], invalid == "wrong-registration" ? Path.Combine(scratch, "Other", "KillerBench.exe") : exe, _ => invalid != "untrusted"); }
            catch (InvalidOperationException) { refused = true; }
            if (!refused || !File.Exists(exe) || File.Exists(extra) && File.ReadAllText(extra) != "Preserved unrelated document")
                throw new Exception("Setup failed to preserve/refuse ownership case " + invalid + ".");
            if (File.Exists(extra)) File.Delete(extra);
        }

        bool metadataRemoved = false;
        if (!InstallationOwnership.UnregisterPreservingFiles(directory, Path.Combine(directory, "KillerBench.exe"), () => metadataRemoved = true) ||
            !metadataRemoved || File.ReadAllText(sentinel) != "Unrelated sentinel must remain intact.")
            throw new Exception("Unregistering a matching copy removed its application folder or unrelated files.");
        metadataRemoved = false;
        if (InstallationOwnership.UnregisterPreservingFiles(directory, exe, () => metadataRemoved = true) || metadataRemoved)
            throw new Exception("Unregistering one folder changed another installation's metadata.");

        string link = Path.Combine(scratch, "redirected-" + Guid.NewGuid().ToString("N"));
        var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { "/d", "/c", "mklink", "/J", Path.GetFullPath(link), Path.GetFullPath(directory) }) start.ArgumentList.Add(argument);
        using (var process = Process.Start(start) ?? throw new Exception("Owned scratch junction fixture did not start."))
        {
            var fixtureError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15_000)) { process.Kill(true); throw new Exception("Owned scratch junction fixture timed out."); }
            if (process.ExitCode != 0) throw new Exception("Owned scratch junction fixture failed: " + fixtureError.GetAwaiter().GetResult());
        }
        try
        {
            refused = false;
            try { InstallationOwnership.ValidateDestination(link, ["KillerBench.exe"], Path.Combine(link, "KillerBench.exe"), _ => true); }
            catch (InvalidOperationException) { refused = true; }
            if (!refused || File.ReadAllText(sentinel) != "Unrelated sentinel must remain intact.")
                throw new Exception("A redirected installation destination was accepted or its target changed.");
        }
        finally { Directory.Delete(link); }
        PublicationCases(scratch);
    }

    private static void PublicationCases(string scratch)
    {
        foreach (string mode in new[] { "success", "registration-failed", "rename-failed" })
        {
            string folder = Path.Combine(scratch, "publication-" + Guid.NewGuid().ToString("N"));
            string destination = Path.Combine(folder, "app"), staging = Path.Combine(folder, "app.staging-fixture");
            Directory.CreateDirectory(destination);
            Directory.CreateDirectory(staging);
            string oldExe = Path.Combine(destination, "KillerBench.exe");
            File.WriteAllText(oldExe, "Old payload must be preserved");
            File.WriteAllText(Path.Combine(destination, "payload.manifest"), Entry(oldExe, "KillerBench.exe"));
            File.WriteAllText(Path.Combine(staging, "KillerBench.exe"), "New prepared payload");
            var retained = new List<string>();
            bool failed = false;
            try
            {
                InstallationOwnership.PublishPreparedDirectory(destination, staging,
                    path => InstallationOwnership.ValidateDestination(path, ["KillerBench.exe"], oldExe, _ => true), () =>
                    {
                        string backup = Directory.GetDirectories(folder, "app.previous-*").Single();
                        File.WriteAllText(Path.Combine(backup, "inserted-after-validation.txt"), "Unowned sentinel inserted after final validation");
                        if (mode == "registration-failed") throw new IOException("Substituted registration failure.");
                    }, retained.Add,
                    (source, target) =>
                    {
                        if (mode == "rename-failed" && source == staging)
                        {
                            Directory.CreateDirectory(destination);
                            File.WriteAllText(Path.Combine(destination, "foreign.txt"), "Concurrent foreign destination");
                            throw new IOException("Substituted rename failure after a foreign destination appeared.");
                        }
                        Directory.Move(source, target);
                    });
            }
            catch (IOException) { failed = true; }
            string previous = Directory.GetDirectories(folder, "app.previous-*").Single();
            if (failed != (mode != "success") || File.ReadAllText(Path.Combine(previous, "KillerBench.exe")) != "Old payload must be preserved" || !retained.Contains(previous))
                throw new Exception("Setup did not preserve/report the previous root for publication case " + mode + ".");
            if (mode == "rename-failed")
            {
                if (File.ReadAllText(Path.Combine(destination, "foreign.txt")) != "Concurrent foreign destination" ||
                    File.ReadAllText(Path.Combine(staging, "KillerBench.exe")) != "New prepared payload" || !retained.Contains(staging) || !retained.Contains(destination))
                    throw new Exception("Failed publication removed a foreign destination or staged recovery content.");
            }
            else if (File.ReadAllText(Path.Combine(destination, "KillerBench.exe")) != "New prepared payload" ||
                File.ReadAllText(Path.Combine(previous, "inserted-after-validation.txt")) != "Unowned sentinel inserted after final validation" ||
                mode == "registration-failed" && !retained.Contains(destination))
                throw new Exception("Publication cleanup lost a post-validation sentinel, new payload, or recovery path.");
        }
        RestoredPublicationCase(scratch);
    }

    private static void RestoredPublicationCase(string scratch)
    {
        // The staged payload cannot be moved in, so the previous install is moved back. The restored
        // live install is not recovery data and must never be reported as retained.
        string folder = Path.Combine(scratch, "publication-" + Guid.NewGuid().ToString("N"));
        string destination = Path.Combine(folder, "app"), staging = Path.Combine(folder, "app.staging-fixture");
        Directory.CreateDirectory(destination);
        Directory.CreateDirectory(staging);
        string oldExe = Path.Combine(destination, "KillerBench.exe");
        File.WriteAllText(oldExe, "Old payload must be preserved");
        File.WriteAllText(Path.Combine(staging, "KillerBench.exe"), "New prepared payload");
        var retained = new List<string>();
        bool failed = false;
        try
        {
            InstallationOwnership.PublishPreparedDirectory(destination, staging, _ => { }, () => { }, retained.Add,
                (source, target) =>
                {
                    if (source == staging) throw new IOException("Substituted staging move failure.");
                    Directory.Move(source, target);
                });
        }
        catch (IOException) { failed = true; }
        if (!failed || File.ReadAllText(oldExe) != "Old payload must be preserved" || Directory.GetDirectories(folder, "app.previous-*").Length != 0)
            throw new Exception("A failed staging move did not restore the previous install.");
        if (retained.Contains(destination) || !retained.Contains(staging))
            throw new Exception("Setup reported the restored live install as retained recovery data, or lost the staging path: " + string.Join(", ", retained));
        Console.WriteLine("PASS: a restored live install is not reported as retained recovery data.");
    }

    private static string Entry(string path, string name)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) + "\t" + new FileInfo(path).Length + "\t" + name + "\n";
}
