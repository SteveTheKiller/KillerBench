using System.Text.Json;

namespace KillerBench.Engine.Knowledge;

public static class SearchCatalog
{
    public static RepairStep CreateReadStep()
    {
        const string script = """
            $ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'
            if((Get-Service -Name WSearch -ErrorAction Stop).Status -ne 'Running'){throw 'Windows Search is not running; no service start attempted'}
            Add-Type -TypeDefinition @'
            using System;
            using System.Runtime.InteropServices;
            public static class KillerBenchSearchSnapshot {
                [ComImport, Guid("AB310581-AC80-11D1-8DF3-00C04FB6EF69"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                private interface Manager {
                    void GetIndexerVersionStr(out IntPtr value);
                    void GetIndexerVersion(out uint major, out uint minor);
                    void GetParameter([MarshalAs(UnmanagedType.LPWStr)] string name, out IntPtr value);
                    void SetParameter([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr value);
                    void GetProxyName(out IntPtr name);
                    void GetBypassList(out IntPtr value);
                    void SetProxy(int mode, [MarshalAs(UnmanagedType.Bool)] bool bypass, uint port,
                        [MarshalAs(UnmanagedType.LPWStr)] string proxy, [MarshalAs(UnmanagedType.LPWStr)] string list);
                    void GetCatalog([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.Interface)] out Catalog catalog);
                }
                [ComImport, Guid("AB310581-AC80-11D1-8DF3-00C04FB6EF50"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                private interface Catalog {
                    void GetName(out IntPtr name);
                    void GetParameter([MarshalAs(UnmanagedType.LPWStr)] string name, out IntPtr value);
                    void SetParameter([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr value);
                    void GetCatalogStatus(out int status, out int reason);
                }
                public static int[] Read() {
                    object instance = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("7D096C5F-AC08-4F1F-BEB7-5C22C517CE39")));
                    Catalog catalog = null;
                    try {
                        ((Manager)instance).GetCatalog("SystemIndex", out catalog);
                        int status, reason;
                        catalog.GetCatalogStatus(out status, out reason);
                        return new int[] {status, reason};
                    } finally {
                        if(catalog != null) Marshal.ReleaseComObject(catalog);
                        Marshal.ReleaseComObject(instance);
                    }
                }
            }
            '@
            $value=[KillerBenchSearchSnapshot]::Read()
            [pscustomobject]@{catalog='SystemIndex';status=$value[0];reason=$value[1]}|ConvertTo-Json -Compress
            """;
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Search catalog inspection requires Windows.");
        return Interpret(await BoundedTool.RunAsync(CreateReadStep(), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        const string name = "Windows Search catalog";
        WorkflowCheck Unknown() => new(name, CheckState.Unknown, "Catalog state unavailable or incomplete. No index or service change selected.", Evidence: evidence);
        if (!TaskRegistration.Complete(evidence) || evidence.Output.Length > 4096) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                root.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != 3 ||
                root.GetProperty("catalog").GetString() != "SystemIndex" ||
                !root.GetProperty("status").TryGetInt32(out int status) || status is < 0 or > 6 ||
                !root.GetProperty("reason").TryGetInt32(out int reason) || reason is < 0 or > 10 ||
                (status != 1 && reason != 0)) return Unknown();
            string[] states = ["Idle", "Paused", "Recovering", "Full crawl", "Incremental crawl", "Processing notifications", "Shutting down"];
            string[] reasons = ["No reason reported", "High disk activity", "High CPU activity", "High notification rate", "Low battery", "Low memory",
                "Low disk space", "Delayed recovery", "User activity", "External request", "Upgrade in progress"];
            string detail = "SystemIndex: " + states[status] + (status == 1 ? ". Pause reason: " + reasons[reason] : "") + ". ";
            if (status == 1 && reason is 5 or 6) detail += "Review available memory or free space before retrying indexing. ";
            detail += "Point-in-time catalog status does not establish complete search results or absence of indexing errors. No resume, rebuild, deletion or policy change was performed.";
            return new(name, status == 1 && reason is 5 or 6 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        { return Unknown(); }
    }
}
