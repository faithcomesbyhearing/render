using ReactiveUI;
using Render.Kernel;
using Render.Pages.AppStart.Home;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;

namespace Render.Components.TitleBar.MenuActions
{
    public class HomeActionViewModel : MenuActionViewModel
    {
        public HomeActionViewModel(IViewModelContextProvider viewModelContextProvider, string pageName)
            : base("HomeAction", viewModelContextProvider, pageName)
        {
            var color = ((ColorReference)ResourceExtensions.GetResourceValue("Option")) ?? new ColorReference();
            var imageSource = IconExtensions.BuildFontImageSource(Icon.Home, color.Color, 40);
            var title = AppResources.ProjectHome;
            var command = ReactiveCommand.CreateFromTask(NavigateToAsync);
            SetSources(imageSource, command, title);
        }

        private async Task<IRoutableViewModel> NavigateToAsync()
        {
            CloseMenu();
            
            var homeViewModel = await Task.Run(async () => await HomeViewModel.CreateAsync(GetProjectId(), ViewModelContextProvider));
            return await NavigateToAndReset(homeViewModel);
        }
    }
}