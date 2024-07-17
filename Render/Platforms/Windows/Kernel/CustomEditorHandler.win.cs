using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace Render.Platforms.Kernel
{
    public class CustomEditorHandler : EditorHandler 
    {
        protected override void ConnectHandler(TextBox platformView)
        {
            base.ConnectHandler(platformView);

            platformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        }
    }
}