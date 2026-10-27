using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace KillerLauncher
{
    public partial class InstallerWizard : Window
    {
        private int _page;
        private bool _installed;
        private bool _installing;
        private int _exitCode = KillerFamily.Deployment.InstallerExitCodes.Cancelled;
        private bool _closeAfterNotice;
        private string _installedDirectory = string.Empty;
        private readonly Program.InstalledCopy? _existing;

        private InstallerWizard()
        {
            InitializeComponent();
            new KillerFamily.Accessibility.OverlayFocus(this).Track(NoticeOverlay);
            Closing += (_, e) =>
            {
                if (_installing) e.Cancel = true;
                else if (NoticeOpen && !_closeAfterNotice) { e.Cancel = true; DismissNotice(); }
            };
            PreviewKeyDown += (_, e) =>
            {
                if (NoticeOpen && e.Key == Key.Escape) { e.Handled = true; NoticeOk_Click(this, new RoutedEventArgs()); }
            };
            TaskbarIdentity.Track(this);
            SetupVersionLabel.Text = typeof(InstallerWizard).Assembly.GetName().Version!.ToString(3);
            _existing = Program.FindInstalledCopy();
            InstallFolder.Text = _existing?.Directory ?? Program.DefaultInstallDirectory(false);
            if (_existing != null)
            {
                CurrentUser.IsChecked = !_existing.Machine;
                AllUsers.IsChecked = _existing.Machine;
                DesktopShortcut.IsChecked = _existing.DesktopShortcut;
            }
            ImageBrush grain = CreateGrain();
            GrainLayer.Background = grain;
            SidebarGrain.Background = grain;
            FrameGrain.Background = grain;
            RenderPage();
        }

        internal static int Run(string[] args)
        {
            var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var wizard = new InstallerWizard();
            wizard.ShowDialog();
            application.Shutdown();
            return wizard.ExitCode;
        }

        internal int ExitCode => _exitCode;
        private bool NoticeOpen => NoticeOverlay?.Visibility == Visibility.Visible;

        internal static int ShowFailure(string message, int exitCode = KillerFamily.Deployment.InstallerExitCodes.Failure)
        {
            var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var wizard = new InstallerWizard { _closeAfterNotice = true };
            wizard.Loaded += (_, _) => wizard.ShowNotice(
                LauncherStrings.Get("CannotContinue"), message, NoticeKind.Error);
            wizard.ShowDialog();
            application.Shutdown();
            return exitCode;
        }

        private void RenderPage()
        {
            if (_existing != null && !_installed)
            {
                Options.Visibility = Visibility.Collapsed;
                RuntimeStatus.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Visible;
                UninstallButton.Visibility = Visibility.Visible;
                SetRuntimeStatus();
                bool current = string.Equals(_existing.Version, Program.SetupVersion, StringComparison.Ordinal);
                Heading.Text = current
                    ? LauncherStrings.Get("AlreadyInstalledHeading")
                    : LauncherStrings.Get("UpgradeReadyHeading");
                Copy.Text = LauncherStrings.Format("MaintenanceCopy", _existing.Version);
                NextButton.Content = current
                    ? LauncherStrings.Get("Reinstall")
                    : LauncherStrings.Get("Upgrade");
                return;
            }

            bool options = _page == 1;
            Options.Visibility = options ? Visibility.Visible : Visibility.Collapsed;
            RuntimeStatus.Visibility = options ? Visibility.Visible : Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            UninstallButton.Visibility = Visibility.Collapsed;
            BackButton.IsEnabled = _page > 0 && !_installed;
            CancelButton.Visibility = _installed ? Visibility.Collapsed : Visibility.Visible;
            if (_page == 0)
            {
                Heading.Text = LauncherStrings.Get("Welcome");
                Copy.Text = LauncherStrings.Get("WelcomeCopy");
                NextButton.Content = LauncherStrings.Get("Next");
            }
            else if (options)
            {
                Heading.Text = LauncherStrings.Get("OptionsHeading");
                Copy.Text = LauncherStrings.Get("OptionsCopy");
                SetRuntimeStatus();
                NextButton.Content = LauncherStrings.Get("Next");
            }
            else
            {
                Heading.Text = _installed ? LauncherStrings.Get("CompleteHeading") : LauncherStrings.Get("ReadyHeading");
                Copy.Text = _installed ? LauncherStrings.Get("CompleteCopy") :
                    (AllUsers.IsChecked == true ? LauncherStrings.Get("AllUsers") : LauncherStrings.Get("CurrentUser")) +
                    (DesktopShortcut.IsChecked == true ? "  •  " + LauncherStrings.Get("WithShortcut") : "  •  " + LauncherStrings.Get("WithoutShortcut"));
                SetRuntimeStatus();
                NextButton.Content = _installed ? LauncherStrings.Get("Launch") : LauncherStrings.Get("Install");
            }
        }

        private void SetRuntimeStatus()
        {
            if (Program.IsStorePackage) { RuntimeStatus.Visibility = Visibility.Collapsed; return; }
            bool ready = Program.HasDesktopRuntime10();
            RuntimeStatus.Text = ready ? LauncherStrings.Get("RuntimeDetected") : LauncherStrings.Get("RuntimeRequired");
            RuntimeStatus.Foreground = new SolidColorBrush(ready
                ? Color.FromRgb(234, 217, 0) : Color.FromRgb(255, 190, 80));
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_installing || NoticeOpen) return;
            if (_installed)
            {
                Process.Start(new ProcessStartInfo(Program.InstalledExecutable(_installedDirectory)) { UseShellExecute = true });
                DialogResult = true;
                return;
            }
            if (_existing == null && _page < 2) { _page++; RenderPage(); return; }
            string installDirectory;
            bool machine = _existing?.Machine ?? AllUsers.IsChecked == true;
            try { installDirectory = Program.PrepareInstallDirectory(InstallFolder.Text, machine); }
            catch (Exception ex)
            {
                _page = 1;
                RenderPage();
                InstallFolder.Focus();
                InstallFolder.SelectAll();
                ShowNotice(LauncherStrings.Get("CheckOptions"), ex.Message, NoticeKind.Warning);
                return;
            }
            if (!Program.RuntimeReady())
            {
                Process.Start(new ProcessStartInfo("https://dotnet.microsoft.com/en-us/download/dotnet/10.0") { UseShellExecute = true });
                ShowNotice(LauncherStrings.Get("RuntimeRequired"),
                    LauncherStrings.Get("InstallRuntime"), NoticeKind.Information);
                return;
            }
            try
            {
                SetInstalling(true);
                Heading.Text = LauncherStrings.Get("Installing");
                Copy.Text = LauncherStrings.Get("InstallingCopy");
                RuntimeStatus.Visibility = Visibility.Collapsed;
                InstallProgress.Visibility = Visibility.Visible;
                bool desktop = _existing?.DesktopShortcut ?? DesktopShortcut.IsChecked == true;
                Task<int> install = Task.Run(() => machine ? InstallForEveryone(desktop, installDirectory)
                    : Program.Install(false, desktop, installDirectory));
                await Task.WhenAll(install, Task.Delay(2000));
                int result = install.Result;
                _exitCode = result;
                if (result != 0)
                {
                    InstallProgress.Visibility = Visibility.Collapsed;
                    RenderPage();
                    // The elevated setup returns only an exit code, so look for the folders it kept for recovery.
                    string detail = LauncherStrings.Format("SetupReturned", result);
                    var retained = KillerBench.Services.InstallationOwnership.FindRetainedDirectories(installDirectory);
                    if (retained.Count > 0)
                        detail += Environment.NewLine + Environment.NewLine +
                            LauncherStrings.Format("RetainedDirectories", string.Join(Environment.NewLine, retained));
                    ShowNotice(LauncherStrings.Get("InstallFailed"), detail, NoticeKind.Error);
                    return;
                }
                _installedDirectory = installDirectory;
                _installed = true;
                InstallProgress.Visibility = Visibility.Collapsed;
                RenderPage();
            }
            catch (Exception ex)
            {
                _exitCode = KillerFamily.Deployment.InstallerExitCodes.FromException(ex);
                InstallProgress.Visibility = Visibility.Collapsed;
                RenderPage();
                ShowNotice(LauncherStrings.Get("InstallFailed"), ex.Message, NoticeKind.Error);
            }
            finally { SetInstalling(false); }
        }

        private void SetInstalling(bool value)
        {
            _installing = value;
            NextButton.IsEnabled = CancelButton.IsEnabled = CloseButton.IsEnabled = UninstallButton.IsEnabled = Options.IsEnabled = !value;
            BackButton.IsEnabled = !value && _page > 0 && !_installed;
        }

        private enum NoticeKind { Information, Warning, Error }

        private void ShowNotice(string heading, string message, NoticeKind kind)
        {
            NoticeHeading.Text = heading;
            NoticeMessage.Text = message;
            NoticeGlyph.Text = kind == NoticeKind.Error ? "×" : kind == NoticeKind.Warning ? "!" : "i";
            NoticeGlyph.Foreground = new SolidColorBrush(kind == NoticeKind.Error
                ? Color.FromRgb(227, 93, 106)
                : kind == NoticeKind.Warning ? Color.FromRgb(255, 190, 80) : Color.FromRgb(234, 217, 0));
            NoticeRing.BorderBrush = NoticeGlyph.Foreground;
            NoticeOverlay.Visibility = Visibility.Visible;
            ContentCard.IsEnabled = SidebarCard.IsEnabled = false;
            NoticeOk.Focus();
        }

        private void NoticeOk_Click(object sender, RoutedEventArgs e)
        {
            if (_closeAfterNotice) { DialogResult = false; return; }
            DismissNotice();
        }

        private void DismissNotice()
        {
            NoticeOverlay.Visibility = Visibility.Collapsed;
            ContentCard.IsEnabled = SidebarCard.IsEnabled = true;
        }

        private static int InstallForEveryone(bool desktop, string installDirectory)
        {
            string arguments = "/silent " + (desktop ? "/desktop " : string.Empty) +
                Program.EncodeInstallDirectoryArgument(installDirectory);
            var start = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,
                arguments) { UseShellExecute = true, Verb = "runas" };
            using (Process elevated = Process.Start(start))
            {
                if (elevated == null) return 1;
                elevated.WaitForExit();
                return elevated.ExitCode;
            }
        }

        private void Scope_Checked(object sender, RoutedEventArgs e)
        {
            if (_installing || NoticeOpen) return;
            if (InstallFolder == null) return;
            string user = Program.DefaultInstallDirectory(false);
            string machine = Program.DefaultInstallDirectory(true);
            if (string.IsNullOrWhiteSpace(InstallFolder.Text) ||
                string.Equals(InstallFolder.Text, user, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(InstallFolder.Text, machine, StringComparison.OrdinalIgnoreCase))
                InstallFolder.Text = Program.DefaultInstallDirectory(AllUsers.IsChecked == true);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            if (_installing || NoticeOpen) return;
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = LauncherStrings.Get("ChooseFolder"),
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(InstallFolder.Text) ? InstallFolder.Text :
                    (Directory.Exists(Path.GetDirectoryName(InstallFolder.Text)) ? Path.GetDirectoryName(InstallFolder.Text) : string.Empty)
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                InstallFolder.Text = dialog.SelectedPath;
        }

        private void Back_Click(object sender, RoutedEventArgs e) { if (!_installing && !NoticeOpen && _page > 0) { _page--; RenderPage(); } }
        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            if (_installing || NoticeOpen || _existing == null) return;
            Process.Start(new ProcessStartInfo(_existing.Executable, "/uninstall") { UseShellExecute = true });
            _exitCode = KillerFamily.Deployment.InstallerExitCodes.Success;
            DialogResult = true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) { if (!_installing && !NoticeOpen) DialogResult = false; }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Website_Click(object sender, RoutedEventArgs e)
        {
            if (!_installing && !NoticeOpen) Process.Start(new ProcessStartInfo("https://thekiller.net") { UseShellExecute = true });
        }

        internal static ImageBrush CreateGrain()
        {
            // The family film grain from KillerUI GrainTexture: about a third of the pixels carry a speck,
            // half bright and half dark, so the surfaces keep their theme color instead of lifting toward gray.
            const int size = 256;
            var pixels = new byte[size * size * 4];
            var random = new Random(1337);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (random.Next(3) != 0) continue;
                bool bright = random.Next(2) == 0;
                byte value = bright ? (byte)random.Next(190, 255) : (byte)random.Next(0, 50);
                pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
                pixels[i + 3] = (byte)random.Next(35, 95);
            }
            var bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4);
            bitmap.Freeze();
            return new ImageBrush(bitmap) { TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, size, size), Stretch = Stretch.None };
        }
    }
}
