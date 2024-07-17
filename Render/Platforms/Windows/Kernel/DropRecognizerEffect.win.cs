using Render.Kernel.DragAndDrop;
using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml;
using DragEventArgs = Microsoft.UI.Xaml.DragEventArgs;

namespace Render.Platforms.Kernel
{
    public class DropRecognizerEffect : PlatformEffect
    {
        private FrameworkElement _frameworkElement;
        private Render.Kernel.DragAndDrop.DropRecognizerEffect _effect;

        protected override void OnAttached()
        {
            _frameworkElement = Control ?? Container;
            _effect = (Render.Kernel.DragAndDrop.DropRecognizerEffect)Element
                .Effects
                .FirstOrDefault(effect => effect is Render.Kernel.DragAndDrop.DropRecognizerEffect);

            if (_effect is not null && _frameworkElement is not null)
            {
                _frameworkElement.AllowDrop = true;
                _frameworkElement.Drop += FrameworkElement_Drop;
                _frameworkElement.DragEnter += FrameworkElement_DragEnter;
                _frameworkElement.DragLeave += FrameworkElement_DragLeave;
            }
        }

        protected override void OnDetached()
        {
            _frameworkElement.Drop -= FrameworkElement_Drop;
            _frameworkElement.DragEnter -= FrameworkElement_DragEnter;
            _frameworkElement.DragLeave -= FrameworkElement_DragLeave;
        }

        private void FrameworkElement_DragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

            if (!string.IsNullOrEmpty(_effect?.DropText))
            {
                e.DragUIOverride.Caption = _effect?.DropText;
                e.DragUIOverride.IsCaptionVisible = true;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = false;
            }

            var data = e.DataView.Properties["Data"];
            _effect.SendDragOver(Element, new DragAndDropEventArgs(data));
        }

        private void FrameworkElement_DragLeave(object sender, DragEventArgs e)
        {
            var data = e.DataView.Properties["Data"];
            _effect.SendDragLeave(Element, new DragAndDropEventArgs(data));
        }

        private void FrameworkElement_Drop(object sender, DragEventArgs e)
        {
            var data = e.DataView.Properties["Data"];
            _effect.SendDrop(Element, new DragAndDropEventArgs(data));
        }
    }
}