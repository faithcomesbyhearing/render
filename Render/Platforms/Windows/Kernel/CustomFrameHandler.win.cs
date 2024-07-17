using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml.Input;
using Windows.UI.Core;

namespace Render.Platforms.Kernel
{
    public class CustomFrameHandler : Microsoft.Maui.Controls.Handlers.Compatibility.FrameRenderer
    {
        private readonly CoreCursor _originalHandCursor;

        private MauiWinUIWindow CurrentWindow
        {
            get => Application.Current.Windows.First().Handler.PlatformView as MauiWinUIWindow;
        }

        public CustomFrameHandler()
        {
            _originalHandCursor = CurrentWindow.CoreWindow.PointerCursor;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement == null)
            {
                Control.PointerExited += Control_PointerExited;
                Control.PointerMoved += Control_PointerMoved;
            }
        }

        private void Control_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            CurrentWindow.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 1);
        }

        private void Control_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_originalHandCursor != null)
            {
                CurrentWindow.CoreWindow.PointerCursor = _originalHandCursor;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Control.PointerExited -= Control_PointerExited;
            Control.PointerMoved -= Control_PointerMoved;

            base.Dispose(disposing);
        }
    }
}