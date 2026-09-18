using KillerBench.Engine;

internal static class ProxyChecks
{
    internal static void Run()
    {
        foreach (var item in new[] {
            new ProxyConfiguration(ProxyScope.WinHttpDefault, true, 1),
            new ProxyConfiguration(ProxyScope.WinHttpDefault, true, 3, Proxy: "proxy.example.test:8080", Bypass: "<local>"),
            new ProxyConfiguration(ProxyScope.CurrentUserInternet, true, AutoDetect: true, AutoConfigUrl: "https://proxy.example.test/config.pac"),
            new ProxyConfiguration(ProxyScope.CurrentUserInternet, true, AutoDetect: false) })
        {
            var check = ProxyDiagnostics.Assess(item);
            if (check.State != CheckState.Information || check.Repair is not null || check.Evidence is null)
                throw new Exception("Proxy observation must remain informational with evidence.");
        }
        foreach (var item in new[] {
            new ProxyConfiguration(ProxyScope.WinHttpDefault, false, ErrorCode: 5),
            new ProxyConfiguration(ProxyScope.WinHttpDefault, true, 3),
            new ProxyConfiguration(ProxyScope.WinHttpDefault, true, 99),
            new ProxyConfiguration(ProxyScope.CurrentUserInternet, true),
            new ProxyConfiguration(ProxyScope.CurrentUserInternet, true, AutoDetect: false, Proxy: "bad\nvalue"),
            new ProxyConfiguration(ProxyScope.CurrentUserInternet, true, AutoDetect: false, Proxy: new string('x', 8193)) })
            if (ProxyDiagnostics.Assess(item).State != CheckState.Unknown) throw new Exception("Incomplete proxy configuration was accepted.");
        var longValue = ProxyDiagnostics.Assess(new(ProxyScope.WinHttpDefault, true, 3, Proxy: new string('x', 500)));
        if (!longValue.Detail.Contains("[shortened") || !longValue.Evidence!.Output.Contains(new string('x', 500)))
            throw new Exception("Proxy display limits discarded original evidence.");
        Console.WriteLine("PASS: direct, named and automatic proxy observations, incomplete data and bounded previews; no proxy changes.");
    }
}
