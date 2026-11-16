using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace KillerBench.Controls
{
    /// <summary>Gives every TextBox, RichTextBox and PasswordBox in the app the family
    /// TextInputContextMenu (Controls.xaml) instead of the stock Windows Cut/Copy/Paste menu.
    /// Class handlers cover explicit styles and controls built in code. A box that sets its own
    /// ContextMenu keeps it.</summary>
    internal static class TextInputMenu
    {
        private static bool _registered;

        internal static void Register()
        {
            if (_registered) return;
            _registered = true;
            Register(typeof(TextBoxBase));
            Register(typeof(PasswordBox));
        }

        private static void Register(System.Type type)
        {
            EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent,
                new RoutedEventHandler(Assign), handledEventsToo: true);
            // WPF may omit Loaded broadcasts for a subtree with no instance lifecycle handlers.
            // Assign before mouse or keyboard context actions, before the stock editor menu opens.
            EventManager.RegisterClassHandler(type, UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(Assign), handledEventsToo: true);
            EventManager.RegisterClassHandler(type, Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler((sender, e) =>
                {
                    if (e.Key == Key.Apps || e.Key == Key.F10 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                        Assign(sender, e);
                }), handledEventsToo: true);
        }

        private static void Assign(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement box || box.ContextMenu != null) return;
            box.SetResourceReference(FrameworkElement.ContextMenuProperty, "TextInputContextMenu");
        }
    }
}
