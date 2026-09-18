using KillerBench.Engine;

internal static class FocusedDefenderOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = FocusedDefenderOperationalCorrelation.Check(Evidence());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("25 exact") &&
            clear.Detail.Contains("not a malware scan"), "Complete focused Defender evidence produces a bounded informational conclusion");
        var attention = FocusedDefenderOperationalCorrelation.Check(Evidence("Tamper protection", CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("1 need attention"),
            "Focused Defender attention remains review-only");
        var incomplete = FocusedDefenderOperationalCorrelation.Check(Evidence("Cloud protection", CheckState.Unknown));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete focused Defender evidence takes precedence");
        Require(FocusedDefenderOperationalCorrelation.Check(Evidence().Skip(1).ToArray()).State == CheckState.Unknown &&
            FocusedDefenderOperationalCorrelation.Check(Evidence().Append(Evidence()[0]).ToArray()).State == CheckState.Unknown,
            "Missing and duplicate focused Defender identities fail closed");
        try { FocusedDefenderOperationalCorrelation.Check(Evidence(), new CancellationToken(true)); throw new Exception("Defender correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: focused Defender evidence produces one bounded conclusion without claiming a malware scan or selecting repair.");
    }

    private static WorkflowCheck[] Evidence(string? changed = null, CheckState state = CheckState.Information)
    {
        string[] names =
        [
            "TPM readiness", "Secure Boot", "Virtualization-based security", "Memory integrity", "Credential Guard",
            "AppLocker Executable", "AppLocker Windows Installer", "AppLocker Script", "AppLocker DLL", "AppLocker Packaged app",
            "Defender active protection", "Behavior monitoring", "Downloaded-file scanning", "Network inspection system",
            "Tamper protection", "Potentially unwanted application protection", "Network protection", "Cloud protection",
            "Automatic sample submission", "Controlled folder access", "Microsoft Defender threat history", "Microsoft Defender scan history",
            "Recent Microsoft Defender events", "Administrator", "Restart"
        ];
        return names.Select(name => new WorkflowCheck(name, name == changed ? state : CheckState.Information, "fixture")).ToArray();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
