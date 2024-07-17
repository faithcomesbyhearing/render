using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using Render.Kernel.CustomRenderer;
using System.ComponentModel;

namespace Render.Platforms.Kernel
{
    public class PanelHandler : Microsoft.Maui.Controls.Handlers.Compatibility.FrameRenderer
    {
        private Panel Panel
        {
            get => Element as Panel;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                UpdateCornerRadius();
                UpdateBorder();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(Panel.BorderRadius))
            {
                UpdateCornerRadius();
            }

            if (e.PropertyName == nameof(Panel.BorderThickness) ||
                e.PropertyName == nameof(Panel.HasShadow) ||
                e.PropertyName == nameof(Panel.BorderColor))
            {
                UpdateBorder();
            }
        }

        private void UpdateCornerRadius()
        {
            var cornerRadius = Panel?.BorderRadius ?? new CornerRadius();

            Control.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(
                topLeft: cornerRadius.TopLeft,
                topRight: cornerRadius.TopRight,
                bottomLeft: cornerRadius.BottomLeft,
                bottomRight: cornerRadius.BottomRight);
        }

        private void UpdateBorder()
        {
            var borderThickness = Panel?.BorderThickness ?? new Thickness();

            Control.BorderThickness = new Microsoft.UI.Xaml.Thickness(
                left: borderThickness.Left,
                top: borderThickness.Top,
                right: borderThickness.Right,
                bottom: borderThickness.Bottom);


            if (Element.BorderColor != null)
            {
                Control.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Element.BorderColor.ToWindowsColor());
            }
        }
    }
}