using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Render.Kernel.TouchActions;

namespace Render.Platforms.Kernel
{
    public class TouchEffect : PlatformEffect
    {
        private FrameworkElement _frameworkElement;
        private Render.Kernel.TouchActions.TouchEffect _effect;
        private Action<Element, TouchActionEventArgs> _onTouchAction;

        protected override void OnAttached()
        {
            // Get the Windows FrameworkElement corresponding to the Element that the effect is attached to
            _frameworkElement = Control == null ? Container : Control;

            // Get access to the TouchEffect class in the .NET Standard library
            _effect = (Render.Kernel.TouchActions.TouchEffect)Element
                .Effects
                .FirstOrDefault(effect => effect is Render.Kernel.TouchActions.TouchEffect);

            if (_effect != null && _frameworkElement != null)
            {
                // Save the method to call on touch events
                _onTouchAction = _effect.OnTouchAction;

                // Set event handlers on FrameworkElement
                _frameworkElement.PointerEntered += OnPointerEntered;
                _frameworkElement.PointerPressed += OnPointerPressed;
                _frameworkElement.PointerMoved += OnPointerMoved;
                _frameworkElement.PointerReleased += OnPointerReleased;
                _frameworkElement.PointerExited += OnPointerExited;
                _frameworkElement.PointerCanceled += OnPointerCancelled;
            }
        }

        protected override void OnDetached()
        {
            if (_onTouchAction != null)
            {
                // Release event handlers on FrameworkElement
                _frameworkElement.PointerEntered -= OnPointerEntered;
                _frameworkElement.PointerPressed -= OnPointerPressed;
                _frameworkElement.PointerMoved -= OnPointerMoved;
                _frameworkElement.PointerReleased -= OnPointerReleased;
                _frameworkElement.PointerExited -= OnPointerEntered;
                _frameworkElement.PointerCanceled -= OnPointerCancelled;
                _onTouchAction = null;
            }
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs args)
        {
            CommonHandler(sender, TouchActionType.Entered, args);
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            CommonHandler(sender, TouchActionType.Pressed, args);

            // Check setting of Capture property
            if (_effect.Capture)
            {
                (sender as FrameworkElement).CapturePointer(args.Pointer);
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            CommonHandler(sender, TouchActionType.Moved, args);
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            CommonHandler(sender, TouchActionType.Released, args);
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs args)
        {
            CommonHandler(sender, TouchActionType.Exited, args);
        }

        private void OnPointerCancelled(object sender, PointerRoutedEventArgs args)
        {
            CommonHandler(sender, TouchActionType.Cancelled, args);
        }

        private void CommonHandler(object sender, TouchActionType touchActionType, PointerRoutedEventArgs args)
        {
            var pointerPoint = args.GetCurrentPoint(sender as UIElement);
            Windows.Foundation.Point windowsPoint = pointerPoint.Position;

            _onTouchAction?.Invoke(Element, new TouchActionEventArgs(
                id: args.Pointer.PointerId,
                type: touchActionType,
                location: new Point(windowsPoint.X, windowsPoint.Y),
                isInContact: args.Pointer.IsInContact));
        }
    }
}