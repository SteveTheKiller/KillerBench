using System.Diagnostics;
using System.IO;
using KillerBench.Engine;

internal static class ProfileBackupBoundaryChecks
{
    internal static void Run(string scratch)
    {
        string root = Path.Combine(scratch, "profile-boundary-" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(root, "Users", "Fixture");
        string outside = Path.Combine(root, "outside");
        string backups = Path.Combine(root, "backups");
        string alias = Path.Combine(root, "alias");
        string sourceAlias = Path.Combine(root, "sourcealias");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(backups);
        File.WriteAllText(Path.Combine(source, "note.txt"), "fixture");
        Junction(alias, source);
        Junction(sourceAlias, Path.Combine(root, "Users"));
        ProfileSnapshot Target(string path) => new("S-1-5-21-1-2-3-1001", path, false, false, DateTimeOffset.UtcNow.AddDays(-120), false);
        IProfileBackupHost host = new WindowsProfileBackupHost();
        bool Ready(string path, string destination) => host.BoundaryProblemAsync(Target(path), destination, default).GetAwaiter().GetResult() is null;

        if (!Ready(source, Path.Combine(backups, "new"))) throw new Exception("An ordinary separate backup destination was refused.");
        if (Ready(source, Path.Combine(alias, "new"))) throw new Exception("A destination ancestor redirected into the source was accepted.");
        if (Ready(Path.Combine(sourceAlias, "Fixture"), Path.Combine(backups, "new"))) throw new Exception("A redirected source ancestor was accepted.");

        using (ProfileBackupPathGuard.Open(source, Path.Combine(backups, "new")))
        {
            bool blocked = false;
            try { Directory.Move(backups, backups + ".moved"); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { blocked = true; }
            if (!blocked) throw new Exception("A destination ancestor could be retargeted while held.");
        }

        Junction(Path.Combine(source, "Application Data"), outside);
        if (!Ready(source, Path.Combine(backups, "new"))) throw new Exception("A compatibility junction blocked an ordinary profile.");
        string copy = Path.Combine(backups, "copy");
        Directory.CreateDirectory(copy);
        File.Copy(Path.Combine(source, "note.txt"), Path.Combine(copy, "note.txt"));
        var verification = ProfileBackupArtifactWriter.CreateForExistingCopyAsync(Target(source), copy).GetAwaiter().GetResult();
        if (!verification.Verified || !verification.Detail.Contains("1 junction", StringComparison.Ordinal))
            throw new Exception("A profile with a junction did not verify with its omission disclosed: " + verification.Detail);

        string link = Path.Combine(source, "linked");
        try { Directory.CreateSymbolicLink(link, outside); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { link = ""; }
        if (link.Length > 0 && Ready(source, Path.Combine(backups, "new"))) throw new Exception("A symbolic link was accepted before copying.");
        if (link.Length > 0) Directory.Delete(link);
        Console.WriteLine("PASS: profile backup refuses redirected ancestors and unsupported links before copying, holds ancestors, and discloses omitted junctions" +
            (link.Length == 0 ? "; symbolic link case skipped without the privilege." : "."));
    }

    private static void Junction(string link, string target)
    {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true })!;
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new Exception("The fixture junction was not created.");
    }
}
