using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private const string SignerName = "Stephen Riley";
    private const string UpdateConfigurationName = "knowledge-updates.json";
    private CancellationTokenSource? knowledgeUpdateCancellation;

    private static string KnowledgeStorageRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Knowledge");

    private static string UpdateConfigurationPath => Path.Combine(AppContext.BaseDirectory, UpdateConfigurationName);

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
        var signer = GetSignerInfo();
        AboutPublisherBlock.Text = signer.Valid ? signer.Subject : "(not signed or chain failed)";
        AboutThumbprintBlock.Text = signer.Thumbprint;
        AboutAkaRun.Text = (char)0x201C + "Steve the Killer" + (char)0x201D;
        AboutAkaBlock.Visibility = signer.Valid && signer.Subject.Contains(SignerName, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;
        AboutSha256Block.Text = "computing...";
        AboutDatabaseUpdate.IsEnabled = false;
        AboutDatabaseInfo.Text = "Inspecting configured databases...";
        AboutDatabaseStatus.Text = "";
        AboutOverlay.Opacity = 1;
        AboutOverlay.Visibility = Visibility.Visible;
        _ = Task.Run(ExecutableSha256).ContinueWith(task => Dispatcher.BeginInvoke(() =>
            AboutSha256Block.Text = task.Status == TaskStatus.RanToCompletion ? task.Result : "(unavailable)"));
        await RefreshDatabaseStatusAsync();
    }

    private async Task RefreshDatabaseStatusAsync(bool preserveMessage = false)
    {
        if (!File.Exists(UpdateConfigurationPath))
        {
            AboutDatabaseInfo.Text = "No publisher configuration installed.";
            AboutDatabaseStatus.Text = "Database updates will become available when a reviewed configuration ships with KillerBench.";
            AboutDatabaseUpdate.IsEnabled = false;
            return;
        }
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var configuration = await KnowledgeUpdateConfiguration.ReadFileAsync(UpdateConfigurationPath, deadline.Token);
            var statuses = await configuration.InspectAsync(KnowledgeStorageRoot, deadline.Token);
            int verified = statuses.Count(status => status.Verified);
            int entries = statuses.Where(status => status.Verified).Sum(status => status.EntryCount ?? 0);
            AboutDatabaseInfo.Text = $"{verified:N0} of {statuses.Count:N0} configured databases verified, {entries:N0} entries.";
            if (!preserveMessage)
            {
                AboutDatabaseStatus.Text = string.Join(" ", statuses.Where(status => !status.Verified)
                    .Take(3).Select(status => status.PackId + ": " + status.Detail));
                if (statuses.All(status => status.Verified)) AboutDatabaseStatus.Text = "Installed database signatures and revisions are verified.";
            }
            AboutDatabaseUpdate.IsEnabled = knowledgeUpdateCancellation is null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            CryptographicException or System.Text.Json.JsonException or OperationCanceledException)
        {
            AboutDatabaseInfo.Text = "Publisher configuration unavailable.";
            AboutDatabaseStatus.Text = error.Message;
            AboutDatabaseUpdate.IsEnabled = false;
        }
    }

    private async void OnAboutDatabaseUpdate(object sender, RoutedEventArgs e)
    {
        if (knowledgeUpdateCancellation is not null) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        knowledgeUpdateCancellation = cancellation;
        AboutDatabaseUpdate.IsEnabled = false;
        try
        {
            var configuration = await KnowledgeUpdateConfiguration.ReadFileAsync(UpdateConfigurationPath, cancellation.Token);
            var plan = configuration.CreatePlan(KnowledgeStorageRoot);
            using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("KillerBench/0.1.0");
            var source = new HttpKnowledgeCandidateSource(client, plan.Endpoints);
            var progress = new Progress<KnowledgeUpdateProgress>(item =>
            {
                AboutDatabaseStatus.Text = item.PackId + ": " + item.Detail;
                Report("Database " + item.PackId + ": " + item.Stage + ".");
            });
            var results = await KnowledgeUpdates.UpdateAllAsync(plan.Requests, source, progress, cancellation.Token);
            foreach (var request in plan.Requests)
            {
                try
                {
                    var active = await KnowledgeActivation.LoadActiveAsync(request.StorageDirectory,
                        request.TrustedPublicKey, request.PackId, request.MinimumRevision, cancellation.Token);
                    sessionKnowledgeCatalogs.AddOrReplace(active.Catalog);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
                    CryptographicException or System.Text.Json.JsonException) { }
            }
            int failed = results.Count(result => result.State == KnowledgeUpdateState.Failed);
            AboutDatabaseStatus.Text = failed == 0
                ? "All configured databases are current and verified."
                : $"{failed:N0} database update{(failed == 1 ? "" : "s")} failed. Prior verified data was retained.";
            Report(failed == 0 ? "Knowledge databases are current and verified." : "Some database updates failed. Prior verified data was retained.");
        }
        catch (OperationCanceledException)
        { AboutDatabaseStatus.Text = "Database update stopped or timed out. Prior verified data was retained."; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            CryptographicException or System.Text.Json.JsonException or HttpRequestException or ArgumentException)
        { AboutDatabaseStatus.Text = "Database update unavailable: " + error.Message; }
        finally
        {
            knowledgeUpdateCancellation = null;
            await RefreshDatabaseStatusAsync(true);
        }
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

    [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern uint WinVerifyTrust(IntPtr window, ref Guid action, IntPtr data);
}
