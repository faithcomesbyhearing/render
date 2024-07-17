using Microsoft.Maui.Handlers;

namespace Render.Platforms.Kernel
{
    public class CustomSliderHandler : SliderHandler
    {
        protected override void ConnectHandler(Microsoft.UI.Xaml.Controls.Slider platformView)
        {
            base.ConnectHandler(platformView);

            platformView.Style = MauiWinUIApplication.Current.Resources["CustomSliderStyle"] as Microsoft.UI.Xaml.Style;
        }
    }
}