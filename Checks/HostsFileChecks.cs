using KillerBench.Engine;

internal static class HostsFileChecks
{
    public static void Run()
    {
        var clean = HostsFileDiagnostics.Analyze("# private comment\n127.0.0.1 localhost\n::1 localhost6\n192.0.2.10 app.example.test alias.example.test\n");
        Require(clean.State == CheckState.Information && clean.Repair is null && clean.Evidence is not null &&
            clean.Detail.Contains("Reviewed 4 Hosts file mappings", StringComparison.Ordinal) &&
            !clean.Evidence.Output.Contains("private comment", StringComparison.Ordinal),
            "Complete Hosts mappings are retained without comments or repair selection");
        var attention = HostsFileDiagnostics.Analyze("bad line\n192.0.2.1 conflict.test\n192.0.2.2 CONFLICT.test\n192.0.2.2 repeat.test\n192.0.2.2 repeat.test\n");
        Require(attention.State == CheckState.Attention && attention.Detail.Contains("Invalid lines: 1", StringComparison.Ordinal) &&
            attention.Detail.Contains("conflicting names: 1", StringComparison.Ordinal) && attention.Detail.Contains("repeated mappings: 1", StringComparison.Ordinal),
            "Malformed, conflicting and repeated Hosts entries request review");
        Require(HostsFileDiagnostics.Analyze("127.0.0.1 only.test # secret comment and path C:\\Private\n").Evidence?.Output.Contains("secret", StringComparison.Ordinal) == false,
            "Inline Hosts comments are excluded from retained evidence");
        Require(HostsFileDiagnostics.Analyze("127.0.0.1 bad/name\n").State == CheckState.Attention,
            "Invalid host tokens cannot enter mapping evidence");
        string tooManyLines = string.Join("\n", Enumerable.Repeat("#", 4097));
        Require(HostsFileDiagnostics.Analyze(tooManyLines).State == CheckState.Unknown,
            "Oversized Hosts line coverage remains unknown");
        try { HostsFileDiagnostics.Analyze(null!); throw new Exception("Null Hosts evidence accepted"); }
        catch (ArgumentNullException) { }
        Console.WriteLine("PASS: bounded Hosts mappings, comment privacy, conflicts and malformed lines; no resolver changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
