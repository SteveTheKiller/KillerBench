using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KillerBench.Controls;
using KillerBench.Features.About;
using KillerBench.Services;

internal static class AboutUpdateIntegrationChecks
{
    internal static int Run()
    {
        IsolatedDesktop.Verify();
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Strings/en-US.xaml" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        var owner = new Window { Width = 700, Height = 300, ShowInTaskbar = false };
        var signer = AboutController.ReadSigner;
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        AboutController.ReadSigner = () => (false, "fixture", "fixture");
        try
        {
            owner.Show();
            var host = new Host(owner);
            var operations = new Operations { DownloadFailure = new UpdateVerificationException("missing checksum") };
            var controller = new AboutController(host, operations);
            controller.Show();
            PumpUntil(() => host.UpdateVisible, "update offer");
            ConfirmUpdate(controller, owner);
            PumpUntil(() => operations.Downloads == 1 && host.UpdateEnabled, "refused update recovery");
            Require(host.Caption.Text == host.Loc("Str_About_UpdateRejected") && host.Caption.IsVisible &&
                owner.IsVisible && operations.Handoffs == 0,
                "A checksum rejection must be explained in the rendered About status, preserve the open app and restore retry without handoff.");
            foreach (string rejection in new[] { "checksum mismatch", "wrong signer", "untrusted signer" })
            {
                operations.DownloadFailure = new UpdateVerificationException(rejection);
                int before = operations.Downloads;
                ConfirmUpdate(controller, owner);
                PumpUntil(() => operations.Downloads == before + 1 && host.UpdateEnabled, rejection);
                Require(host.Caption.Text == host.Loc("Str_About_UpdateRejected") && owner.IsVisible && operations.Handoffs == 0,
                    "Every checksum or signature refusal must retain an explanation, the open app and retry controls without handoff: " + rejection);
            }
            operations.DownloadFailure = null;
            operations.HandoffFailure = new Win32Exception(5);
            ConfirmUpdate(controller, owner);
            PumpUntil(() => operations.Handoffs == 1 && host.UpdateEnabled, "failed handoff recovery");
            Require(host.Caption.Text == host.Loc("Str_About_UpdateHandoffFailed") && owner.IsVisible,
                "A denied handoff must be explained in About while the app remains open and Update is available for retry.");
            operations.HandoffFailure = new Win32Exception(1223);
            ConfirmUpdate(controller, owner);
            PumpUntil(() => operations.Handoffs == 2 && host.UpdateEnabled, "canceled elevation recovery");
            Require(host.Caption.Text == host.Loc("Str_About_UpdateElevationCanceled") && owner.IsVisible,
                "Canceled elevation must be distinguished from update rejection or a denied helper launch.");
            foreach (string locale in new[] { "de-DE", "fr-FR" })
            {
                var dictionary = new ResourceDictionary { Source = new Uri("/KillerBench;component/Strings/" + locale + ".xaml", UriKind.Relative) };
                app.Resources.MergedDictionaries.Add(dictionary);
                operations.DownloadFailure = new UpdateVerificationException("checksum mismatch");
                int before = operations.Downloads;
                ConfirmUpdate(controller, owner);
                PumpUntil(() => operations.Downloads == before + 1 && host.UpdateEnabled, locale + " refusal");
                Require(host.Caption.Text == dictionary["Str_About_UpdateRejected"] as string && owner.IsVisible,
                    "The rendered failure must use the active " + locale + " resource.");
                app.Resources.MergedDictionaries.Remove(dictionary);
            }
            operations.DownloadFailure = null;
            operations.HandoffFailure = null;
            ConfirmUpdate(controller, owner);
            PumpUntil(() => !owner.IsVisible, "successful update shutdown");
            controller.Update();
            Require(operations.Handoffs == 3 && !host.UpdateEnabled,
                "A successful verified handoff must close the app once and keep repeated update actions disabled.");
            Console.WriteLine("PASS: 9 real About update cases (4 verification refusals, denied launch, canceled elevation, 2 translated notices and one successful handoff).");
            return 0;
        }
        finally
        {
            AboutController.ReadSigner = signer;
            SynchronizationContext.SetSynchronizationContext(context);
            owner.Close();
            app.Shutdown();
        }
    }

    private static void ConfirmUpdate(AboutController controller, Window owner)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        var elapsed = Stopwatch.StartNew();
        bool confirmed = false;
        timer.Tick += (_, _) =>
        {
            var dialog = owner.OwnedWindows.OfType<ConfirmDialog>().SingleOrDefault();
            if (dialog?.FindName("OkButton") is Button { IsVisible: true, IsEnabled: true } button)
            {
                confirmed = true;
                timer.Stop();
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else if (elapsed.Elapsed > TimeSpan.FromSeconds(5))
            {
                timer.Stop();
                dialog?.Close();
            }
        };
        timer.Start();
        try { controller.Update(); }
        finally { timer.Stop(); }
        Require(confirmed, "The real update confirmation must become ready within five seconds.");
    }

    private static void PumpUntil(Func<bool> condition, string stage)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException(stage);
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            Thread.Sleep(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class Operations : IAboutUpdateOperations
    {
        internal Exception? DownloadFailure;
        internal Exception? HandoffFailure;
        internal int Downloads, Handoffs;
        public Task<string?> CheckAsync(Version? current) => Task.FromResult<string?>("v9.9.9");
        public Task<string> DownloadAsync(string tag)
        {
            Downloads++;
            return DownloadFailure is null ? Task.FromResult("verified-fixture.exe") : Task.FromException<string>(DownloadFailure);
        }
        public void StartSwap(string path, string version)
        {
            Interlocked.Increment(ref Handoffs);
            if (HandoffFailure is not null) throw HandoffFailure;
        }
    }

    private sealed class Host : IAboutHost
    {
        internal readonly TextBlock Caption = new() { TextWrapping = TextWrapping.Wrap };
        private readonly Button retry = new() { Content = "Update" };
        internal Host(Window owner)
        {
            Window = owner;
            var body = new StackPanel { Margin = new Thickness(20) };
            body.Children.Add(Caption);
            body.Children.Add(retry);
            owner.Content = body;
        }
        public Window Window { get; }
        public string Loc(string key) => LocaleManager.Loc(key);
        public void SetStatus(string text) { }
        public string Version { private get; set; } = "";
        public string ReleaseDate { private get; set; } = "";
        public string Publisher { private get; set; } = "";
        public string Alias { private get; set; } = "";
        public bool AliasVisible { private get; set; }
        public string Thumbprint { private get; set; } = "";
        public string Sha256 { private get; set; } = "";
        public string UpdateText { set => Caption.Text = value; }
        public bool UpdateVisible { get; set; }
        public bool UpdateEnabled { get => retry.IsEnabled; set => retry.IsEnabled = value; }
        public bool StartupUpdateCheck { private get; set; }
        public void ShowCard() => Window.UpdateLayout();
    }
}
