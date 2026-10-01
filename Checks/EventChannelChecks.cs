using KillerBench.Engine;

internal static class EventChannelChecks
{
    internal static void Run()
    {
        const string xml = "<channel xmlns='http://schemas.microsoft.com/win/2004/08/events' name='System' enabled='true'><logging><retention>false</retention><autoBackup>false</autoBackup><maxSize>20971520</maxSize></logging></channel>";
        static ToolResult Evidence(string text) => new(0, text, "", false);
        var good = EventChannelDiagnostics.Interpret("System", Evidence(xml));
        Require(good.State == CheckState.Information && good.Repair is null && good.Detail.Contains("20971520"), "Valid channel configuration is informational, not health or repair eligibility");
        var disabled = EventChannelDiagnostics.Interpret("System", Evidence(xml.Replace("enabled='true'", "enabled='false'")));
        Require(disabled.State == CheckState.Attention && disabled.Repair is null, "Disabled channel is review only");
        var retained = EventChannelDiagnostics.Interpret("System", Evidence(xml.Replace("<retention>false", "<retention>true")));
        Require(retained.State == CheckState.Information && retained.Detail.Contains("Current fullness is not measured"), "Retention does not imply a full log");
        foreach (var bad in new[] { xml.Replace("name='System'", "name='Application'"), xml.Replace("20971520", "0"),
            xml.Replace("20971520", "-1"), xml.Replace("enabled='true'", "enabled='maybe'"),
            xml.Replace("<autoBackup>false", "<autoBackup>true"), xml.Replace("</logging>", "<maxSize>123</maxSize></logging>"),
            xml.Replace("<retention>false</retention>", ""), "<channel>", "<!DOCTYPE channel [<!ENTITY x 'value'>]>" + xml })
            Require(EventChannelDiagnostics.Interpret("System", Evidence(bad)).State == CheckState.Unknown, "Invalid or mismatched channel evidence stays unknown");
        foreach (var bad in new[] { Evidence(xml) with { ExitCode = 5 }, Evidence(xml) with { Truncated = true },
            Evidence(xml) with { TimedOut = true }, Evidence(xml) with { Error = "unavailable" } })
            Require(EventChannelDiagnostics.Interpret("System", bad).State == CheckState.Unknown, "Incomplete command evidence stays unknown");
        Console.WriteLine("PASS: event channel identity, enabled state, retention interpretation and malformed evidence; no settings changed.");
        var visited = new List<string>();
        var partial = EventChannelDiagnostics.CheckAsync((channel, _) =>
        {
            visited.Add(channel);
            if (channel == "Application") throw new UnauthorizedAccessException("fixture denied");
            return Task.FromResult(Evidence(xml.Replace("name='System'", $"name='{channel}'")));
        }).GetAwaiter().GetResult();
        Require(visited.Count == 4 && partial.Count == 4 && partial[1].State == CheckState.Unknown &&
            partial[0].State == CheckState.Information && partial[3].State == CheckState.Information &&
            partial[1].Evidence!.Error.Contains("fixture denied"), "Unavailable channel preserves surrounding evidence and failure reason");
        using var stopped = new CancellationTokenSource();
        visited.Clear();
        try
        {
            EventChannelDiagnostics.CheckAsync((channel, _) =>
            {
                visited.Add(channel);
                stopped.Cancel();
                return Task.FromResult(Evidence(xml));
            }, stopped.Token).GetAwaiter().GetResult();
            throw new InvalidOperationException("Cancellation was ignored");
        }
        catch (OperationCanceledException) { Require(visited.Count == 1, "Cancellation prevents later channel collection"); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
