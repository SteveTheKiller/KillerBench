using KillerBench.Engine;

internal static class FontRegistrationChecks
{
    public static void Run()
    {
        var evidence = new ToolResult(0,
            "{\"registrations\":[{\"scope\":\"Machine\",\"registration\":\"Fixture Regular (TrueType)\",\"valid\":true,\"exists\":true},{\"scope\":\"User\",\"registration\":\"Missing Fixture (TrueType)\",\"valid\":true,\"exists\":false}]}",
            "", false);
        var attention = FontRegistrationDiagnostics.Interpret(evidence).Single();
        Require(attention.State == CheckState.Attention && attention.Evidence == evidence && attention.Repair is null &&
            attention.Detail.Contains("1 machine and 1 current-user") && attention.Detail.Contains("User: Missing Fixture") &&
            attention.Detail.Contains("does not authorize"),
            "Missing registered files are bounded diagnostic evidence without repair authorization");

        var clear = FontRegistrationDiagnostics.Interpret(evidence with
        {
            Output = evidence.Output.Replace("\"exists\":false", "\"exists\":true")
        }).Single();
        Require(clear.State == CheckState.Information && clear.Detail.Contains("does not test font parsing") && clear.Repair is null,
            "Existing registered files do not overclaim rendering or cache health");
        Require(FontRegistrationDiagnostics.Interpret(new(0, "{\"registrations\":[]}", "", false)).Single().State == CheckState.Information,
            "No registrations remains scoped information rather than a corruption claim");

        string tooMany = System.Text.Json.JsonSerializer.Serialize(new
        {
            registrations = Enumerable.Range(0, 1025).Select(i => new { scope = "Machine", registration = "Font " + i, valid = true, exists = true })
        });
        Require(FontRegistrationDiagnostics.Interpret(new(0, tooMany, "", false)).Single().State == CheckState.Unknown,
            "Registration inventory beyond the collection bound is incomplete");

        foreach (string invalid in new[]
        {
            "{}", "{\"registrations\":{}}",
            "{\"registrations\":[{\"scope\":\"Other\",\"registration\":\"Fixture\",\"valid\":true,\"exists\":true}]}",
            "{\"registrations\":[{\"scope\":\"Machine\",\"registration\":\"bad\\nname\",\"valid\":true,\"exists\":true}]}",
            "{\"registrations\":[{\"scope\":\"Machine\",\"registration\":\"Fixture\",\"valid\":true,\"exists\":true},{\"scope\":\"Machine\",\"registration\":\"Fixture\",\"valid\":true,\"exists\":true}]}"
        })
            Require(FontRegistrationDiagnostics.Interpret(new(0, invalid, "", false)).Single().State == CheckState.Unknown,
                "Malformed or duplicate font registration evidence remains unknown");

        foreach (var incomplete in new[]
        {
            evidence with { ExitCode = 1 }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Error = "failure" }
        })
            Require(FontRegistrationDiagnostics.Interpret(incomplete).Single().State == CheckState.Unknown,
                "Incomplete font registration collection remains unknown");

        Console.WriteLine("PASS: bounded font registration inventory, strict evidence validation and no repair inference.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
