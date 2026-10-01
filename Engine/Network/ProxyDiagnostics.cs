using System.Runtime.InteropServices;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public enum ProxyScope { WinHttpDefault, CurrentUserInternet }
public sealed record ProxyConfiguration(ProxyScope Scope, bool Complete, uint? AccessType = null,
    bool? AutoDetect = null, string? AutoConfigUrl = null, string? Proxy = null, string? Bypass = null,
    int? ErrorCode = null, string? CollectionIssue = null);

public static partial class ProxyDiagnostics
{
    public static IReadOnlyList<WorkflowCheck> Check()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Proxy collection requires Windows.");
        return [Assess(ReadDefault()), Assess(ReadUser())];
    }

    public static WorkflowCheck Assess(ProxyConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string name = configuration.Scope == ProxyScope.WinHttpDefault ? "WinHTTP default proxy" : "Current-account Internet proxy";
        bool valid = Enum.IsDefined(configuration.Scope) && configuration.Complete && configuration.ErrorCode is null &&
            configuration.CollectionIssue is null && new[] { configuration.Proxy, configuration.Bypass, configuration.AutoConfigUrl }
                .All(value => value is null || (value.Length <= 8192 && !value.Any(char.IsControl)));
        valid &= configuration.Scope == ProxyScope.WinHttpDefault
            ? configuration.AccessType is 1 or 3 && (configuration.AccessType != 3 || !string.IsNullOrWhiteSpace(configuration.Proxy))
            : configuration.AutoDetect is not null;
        string detail;
        if (!valid) detail = "Proxy configuration was unavailable or incomplete. No direct-connection assumption or repair selected.";
        else if (configuration.Scope == ProxyScope.WinHttpDefault)
            detail = configuration.AccessType == 1 ? "The stored WinHTTP default selects no proxy." : "The stored WinHTTP default selects a named proxy.";
        else detail = "Current process account's Internet settings: auto-detect " + (configuration.AutoDetect == true ? "on" : "off") + ".";
        static string Preview(string? value) => value is null ? "Not reported" : value.Length <= 256 ? value : value[..256] + " [shortened; full value in evidence]";
        if (valid) detail += "\nProxy: " + Preview(configuration.Proxy) + "\nBypass: " + Preview(configuration.Bypass) +
            (configuration.Scope == ProxyScope.CurrentUserInternet ? "\nAutomatic configuration URL: " + Preview(configuration.AutoConfigUrl) : "");
        detail += "\nApplications and sessions can override these settings. PAC/WPAD resolution, proxy reachability, authentication and policy ownership were not tested. No URLs were fetched or settings changed.";
        return new(name, valid ? CheckState.Information : CheckState.Unknown, detail,
            Evidence: new(valid ? 0 : -1, JsonSerializer.Serialize(configuration, Capture.JsonOptions), configuration.CollectionIssue ?? "", false));
    }

    private static ProxyConfiguration ReadDefault()
    {
        ProxyInfo info = default;
        try
        {
            if (!WinHttpGetDefaultProxyConfiguration(out info))
                return new(ProxyScope.WinHttpDefault, false, ErrorCode: Marshal.GetLastWin32Error());
            return new(ProxyScope.WinHttpDefault, true, info.AccessType, Proxy: Text(info.Proxy), Bypass: Text(info.Bypass));
        }
        catch (InvalidDataException error) { return new(ProxyScope.WinHttpDefault, false, CollectionIssue: error.Message); }
        finally { Free(info.Proxy); Free(info.Bypass); }
    }

    private static ProxyConfiguration ReadUser()
    {
        UserProxyInfo info = default;
        try
        {
            if (!WinHttpGetIEProxyConfigForCurrentUser(out info))
                return new(ProxyScope.CurrentUserInternet, false, ErrorCode: Marshal.GetLastWin32Error());
            return new(ProxyScope.CurrentUserInternet, true, AutoDetect: info.AutoDetect != 0,
                AutoConfigUrl: Text(info.AutoConfigUrl), Proxy: Text(info.Proxy), Bypass: Text(info.Bypass));
        }
        catch (InvalidDataException error) { return new(ProxyScope.CurrentUserInternet, false, CollectionIssue: error.Message); }
        finally { Free(info.AutoConfigUrl); Free(info.Proxy); Free(info.Bypass); }
    }

    private static string? Text(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero) return null;
        for (int length = 0; length <= 8192; length++)
            if (Marshal.ReadInt16(pointer, length * 2) == 0) return Marshal.PtrToStringUni(pointer, length);
        throw new InvalidDataException("Proxy configuration exceeds the 8192-character field limit.");
    }
    private static void Free(IntPtr pointer) { if (pointer != IntPtr.Zero) _ = GlobalFree(pointer); }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProxyInfo { public uint AccessType; public IntPtr Proxy; public IntPtr Bypass; }
    [StructLayout(LayoutKind.Sequential)]
    private struct UserProxyInfo { public int AutoDetect; public IntPtr AutoConfigUrl; public IntPtr Proxy; public IntPtr Bypass; }
    [LibraryImport("winhttp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinHttpGetDefaultProxyConfiguration(out ProxyInfo info);
    [LibraryImport("winhttp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinHttpGetIEProxyConfigForCurrentUser(out UserProxyInfo info);
    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalFree(IntPtr memory);
}
