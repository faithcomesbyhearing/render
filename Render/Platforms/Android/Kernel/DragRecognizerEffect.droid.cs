using Microsoft.Maui.Controls.Platform;
using Render.Kernel.DragAndDrop;
using View = Android.Views.View;

namespace Render.Platforms.Kernel
{
    public class DragRecognizerEffect : PlatformEffect
    {
        private View _view;
        private Render.Kernel.DragAndDrop.DragRecognizerEffect _libDragRecognizerEffect;
        private DragGestureRecognizer _dragGestureRecognizer;

        protected override void OnAttached()
        {
            // Get the Android View corresponding to the Element that the effect is attached to
            _view = Control == null ? Container : Control;

            // Get access to the DragRecognizerEffect class in the .NET Standard library
            var dragEffect = (Render.Kernel.DragAndDrop.DragRecognizerEffect)Element
                .Effects
                .FirstOrDefault(effect => effect is Render.Kernel.DragAndDrop.DragRecognizerEffect);

            if (dragEffect is not null && _view is not null)
            {
                _libDragRecognizerEffect = dragEffect;

                AddDragGestureRecognizerToElement();
            }
        }

        private void AddDragGestureRecognizerToElement()
        {
            _dragGestureRecognizer = new DragGestureRecognizer();
            _dragGestureRecognizer.DragStarting += DragGestureRecognizerDragStarting;

            ((Microsoft.Maui.Controls.View)(VisualElement)Element).GestureRecognizers.Add(_dragGestureRecognizer);
        }

        private void DragGestureRecognizerDragStarting(object sender, DragStartingEventArgs e)
        {
            var dragAndDropEventArgs = new DragAndDropEventArgs();
            _libDragRecognizerEffect.SendDragStarting(Element, dragAndDropEventArgs);

            e.Data.Properties.Add("Data", dragAndDropEventArgs.Data);
        }

        protected override void OnDetached()
        {
            _dragGestureRecognizer.DragStarting -= DragGestureRecognizerDragStarting;
        }
    }
}