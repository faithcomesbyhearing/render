using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Render.Platforms.Kernel
{
    public class PropagateScrollEffect : PlatformEffect
    {
        private readonly PointerEventHandler _pointerEventHandler;

        public PropagateScrollEffect()
        {
            _pointerEventHandler = OnPointerWheelChanged;
        }

        protected override void OnAttached()
        {
            Control.AddHandler(UIElement.PointerWheelChangedEvent, _pointerEventHandler, true);
        }

        protected override void OnDetached()
        {
            Control.RemoveHandler(UIElement.PointerWheelChangedEvent, _pointerEventHandler);
        }

        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
        }
    }
}