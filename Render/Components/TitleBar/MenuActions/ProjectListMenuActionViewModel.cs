using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.AppStart.ProjectSelect;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;


namespace Render.Components.TitleBar.MenuActions
{
    public class ProjectListMenuActionViewModel : MenuActionViewModel
    {
        public ProjectListMenuActionViewModel(IViewModelContextProvider viewModelContextProvider, string pageName)
            : base("ProjectListMenuAction", viewModelContextProvider, pageName)
        {
            DisposeOnNavigationCleared = true;
            var title = AppResources.ProjectList;
            var command = ReactiveCommand.CreateFromTask(NavigateToAsync);
            var color = ((ColorReference)ResourceExtensions.GetResourceValue("Option")) ?? new ColorReference();
            var imageSource = IconExtensions.BuildFontImageSource(Icon.ProjectList, color.Color, 40);
            SetSources(imageSource, command, title);
        }

        private async Task<IRoutableViewModel> NavigateToAsync()
        {
            CloseMenu();

            var vm = await Task.Run(async () => await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider));
            return await NavigateToAndReset(vm);
        }
    }
}