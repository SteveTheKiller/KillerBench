using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace KillerBench.Controls;

public partial class ConfirmDialog : Window
{
    public bool Confirmed { get; private set; }

    /// <summary>The optional checkbox, read back after ShowDialog returns.</summary>
    public bool Check1Checked => Check1.IsChecked == true;

    public ConfirmDialog(string heading, string detail, string confirmText, string? cancelText = null)
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyNotice();
        HeadingText.Text = heading;
        DetailText.Text = detail;
        if (string.IsNullOrEmpty(detail)) DetailText.Visibility = Visibility.Collapsed;
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

    // With one checkbox under the detail, such as the update prompt's "Always check on startup".
    public ConfirmDialog(string heading, string detail, string confirmText, string? cancelText,
                         string check1Label, bool check1Initial)
        : this(heading, detail, confirmText, cancelText)
    {
        Check1.Content = new System.Windows.Controls.TextBlock { Text = check1Label, TextWrapping = TextWrapping.Wrap };
        Check1.IsChecked = check1Initial;
        Check1.Visibility = Visibility.Visible;
    }

    // The family setup dialog's notice ring: an amber "!" on a confirmation and the accent "i" on a
    // notice with a single button. The amber darkens on a light card so it keeps its contrast.
    private void ApplyNotice()
    {
        FitHeading();
        bool confirmation = CancelButton.Visibility == Visibility.Visible;
        System.Windows.Media.Brush brush;
        if (confirmation)
        {
            bool light = TryFindResource("BackgroundBrush") is System.Windows.Media.SolidColorBrush card && card.Color.R * 0.299 + card.Color.G * 0.587 + card.Color.B * 0.114 > 140;
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

    // A heading is one short line. Text after its first line break moves into the detail, and
    // a first line too long for a heading stays body-sized instead of shouting.
    private void FitHeading()
    {
        string text = HeadingText.Text ?? "";
        int lineBreak = text.IndexOf('\n');
        if (lineBreak >= 0)
        {
            string rest = text[(lineBreak + 1)..].Trim();
            HeadingText.Text = text[..lineBreak].TrimEnd('\r');
            if (rest.Length > 0)
            {
                string existing = DetailText.Visibility == Visibility.Visible ? DetailText.Text : "";
                DetailText.Text = string.IsNullOrEmpty(existing) ? rest : rest + "\n\n" + existing;
                DetailText.Visibility = Visibility.Visible;
            }
        }
        if ((HeadingText.Text ?? "").Length > 90)
        {
            HeadingText.ClearValue(System.Windows.Controls.TextBlock.FontFamilyProperty);
            HeadingText.ClearValue(System.Windows.Media.TextOptions.TextFormattingModeProperty);
            HeadingText.FontSize = 13;
        }
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
