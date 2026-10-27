using System.IO;
using KillerBench.Services;

internal static class ReleaseUpdateChecks
{
    public static void Run()
    {
        var running = new Version(1, 0, 0);
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v1.0.1","draft":false,"prerelease":false}""", running) == "v1.0.1",
            "A newer stable release is offered");
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v1.0.0","draft":false,"prerelease":false}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.9.9","draft":false,"prerelease":false}""", running) is null,
            "The running release and older releases are not offered");
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v1.1.0","draft":false,"prerelease":true}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v1.1.0","draft":true,"prerelease":false}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v1.1.0","prerelease":null}""", running) is null,
            "Prereleases, drafts and unclear release states are never offered");
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v1.1.0-beta"}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v1.1.0 & del x"}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"1.1.0"}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":2}""", running) is null &&
            SelfUpdate.ParseNewerRelease("[]", running) is null,
            "Only a plain vX.Y.Z tag can reach the update script");
        const string sums = "KillerBench.exe          AAA\nKillerBench-Portable.exe BBB\r\nkillerbench-cli.exe      CCC\n";
        Require(SelfUpdate.ExpectedHash(sums, SelfUpdate.InstallerAsset) == "AAA" &&
            SelfUpdate.ExpectedHash(sums, SelfUpdate.PortableAsset) == "BBB",
            "Each package reads its own checksum line");
        Require(SelfUpdate.ExpectedHash("KillerBench.exe.old AAA\nKillerBench.exe\n", SelfUpdate.InstallerAsset) is null,
            "A checksum listed for another file, or a line with no checksum, is never used");
        Require(SelfUpdate.StartupCheckEnabled(null) && SelfUpdate.StartupCheckEnabled("1") && !SelfUpdate.StartupCheckEnabled("0"),
            "The startup check stays on until it is turned off");
        Console.WriteLine("PASS: self-update offers only newer stable vX.Y.Z releases and reads each package's own checksum.");
        TaggingFailuresStopRelease();
    }

    /// <summary>release.ps1 must stop when the tag cannot be created or pushed, and a failed push must say how to
    /// delete the local tag it leaves behind, since the next run refuses an existing tag. Read only; never run.</summary>
    private static void TaggingFailuresStopRelease()
    {
        string script = File.ReadAllText(CheckSource.Find("release.ps1")).Replace("\r\n", "\n");
        int tag = script.IndexOf("git tag -a $Tag", StringComparison.Ordinal);
        int push = script.IndexOf("git push origin $Tag", StringComparison.Ordinal);
        Require(tag >= 0 && push > tag, "release.ps1 tags and then pushes the tag");
        string afterTag = script[tag..push];
        Require(afterTag.Contains("if ($LASTEXITCODE -ne 0) { throw", StringComparison.Ordinal),
            "release.ps1 stops when git tag -a fails");
        int release = script.IndexOf("gh release create", push, StringComparison.Ordinal);
        string afterPush = script[push..release];
        Require(afterPush.Contains("if ($LASTEXITCODE -ne 0)", StringComparison.Ordinal) &&
            afterPush.Contains("git tag -d $Tag", StringComparison.Ordinal) && afterPush.Contains("throw", StringComparison.Ordinal),
            "A failed tag push stops the release and says how to delete the stranded local tag");
        Console.WriteLine("PASS: release.ps1 stops on a failed tag or tag push and says how to delete a stranded local tag.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
