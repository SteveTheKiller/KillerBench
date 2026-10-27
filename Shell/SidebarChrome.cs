using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KillerBench.Shell
{
    /// <summary>Shared geometry and behavior for the permanent 24px rail and sidebar chrome.</summary>
    internal static class SidebarChrome
    {
        internal const double RailWidth = 24;
        internal const double RailButtonSize = 20;
        internal const double ResizeGripWidth = 5;

        /// <summary>
        /// Keeps both edge fades synchronized with a scrolling list. Opacity ramps over each
        /// fade's height, so neither edge pops on after the first wheel tick. Call once after
        /// InitializeComponent; the handler also covers extent and viewport changes.
        /// </summary>
        internal static void WireEdgeFades(ItemsControl list, Border top, Border bottom)
        {
            void Sync()
            {
                var sv = FindDescendant<ScrollViewer>(list);
                if (sv == null) return;
                top.Opacity = Ramp(sv.VerticalOffset, top.Height, 28);
                bottom.Opacity = Ramp(sv.ScrollableHeight - sv.VerticalOffset, bottom.Height, 28);
            }

            list.AddHandler(ScrollViewer.ScrollChangedEvent,
                new ScrollChangedEventHandler((_, __) => Sync()));
            list.Loaded += (_, __) => Sync();
            list.SizeChanged += (_, __) => Sync();
        }

        private static double Ramp(double distance, double height, double fallback)
        {
            double h = double.IsNaN(height) || height <= 0 ? fallback : height;
            return Math.Min(1, Math.Max(0, distance) / h);
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T hit) return hit;
                var deeper = FindDescendant<T>(child);
                if (deeper != null) return deeper;
            }
            return null;
        }
    }
}
