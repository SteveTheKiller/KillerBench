using System.Text.Json;
using KillerBench.Engine;

internal static class TaskDefinitionChecks
{
    internal static void Run()
    {
        var target = new ScheduledTaskIdentity(@"\Fixture\", "Existing task");
        string xml = $"<Task xmlns='{ScheduledTaskXml.Namespace}'><Actions><Exec><Command>literal &amp; command</Command></Exec></Actions></Task>";
        var evidence = new ToolResult(0, JsonSerializer.Serialize(new { path = target.FullName, xml }), "", false);
        var result = ScheduledTaskDefinition.Interpret(target, evidence);
        if (!result.Observed || result.Xml != xml || result.Evidence != evidence) throw new Exception("Task definition lost exact XML or identity");
        foreach (var invalid in new[] { evidence with { TimedOut = true }, evidence with { ExitCode = 5 }, evidence with { Truncated = true },
            evidence with { Error = "denied" }, evidence with { Output = "[]" }, evidence with { Output = "{}" },
            evidence with { Output = evidence.Output.Replace("Existing task", "Other task") },
            evidence with { Output = "{\"path\":null,\"path\":null}" } })
            if (ScheduledTaskDefinition.Interpret(target, invalid).Observed) throw new Exception("Invalid task definition evidence accepted");
        foreach (string badXml in new[] { "<Task>", "<Task/>", "", new string('x', ScheduledTaskDefinition.MaximumCharacters + 1),
            "<!DOCTYPE Task [<!ENTITY x 'expanded'>]><Task xmlns='" + ScheduledTaskXml.Namespace + "'>&x;</Task>" })
            if (ScheduledTaskDefinition.Interpret(target, evidence with { Output = JsonSerializer.Serialize(new { path = target.FullName, xml = badXml }) }).Observed)
                throw new Exception("Malformed or unbounded task XML accepted");
        Console.WriteLine("PASS: exact task definition, raw XML retention, incomplete evidence, size bounds and DTD rejection; fixtures only.");
    }
}
