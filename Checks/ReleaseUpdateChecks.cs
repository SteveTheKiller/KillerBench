using KillerBench.Services;

internal static class ReleaseUpdateChecks
{
    public static void Run()
    {
        var running = new Version(0, 1, 0);
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.1.1","draft":false,"prerelease":false}""", running) == "v0.1.1",
            "A newer stable release is offered");
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.1.0","draft":false,"prerelease":false}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.0.9","draft":false,"prerelease":false}""", running) is null,
            "The running release and older releases are not offered");
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.2.0","draft":false,"prerelease":true}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.2.0","draft":true,"prerelease":false}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.2.0","prerelease":null}""", running) is null,
            "Prereleases, drafts and unclear release states are never offered");
        Require(SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.2.0-beta"}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"v0.2.0 & del x"}""", running) is null &&
            SelfUpdate.ParseNewerRelease("""{"tag_name":"0.2.0"}""", running) is null &&
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
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
