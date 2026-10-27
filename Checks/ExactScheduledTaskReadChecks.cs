using KillerBench.Engine;

internal static class ExactScheduledTaskReadChecks
{
    internal static void Run()
    {
        // Only a not-found error may read as Missing. Access denied or a stopped Schedule service must stay unknown.
        string script = ExactScheduledTaskDiagnostics.CreateReadStep("\\Fixture\\", "task").Arguments[^1];
        Require(script.Contains("ObjectNotFound", StringComparison.Ordinal) && script.Contains("CmdletizationQuery_NotFound", StringComparison.Ordinal) &&
            script.Contains("exit 1", StringComparison.Ordinal), "Read script separates a missing task from other lookup failures");
        Require(Read(new ToolResult(1, "", "Access is denied.", false)) == ExactScheduledTaskReadState.Unavailable,
            "A failed lookup reads as unavailable");
        Require(Read(new ToolResult(1, "", "", false)) == ExactScheduledTaskReadState.Unavailable, "A nonzero exit reads as unavailable");
        Require(Read(new ToolResult(0, "{\"found\":false}", "", false)) == ExactScheduledTaskReadState.Missing, "A not-found lookup reads as missing");

        // Task identities are literal, so wildcard characters must not reach Get-ScheduledTask as patterns.
        string literal = ExactScheduledTaskDiagnostics.CreateReadStep("\\Fix*ture\\", "a[1]*'?").Arguments[^1];
        Require(literal.Contains("-TaskName 'a`[1`]`*''`?'", StringComparison.Ordinal) && literal.Contains("-TaskPath '\\Fix`*ture\\'", StringComparison.Ordinal),
            "Read script escapes wildcard characters in the task identity");
    }

    private static ExactScheduledTaskReadState Read(ToolResult result) => ExactScheduledTaskDiagnostics.Parse(result, "\\Fixture\\", "task").State;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
