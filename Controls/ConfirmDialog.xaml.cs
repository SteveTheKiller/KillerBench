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
        Loaded += (_, _) => ApplyNotice();
        HeadingText.Text = heading;
        DetailText.Text = detail;
        OkButton.Content = confirmText;
        CancelButton.Content = cancelText ??
            (Application.Current.TryFindResource("Str_Btn_Cancel") as string ?? "Cancel");

        var titleFrames = BitmapDecoder.Create(
            new Uri("pack://application:,,,/KillerBench;component/Assets/AppIcon.ico"),
            BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames;
        TitleIcon.Source = titleFrames[0];
        Loaded += (_, _) =>
        {
            CancelButton.Focus();
            RootBorder.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        };
    }

    // The family setup dialog's notice ring: an amber "!" on a confirmation and the accent "i" on a
    // notice with a single button. The amber darkens on a light card so it keeps its contrast.
    private void ApplyNotice()
    {
        bool confirmation = CancelButton.Visibility == Visibility.Visible;
        System.Windows.Media.Brush brush;
        if (confirmation)
        {
            var card = TryFindResource("BackgroundBrush") as System.Windows.Media.SolidColorBrush;
            bool light = card != null && card.Color.R * 0.299 + card.Color.G * 0.587 + card.Color.B * 0.114 > 140;
            brush = new System.Windows.Media.SolidColorBrush(light
                ? System.Windows.Media.Color.FromRgb(176, 112, 0)
                : System.Windows.Media.Color.FromRgb(255, 190, 80));
        }
        else
        {
            brush = TryFindResource("PrimaryBrush") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray;
        }
        NoticeGlyph.Text = confirmation ? "!" : "i";
        NoticeGlyph.Foreground = brush;
        NoticeRing.BorderBrush = brush;
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
