using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace Render.Platforms.Kernel
{
    public class CustomEntryHandler : EntryHandler 
    {
        protected override void ConnectHandler(TextBox platformView)
        {
            base.ConnectHandler(platformView);

            platformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            platformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        }
    }
}