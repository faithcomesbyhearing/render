using Render.Components.TitleBar;

namespace Render.Kernel.WrappersAndExtensions
{
    public interface IMenuPopupService
    {
        void Show(TitleBarMenuViewModel viewModel);
        void Close();
    }
}