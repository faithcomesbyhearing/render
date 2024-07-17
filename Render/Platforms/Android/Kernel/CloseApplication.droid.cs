using Render.Kernel;

namespace Render.Platforms.Kernel
{
    public class CloseApplication : ICloseApplication
    {
        [Obsolete("Obsolete")]
        public void Close()
        {
            Platform.CurrentActivity.FinishAffinity();
        }
    }
}