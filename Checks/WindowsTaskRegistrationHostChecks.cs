using System.Reflection;
using KillerBench.Engine;

internal static class WindowsTaskRegistrationHostChecks
{
    internal static void Run()
    {
        var probe = typeof(WindowsTaskRegistrationHost).GetMethod("TryReadTargetProbe",
            BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("Task target probe parser is unavailable.");
        foreach (var fixture in new[]
        {
            (Json: "{\"folderExists\":true,\"targetExists\":false}", Accepted: true, Folder: true, Target: false),
            (Json: "{\"folderExists\":true,\"targetExists\":true}", Accepted: true, Folder: true, Target: true),
            (Json: "{\"folderExists\":false,\"targetExists\":false}", Accepted: true, Folder: false, Target: false),
            (Json: "{\"folderExists\":true,\"folderExists\":false}", Accepted: false, Folder: false, Target: false),
            (Json: "{\"folderExists\":true,\"targetExists\":false,\"extra\":false}", Accepted: false, Folder: false, Target: false),
            (Json: "{\"folderExists\":1,\"targetExists\":false}", Accepted: false, Folder: false, Target: false),
            (Json: "[]", Accepted: false, Folder: false, Target: false),
            (Json: "not json", Accepted: false, Folder: false, Target: false)
        })
        {
            object?[] arguments = { new ToolResult(0, fixture.Json, "", false), false, false };
            bool accepted = (bool)probe.Invoke(null, arguments)!;
            if (accepted != fixture.Accepted || accepted &&
                ((bool)arguments[1]! != fixture.Folder || (bool)arguments[2]! != fixture.Target))
                throw new Exception("Task target probe evidence was interpreted incorrectly: " + fixture.Json);
        }

        var validate = typeof(WindowsTaskRegistrationHost).GetMethod("ValidateCreationResult",
            BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("Task creation response validator is unavailable.");
        var target = new ScheduledTaskIdentity("\\Fixture\\", "Create");
        string good = "{\"path\":\"\\\\Fixture\\\\Create\",\"enabled\":false}";
        foreach (var fixture in new[]
        {
            (Evidence: new ToolResult(0, good, "", false), Valid: true),
            (Evidence: new ToolResult(0, "{\"path\":\"\\\\Other\\\\Create\",\"enabled\":false}", "", false), Valid: false),
            (Evidence: new ToolResult(0, "{\"path\":\"\\\\Fixture\\\\Create\",\"enabled\":true}", "", false), Valid: false),
            (Evidence: new ToolResult(0, "{\"path\":\"\\\\Fixture\\\\Create\",\"path\":\"\\\\Other\\\\Create\"}", "", false), Valid: false),
            (Evidence: new ToolResult(0, good, "conflicting error", false), Valid: false),
            (Evidence: new ToolResult(0, good, "", true), Valid: false),
            (Evidence: new ToolResult(0, good, "", false) { TimedOut = true }, Valid: false),
            (Evidence: new ToolResult(1, good, "", false), Valid: false)
        })
        {
            var result = (ToolResult)validate.Invoke(null, new object?[] { target, fixture.Evidence })!;
            bool complete = result.ExitCode == 0 && !result.Truncated && !result.TimedOut && string.IsNullOrWhiteSpace(result.Error);
            if (complete != fixture.Valid)
                throw new Exception("Task creation response was interpreted incorrectly: " + fixture.Evidence.Output);
        }
        Console.WriteLine("PASS: native task host response parsing rejects ambiguous, changed, enabled and incomplete evidence; fixtures only.");
    }
}
