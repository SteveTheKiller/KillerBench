using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace KillerBench.Controls;

public partial class ConfirmDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmDialog(string heading, string detail, string confirmText, string? cancelText = null)
    {
        InitializeComponent();
        HeadingText.Text = heading;
        DetailText.Text = detail;
        OkButton.Content = confirmText;
        CancelButton.Content = cancelText ??
            (Application.Current.TryFindResource("Str_Btn_Cancel") as string ?? "Cancel");

        var titleFrames = BitmapDecoder.Create(
            new Uri("pack://application:,,,/KillerBench;component/Assets/AppIcon.ico"),
            BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames;
        void RefreshTitleIcon() => TitleIcon.Source = titleFrames
            .OrderBy(frame => Math.Abs(frame.PixelWidth - TitleIcon.ActualWidth * VisualTreeHelper.GetDpi(TitleIcon).DpiScaleX))
            .First();
        TitleIcon.Loaded += (_, _) => RefreshTitleIcon();
        TitleIcon.SizeChanged += (_, _) => RefreshTitleIcon();
        Loaded += (_, _) =>
        {
            CancelButton.Focus();
            RootBorder.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        };
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
}
