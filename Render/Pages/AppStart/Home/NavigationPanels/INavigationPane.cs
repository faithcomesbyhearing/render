using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Pages.AppStart.Home.NavigationIcons;

namespace Render.Pages.AppStart.Home.NavigationPanels;

public interface INavigationPane : IViewModelBase
{
    DynamicDataWrapper<NavigationIconViewModel> NavigationIcons { get; }
    bool ShowMiniScrollBar { get; }
}
