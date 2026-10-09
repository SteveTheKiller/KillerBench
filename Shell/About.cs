using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace KillerBench;

public partial class MainWindow
{
    private const string SignerName = "Stephen Riley";

    private async void OnAbout(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        await ShowAboutAsync();
    }

    private async Task ShowAboutAsync()
    {
        if (AboutOverlay.Visibility == Visibility.Visible) { CloseAbout(); return; }
        ShortcutsOverlay.Visibility = Visibility.Collapsed;
        AboutVersionBlock.Text = "v" + (Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");
        AboutReleaseDateBlock.Text = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "ReleaseDate")?.Value ?? "";
        var (signerValid, signerSubject, signerThumbprint) = GetSignerInfo();
        AboutPublisherBlock.Text = signerValid ? signerSubject : "(not signed or chain failed)";
        AboutThumbprintBlock.Text = signerThumbprint;
        AboutAkaRun.Text = (char)0x201C + "Steve the Killer" + (char)0x201D;
        AboutAkaBlock.Visibility = signerValid && signerSubject.Contains(SignerName, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;
        AboutSha256Block.Text = "computing...";
        RefreshAgentRepairSettings();
        AboutOverlay.Opacity = 1;
        AboutOverlay.Visibility = Visibility.Visible;
        _ = Task.Run(ExecutableSha256).ContinueWith(task => Dispatcher.BeginInvoke(() =>
            AboutSha256Block.Text = task.Status == TaskStatus.RanToCompletion ? task.Result : "(unavailable)"));
        await Task.CompletedTask;
    }

    private void OnAboutBackdrop(object sender, MouseButtonEventArgs e) => CloseAbout();
    private void OnAboutCard(object sender, MouseButtonEventArgs e) => e.Handled = true;
    private void OnAboutClose(object sender, RoutedEventArgs e) => CloseAbout();
    private void CloseAbout() => AboutOverlay.Visibility = Visibility.Collapsed;
    private void OnAboutWebsite(object sender, MouseButtonEventArgs e) { OpenUrl("https://killerbench.net"); e.Handled = true; }
    private void OnAboutRelease(object sender, MouseButtonEventArgs e) { OpenUrl("https://github.com/SteveTheKiller/KillerBench/releases"); e.Handled = true; }
    private void OnAboutLink(object sender, RequestNavigateEventArgs e) { OpenUrl(e.Uri.AbsoluteUri); e.Handled = true; }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static string ExecutableSha256()
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

    private static (bool Valid, string Subject, string Thumbprint) GetSignerInfo()
    {
        string path = Environment.ProcessPath ?? "";
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
