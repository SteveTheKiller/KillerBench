using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace KillerBench.Controls
{
    internal static class DwmChrome
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;

        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;

        internal static void SetRoundedCorners(Window w, bool rounded)
        {
            try
            {
                var hwnd = new WindowInteropHelper(w).Handle;
                if (hwnd == IntPtr.Zero) return;
                int pref = rounded ? DWMWCP_ROUND : DWMWCP_DONOTROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch { /* pre-Win11: attribute unsupported */ }
        }

        internal static void SetThemeBorder(Window w)
        {
            try
            {
                var hwnd = new WindowInteropHelper(w).Handle;
                if (hwnd == IntPtr.Zero) return;
                if ((Application.Current.TryFindResource("AppBorderBrush")
                     ?? Application.Current.TryFindResource("PaneBorderBrush")) is SolidColorBrush b)
                {
                    int colorref = b.Color.R | (b.Color.G << 8) | (b.Color.B << 16);
                    DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref colorref, sizeof(int));
                }
            }
            catch { /* pre-Win11: attribute unsupported */ }
        }
    }
}
