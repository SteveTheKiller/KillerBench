using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Threading;

internal static class InstallerWizardStateProbe
{
    [STAThread]
    private static int Main(string[] args)
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
        var wizard = KillerLauncher.InstallerWizard.CreateFixture();
        bool closed = false;
        wizard.Closed += (_, _) => closed = true;
        try
        {
            if (args.Length > 0 && args[0] == "--notice")
            {
                KillerLauncher.Program.FailPreparation = true;
                Button show = (Button)wizard.FindName("NextButton");
                show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var notice = (UIElement)wizard.FindName("NoticeOverlay");
                Require(notice.Visibility == Visibility.Visible, "Invalid options must show the real notice.");
                Require(KeyboardNavigation.GetTabNavigation(notice) == KeyboardNavigationMode.Cycle &&
                    !((Button)wizard.FindName("NextButton")).IsEnabled,
                    "S011: a visible notice must contain keyboard traversal and disable covered actions.");
                var peer = UIElementAutomationPeer.CreatePeerForElement(notice);
                Require(peer != null && peer.GetAutomationControlType() == AutomationControlType.Window &&
                    peer.GetPattern(PatternInterface.Window) is IWindowProvider provider && provider.IsModal,
                    "S011: the real notice must expose a modal Window automation peer.");
                PumpUntil(() => peer!.GetName() == ((TextBlock)wizard.FindName("NoticeHeading")).Text &&
                    peer.GetHelpText() == ((TextBlock)wizard.FindName("NoticeMessage")).Text);
                ((IWindowProvider)peer!.GetPattern(PatternInterface.Window)).Close();
                PumpUntil(() => notice.Visibility == Visibility.Collapsed);
                Require(notice.Visibility == Visibility.Collapsed && ((Button)wizard.FindName("NextButton")).IsEnabled,
                    "S011: dismissing a notice must restore covered actions.");
                Require(new WindowInteropHelper(wizard).Handle == IntPtr.Zero, "Notice fixture must not create a native window.");
                Console.WriteLine("PASS S011: real notice contains traversal, exposes modal Window UIA with heading/message and dismissal, and disables/restores covered actions without a native window.");
                return 0;
            }
            Button next = (Button)wizard.FindName("NextButton");
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            PumpUntil(() => KillerLauncher.Program.InstallEntered.Task.IsCompleted);
            wizard.Close();
            Require(!closed, "S001: system Close terminated the wizard while its installation transaction was pending.");
            foreach (string name in new[] { "CloseButton", "CancelButton", "BackButton", "NextButton", "UninstallButton" })
            {
                var button = (Button)wizard.FindName(name);
                Require(!button.IsEnabled, "S001: pending installation must disable action: " + name);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(!closed, "S001: a disabled action bypassed the active-transaction guard: " + name);
            }
            Require(!((UIElement)wizard.FindName("Options")).IsEnabled && KillerLauncher.Program.InstallCalls == 1,
                "S001: browse/options and repeated install must stay blocked during a transaction.");
#if BENCH_WIZARD
            int result = args.Length == 2 && args[0] == "--result" ? int.Parse(args[1]) : 0;
            KillerLauncher.Program.InstallCompletion.TrySetResult(result);
            PumpUntil(() => ((UIElement)wizard.FindName("InstallProgress")).Visibility == Visibility.Collapsed);
            Require(wizard.ExitCode == result, "S001: wizard must retain the actual terminal installer exit code: " + result);
#endif
            Require(!wizard.IsVisible && new WindowInteropHelper(wizard).Handle == IntPtr.Zero,
                "The installer fixture must never show a window or create its native handle.");
            Console.WriteLine("PASS S001: system Close cannot end a pending installer transaction; fixture performed no installation or display startup.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            KillerLauncher.Program.InstallCompletion.TrySetResult(0);
            PumpUntil(() => ((UIElement)wizard.FindName("InstallProgress")).Visibility == Visibility.Collapsed || closed);
            if (((UIElement)wizard.FindName("NoticeOverlay")).Visibility == Visibility.Visible)
                ((Button)wizard.FindName("NoticeOk")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (!closed) wizard.Close();
            application.Shutdown();
        }
    }

    private static void PumpUntil(Func<bool> completed)
    {
        var frame = new DispatcherFrame();
        var elapsed = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) => { if (completed() || elapsed.Elapsed > TimeSpan.FromSeconds(10)) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        Require(completed(), "Installer fixture did not reach its bounded terminal state.");
    }

    private static void Require(bool condition, string detail)
    { if (!condition) throw new InvalidOperationException(detail); }
}

namespace KillerLauncher
{
    public partial class InstallerWizard
    {
        internal static InstallerWizard CreateFixture() => new InstallerWizard();
    }

    internal static class TaskbarIdentity
    {
        internal static void Track(Window window) { }
    }

    // Replaces only the operating-system deployment boundary in the linked production wizard.
    internal static class Program
    {
        internal static readonly TaskCompletionSource<bool> InstallEntered = new TaskCompletionSource<bool>();
        internal static readonly TaskCompletionSource<int> InstallCompletion = new TaskCompletionSource<int>();
        internal static bool FailPreparation;
        internal static int InstallCalls;
        internal sealed class InstalledCopy
        {
            internal string Directory => "C:\\Fixture\\KillerBench";
            internal string Executable => "C:\\Fixture\\KillerBench\\KillerBench.exe";
            internal string Version => "1.0.0";
            internal bool Machine => false;
            internal bool DesktopShortcut => false;
            internal bool PreviewHandler => false;
        }
        internal static InstalledCopy? FindInstalledCopy() => null;
        internal static string DefaultInstallDirectory(bool machine) => "C:\\Fixture\\KillerBench";
        internal static string SetupVersion => "1.0.0";
        internal static bool IsStorePackage => false;
        internal static bool HasDesktopRuntime10() => true;
        internal static bool RuntimeReady() => true;
        internal static string PrepareInstallDirectory(string path, bool machine)
        {
            if (FailPreparation) throw new InvalidOperationException("Fixture validation failure.");
            return path;
        }
        internal static string ValidateInstallDestination(string path) => PrepareInstallDirectory(path, false);
        internal static string ValidateInstallDirectory(string path) => PrepareInstallDirectory(path, false);
        internal static bool DesktopShortcutOpensInstalledCopy(bool machine, string path) => true;
        internal static string InstalledExecutable(string directory) => "C:\\Fixture\\KillerBench\\KillerBench.exe";
        internal static string EncodeInstallDirectoryArgument(string directory) => "/fixture";
        internal static int Install(bool machine, bool desktop, string directory)
        {
            Interlocked.Increment(ref InstallCalls);
            InstallEntered.TrySetResult(true);
            return InstallCompletion.Task.GetAwaiter().GetResult();
        }
        internal static int Install(bool machine, bool desktop, string directory, bool preview) => Install(machine, desktop, directory);
    }
}
