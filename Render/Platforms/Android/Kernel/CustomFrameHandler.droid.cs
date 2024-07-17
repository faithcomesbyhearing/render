using Android.Content;

namespace Render.Platforms.Kernel
{
    public class CustomFrameHandler : Microsoft.Maui.Controls.Handlers.Compatibility.FrameRenderer
    {
        public CustomFrameHandler(Context context) 
            : base(context) { }
    }
}