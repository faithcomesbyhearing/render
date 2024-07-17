using Render.Kernel;

namespace Render.Platforms.Kernel
{
    public class CloseApplication : ICloseApplication
    {
        public void Close()
        {
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
    }
}