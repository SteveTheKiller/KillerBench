using KillerBench.Engine;

internal static class CertificateChecks
{
    internal static void Run()
    {
        var now = DateTimeOffset.Parse("2026-09-14T12:00:00.0000000+00:00");
        string Row(string store, string thumbprint, string notBefore, string notAfter, string subject = "CN=Fixture") =>
            $"{{\"store\":\"{store}\",\"thumbprint\":\"{thumbprint}\",\"subject\":\"{subject}\",\"issuer\":\"CN=Fixture CA\",\"notBefore\":\"{notBefore}\",\"notAfter\":\"{notAfter}\"}}";
        string thumbA = new('A', 40), thumbB = new('B', 40), thumbC = new('C', 40);
        string evidence = "{\"complete\":true,\"certificates\":[" +
            Row("LocalMachine/My", thumbA, "2025-01-01T00:00:00.0000000+00:00", "2026-09-01T00:00:00.0000000+00:00") + "," +
            Row("CurrentUser/My", thumbB, "2025-01-01T00:00:00.0000000+00:00", "2026-09-30T00:00:00.0000000+00:00") + "," +
            Row("CurrentUser/My", thumbC, "2026-09-20T00:00:00.0000000+00:00", "2027-09-20T00:00:00.0000000+00:00") + "]}";
        var attention = CertificateDiagnostics.Interpret(new(0, evidence, "", false), now);
        Require(attention.State == CheckState.Attention && attention.Repair is null && attention.Detail.Contains("1 expired, 1 expiring") &&
            attention.Detail.Contains("1 not yet valid"),
            "Certificate date observations request review without exposing a repair");

        var empty = CertificateDiagnostics.Interpret(new(0, "{\"complete\":true,\"certificates\":[]}", "", false), now);
        Require(empty.State == CheckState.Information && empty.Detail.Contains("does not evaluate trust chains") && empty.Repair is null,
            "An empty bounded date review remains informational instead of claiming certificate health");

        foreach (var result in new[]
        {
            new ToolResult(1, "", "denied", false),
            new ToolResult(0, "{\"complete\":false,\"certificates\":[]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[]}", "", true),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[]}", "", false) { Truncated = true },
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + Row("Other/My", thumbA, "2025-01-01T00:00:00.0000000+00:00", "2026-09-01T00:00:00.0000000+00:00") + "]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + Row("LocalMachine/My", "XYZ", "2025-01-01T00:00:00.0000000+00:00", "2026-09-01T00:00:00.0000000+00:00") + "]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + Row("LocalMachine/My", thumbA, "2025-01-01T00:00:00.0000000+00:00", "2027-09-01T00:00:00.0000000+00:00") + "]}", "", false)
        })
        {
            var unknown = CertificateDiagnostics.Interpret(result, now);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null, "Incomplete or malformed certificate evidence cannot pass or authorize repair");
        }

        string ChainRow(string store, string thumbprint, bool trusted, string statuses, string subject = "CN=Fixture") =>
            $"{{\"store\":\"{store}\",\"thumbprint\":\"{thumbprint}\",\"subject\":\"{subject}\",\"trusted\":{trusted.ToString().ToLowerInvariant()},\"statuses\":[{statuses}]}}";
        var chainClear = CertificateChainDiagnostics.Interpret(new(0,
            "{\"complete\":true,\"certificates\":[" + ChainRow("LocalMachine/My", thumbA, true, "") + "]}", "", false));
        Require(chainClear.State == CheckState.Information && chainClear.Repair is null &&
            chainClear.Detail.Contains("cache-only chains for 1 personal certificates") &&
            chainClear.Detail.Contains("does not establish revocation"),
            "A locally trusted cache-only chain remains informational and bounded in scope");

        var chainAttention = CertificateChainDiagnostics.Interpret(new(0,
            "{\"complete\":true,\"certificates\":[" +
            ChainRow("LocalMachine/My", thumbA, false, "\"UntrustedRoot\"") + "," +
            ChainRow("CurrentUser/My", thumbB, false, "\"PartialChain\",\"NotTimeValid\"") + "]}", "", false));
        Require(chainAttention.State == CheckState.Attention && chainAttention.Repair is null &&
            chainAttention.Detail.Contains("2 did not build") && chainAttention.Detail.Contains("UntrustedRoot") &&
            chainAttention.Detail.Contains("PartialChain, NotTimeValid"),
            "Local chain failures request review without changing trust or selecting repair");

        foreach (var result in new[]
        {
            new ToolResult(1, "", "denied", false),
            new ToolResult(0, "{\"complete\":false,\"certificates\":[]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[]}", "", false) { TimedOut = true },
            new ToolResult(0, "{\"complete\":true,\"certificates\":[]}", "", true),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + ChainRow("Other/My", thumbA, true, "") + "]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + ChainRow("LocalMachine/My", "XYZ", true, "") + "]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + ChainRow("LocalMachine/My", thumbA, true, "\"UntrustedRoot\"") + "]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + ChainRow("LocalMachine/My", thumbA, false, "") + "]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + ChainRow("LocalMachine/My", thumbA, false, "\"FutureStatus\"") + "]}", "", false),
            new ToolResult(0, "{\"complete\":true,\"certificates\":[" + ChainRow("LocalMachine/My", thumbA, false, "\"UntrustedRoot\",\"UntrustedRoot\"") + "]}", "", false)
        })
        {
            var unknown = CertificateChainDiagnostics.Interpret(result);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null,
                "Incomplete, inconsistent or malformed chain evidence cannot pass or authorize repair");
        }
        Console.WriteLine("PASS: personal certificate dates and local cache-only chain trust are bounded; incomplete evidence stays unknown and no repair is selected.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
