using Microsoft.Maui.Controls.Platform;
using Render.Kernel.DragAndDrop;
using View = Android.Views.View;

namespace Render.Platforms.Kernel
{
    public class DropRecognizerEffect : PlatformEffect
    {
        private View _view;
        private Render.Kernel.DragAndDrop.DropRecognizerEffect _libDropRecognizerEffect;
        private DropGestureRecognizer _dropGestureRecognizer;

        protected override void OnAttached()
        {
            // Get the Android View corresponding to the Element that the effect is attached to
            _view = Control == null ? Container : Control;

            // Get access to the DropRecognizerEffect class in the .NET Standard library
            Render.Kernel.DragAndDrop.DropRecognizerEffect dropEffect = (Render.Kernel.DragAndDrop.DropRecognizerEffect)Element
                .Effects
                .FirstOrDefault(effect => effect is Render.Kernel.DragAndDrop.DropRecognizerEffect);

            if (dropEffect is not null && _view is not null)
            {
                _libDropRecognizerEffect = dropEffect;

                AddDropGestureRecognizerToElement();
            }
        }

        private void AddDropGestureRecognizerToElement()
        {
            _dropGestureRecognizer = new DropGestureRecognizer();
            _dropGestureRecognizer.DragOver += DropGestureRecognizerDragOver;
            _dropGestureRecognizer.DragLeave += DropGestureRecognizerDragLeave;
            _dropGestureRecognizer.Drop += DropGestureRecognizerDrop;

            ((Microsoft.Maui.Controls.View)(VisualElement)Element).GestureRecognizers.Add(_dropGestureRecognizer);
        }

        private void DropGestureRecognizerDragOver(object sender, DragEventArgs e)
        {
            if (!string.IsNullOrEmpty(_effect?.DropText))
            {
                e.DragUIOverride.Caption = _effect?.DropText;
                e.DragUIOverride.IsCaptionVisible = true;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = false;
            }

            var data = e.Data.Properties["Data"];
            _libDropRecognizerEffect.SendDragOver(Element, new DragAndDropEventArgs(data));
        }

        private void DropGestureRecognizerDragLeave(object sender, DragEventArgs e)
        {
            var data = e.Data.Properties["Data"];
            _libDropRecognizerEffect.SendDragLeave(Element, new DragAndDropEventArgs(data));
        }

        private void DropGestureRecognizerDrop(object sender, DropEventArgs e)
        {
            var data = e.Data.Properties["Data"];
            _libDropRecognizerEffect.SendDrop(Element, new DragAndDropEventArgs(data));
        }

        protected override void OnDetached()
        {
            _dropGestureRecognizer.DragOver -= DropGestureRecognizerDragOver;
            _dropGestureRecognizer.DragLeave -= DropGestureRecognizerDragLeave;
            _dropGestureRecognizer.Drop -= DropGestureRecognizerDrop;
        }
    }
}