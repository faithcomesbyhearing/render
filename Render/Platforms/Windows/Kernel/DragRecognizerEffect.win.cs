using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Render.Kernel.DragAndDrop;

namespace Render.Platforms.Kernel
{
    public class DragRecognizerEffect : PlatformEffect
    {
        private FrameworkElement _frameworkElement;
        private Render.Kernel.DragAndDrop.DragRecognizerEffect _effect;
        private DragAndDropEventArgs _dragAndDropRecognizerEffectEventArgs;

        protected override void OnAttached()
        {
            _frameworkElement = Control ?? Container;
            _effect = (Render.Kernel.DragAndDrop.DragRecognizerEffect)Element
                .Effects
                .FirstOrDefault(effect => effect is Render.Kernel.DragAndDrop.DragRecognizerEffect);

            if (_effect != null && _frameworkElement != null)
            {
                _frameworkElement.PointerPressed += OnPointerPressed;
            }
        }

        protected override void OnDetached()
        {
            _frameworkElement.PointerPressed -= OnPointerPressed;
            _dragAndDropRecognizerEffectEventArgs = null;
        }

        void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            var senderFrameworkElement = (FrameworkElement)sender;
            senderFrameworkElement.DragStarting += DragStarting;
            senderFrameworkElement.DropCompleted += DropCompleted;
            
            _ = senderFrameworkElement.StartDragAsync(args.GetCurrentPoint(senderFrameworkElement));
        }

        private void DropCompleted(UIElement sender, Microsoft.UI.Xaml.DropCompletedEventArgs args)
        {
            var senderFrameworkElement = (FrameworkElement)sender;
            senderFrameworkElement.DragStarting -= DragStarting;
            senderFrameworkElement.DropCompleted -= DropCompleted;
            
            if (_dragAndDropRecognizerEffectEventArgs is null)
            {
                return;
            }

            _dragAndDropRecognizerEffectEventArgs.IsDragEnded = true;
            _effect.SendDragEnded(Element, _dragAndDropRecognizerEffectEventArgs);
            _dragAndDropRecognizerEffectEventArgs = null;
        }

        private void DragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
        {
            _dragAndDropRecognizerEffectEventArgs = new DragAndDropEventArgs();
            _effect.SendDragStarting(Element, _dragAndDropRecognizerEffectEventArgs);

            var data = _dragAndDropRecognizerEffectEventArgs.Data;
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

            if (args.Data.Properties["Data"] is not null)
            {
                args.Data.Properties.Remove("Data");
            }
            args.Data.Properties.Add("Data", data);
        }
    }
}