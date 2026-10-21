using System.IO;
using KillerBench.Cli;

/// <summary>Feeds damaged pack and workflow files to the public commands. Each must refuse with a readable error, never crash.</summary>
internal static class MalformedInputChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "malformed-input");
        Directory.CreateDirectory(root);

        const string pack = "{\"SchemaVersion\":1,\"Id\":\"test.pack\",\"Revision\":1,\"Platform\":\"Windows\",\"Title\":\"Test\",";
        await Refuse(root, "null recipe entry", pack + "\"Recipes\":[null]}", path => ["pack", "validate", path], 3);
        await Refuse(root, "null recipes", pack + "\"Recipes\":null}", path => ["pack", "validate", path], 3);
        await Refuse(root, "two null recipe entries", pack + "\"Recipes\":[null,null]}", path => ["pack", "validate", path], 3);

        const string workflow = "{\"kind\":\"Vitals\",\"machine\":\"Fixture\",\"status\":\"No issues detected in checked scopes\",\"startedUtc\":\"2026-10-21T00:00:00Z\",\"journalPath\":\"not-followed\",";
        const string check = "{\"name\":\"Disk\",\"state\":\"Clear\",\"detail\":\"Healthy\"}";
        string report = Path.Combine(root, "report.txt");
        string[] Report(string path) => ["workflow-report", path, report];
        await Refuse(root, "null check", workflow + "\"checks\":[null],\"repairs\":[]}", Report, 1);
        await Refuse(root, "null repair", workflow + "\"checks\":[" + check + "],\"repairs\":[null]}", Report, 1);
        await Refuse(root, "null recipe result", workflow + "\"checks\":[" + check + "],\"repairs\":[],\"recipes\":[null]}", Report, 1);
        await Refuse(root, "null action", workflow + "\"checks\":[" + check + "],\"repairs\":[],\"actions\":[null]}", Report, 1);
        await Refuse(root, "null check name", workflow + "\"checks\":[{\"name\":null,\"state\":\"Clear\",\"detail\":\"Healthy\"}],\"repairs\":[]}", Report, 1);
        await Refuse(root, "undefined check state", workflow + "\"checks\":[{\"name\":\"Disk\",\"state\":99,\"detail\":\"Healthy\"}],\"repairs\":[]}", Report, 1);
        if (File.Exists(report)) throw new Exception("A refused workflow journal must not leave a report behind.");

        string valid = Path.Combine(root, "valid.json");
        await File.WriteAllTextAsync(valid, workflow + "\"checks\":[" + check + "],\"repairs\":[]}");
        var (code, _, error) = await Run(Report(valid));
        if (code != 0 || !File.Exists(report)) throw new Exception("A well-formed workflow journal must still export: " + error);
        Console.WriteLine("PASS: damaged packs and workflow journals return controlled errors; a well-formed journal still exports.");
    }

    private static async Task Refuse(string root, string name, string json, Func<string, string[]> command, int expectedCode)
    {
        string path = Path.Combine(root, name.Replace(' ', '-') + ".json");
        await File.WriteAllTextAsync(path, json);
        int code;
        string error;
        try { (code, _, error) = await Run(command(path)); }
        catch (Exception crash) { throw new Exception(name + ": the command crashed instead of refusing: " + crash.GetType().Name + " " + crash.Message, crash); }
        if (code != expectedCode || string.IsNullOrWhiteSpace(error))
            throw new Exception(name + ": expected exit " + expectedCode + " with a readable error, got " + code + ": " + error);
    }

    private static async Task<(int Code, string Output, string Error)> Run(string[] args)
    {
        var previousOut = Console.Out;
        var previousError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            int code = await CommandEngine.RunAsync(args);
            return (code, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
        }
    }
}
