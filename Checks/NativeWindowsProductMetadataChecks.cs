using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Windows.Validation;

internal static class NativeWindowsProductMetadataChecks
{
    internal static void Run()
    {
        Expect("""{"Caption":"Microsoft Windows 11 Enterprise","ProductType":1,"Version":"10.0.26100","BuildNumber":"26100"}""", "Microsoft Windows 11 Enterprise");
        Expect("""{"Caption":"Microsoft Windows 10 Pro","ProductType":1,"Version":"10.0.19045","BuildNumber":"19045"}""", "Microsoft Windows 10 Pro");
        Expect("""{"Caption":"Microsoft Windows Server 2025 Standard","ProductType":3,"Version":"10.0.26100","BuildNumber":"26100"}""", "Microsoft Windows Server 2025 Standard");
        Expect("""{"Caption":"Microsoft Windows Server 2022 Standard","ProductType":2,"Version":"10.0.20348","BuildNumber":"20348"}""", "Microsoft Windows Server 2022 Standard");
        Expect("""{"Caption":" Microsoft Windows 11 Pro ","ProductType":1,"Version":"10.0.22000","BuildNumber":"22000"}""", "Microsoft Windows 11 Pro");
        foreach (string invalid in new[]
        {
            "", "[]", "not json",
            """{"Caption":"Windows 11","Version":"10.0.26100","BuildNumber":"26100"}""",
            """{"Caption":"Windows 11","ProductType":0,"Version":"10.0.26100","BuildNumber":"26100"}""",
            """{"Caption":"Windows 11","ProductType":"1","Version":"10.0.26100","BuildNumber":"26100"}""",
            """{"Caption":"Windows 11","ProductType":1,"Version":"10.0.26100","BuildNumber":"19045"}""",
            """{"Caption":"Windows 11","ProductType":1,"Version":"unknown","BuildNumber":"26100"}""",
            """{"Caption":"","ProductType":1,"Version":"10.0.26100","BuildNumber":"26100"}"""
        }) Expect(invalid, NativeWindowsProductMetadata.Unavailable);
        string valid = """{"Caption":"Microsoft Windows 11 Enterprise","ProductType":1,"Version":"10.0.26100","BuildNumber":"26100"}""";
        foreach (var failed in new[] { new ToolResult(1, valid, "", false), new ToolResult(0, valid, "", true), new ToolResult(0, valid, "error", false), new ToolResult(0, valid, "", false) { TimedOut = true } })
            if (NativeWindowsProductMetadata.FromProvider(failed) != NativeWindowsProductMetadata.Unavailable) throw new Exception("Failed OS metadata was treated as complete.");
        Console.WriteLine("PASS: native OS labels use complete current WMI identity for Windows 11, Windows 10 and Server, including shared build 26100; incomplete providers stay unavailable.");
    }

    private static void Expect(string output, string expected)
    {
        if (NativeWindowsProductMetadata.FromProvider(new ToolResult(0, output, "", false)) != expected)
            throw new Exception("Incorrect native Windows product metadata result.");
    }
}
