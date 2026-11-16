using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

internal static class TextMenuChecks
{
    internal static void Run(Window owner)
    {
        var explicitStyle = new TextBox { Style = (Style)owner.FindResource("DarkTextBox"), Text = "Fixture" };
        var rich = new RichTextBox();
        var password = new PasswordBox();
        var customMenu = new ContextMenu();
        var custom = new TextBox { ContextMenu = customMenu };
        var content = new StackPanel();
        foreach (var box in new Control[] { explicitStyle, rich, password, custom }) content.Children.Add(box);
        var dialog = new Window { Owner = owner, Content = content, Width = 300, Height = 200, ShowInTaskbar = false };
        try
        {
            dialog.Show();
            dialog.UpdateLayout();
            dialog.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            foreach (var box in new Control[] { explicitStyle, rich, password })
            {
                box.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
                    { RoutedEvent = UIElement.PreviewMouseRightButtonDownEvent });
                if (box.ContextMenu is not { } menu || !menu.Items.OfType<MenuItem>().Any(item =>
                    ReferenceEquals(item.Command, ApplicationCommands.Copy)))
                    throw new InvalidOperationException(box.GetType().Name + " must expose the family menu before its right-click context action.");
            }
            var keyboardBox = new RichTextBox();
            content.Children.Add(keyboardBox);
            dialog.UpdateLayout();
            keyboardBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(keyboardBox),
                Environment.TickCount, Key.Apps) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            if (keyboardBox.ContextMenu is null)
                throw new InvalidOperationException("The keyboard context action must expose the same family menu.");
            if (!ReferenceEquals(custom.ContextMenu, customMenu))
                throw new InvalidOperationException("An explicitly assigned text menu must remain unchanged.");
            Console.WriteLine("PASS: family text menus cover separately styled dialog text, rich text and passwords while preserving explicit menus.");
        }
        finally { dialog.Close(); }
    }
}
