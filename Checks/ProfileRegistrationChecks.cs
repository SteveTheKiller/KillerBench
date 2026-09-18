using KillerBench.Engine;

internal static class ProfileRegistrationChecks
{
    internal static void Run()
    {
        const string sid = "S-1-5-21-1-2-3-1001";
        string ordinary = "{\"sid\":\"" + sid + "\",\"path\":\"C:\\\\Users\\\\Fixture\",\"key\":\"" + sid + "\",\"backup\":false,\"state\":0,\"refCount\":0}";
        string backup = "{\"sid\":\"" + sid + "\",\"path\":\"C:\\\\Users\\\\Fixture\",\"key\":\"" + sid + ".bak\",\"backup\":true,\"state\":1,\"refCount\":2}";
        var clear = ProfileRegistrations.Interpret(new(0, "{\"items\":[" + ordinary + "]}", "", false));
        if (!clear.Complete || clear.Items.Count != 1 || clear.ForSid(sid).Count != 1 ||
            ProfileRegistrations.Check(clear).State != CheckState.Information) throw new Exception("Clear profile registration evidence mismatch");
        var review = ProfileRegistrations.Interpret(new(0, "{\"items\":[" + ordinary + "," + backup + "]}", "", false));
        if (!review.Complete || review.Items.Count != 2 || ProfileRegistrations.Check(review).State != CheckState.Attention ||
            !ProfileRegistrations.Check(review).Detail.Contains("1 backup keys") || !ProfileRegistrations.Check(review).Detail.Contains("1 nonzero reference counts"))
            throw new Exception("Profile registration review evidence mismatch");
        var unresolved = ProfileRegistrations.Interpret(new(0, "{\"items\":[" + ordinary.Replace("\"refCount\":0", "\"refCount\":null") + "]}", "", false));
        if (!unresolved.Complete || ProfileRegistrations.Check(unresolved).State != CheckState.Unknown)
            throw new Exception("Unresolved profile registration value did not stay unknown");
        foreach (var invalid in new[]
        {
            new ToolResult(5, "{\"items\":[]}", "", false),
            new ToolResult(0, "{\"items\":[]}", "error", false),
            new ToolResult(0, "{\"items\":[]}", "", true),
            new ToolResult(0, "{\"items\":[]}", "", false) { TimedOut = true },
            new ToolResult(0, "{}", "", false),
            new ToolResult(0, "{\"items\":[],\"items\":[]}", "", false),
            new ToolResult(0, "{\"items\":[" + ordinary + "," + ordinary + "]}", "", false),
            new ToolResult(0, "{\"items\":[" + ordinary.Replace("\"backup\":false", "\"backup\":true") + "]}", "", false),
            new ToolResult(0, "{\"items\":[" + ordinary.Replace("C:\\\\Users\\\\Fixture", "\\\\\\\\server\\\\share") + "]}", "", false)
        })
            if (ProfileRegistrations.Interpret(invalid).Complete) throw new Exception("Invalid profile registration evidence was accepted");
        if (ProfileRegistrations.Check(new([], false)).State != CheckState.Unknown)
            throw new Exception("Incomplete profile registration evidence did not stay unknown");
        Console.WriteLine("PASS: exact ProfileList registrations, backup keys, reference counts, state and incomplete evidence; no registry changes.");
    }
}
