using KillerBench.Engine;

internal static class ProfileBackupCopyChecks
{
    internal static void Run()
    {
        var target = new ProfileSnapshot("S-1-5-21-1-2-3-1001", @"C:\Users\Fixture", false, false,
            DateTimeOffset.UtcNow.AddDays(-120), false);
        const string destination = @"D:\KillerBenchBackups\Fixture";
        RepairStep step = ProfileBackupCopy.CreateStep(target, destination);
        string[] required = ["/E", "/COPYALL", "/DCOPY:DAT", "/B", "/EFSRAW", "/XJ", "/SL", "/R:1", "/W:1", "/NFL", "/NDL", "/NJH", "/NJS", "/NP"];
        if (step.Tool != "robocopy.exe" || step.Verification || step.Arguments.Length != required.Length + 2 ||
            step.Arguments[0] != target.Path || step.Arguments[1] != destination ||
            !step.Arguments.Skip(2).SequenceEqual(required))
            throw new Exception("The native profile backup command is not the fixed bounded plan");

        foreach (int exitCode in Enumerable.Range(0, 8))
            if (!ProfileBackupCopy.Accepted(new(exitCode, "", "", false)))
                throw new Exception("A documented successful Robocopy outcome was rejected: " + exitCode);
        foreach (var rejected in new[]
        {
            new ToolResult(8, "", "", false),
            new ToolResult(-1, "", "", false),
            new ToolResult(0, "", "failure", false),
            new ToolResult(0, "", "", true),
            new ToolResult(0, "", "", false) { TimedOut = true }
        })
            if (ProfileBackupCopy.Accepted(rejected))
                throw new Exception("An incomplete Robocopy outcome was accepted");

        foreach (string invalid in new[] { target.Path, target.Path + "\\Nested", @"C:\Users", @"\\server\share\Fixture" })
        {
            try { ProfileBackupCopy.CreateStep(target, invalid); throw new Exception("An invalid backup destination was accepted"); }
            catch (ArgumentException) { }
        }

        Console.WriteLine("PASS: native profile backup uses one fixed bounded metadata-preserving Robocopy plan and accepts only complete exit evidence; command execution was substituted.");
    }
}
