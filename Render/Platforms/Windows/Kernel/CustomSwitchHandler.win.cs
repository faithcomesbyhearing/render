using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace Render.Platforms.Kernel
{
    public class CustomSwitchHandler : SwitchHandler
    {
        protected override void ConnectHandler(ToggleSwitch platformView)
        {
            base.ConnectHandler(platformView);

            platformView.OffContent = string.Empty;
            platformView.OnContent = string.Empty;
            
            // Hack to force 'end' alignment of WinUI 3 switch
            if (VirtualView is Switch customSwitch && 
                customSwitch.HorizontalOptions.Alignment is LayoutAlignment.End)
            {
                platformView.Margin = new Microsoft.UI.Xaml.Thickness
                {
                    Right = -110,
                };
            }
        }
    }
}