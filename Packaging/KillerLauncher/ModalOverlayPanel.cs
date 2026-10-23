#nullable enable
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace KillerFamily.Accessibility
{
    /// <summary>Exposes an in-window modal overlay as a dialog to accessibility clients.</summary>
    public sealed class ModalOverlayPanel : Grid
    {
        public static readonly RoutedEvent DismissRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(DismissRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModalOverlayPanel));

        public event RoutedEventHandler DismissRequested
        {
            add => AddHandler(DismissRequestedEvent, value);
            remove => RemoveHandler(DismissRequestedEvent, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ModalOverlayPeer(this);

        private sealed class ModalOverlayPeer(ModalOverlayPanel owner) : FrameworkElementAutomationPeer(owner), IWindowProvider
        {
            protected override string GetClassNameCore() => "ModalOverlayDialog";
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;
            protected override bool IsControlElementCore() => true;
            protected override bool IsContentElementCore() => true;
            public override object? GetPattern(PatternInterface patternInterface) =>
                patternInterface == PatternInterface.Window ? this : base.GetPattern(patternInterface);

            bool IWindowProvider.Maximizable => false;
            bool IWindowProvider.Minimizable => false;
            bool IWindowProvider.IsModal => true;
            bool IWindowProvider.IsTopmost => true;
            WindowInteractionState IWindowProvider.InteractionState => WindowInteractionState.ReadyForUserInteraction;
            WindowVisualState IWindowProvider.VisualState => WindowVisualState.Normal;
            void IWindowProvider.Close() => owner.Dispatcher.BeginInvoke(new Action(() =>
                owner.RaiseEvent(new RoutedEventArgs(DismissRequestedEvent, owner))));
            void IWindowProvider.SetVisualState(WindowVisualState state)
            {
                if (state != WindowVisualState.Normal)
                    throw new InvalidOperationException("A modal overlay cannot be minimized or maximized.");
            }
            bool IWindowProvider.WaitForInputIdle(int milliseconds) => false;
        }
    }
}
