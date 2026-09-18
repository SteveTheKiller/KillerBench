using KillerBench.Engine;

internal static class UpdateServiceChecks
{
    internal static void Run()
    {
        const string windowsUpdate = "9482f4b4-e343-43b6-b170-9a65bc822c77";
        const string microsoftUpdate = "7971f918-a847-4430-9279-4a52d1efe18d";
        ToolResult Evidence(string services, int total = 1) => new(0, $"{{\"total\":{total},\"services\":{services}}}", "", false);

        var step = UpdateServiceDiagnostics.Step();
        Require(step.Verification && step.Tool.EndsWith("powershell.exe", StringComparison.OrdinalIgnoreCase) &&
            step.Arguments.Contains("-NonInteractive") && step.Arguments[^1].Contains("Microsoft.Update.ServiceManager", StringComparison.Ordinal),
            "Update source collection is an exact read-only WUA service query");
        var both = UpdateServiceDiagnostics.Interpret(Evidence("[" +
            $"{{\"id\":\"{windowsUpdate}\",\"name\":\"Windows Update\",\"managed\":false,\"registered\":true,\"windows\":true}}," +
            $"{{\"id\":\"{microsoftUpdate}\",\"name\":\"Microsoft Update\",\"managed\":false,\"registered\":false,\"windows\":true}}]", 2));
        Require(both is { State: CheckState.Information, Repair: null } && both.Detail.Contains("Microsoft Update: registered") && both.Evidence is not null,
            "Registered Windows and Microsoft Update services remain informational evidence");
        Require(UpdateServiceDiagnostics.AssessSource(new(Source: UpdateSearchSource.MicrosoftUpdate), both.Evidence!) is
            { State: CheckState.Information, Repair: null } ready && ready.Detail.Contains("registered and selected"),
            "Registered Microsoft Update is ready for explicit read-only discovery");
        Require(UpdateServiceDiagnostics.AssessSource(new(Source: UpdateSearchSource.Configured), both.Evidence!).Detail.Contains("current managed or Automatic Updates") &&
            UpdateServiceDiagnostics.AssessSource(new(Source: UpdateSearchSource.WindowsUpdate), both.Evidence!).Detail.Contains("selected explicitly"),
            "Configured and Windows Update choices retain distinct source guidance");
        var noneAutomatic = UpdateServiceDiagnostics.Interpret(Evidence($"[{{\"id\":\"{windowsUpdate}\",\"name\":\"Windows Update\",\"managed\":true,\"registered\":false,\"windows\":true}}]"));
        Require(noneAutomatic is { State: CheckState.Attention, Repair: null } && noneAutomatic.Detail.Contains("No reported service is registered"),
            "Missing Automatic Updates registration requests review without mutation");
        Require(UpdateServiceDiagnostics.AssessSource(new(Source: UpdateSearchSource.MicrosoftUpdate), noneAutomatic.Evidence!) is
            { State: CheckState.Attention, Repair: null } missing && missing.Detail.Contains("No search was started"),
            "Missing Microsoft Update registration blocks its search without enrollment");

        foreach (var invalid in new[]
        {
            new ToolResult(1, "", "Unavailable", false),
            Evidence("[]", 0),
            Evidence($"[{{\"id\":\"{windowsUpdate}\",\"name\":\"Windows Update\",\"managed\":false,\"registered\":true,\"windows\":true}},{{\"id\":\"{windowsUpdate}\",\"name\":\"Duplicate\",\"managed\":false,\"registered\":false,\"windows\":true}}]", 2),
            Evidence($"[{{\"id\":\"{windowsUpdate}\",\"name\":\"Windows Update\",\"name\":\"Changed\",\"managed\":false,\"registered\":true,\"windows\":true}}]"),
            Evidence("[{\"id\":\"bad\",\"name\":\"Invalid\",\"managed\":false,\"registered\":true,\"windows\":true}]") with { Truncated = true }
        })
            Require(UpdateServiceDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Failed, empty, duplicate, malformed or truncated service evidence remains unknown");
        Console.WriteLine("PASS: Windows Update service registration evidence is bounded, classified and read-only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
