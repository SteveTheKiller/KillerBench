using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KillerBench.Services;

/// <summary>
/// Authenticode state and content hash of the running exe, and the same signature check for a
/// downloaded update. Reading a certificate out of a file only proves a certificate is embedded;
/// WinVerifyTrust does the full chain check, so a tampered or badly signed file cannot present its
/// publisher as verified.
/// </summary>
internal static partial class CodeSignature
{
    /// <summary>The running exe by default; UpdateService also checks a downloaded package with it.
    /// Retrieval is cache-only (0x1000), so nothing here waits on the network.</summary>
    internal static (bool Valid, string Subject, string Thumbprint) GetSignerInfo(string? file = null)
    {
        string path = file ?? Environment.ProcessPath ?? "";
        if (path.Length == 0) return (false, "(unavailable)", "(none)");
        string subject;
        string thumbprint;
        try
        {
#pragma warning disable SYSLIB0057
            using var source = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            using var certificate = new X509Certificate2(source);
            subject = certificate.GetNameInfo(X509NameType.SimpleName, false);
            if (string.IsNullOrEmpty(subject)) subject = certificate.Subject;
            thumbprint = string.IsNullOrEmpty(certificate.Thumbprint) ? "(none)" : certificate.Thumbprint;
        }
        catch { return (false, "(not signed)", "(none)"); }

        IntPtr pathPointer = Marshal.StringToHGlobalUni(path);
        IntPtr filePointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
        IntPtr dataPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustData>());
        try
        {
            Marshal.StructureToPtr(new WinTrustFileInfo { Size = (uint)Marshal.SizeOf<WinTrustFileInfo>(), FilePath = pathPointer }, filePointer, false);
            Marshal.StructureToPtr(new WinTrustData { Size = (uint)Marshal.SizeOf<WinTrustData>(), UiChoice = 2,
                UnionChoice = 1, Union = filePointer, ProviderFlags = 0x1000 }, dataPointer, false);
            Guid action = WinTrustGenericVerify;
            return (WinVerifyTrust(IntPtr.Zero, ref action, dataPointer) == 0, subject, thumbprint);
        }
        catch { return (false, subject, thumbprint); }
        finally
        {
            Marshal.FreeHGlobal(dataPointer);
            Marshal.FreeHGlobal(filePointer);
            Marshal.FreeHGlobal(pathPointer);
        }
    }

    /// <summary>SHA-256 of the running exe as lower-case hex. Slow on a large file, so callers run it
    /// off the UI thread.</summary>
    internal static string ExeSha256()
    {
        try
        {
            string? path = Environment.ProcessPath;
            if (string.IsNullOrEmpty(path)) return "(unavailable)";
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
        catch { return "(unavailable)"; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustFileInfo
    { public uint Size; public IntPtr FilePath; public IntPtr File; public IntPtr KnownSubject; }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustData
    {
        public uint Size; public IntPtr PolicyCallbackData; public IntPtr SipClientData; public uint UiChoice;
        public uint RevocationChecks; public uint UnionChoice; public IntPtr Union; public uint StateAction;
        public IntPtr StateData; public IntPtr UrlReference; public uint ProviderFlags; public uint UiContext;
        public IntPtr SignatureSettings;
    }

    private static readonly Guid WinTrustGenericVerify = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    [LibraryImport("wintrust.dll")]
    private static partial uint WinVerifyTrust(IntPtr window, ref Guid action, IntPtr data);
}
